using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Xamarin.Forms.UniversalSetter;

namespace Movies
{
    public class CacheAsideLink<TArgs> : ChainLinkAsync<TArgs> where TArgs : AsyncChainEventArgs
    {
        public CacheAsideLink(AsyncChainLinkEventHandler<TArgs> handler, ChainLink<TArgs> next = null) : base(new Wrapper(handler).GetAsync, next)
        {
        }

        private class Wrapper
        {
            public AsyncChainLinkEventHandler<TArgs> Handler { get; }

            public Wrapper(AsyncChainLinkEventHandler<TArgs> handler)
            {
                Handler = handler;
            }

            public Task GetAsync(TArgs args, ChainLinkEventHandler<TArgs> next)
            {
                return next.InvokeAsync(args);
            }
        }
    }

    public class CacheAsideLink : ChainLinkAsync<MultiRestEventArgs>
    {
        public CacheAsideLink(IDataStore<Uri, State> datastore, ChainLink<MultiRestEventArgs> next = null) : this(new RestCache(datastore), next) { }

        public CacheAsideLink(DatastoreCache<Uri, State, MultiRestEventArgs> handlers, ChainLink<MultiRestEventArgs> next = null) : base(new CacheAside(handlers).GetAsync, next) { }

        private class CacheAside
        {
            public CacheAside<MultiRestEventArgs> Handlers { get; }

            private readonly object BufferLock = new object();
            private Dictionary<Uri, TaskCompletionSource<Task<State>>> Buffer = new Dictionary<Uri, TaskCompletionSource<Task<State>>>();

            public CacheAside(CacheAside<MultiRestEventArgs> handlers)
            {
                Handlers = handlers;
            }

            public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                var buffer = new Dictionary<Uri, TaskCompletionSource<Task<State>>>();
                var buffered = new List<Task>();

                try
                {
                    await GetAsync(e, args => next.InvokeAsync(args), buffer, buffered);
                }
                finally
                {
                    // Make sure all TaskCompletionSource's in cache have been transitioned
                    foreach (var source in buffer.Values)
                    {
                        source.TrySetResult(Task.FromResult<State>(null));
                    }

                    await Task.WhenAll(buffered.Select(Safely));
                }
            }

            private async Task GetAsync(MultiRestEventArgs e, AsyncChainLinkEventHandler<MultiRestEventArgs> next, Dictionary<Uri, TaskCompletionSource<Task<State>>> buffer, List<Task> buffered)
            {
                var eCache = new MultiRestEventArgs();
                var ready = new TaskCompletionSource<bool>();
                var cacheResponse = WrapRequest(ready.Task, Handlers.HandleGet, e, eCache);

                // Handle from the cache
                lock (BufferLock)
                {
                    foreach (var request in e.Unhandled)
                    {
                        if (Buffer.TryGetValue(request.Uri, out var source))
                        {
                            buffered.Add(request.Handle(Unwrap(source.Task)));
                        }
                        else
                        {
                            eCache.AddRequest(request);
                            source = new TaskCompletionSource<Task<State>>();
                            Buffer.Add(request.Uri, source);
                            buffer.Add(request.Uri, source);
                        }
                    }
                }

                // Could lock here, but not that worried about the same resource being requested multiple times from the cache
                ready.SetResult(true);
                await cacheResponse;

                var eNext = new MultiRestEventArgs();
                Task nextResponse;

                lock (BufferLock)
                {
                    foreach (var request in eCache)
                    {
                        if (request.Handled)
                        {
                            Buffer.Remove(request.Uri);
                        }

                        if (buffer.TryGetValue(request.Uri, out var source) && source.Task.IsCompleted)
                        {
                            buffer.Remove(request.Uri);
                            buffered.Add(request.Handle(Unwrap(source.Task)));
                        }
                        else if (!request.Handled)
                        {
                            eNext.AddRequest(request);
                        }
                    }

                    nextResponse = SendAsync(next, e, eNext, true);
                }

                await nextResponse;
                //Print.Log("got from next", string.Join("\n\t", eNext.Select(r => r.Uri)));

                // Rehandle requests with newer data
                foreach (var request in e)
                {
                    if (Buffer.TryGetValue(request.Uri, out var response))
                    {
                        response.TrySetResult(Task.FromResult<State>(null));
                        buffered.Add(Rehandle(request, Unwrap(response.Task)));
                    }
                }

                foreach (var request in eNext)
                {
                    UpdateBufferOnWriteComplete(request.Uri, request.Response);
                }
            }

