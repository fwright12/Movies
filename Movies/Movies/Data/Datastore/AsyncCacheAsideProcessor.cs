using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            try
            {
                return await ProcessAsync1(e, next);
            }
            catch (Exception e1)
            {
                Print.Log(e1);
                return false;
            }
        }

        private async Task<bool> ProcessAsync1(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            var buffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>();
            var eCache = new BulkEventArgs<TArgs>();
            var eNext = new BulkEventArgs<TArgs>();

            Task<Task<IEnumerable<TArgs>>> cacheResponse;

            lock (BufferLock)
            {
                foreach (var arg in e)
                {
                    var key = Cache.GetKey(arg);

                    if (!WriteBuffer.TryGetValue(key, out var source))
                    {
                        WriteBuffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                        eCache.Add(arg);
                    }

                    // We will maintain a local version of the buffer in case items are removed from
                    // the global Buffer (because they finished writing) while we are reading from the cache.
                    // This avoids a scenario where we have a cache miss, but while waiting for that response
                    // to return from the cache it is updated with the value we requested.
                    buffer[key] = source;
                }

                cacheResponse = CacheProcessAsync(next, buffer, e, eCache, eNext);

                // The handler is allowed to add extra requests (representing additional resources that
                // were retured as a side effect). Make the primary batch request aware of these
                (e as BulkEventArgs<TArgs>)?.Add(eCache);
                // Make sure any extra requests are in the buffer, and alert existing requests that we
                // are expecting a response.
                SyncBuffers(eCache, buffer, WriteBuffer);
            }

            // Handle from the cache
            var response = Task.WhenAll(e.Select(request => buffer.TryGetValue(Cache.GetKey(request), out var source) ? Handle(request, source) : Task.CompletedTask));

            if (eCache.Count > 0)
            {
                SetResultAsync(cacheResponse, buffer, eNext);
            }

            await response;

            Task handling;

            // Rehandle requests with newer data
            lock (BufferLock)
            {
                handling = Task.WhenAll(e.Select(request =>
                {
                    var key = Cache.GetKey(request);
                    return WriteBuffer.TryGetValue(key, out var source) || buffer.TryGetValue(key, out source) ? Handle(request, source) : Task.CompletedTask;
                }));
            }

            await handling;

            return e.All(arg => arg.IsHandled);
        }

        private async void SetResultAsync(Task<Task<IEnumerable<TArgs>>> response, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, IEnumerable<TArgs> nextArgs)
        {
            Exception exception = null;

            try
            {
                var task = await response;

                foreach (var arg in nextArgs)
                {
                    var key = Cache.GetKey(arg);

                    if (buffer.TryGetValue(key, out var source))
                    {
                        source.TrySetResult(GetResponseAsync(task, arg));
                    }
                }

                nextArgs = await task ?? nextArgs;
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                try
                {
                    await Cache.Write(nextArgs.Where(arg => arg.IsHandled));
                }
                finally
                {
                    try
                    {
                        lock (BufferLock)
                        {
                            foreach (var kvp in buffer)
                            {
                                if (exception != null)
                                {
                                    kvp.Value.TrySetException(exception);
                                }
                                else
                                {
                                    kvp.Value.TrySetResult(Task.FromResult<TArgs>(null));
                                }

                                if (WriteBuffer.TryGetValue(kvp.Key, out var source) && source == kvp.Value)
                                {
                                    WriteBuffer.Remove(kvp.Key);
                                }
                            }
                        }
                    }
                    catch (Exception e1)
                    {
                        System.Diagnostics.Debug.WriteLine(e1);
                    }
                }
            }
        }

        private async Task<Task<IEnumerable<TArgs>>> CacheProcessAsync(IAsyncEventProcessor<IEnumerable<TArgs>> next, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, IEnumerable<TArgs> e, IEnumerable<TArgs> eCache, BulkEventArgs<TArgs> eNext)
        {
            try
            {
                var cacheSource = new TaskCompletionSource<Task<bool>>();

                SetCacheResultAsync(cacheSource, eCache, new ProcessorFunc<IEnumerable<TArgs>>(async e1 =>
                {
                    Task<bool> response;

                    lock (BufferLock)
                    {
                        // No point in syncing buffers on eCache here - anything that was added asynchronously
                        // to eCache is supposed to already be handled, in which case it would just be removed
                        // in the next step

                        foreach (var arg in eCache.Where(arg => arg.IsHandled))
                        {
                            var key = Cache.GetKey(arg);

                            if (WriteBuffer.Remove(key, out var source) | buffer.Remove(key, out source))
                            {
                                source.TrySetResult(Task.FromResult(arg));
                            }
                        }

                        // Check if the request has been fulfilled in the time it took to check the cache. If the task
                        // is not complete, the underlying source has not been transitioned, meaning it's the same
                        // source we added before checking the cache. We can't await that task because it won't
                        // complete (we are expected to complete it with the response from next)
                        foreach (var arg in e1)
                        {
                            if (!buffer.TryGetValue(Cache.GetKey(arg), out var source) || !source.Task.IsCompleted)
                            {
                                eNext.Add(arg);
                            }
                        }

                        response = eNext.Count == 0 ? Task.FromResult(true) : next.ProcessAsync(eNext);

                        (e1 as BulkEventArgs<TArgs>)?.Add(eNext);
                        SyncBuffers(eNext, buffer, WriteBuffer);
                    }

                    cacheSource.SetResult(response);

                    try
                    {
                        return await response;
                    }
                    finally
                    {
                        (e1 as BulkEventArgs<TArgs>)?.Add(eNext);
                    }
                }));

                return NextProcessAsync(await cacheSource.Task, buffer, e, eCache, eNext);
            }
            finally
            {
                (e as BulkEventArgs<TArgs>)?.Add(eNext);
                (e as BulkEventArgs<TArgs>)?.Add(eCache);

                // No buffer sync here - async call to the cache has already completed at this point so
                // no more work to be done. Anything added since last sync should already be handled and
                // therefore would not have been sent to next
            }
        }

        private async Task<IEnumerable<TArgs>> NextProcessAsync(Task<bool> task, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, IEnumerable<TArgs> args, IEnumerable<TArgs> cacheArgs, IEnumerable<TArgs> nextArgs)
        {
            try
            {
                return await task ? nextArgs.ToArray() : null;
            }
            finally
            {
                (args as BulkEventArgs<TArgs>)?.Add(nextArgs);
                (args as BulkEventArgs<TArgs>)?.Add(cacheArgs);

                lock (BufferLock)
                {
                    SyncBuffers(nextArgs, buffer, WriteBuffer);
                }
            }
        }

        private async void SetCacheResultAsync(TaskCompletionSource<Task<bool>> cacheSource, IEnumerable<TArgs> eCache, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            try
            {
                var response = await Cache.ProcessAsync(eCache, next);
                cacheSource.TrySetResult(Task.FromResult(response));
            }
            catch (Exception e)
            {
                cacheSource.TrySetException(e);
            }
        }

        private void SyncBuffers(IEnumerable<TArgs> args, Dictionary<object, TaskCompletionSource<Task<TArgs>>> primaryBuffer, Dictionary<object, TaskCompletionSource<Task<TArgs>>> secondaryBuffer)
        {
            foreach (var request in args)
            {
                var key = Cache.GetKey(request);

                if (!primaryBuffer.TryGetValue(key, out var source))
                {
                    primaryBuffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                }
                secondaryBuffer[key] = source;

                if (request.IsHandled)
                {
                    source.TrySetResult(Task.FromResult(request));
                }
            }
        }

        private async Task Handle(TArgs e, TaskCompletionSource<Task<TArgs>> buffered)
        {
            Cache.Process(e, await (await buffered.Task));
        }

        private static async Task<TArgs> GetResponseAsync(Task task, TArgs e)
        {
            await task;
            return e;
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
    }
}