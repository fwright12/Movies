using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            private readonly object CacheLock = new object();
            private Dictionary<Uri, Task<State>> Cache = new Dictionary<Uri, Task<State>>();

            public CacheAside(CacheAside<MultiRestEventArgs> handlers)
            {
                Handlers = handlers;
            }

            public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                try
                {
                    await Handlers.HandleGet(e);
                }
                catch { }

                if (next == null || !e.Unhandled.Any())
                {
                    return;
                }

                var unhandled = new HashSet<RestRequestArgs>();
                List<Task> cached = new List<Task>();
                Task handling;
                MultiRestEventArgs eNext;

                lock (CacheLock)
                {
                    foreach (var request in e)
                    {
                        // We've already issued a request for this resource and are waiting for a response,
                        // let's not make another request
                        if (Cache.TryGetValue(request.Uri, out var response))
                        {
                            cached.Add(request.Handle(response));
                        }
                        else
                        {
                            unhandled.Add(request);
                        }
                    }

                    eNext = new MultiRestEventArgs(unhandled);
                    handling = next.InvokeAsync(eNext);
                    // Pass anything extra that got added back up
                    e.AddRequests(eNext.Where(request => !unhandled.Contains(request)));

                    // All unhandled plus whatever else came back from next
                    foreach (var request in eNext)
                    {
                        Cache[request.Uri] = GetResponseAsync(handling, request);
                    }
                }

                try
                {
                    await handling;
                }
                catch { }

                foreach (var request in eNext)
                {
                    // Cache any new resources that we weren't aware of before
                    // If we were aware of it, update in case value has changed? (unlikely)
                    lock (CacheLock)
                    {
                        Cache[request.Uri] = Task.FromResult(request.Response);
                    }

                    UpdateCacheOnWriteComplete(request.Uri, request.Response);
                }

                await Task.WhenAll(cached.Select(Safe));
            }

            // Wait until the value has been written, then remove from the cache
            private async void UpdateCacheOnWriteComplete(Uri uri, State state)
            {
                if (state != null)
                {
                    var request = new RestRequestArgs(uri, state);
                    await Handlers.HandleSet(new MultiRestEventArgs(request));
                }

                lock (CacheLock)
                {
                    Cache.Remove(uri);
                }
            }

            private static async Task<State> GetResponseAsync(Task task, RestRequestArgs e)
            {
                await task;
                return e.Response;
            }

            private static async Task Safe(Task task)
            {
                try
                {
                    await task;
                }
                catch { }
            }
        }
    }
}