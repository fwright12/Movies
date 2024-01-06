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
        private Dictionary<object, TaskCompletionSource<Task<TArgs>>> WriteBuffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>();

        public AsyncCacheAsideProcessor(BufferedCache<TArgs> cache)
        {
            Cache = cache;
        }

        private class ProcessorFunc<T> : IAsyncEventProcessor<T>
        {
            public Func<T, Task<bool>> Func { get; }

            public ProcessorFunc(Func<T, Task<bool>> func)
            {
                Func = func;
            }

            public Task<bool> ProcessAsync(T e) => Func(e);
        }

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            var buffered = new List<Task>();

            try
            {
                return await ProcessAsync(e, next, buffered);
            }
            catch
            {
                return false;
            }
            finally
            {
                await Task.WhenAll(buffered.Select(Safely));
            }
        }

        private async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next, List<Task> buffered)
        {
            var buffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>();
            var eCache = new BulkEventArgs<TArgs>();

            // Handle from the cache
            lock (BufferLock)
            {
                buffered.AddRange(CheckBuffer(WriteBuffer, e, eCache));

                foreach (var request in eCache)
                {
                    var key = Cache.GetKey(request);

                    // We will maintain a local version of the buffer in case items are removed from
                    // the global Buffer (because they finished writing) while we are reading from the cache.
                    // This avoids a scenario where we have a cache miss, but while waiting for that response
                    // to return from the cache it is updated with the value we requested.
                    var source = new TaskCompletionSource<Task<TArgs>>();
                    WriteBuffer.Add(key, source);
                    buffer.Add(key, source);
                }
            }

            if (eCache.Count == 0)
            {
                return true;
            }

            Task writeTask = Task.CompletedTask;

            var nextWrapper = new ProcessorFunc<IEnumerable<TArgs>>(async e1 =>
            {
                var eNext = new BulkEventArgs<TArgs>();
                Task<bool> response;

                lock (BufferLock)
                {
                    //foreach (var request in e1)
                    //{
                    //    // The resource was already in the cache, so no need to write anything
                    //    if (request.IsHandled)
                    //    {
                    //        WriteBuffer.Remove(Cache.GetKey(request));
                    //    }
                    //}

                    // Check if the request has been fulfilled in the time it took to check the cache. If the task
                    // is not complete, the underlying source has not been transitioned, meaning it's the same
                    // source we added before checking the cache. We can't await that task because it won't
                    // complete (we are expected to complete it with the response from next)
                    var completedBuffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>(buffer.Where(kvp => kvp.Value.Task.IsCompleted));
                    buffered.AddRange(CheckBuffer(completedBuffer, e1, eNext));

                    // Little bit of a hack, assuming TMDbClient will be next - if we're going to request new data,
                    // might as well update everything
                    if (Cache.Cache is TMDbLocalCache)
                    {
                        (eNext as BulkEventArgs<TArgs>)?.Add(e);
                    }

                    if (eNext.Count == 0)
                    {
                        eCache.Add(e1);
                        return true;
                    }

                    response = next.ProcessAsync(eNext);
                    // The handler is allowed to add extra requests (representing additional resources that
                    // were retured as a side effect). Make the primary batch request aware of these
                    (e1 as BulkEventArgs<TArgs>)?.Add(eNext);
                    eCache.Add(e1);

                    // Make sure any extra requests are in the buffer, and alert existing requests that we
                    // are expecting a response. We can choose to update items before we actually get the
                    // response (preemptively) or wait until we have it.
                    UpdateBuffer(eCache, response);
                }

                try
                {
                    return await response;
                    //Print.Log("got from next", string.Join("\n\t", eNext));
                }
                finally
                {
                    (e1 as BulkEventArgs<TArgs>)?.Add(eNext);
                    eCache.Add(e1);
                    UpdateBuffer(eNext, response);

                    writeTask = Safely(Cache.Write(eNext.Where(arg => arg.IsHandled)));
                }
            });

            var response = Task.FromResult(false);

            try
            {
                response = Cache.ProcessAsync(eCache, nextWrapper);

                if (!response.IsCompleted)
                {
                    (e as BulkEventArgs<TArgs>)?.Add(eCache);
                    UpdateBuffer(eCache);
                }

                return await response;
            }
            finally
            {
                (e as BulkEventArgs<TArgs>)?.Add(eCache);

                lock (BufferLock)
                {
                    // Rehandle requests with newer data
                    buffered.AddRange(CheckBuffer(WriteBuffer, e.Except(eCache)));
                    UpdateBuffer(eCache, response);
                }

                // Remove items from the buffer once writing to the cache has completed
                UpdateBufferOnWriteComplete(eCache, writeTask);
            }
        }

        private List<Task> CheckBuffer(Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, IEnumerable<TArgs> e, BulkEventArgs<TArgs> unhandled = null)
        {
            var tasks = new List<Task>();

            foreach (var request in e)
            {
                var key = Cache.GetKey(request);

                if (buffer.TryGetValue(key, out var source))
                {
                    tasks.Add(Handle(request, source));
                }
                else if (unhandled != null)
                {
                    unhandled.Add(request);
                }
            }

            return tasks;
        }

        private void UpdateBuffer(IEnumerable<TArgs> args, Task handling = null)
        {
            lock (BufferLock)
            {
                foreach (var request in args)
                {
                    var key = Cache.GetKey(request);

                    if (!WriteBuffer.TryGetValue(key, out var source))
                    {
                        WriteBuffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                    }

                    if (request.IsHandled)
                    {
                        source.TrySetResult(Task.FromResult(request));
                    }
                    else if (handling != null)
                    {
                        if (handling.IsFaulted)
                        {
                            source.TrySetException(handling.Exception);
                        }
                        // We are expecting a response but don't have it yet
                        else
                        {
                            source.TrySetResult(GetResponseAsync(handling, request));
                        }
                    }
                }
            }
        }

        // Wait until the value has been written, then remove from the cache
        private async void UpdateBufferOnWriteComplete(IEnumerable<TArgs> args, Task task)
        {
            await task;

            lock (BufferLock)
            {
                foreach (var arg in args)
                {
                    WriteBuffer.Remove(Cache.GetKey(arg));
                }
            }
        }

        private async void UpdateBufferOnWriteComplete(IEnumerable<TArgs> readArgs)
        {
            //var writeArgs = readArgs.Where(args => args.IsHandled).Select(GetWriteArgsFromFulfilledReadArgs);
            await Safely(Cache.Write(readArgs.Where(arg => arg.IsHandled)));

            lock (BufferLock)
            {
                foreach (var args in readArgs)
                {
                    WriteBuffer.Remove(Cache.GetKey(args));
                }
            }
        }

        private async Task Handle(TArgs e, TaskCompletionSource<Task<TArgs>> buffered) => Cache.Process(e, await (await buffered.Task));

        private static async Task<TArgs> GetResponseAsync(Task task, TArgs e)
        {
            try
            {
                await task;
            }
            catch { }

            return e;
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