            private async Task<MultiRestEventArgs> self(MultiRestEventArgs e, AsyncChainLinkEventHandler<MultiRestEventArgs> handler, IDictionary<Uri, TaskCompletionSource<Task<State>>> cache, List<Task> cached)
            {
                MultiRestEventArgs eSelf;
                Task self = Task.CompletedTask;

                lock (BufferLock)
                {
                    eSelf = HandleFromCache(e, Buffer, cached);

                    try
                    {
                        self = Handlers.HandleGet(eSelf);
                    }
                    catch { }

                    e.AddRequests(eSelf);
                    RefreshBuffer(Buffer, eSelf);

                    foreach (var request in eSelf)
                    {
                        //var source = new TaskCompletionSource<Task<State>>();

                        cache.Add(request.Uri, Buffer[request.Uri]);
                        //Cache.Add(request.Uri, source);
                    }
                }

                try
                {
                    await self;
                }
                catch { }

                e.AddRequests(eSelf);
                return eSelf;
            }

            private async Task<MultiRestEventArgs> next(MultiRestEventArgs e, MultiRestEventArgs eSelf, MultiRestEventArgs eNext, ChainLinkEventHandler<MultiRestEventArgs> handler, IDictionary<Uri, TaskCompletionSource<Task<State>>> cache, List<Task> cached)
            {
                Task handling = Task.CompletedTask;

                lock (BufferLock)
                {
                    foreach (var request in eSelf)
                    {
                        if (cache.TryGetValue(request.Uri, out var source) && !source.Task.IsCompleted)
                        {
                            cache.Remove(request.Uri);
                        }
                    }

                    eNext.AddRequests(HandleFromCache(eSelf, cache, cached));

                    if (eNext.Count > 0)
                    {
                        // Little bit of a hack - if we're going to request new data might as well update everything
                        if (eNext.Count > 0 && Handlers is TMDbLocalHandlers)
                        {
                            eNext.AddRequests(e);
                        }

                        try
                        {
                            handling = handler.InvokeAsync(eNext);
                        }
                        catch { }
                        // Pass anything extra that got added back up
                        e.AddRequests(eNext);

                        RefreshBuffer(Buffer, eNext, handling);
                    }
                }

                try
                {
                    await handling;
                }
                catch { }

                e.AddRequests(eNext);
                return eNext;
            }

            private async Task SendAsync(AsyncChainLinkEventHandler<MultiRestEventArgs> handler, MultiRestEventArgs primary, MultiRestEventArgs subset, bool refreshPreemptively)
            {
                if (subset.Count == 0)
                {
                    return;
                }

                // Little bit of a hack - if we're going to request new data might as well update everything
                if (handler != Handlers.HandleGet && Handlers is TMDbLocalHandlers)
                {
                    subset.AddRequests(primary);
                }

                var response = Task.CompletedTask;
                try { response = handler(subset); } catch { }

                primary.AddRequests(subset);
                RefreshBuffer(Buffer, subset, refreshPreemptively ? response : null);

                await Safely(response);

                primary.AddRequests(subset);
                RefreshBuffer(Buffer, subset);
            }

            private static void RefreshBuffer(IDictionary<Uri, TaskCompletionSource<Task<State>>> buffer, IEnumerable<RestRequestArgs> args, Task handling = null)
            {
                foreach (var request in args)
                {
                    if (!buffer.TryGetValue(request.Uri, out var source))
                    {
                        buffer[request.Uri] = source = new TaskCompletionSource<Task<State>>();
                    }

                    if (handling != null)
                    {
                        source.TrySetResult(GetResponseAsync(handling, request));
                    }
                    else if (request.Handled)
                    {
                        source.TrySetResult(Task.FromResult(request.Response));
                    }
                }
            }

            private MultiRestEventArgs HandleFromCache(IEnumerable<RestRequestArgs> args, IDictionary<Uri, TaskCompletionSource<Task<State>>> cache, List<Task> cached)
            {
                var unhandled = new MultiRestEventArgs();

                foreach (var request in args)
                {
                    if (request.Handled)
                    {
                        if (cache.Remove(request.Uri, out var response))
                        {
                            response.SetResult(Task.FromResult(request.Response));
                        }

                        lock (BufferLock)
                        {
                            Buffer.Remove(request.Uri);
                        }
                    }
                    else
                    {
                        if (cache.TryGetValue(request.Uri, out var response))
                        {
                            request.Handle(Unwrap(response.Task));
                        }
                        else
                        {
                            cache.Add(request.Uri, new TaskCompletionSource<Task<State>>());
                            unhandled.AddRequest(request);
                        }
                    }
                }

                return unhandled;
            }

