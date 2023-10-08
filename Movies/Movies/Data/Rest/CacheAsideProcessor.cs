using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Key = System.EventArgs;
using Value = Movies.DatastoreResponse;

namespace Movies
{
    public class CacheAsideProcessor<T> : IAsyncCoRProcessor<IEnumerable<T>> where T : DatastoreArgs
    {
        public CacheAside<BatchDatastoreArgs<RestRequestArgs>> Handlers { get; }
        //public IAsyncEventProcessor<MultiRestEventArgs> Processor { get; }

        private readonly object BufferLock = new object();
        private Dictionary<Key, TaskCompletionSource<Task<Value>>> Buffer = new Dictionary<Key, TaskCompletionSource<Task<Value>>>();

        public CacheAsideProcessor(IDataStore<Uri, State> datastore) : this(new RestCache(datastore)) { }

        public CacheAsideProcessor(DatastoreCache<Uri, State, BatchDatastoreArgs<RestRequestArgs>> cache)
        {
            Handlers = cache;
        }

        private class Wrapper : IAsyncEventProcessor<IEnumerable<DatastoreArgs>>
        {
            public CacheAside<BatchDatastoreArgs<RestRequestArgs>> Handlers { get; }

            public Wrapper(CacheAside<BatchDatastoreArgs<RestRequestArgs>> handlers)
            {
                Handlers = handlers;
            }

            public async Task<bool> ProcessAsync(IEnumerable<DatastoreArgs> e)
            {
                await (Handlers as IAsyncEventProcessor<IEnumerable<T>>)?.ProcessAsync((IEnumerable<T>)e);
                return e.All(request => request.IsHandled);
            }
        }

        public Task<bool> ProcessAsync(MultiRestEventArgs e, IAsyncEventProcessor<MultiRestEventArgs> next) => ProcessAsync((IEnumerable<T>)e, null);

        public async Task<bool> ProcessAsync(IEnumerable<T> e, IAsyncEventProcessor<IEnumerable<T>> next)
        {
            var buffer = new Dictionary<Key, TaskCompletionSource<Task<Value>>>();
            var buffered = new List<Task>();

            try
            {
                await GetAsync(e, next, buffer, buffered);
            }
            finally
            {
                // Make sure all TaskCompletionSource's in buffer have been transitioned
                foreach (var source in buffer.Values)
                {
                    source.TrySetResult(Task.FromResult<Value>(null));
                }

                await Task.WhenAll(buffered.Select(Safely));
            }

            return e.All(request => request.IsHandled);
        }

        private async Task GetAsync(IEnumerable<T> e, IAsyncEventProcessor<IEnumerable<T>> next, Dictionary<Key, TaskCompletionSource<Task<Value>>> buffer, List<Task> buffered)
        {
            //var e = (BatchDatastoreArgs<RestRequestArgs>)e1;
            var eCache = new BatchDatastoreArgs<T>();
            var ready = new TaskCompletionSource<bool>();
            // Delay the request to the cache until we are ready for it to go through
            var cacheResponse = WrapRequest(ready.Task, new Wrapper(Handlers), e, eCache);

            // Handle from the cache
            lock (BufferLock)
            {
                foreach (var request in e.Where(request => !request.IsHandled))
                {
                    if (Buffer.TryGetValue(request, out var source))
                    {
                        //buffered.Add(request.Handle(Unwrap(source.Task)));
                        buffered.Add(HandleAsync(request, source.Task));
                    }
                    else
                    {
                        // We will maintain a local version of the buffer in case items are removed from
                        // the global Buffer (because they finished writing) while we are reading from the cache.
                        // This avoids a scenario where we have a cache miss, but while waiting for that response
                        // to return from the cache it is updated with the value we requested.
                        eCache.AddRequest((T) request);
                        source = new TaskCompletionSource<Task<Value>>();
                        Buffer.Add(request, source);
                        buffer.Add(request, source);
                    }
                }
            }

            // Could lock here, but not that worried about the same resource being requested
            // multiple times from the cache. It's up to the cache implementation to handle that.
            ready.SetResult(true);
            await cacheResponse;

            var eNext = new BatchDatastoreArgs<T>();
            Task nextResponse;

            lock (BufferLock)
            {
                foreach (var request in eCache)
                {
                    // The resource was already in the cache, so no need to write anything
                    if (request.IsHandled)
                    {
                        Buffer.Remove(request);
                    }

                    // Check if the request has been fulfilled in the time it took to check the cache. If the task
                    // is not complete, the underlying source has not been transitioned, meaning it's the same
                    // source we added before checking the cache. We can't await that task because it won't
                    // complete (we are expected to complete it with the response from next)
                    if (buffer.TryGetValue(request, out var source) && source.Task.IsCompleted)
                    {
                        buffer.Remove(request);
                        //buffered.Add(request.Handle(Unwrap(source.Task)));
                        buffered.Add(HandleAsync(request, source.Task));
                    }
                    else if (!request.IsHandled)
                    {
                        eNext.AddRequest((T)request);
                    }
                }

                nextResponse = SendAsync(next, e, eNext, true);
            }

            await nextResponse;
            //Print.Log("got from next", string.Join("\n\t", eNext.Select(r => r.Uri)));

            // Rehandle requests with newer data
            foreach (var request in e)
            {
                if (Buffer.TryGetValue(request, out var response))
                {
                    response.TrySetResult(Task.FromResult<Value>(null));

                    try
                    {
                        buffered.Add(Rehandle(request, response.Task));
                    }
                    catch { }
                }
            }

            // Remove items from the buffer once writing to the cache has completed
            foreach (var request in eNext)
            {
                UpdateBufferOnWriteComplete(request);
            }
        }

