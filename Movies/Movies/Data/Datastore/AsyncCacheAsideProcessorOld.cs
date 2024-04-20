using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class AsyncCacheAsideProcessorOld<TArgs> : IAsyncCoRProcessor<IEnumerable<TArgs>> where TArgs : EventArgsRequest
    {
        public BufferedCache<TArgs> Cache { get; }

        private readonly object BufferLock = new object();
        private Dictionary<object, TaskCompletionSource<Task<TArgs>>> WriteBuffer = new Dictionary<object, TaskCompletionSource<Task<TArgs>>>();

        public AsyncCacheAsideProcessorOld(BufferedCache<TArgs> cache)
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
            var args = e.ToArray();

            Task<Task<bool>> cacheResponse;

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

                cacheResponse = CacheProcessAsync(next, buffer, eCache);

                // The handler is allowed to add extra requests (representing additional resources that
                // were retured as a side effect). Make the primary batch request aware of these
                (e as BulkEventArgs<TArgs>)?.Add(eCache);
                // Make sure any extra requests are in the buffer, and alert existing requests that we
                // are expecting a response.
                //SyncBuffers(eCache, buffer, WriteBuffer);
            }

            // Handle from the cache
            var response = Task.WhenAll(args.Select(request => buffer.TryGetValue(Cache.GetKey(request), out var source) ? Handle(request, source) : Task.CompletedTask));

            if (eCache.Count > 0)
            {
                SetResultAsync(cacheResponse, buffer, eCache);
            }

            try
            {
                await response;
            }
            finally
            {
                (e as BulkEventArgs<TArgs>)?.Add(eCache);
                // No buffer sync here - async call to the cache has already completed at this point so
                // no more work to be done. Anything added since last sync should already be handled and
                // therefore would not have been sent to next
            }

            Task handling;

            // Rehandle requests with newer data
            lock (BufferLock)
            {
                handling = Task.WhenAll(args.Select(request =>
                {
                    var key = Cache.GetKey(request);
                    return WriteBuffer.TryGetValue(key, out var source) || buffer.TryGetValue(key, out source) ? Handle(request, source) : Task.CompletedTask;
                }));
            }

            await handling;

            return e.All(arg => arg.IsHandled);
        }

        private async void SetResultAsync(Task<Task<bool>> response, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, IEnumerable<TArgs> cacheArgs)
        {
            object result;

            try
            {
                result = await response;
            }
            catch (Exception e)
            {
                result = e;
            }

            foreach (var arg in cacheArgs)
            {
                var key = Cache.GetKey(arg);

                if (arg.IsHandled)
                {
                    foreach (var buffer1 in new[] { WriteBuffer, buffer })
                    {
                        if (buffer1.TryGetValue(key, out var source))
                        {
                            source.TrySetResult(Task.FromResult(arg));
                        }
                    }
                }
                else if (buffer.TryGetValue(key, out var source))
                {
                    if (result is Exception exception)
                    {
                        source.TrySetException(exception);
                    }
                    else if (result is Task task)
                    {
                        source.TrySetResult(GetResponseAsync(task, arg));
                    }
                }
            }
        }

        private async void Write(IEnumerable<TArgs> args)
        {
            var writing = new List<TArgs>();

            try
            {
                lock (BufferLock)
                {
                    foreach (var arg in args)
                    {
                        var key = Cache.GetKey(arg);

                        if (arg.IsHandled)
                        {
                            writing.Add(arg);
                            WriteBuffer.TryAdd(key, new TaskCompletionSource<Task<TArgs>>());
                        }
                        else
                        {
                            WriteBuffer.Remove(key);
                        }
                    }
                }

                await Cache.Write(writing);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
            finally
            {
                lock (BufferLock)
                {
                    foreach (var arg in writing)
                    {
                        WriteBuffer.Remove(Cache.GetKey(arg));
                    }
                }
            }
        }

        private async Task<Task<bool>> CacheProcessAsync(IAsyncEventProcessor<IEnumerable<TArgs>> next, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, BulkEventArgs<TArgs> eCache)
        {
            var eNext = new BulkEventArgs<TArgs>();
            var cacheSource = new TaskCompletionSource<Task<bool>>();

            SetCacheResultAsync(cacheSource, eCache, new ProcessorFunc<IEnumerable<TArgs>>(async e1 =>
            {
                Task<bool> response;

                lock (BufferLock)
                {
                    // No point in syncing buffers on eCache here - anything that was added asynchronously
                    // to eCache is supposed to already be handled, in which case it would just be removed
                    // in the next step

                    // Check if the request has been fulfilled in the time it took to check the cache. If the task
                    // is not complete, the underlying source has not been transitioned, meaning it's the same
                    // source we added before checking the cache. We can't await that task because it won't
                    // complete (we are expected to complete it with the response from next)
                    foreach (var arg in e1)
                    {
                        var key = Cache.GetKey(arg);

                        if (!buffer.TryGetValue(key, out var source) || !source.Task.IsCompleted)
                        {
                            eNext.Add(arg);
                        }
                    }

                    response = eNext.Count == 0 ? Task.FromResult(true) : next.ProcessAsync(eNext);

                    (e1 as BulkEventArgs<TArgs>)?.Add(eNext);
                    //SyncBuffers(eNext, WriteBuffer, buffer);

                    foreach (var request in eNext)
                    {
                        var key = Cache.GetKey(request);

                        if (!WriteBuffer.TryGetValue(key, out var source))
                        {
                            WriteBuffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                        }

                        //source.TrySetResult(GetResponseAsync(response, request));
                    }
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

            //var nextSource = new TaskCompletionSource<Task<bool>>();

            //await cacheSource.Task;
            //Task<bool> response;

            //lock (BufferLock)
            //{
            //    // Check cache

            //    response = next.ProcessAsync(eNext);
            //}

            //nextSource.SetResult(response);
            //return response;

            try
            {
                return NextProcessAsync(await cacheSource.Task, buffer, eCache, eNext);
            }
            finally
            {
                Print.Log(Cache.Cache, "start 1");
                eCache.Add(eNext);
                Print.Log(Cache.Cache, "end 1");
            }
        }

        private async Task<bool> NextProcessAsync(Task<bool> response, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, BulkEventArgs<TArgs> cacheArgs, IEnumerable<TArgs> nextArgs)
        {
            try
            {
                return await response;
            }
            finally
            {
                try
                {
                    Print.Log(Cache.Cache, "start 2");
                    cacheArgs.Add(nextArgs);
                    Print.Log(Cache.Cache, "end 2");

                    lock (BufferLock)
                    {
                        //SyncBuffers(nextArgs, buffer, WriteBuffer);
                    }

                    Write(nextArgs);
                }
                catch (Exception e)
                {
                    Print.Log(e);
                    throw e;
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
            finally
            {
                //var result = await cacheSource.Task;
                //Write(result.Item1);
            }
        }

        private void UpdateBuffer(IEnumerable<TArgs> args, Task response)
        {
            foreach (var request in args)
            {
                var key = Cache.GetKey(request);

                if (!WriteBuffer.TryGetValue(key, out var source))
                {
                    WriteBuffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                }

                source.TrySetResult(GetResponseAsync(response, request));
            }
        }

        private void SyncBuffers(IEnumerable<TArgs> args, Dictionary<object, TaskCompletionSource<Task<TArgs>>> primaryBuffer, Dictionary<object, TaskCompletionSource<Task<TArgs>>> secondaryBuffer)
        {
            foreach (var request in args)
            {
                var key = Cache.GetKey(request);

                if (!primaryBuffer.TryGetValue(key, out var source))
                {
                    if (!secondaryBuffer.ContainsKey(key))
                    {
                        primaryBuffer[key] = secondaryBuffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                    }
                }
                else if (secondaryBuffer.TryGetValue(key, out var source1) && source1 != source)
                {
                    //TrySetResult(source, source1);
                }
                secondaryBuffer.TryAdd(key, source);
                //secondaryBuffer[key] = source;
            }
        }

        private async Task Handle(TArgs e, TaskCompletionSource<Task<TArgs>> buffered)
        {
            var args = await (await buffered.Task);

            if (args?.IsHandled == true)
            {
                Cache.Process(e, args);
            }
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