            private static async Task Rehandle(RestRequestArgs request, Task<State> response)
            {
                var state = await response;

                if (request.Response != state)
                {
                    request.Handle(state);
                }
            }

            // Wait until the value has been written, then remove from the cache
            private async void UpdateBufferOnWriteComplete(Uri uri, State state)
            {
                if (state != null)
                {
                    var request = new RestRequestArgs(uri, state);
                    await Handlers.HandleSet(new MultiRestEventArgs(request));
                }

                lock (BufferLock)
                {
                    Buffer.Remove(uri);
                }
            }

            private static async Task<State> Unwrap(Task<Task<State>> wrapped) => await (await wrapped);

            private async Task WrapRequest(Task ready, AsyncChainLinkEventHandler<MultiRestEventArgs> handler, MultiRestEventArgs primary, MultiRestEventArgs subset)
            {
                await ready;
                await SendAsync(handler, primary, subset, false);
            }

            private static async Task<State> GetResponseAsync(Task task, RestRequestArgs e)
            {
                await task;
                return e.Response;
            }

            private static async Task Safely(Task task)
            {
                try
                {
                    await task;
                }
                catch { }
            }

#if false
            public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                var cached = new List<Task>();
                var eSelf = new MultiRestEventArgs();
                var cache = new Dictionary<Uri, TaskCompletionSource<State>>();

                lock (CacheLock)
                {
                    foreach (var request in e.Unhandled)
                    {
                        // We've already issued a request for this resource and are waiting for a response,
                        // let's not make another request
                        if (Cache.TryGetValue(request.Uri, out var response))
                        {
                            cached.Add(request.Handle(response.Task));
                        }
                        else
                        {
                            eSelf.AddRequest(request);
                        }
                    }

                    foreach (var request in eSelf)
                    {
                        var source = new TaskCompletionSource<State>();

                        cache.Add(request.Uri, source);
                        Cache.Add(request.Uri, source);
                    }
                }

                try
                {
                    var self = Handlers.HandleGet(eSelf);
                    e.AddRequests(eSelf);

                    await self;
                }
                catch { }

                e.AddRequests(eSelf);
                var eNext = new MultiRestEventArgs();

                foreach (var request in eSelf.Unhandled)
                {
                    if (cache.TryGetValue(request.Uri, out var response) && !response.Task.IsCompleted)
                    {
                        cache.Remove(request.Uri);
                    }
                }

                foreach (var request in eSelf)
                {
                    if (request.Handled)
                    {
                        if (cache.Remove(request.Uri, out var response))
                        {
                            response.SetResult(request.Response);
                        }

                        lock (CacheLock)
                        {
                            Cache.Remove(request.Uri);
                        }
                    }
                    else
                    {
                        if (cache.TryGetValue(request.Uri, out var response))// && response.Task.IsCompleted)
                        {
                            cached.Add(request.Handle(response.Task));
                        }
                        else
                        {
                            eNext.AddRequest(request);
                        }
                    }
                }

                if (next != null && eNext.Count > 0)
                {
                    // Little bit of a hack - if we're going to request new data might as well update everything
                    if (eNext.Count > 0 && Handlers is TMDbLocalHandlers)
                    {
                        eNext = new MultiRestEventArgs(e);
                    }

                    Task handling = Task.CompletedTask;
                    try
                    {
                        handling = next.InvokeAsync(eNext);
                    }
                    catch { }
                    // Pass anything extra that got added back up
                    e.AddRequests(eNext);

                    lock (CacheLock)
                    {
                        // Everything that we get back from next, whether we requested it or not
                        foreach (var request in eNext)
                        {
                            Cache.TryAdd(request.Uri, new TaskCompletionSource<State>());
                        }
                    }

                    try
                    {
                        await handling;
                    }
                    catch { }

                    lock (CacheLock)
                    {
                        foreach (var request in eNext)
                        {
                            // Cache any new resources that we weren't aware of before
                            // If we were aware of it, update in case value has changed? (unlikely)
                            if (!Cache.TryGetValue(request.Uri, out var source))
                            {
                                Cache[request.Uri] = source = new TaskCompletionSource<State>();
                            }

                            source.SetResult(request.Response);
                        }

                        // Rehandle requests with newer data
                        foreach (var request in e)
                        {
                            if (Cache.TryGetValue(request.Uri, out var response))
                            {
                                cached.Add(Rehandle(request, response.Task));
                            }
                        }
                    }

                    foreach (var request in eNext)
                    {
                        UpdateCacheOnWriteComplete(request.Uri, request.Response);
                    }
                }

                await Task.WhenAll(cached.Select(Safe));
            }
#endif
        }
    }
}