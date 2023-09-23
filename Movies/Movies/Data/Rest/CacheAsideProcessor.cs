using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class CacheAsideProcessor : IRestCoRProcessor
    {
        public CacheAside<MultiRestEventArgs> Handlers { get; }

        private readonly object BufferLock = new object();
        private Dictionary<Uri, TaskCompletionSource<Task<State>>> Buffer = new Dictionary<Uri, TaskCompletionSource<Task<State>>>();

        public CacheAsideProcessor(IDataStore<Uri, State> datastore) : this(new RestCache(datastore)) { }

        public CacheAsideProcessor(DatastoreCache<Uri, State, MultiRestEventArgs> cache)
        {
            Handlers = cache;
        }

        public async Task ProcessAsync(MultiRestEventArgs e, IAsyncEventProcessor<MultiRestEventArgs> next)
        {
            var buffer = new Dictionary<Uri, TaskCompletionSource<Task<State>>>();
            var buffered = new List<Task>();

            try
            {
                await GetAsync(e, args => next.ProcessAsync(args), buffer, buffered);
            }
            finally
            {
                // Make sure all TaskCompletionSource's in buffer have been transitioned
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
            // Delay the request to the cache until we are ready for it to go through
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
                        // We will maintain a local version of the buffer in case items are removed from
                        // the global Buffer (because they finished writing) while we are reading from the cache.
                        // This avoids a scenario where we have a cache miss, but while waiting for that response
                        // to return from the cache it is updated with the value we requested.
                        eCache.AddRequest(request);
                        source = new TaskCompletionSource<Task<State>>();
                        Buffer.Add(request.Uri, source);
                        buffer.Add(request.Uri, source);
                    }
                }
            }

            // Could lock here, but not that worried about the same resource being requested
            // multiple times from the cache. It's up to the cache implementation to handle that.
            ready.SetResult(true);
            await cacheResponse;

            var eNext = new MultiRestEventArgs();
            Task nextResponse;

            lock (BufferLock)
            {
                foreach (var request in eCache)
                {
                    // The resource was already in the cache, so no need to write anything
                    if (request.IsHandled)
                    {
                        Buffer.Remove(request.Uri);
                    }

                    // Check if the request has been fulfilled in the time it took to check the cache. If the task
                    // is not complete, the underlying source has not been transitioned, meaning it's the same
                    // source we added before checking the cache. We can't await that task because it won't
                    // complete (we are expected to complete it with the response from next)
                    if (buffer.TryGetValue(request.Uri, out var source) && source.Task.IsCompleted)
                    {
                        buffer.Remove(request.Uri);
                        buffered.Add(request.Handle(Unwrap(source.Task)));
                    }
                    else if (!request.IsHandled)
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

                    try
                    {
                        buffered.Add(Rehandle(request, Unwrap(response.Task)));
                    }
                    catch { }
                }
            }

            // Remove items from the buffer once writing to the cache has completed
            foreach (var request in eNext)
            {
                UpdateBufferOnWriteComplete(request.Uri, request.Response);
            }
        }

        // Make a request with a subset of the requests from primary
        private async Task SendAsync(AsyncChainLinkEventHandler<MultiRestEventArgs> handler, MultiRestEventArgs primary, MultiRestEventArgs subset, bool refreshPreemptively)
        {
            if (subset.Count == 0)
            {
                return;
            }

            // Little bit of a hack, assuming TMDbClient will be next - if we're going to request new data,
            // might as well update everything
            if (handler != Handlers.HandleGet && Handlers is TMDbLocalHandlers)
            {
                subset.AddRequests(primary);
            }

            Task response;
            try
            {
                response = handler(subset);
            }
            catch
            {
                response = Task.CompletedTask;
            }

            // The handler is allowed to add extra requests (representing additional resources that
            // were retured as a side effect). Make the primary batch request aware of these
            primary.AddRequests(subset);
            // Make sure any extra requests are in the buffer, and alert existing requests that we
            // are expecting a response. We can choose to update items before we actually get the
            // response (preemptively) or wait until we have it.
            RefreshBuffer(Buffer, subset, refreshPreemptively ? response : null);

            await Safely(response);

            // Repeat steps from before with any extra requests that were added asynchronously
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
                    // We are expecting a response but don't have it yet
                    source.TrySetResult(GetResponseAsync(handling, request));
                }
                else if (request.IsHandled)
                {
                    source.TrySetResult(Task.FromResult(request.Response));
                }
            }
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
                await Safely(Handlers.HandleSet(new MultiRestEventArgs(request)));
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
    }
}