        // Make a request with a subset of the requests from primary
        private async Task SendAsync(IAsyncEventProcessor<IEnumerable<T>> handler, IEnumerable<DatastoreArgs> primary, IEnumerable<T> subset, bool refreshPreemptively)
        {
            if (!subset.Any())
            {
                return;
            }

            // Little bit of a hack, assuming TMDbClient will be next - if we're going to request new data,
            // might as well update everything
            if (handler is Wrapper == false && Handlers is TMDbLocalHandlers)
            {
                (subset as BatchDatastoreArgs<T>)?.AddRequests(primary.OfType<T>());
            }

            Task response;
            try
            {
                response = handler.ProcessAsync(subset);
            }
            catch
            {
                response = Task.CompletedTask;
            }

            // The handler is allowed to add extra requests (representing additional resources that
            // were retured as a side effect). Make the primary batch request aware of these
            (primary as BatchDatastoreArgs<T>)?.AddRequests(subset.OfType<T>());
            // Make sure any extra requests are in the buffer, and alert existing requests that we
            // are expecting a response. We can choose to update items before we actually get the
            // response (preemptively) or wait until we have it.
            RefreshBuffer(Buffer, subset, refreshPreemptively ? response : null);

            await Safely(response);

            // Repeat steps from before with any extra requests that were added asynchronously
            (primary as BatchDatastoreArgs<T>)?.AddRequests(subset.OfType<T>());
            RefreshBuffer(Buffer, subset);
        }

        private static void RefreshBuffer(IDictionary<Key, TaskCompletionSource<Task<Value>>> buffer, IEnumerable<DatastoreArgs> args, Task handling = null)
        {
            foreach (var request in args)
            {
                if (!buffer.TryGetValue(request, out var source))
                {
                    buffer[request] = source = new TaskCompletionSource<Task<Value>>();
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

        private static async Task Rehandle(DatastoreArgs request, Task<Task<Value>> response)
        {
            var value = await (await response);

            if (request.Response != value.Response)
            {
                (request as RestRequestArgs)?.Handle(() => value.Response as State);
            }
        }

        // Wait until the value has been written, then remove from the cache
        private async void UpdateBufferOnWriteComplete(DatastoreArgs args1)
        {
            var args = (RestRequestArgs)args1;
            if (args.Response != null)
            {
                var request = new RestRequestArgs(args.Key, args.Response);
                await Safely(Handlers.HandleSet(new MultiRestEventArgs(request)));
            }

            lock (BufferLock)
            {
                Buffer.Remove(args);
            }
        }

        private static async Task HandleAsync(DatastoreArgs request, Task<Task<Value>> response) => request.Handle(await (await response));

        private async Task WrapRequest(Task ready, IAsyncEventProcessor<IEnumerable<DatastoreArgs>> handler, IEnumerable<DatastoreArgs> primary, IEnumerable<T> subset)
        {
            await ready;
            await SendAsync(handler, primary, subset, false);
        }

        private static async Task<Value> GetResponseAsync(Task task, DatastoreArgs e)
        {
            await task;
            return new DatastoreResponse<State>(((RestRequestArgs)e).Response);
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