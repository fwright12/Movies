using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class AsyncCacheAsideProcessor<TArgs> : IAsyncCoRProcessor<IEnumerable<TArgs>> where TArgs : EventArgsRequest
    {
        public BufferedCache<TArgs> Cache { get; }

        private readonly object BufferLock = new object();
        private Dictionary<object, TaskCompletionSource<Task<TArgs>>> Buffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>();

        public AsyncCacheAsideProcessor(BufferedCache<TArgs> cache)
        {
            Cache = cache;
        }

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            var buffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>();
            var buffered = new List<Task>();

            try
            {
                await ProcessAsync(e, next, buffer, buffered);
            }
            finally
            {
                // Make sure all TaskCompletionSource's in buffer have been transitioned
                foreach (var source in buffer.Values)
                {
                    source.TrySetResult(Task.FromResult<TArgs>(null));
                }

                await Task.WhenAll(buffered.Select(Safely));
            }

            return e.All(arg => arg.IsHandled);
        }

        private async Task ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, List<Task> buffered)
        {
            var eCache = new BulkEventArgs<TArgs>();

            // Handle from the cache
            lock (BufferLock)
            {
                foreach (var request in e)
                {
                    var key = Cache.GetKey(request);

                    if (Buffer.TryGetValue(key, out var source))
                    {
                        buffered.Add(Cache.Process(request, Unwrap(source)));
                    }
                    else
                    {
                        // We will maintain a local version of the buffer in case items are removed from
                        // the global Buffer (because they finished writing) while we are reading from the cache.
                        // This avoids a scenario where we have a cache miss, but while waiting for that response
                        // to return from the cache it is updated with the value we requested.
                        eCache.Add(request);
                        source = new TaskCompletionSource<Task<TArgs>>();
                        Buffer.Add(key, source);
                        buffer.Add(key, source);
                    }
                }
            }

            // Could lock here, but not that worried about the same resource being requested
            // multiple times from the cache. It's up to the cache implementation to handle that.
            var handled = await SendAsync(Cache, e, eCache, false);

            var eNext = new BulkEventArgs<TArgs>();
            Task<bool> nextResponse;

            lock (BufferLock)
            {
                foreach (var request in eCache)
                {
                    var key = Cache.GetKey(request);

                    // The resource was already in the cache, so no need to write anything
                    if (request.IsHandled)
                    {
                        Buffer.Remove(key);
                    }

                    // Check if the request has been fulfilled in the time it took to check the cache. If the task
                    // is not complete, the underlying source has not been transitioned, meaning it's the same
                    // source we added before checking the cache. We can't await that task because it won't
                    // complete (we are expected to complete it with the response from next)
                    if (buffer.TryGetValue(key, out var source) && source.Task.IsCompleted)
                    {
                        buffer.Remove(key);
                        buffered.Add(Cache.Process(request, Unwrap(source)));
                    }
                    else if (!handled)
                    {
                        eNext.Add(request);
                    }
                }

                nextResponse = SendAsync(next, e, eNext, true);
            }

            if (!handled)
            {
                await nextResponse;
                //Print.Log("got from next", string.Join("\n\t", eNext.Select(r => r.Uri)));

                // Rehandle requests with newer data
                foreach (var request in e)
                {
                    if (Buffer.TryGetValue(Cache.GetKey(request), out var response))
                    {
                        response.TrySetResult(Task.FromResult<TArgs>(null));

                        try
                        {
                            buffered.Add(Cache.Process(request, Unwrap(response)));
                        }
                        catch { }
                    }
                }
            }

            // Remove items from the buffer once writing to the cache has completed
            UpdateBufferOnWriteComplete(eNext);
        }

        // Make a request with a subset of the requests from primary
        private async Task<bool> SendAsync(IAsyncEventProcessor<IEnumerable<TArgs>> handler, IEnumerable<TArgs> primary, IEnumerable<TArgs> subset, bool refreshPreemptively)
        {
            if (!subset.Any())
            {
                return true;
            }

            // Little bit of a hack, assuming TMDbClient will be next - if we're going to request new data,
            // might as well update everything
            if (handler != Cache && Cache.Cache is TMDbLocalCache)
            {
                (subset as BulkEventArgs<TArgs>)?.Add(primary);
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
            (primary as BulkEventArgs<TArgs>)?.Add(subset);
            // Make sure any extra requests are in the buffer, and alert existing requests that we
            // are expecting a response. We can choose to update items before we actually get the
            // response (preemptively) or wait until we have it.
            RefreshBuffer(subset, refreshPreemptively ? response : null);

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
            (primary as BulkEventArgs<TArgs>)?.Add(subset);
            RefreshBuffer(subset);

            return result;
        }

        private void RefreshBuffer(IEnumerable<TArgs> args, Task handling = null)
        {
            foreach (var request in args)
            {
                var key = Cache.GetKey(request);

                if (!Buffer.TryGetValue(key, out var source))
                {
                    Buffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                }

                if (handling != null)
                {
                    // We are expecting a response but don't have it yet
                    source.TrySetResult(GetResponseAsync(handling, request));
                }
                else if (request.IsHandled)
                {
                    source.TrySetResult(Task.FromResult(request));
                }
            }
        }

        // Wait until the value has been written, then remove from the cache
        private async void UpdateBufferOnWriteComplete(IEnumerable<TArgs> readArgs)
        {
            //var writeArgs = readArgs.Where(args => args.IsHandled).Select(GetWriteArgsFromFulfilledReadArgs);
            await Safely(Cache.Write(readArgs.Where(arg => arg.IsHandled)));

            lock (BufferLock)
            {
                foreach (var args in readArgs)
                {
                    Buffer.Remove(Cache.GetKey(args));
                }
            }
        }

        private static async Task<TArgs> GetResponseAsync(Task task, TArgs e)
        {
            await task;
            return e;
        }

        private static async Task<TArgs> Unwrap(TaskCompletionSource<Task<TArgs>> source) => await (await source.Task);

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