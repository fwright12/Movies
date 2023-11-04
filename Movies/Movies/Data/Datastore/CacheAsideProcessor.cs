using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class CacheAsideProcessor<T> : IAsyncCoRProcessor<IEnumerable<T>> where T : DatastoreReadArgs
    {
        public IAsyncEventProcessor<IEnumerable<T>> ReadProcessor { get; }
        public IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>> WriteProcessor { get; }

        private readonly object BufferLock = new object();
        private Dictionary<T, TaskCompletionSource<Task<DatastoreResponse>>> Buffer = new Dictionary<T, TaskCompletionSource<Task<DatastoreResponse>>>();

        public CacheAsideProcessor(IAsyncEventProcessor<IEnumerable<T>> readProcessor, IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>> writeProcessor)
        {
            ReadProcessor = readProcessor;
            WriteProcessor = writeProcessor;
        }

        public static CacheAsideProcessor<T> Create<TProcessor>(TProcessor processor) where TProcessor : IAsyncEventProcessor<IEnumerable<T>>, IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>> => new CacheAsideProcessor<T>(processor, processor);

        public async Task<bool> ProcessAsync(IEnumerable<T> e, IAsyncEventProcessor<IEnumerable<T>> next)
        {
            var buffer = new Dictionary<T, TaskCompletionSource<Task<DatastoreResponse>>>();
            var buffered = new List<Task>();
            var result = false;

            try
            {
                result = await ProcessAsync(e, next, buffer, buffered);
            }
            finally
            {
                // Make sure all TaskCompletionSource's in buffer have been transitioned
                foreach (var source in buffer.Values)
                {
                    source.TrySetResult(Task.FromResult<DatastoreResponse>(null));
                }

                await Task.WhenAll(buffered.Select(Safely));
            }

            return result;// e.All(request => request.IsHandled);
        }

        private async Task<bool> ProcessAsync(IEnumerable<T> e, IAsyncEventProcessor<IEnumerable<T>> next, Dictionary<T, TaskCompletionSource<Task<DatastoreResponse>>> buffer, List<Task> buffered)
        {
            //var e = (BatchDatastoreArgs<RestRequestArgs>)e1;
            var eCache = new BulkEventArgs<T>();
            var ready = new TaskCompletionSource<bool>();
            // Delay the request to the cache until we are ready for it to go through
            var cacheResponse = WrapRequest(ready.Task, ReadProcessor, e, eCache);

            // Handle from the cache
            lock (BufferLock)
            {
                foreach (var request in e)
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
                        eCache.Add(request);
                        source = new TaskCompletionSource<Task<DatastoreResponse>>();
                        Buffer.Add(request, source);
                        buffer.Add(request, source);
                    }
                }
            }

            // Could lock here, but not that worried about the same resource being requested
            // multiple times from the cache. It's up to the cache implementation to handle that.
            ready.SetResult(true);
            var handled = await cacheResponse;

            var eNext = new BulkEventArgs<T>();
            Task<bool> nextResponse;

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
                        buffered.Add(HandleAsync(request, source.Task));
                    }
                    //else if (!request.IsHandled)
                    else if (!handled)
                    {
                        eNext.Add(request);
                    }
                }

                nextResponse = SendAsync(next, e, eNext, true);
            }

            if (!handled)
            {
                handled = await nextResponse;
                //Print.Log("got from next", string.Join("\n\t", eNext.Select(r => r.Uri)));

                // Rehandle requests with newer data
                foreach (var request in e)
                {
                    if (Buffer.TryGetValue(request, out var response))
                    {
                        response.TrySetResult(Task.FromResult<DatastoreResponse>(null));

                        try
                        {
                            buffered.Add(Rehandle(request, response.Task));
                        }
                        catch { }
                    }
                }
            }

            // Remove items from the buffer once writing to the cache has completed
            foreach (var request in eNext)
            {
                UpdateBufferOnWriteComplete(request);
            }

            return handled;
        }

        // Make a request with a subset of the requests from primary
        private async Task<bool> SendAsync(IAsyncEventProcessor<IEnumerable<T>> handler, IEnumerable<T> primary, IEnumerable<T> subset, bool refreshPreemptively)
        {
            if (!subset.Any())
            {
                return true;
            }

            // Little bit of a hack, assuming TMDbClient will be next - if we're going to request new data,
            // might as well update everything
            if (handler != ReadProcessor && ReadProcessor is TMDbLocalProcessor)
            {
                (subset as BulkEventArgs<T>)?.Add(primary);
            }

            Task<bool> response;
            try
            {
                response = handler.ProcessAsync(subset);
            }
            catch
            {
                response = Task.FromResult(false);
            }

            // The handler is allowed to add extra requests (representing additional resources that
            // were retured as a side effect). Make the primary batch request aware of these
            (primary as BulkEventArgs<T>)?.Add(subset);
            // Make sure any extra requests are in the buffer, and alert existing requests that we
            // are expecting a response. We can choose to update items before we actually get the
            // response (preemptively) or wait until we have it.
            RefreshBuffer(Buffer, subset, refreshPreemptively ? response : null);

            bool result;
            try
            {
                result = await response;
            }
            catch
            {
                result = false;
            }

            // Repeat steps from before with any extra requests that were added asynchronously
            (primary as BulkEventArgs<T>)?.Add(subset);
            RefreshBuffer(Buffer, subset);

            return result;
        }

        private static void RefreshBuffer(IDictionary<T, TaskCompletionSource<Task<DatastoreResponse>>> buffer, IEnumerable<T> args, Task handling = null)
        {
            foreach (var request in args)
            {
                if (!buffer.TryGetValue(request, out var source))
                {
                    buffer[request] = source = new TaskCompletionSource<Task<DatastoreResponse>>();
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

        private static async Task Rehandle(DatastoreReadArgs request, Task<Task<DatastoreResponse>> response)
        {
            var value = await (await response);

            if (request.Response != value)
            {
                request.Handle(value);
            }
        }

        // Wait until the value has been written, then remove from the cache
        private async void UpdateBufferOnWriteComplete(T args)
        {
            if (args.Response != null)
            {
                var key = (args as DatastoreKeyValueReadArgs<Uri>).Key;
                var value = (args.Response as RestResponse)?.Entities as State;

                //await Safely(Handlers.HandleSet(new BatchDatastoreArgs<RestRequestArgs>(request)));
                await Safely(WriteProcessor.ProcessAsync(new BulkEventArgs<DatastoreKeyValueWriteArgs<Uri, State>>(new DatastoreKeyValueWriteArgs<Uri, State>(key, value))));
            }

            lock (BufferLock)
            {
                Buffer.Remove(args);
            }
        }

        private static async Task HandleAsync(DatastoreReadArgs request, Task<Task<DatastoreResponse>> response) => request.Handle(await (await response));

        private async Task<bool> WrapRequest(Task ready, IAsyncEventProcessor<IEnumerable<T>> handler, IEnumerable<T> primary, IEnumerable<T> subset)
        {
            await ready;
            return await SendAsync(handler, primary, subset, false);
        }

        private static async Task<DatastoreResponse> GetResponseAsync(Task task, DatastoreReadArgs e)
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