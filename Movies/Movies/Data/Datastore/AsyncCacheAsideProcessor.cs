using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class AsyncBufferedProcessor<T> : IAsyncEventProcessor<IEnumerable<T>>
    {
        private Dictionary<T, Task<T>> Buffer = new Dictionary<T, Task<T>>();

        private readonly object BufferLock = new object();

        public Task<bool> ProcessAsync(IEnumerable<T> e)
        {
            var responses = new List<Task<T>>();
            var sources = new List<TaskCompletionSource<T>>();

            lock (BufferLock)
            {
                foreach (var arg in e)
                {
                    if (Buffer.TryGetValue(arg, out var response))
                    {
                        responses.Add(response);
                    }
                    else
                    {
                        var source = new TaskCompletionSource<T>();
                        Buffer[arg] = response = source.Task;
                        sources.Add(source);
                    }
                }
            }

            if (sources.Count > 0)
            {
                //SendAsync()
            }

            return Task.FromResult(false);
        }

        private async void SendAsync(IEnumerable<T> args, Task<T> task, Uri key, TaskCompletionSource<HttpResponseMessage> source)
        {
            try
            {
                //source.SetResult(await task);
            }
            catch (Exception e)
            {
                source.SetException(e);
            }
            finally
            {
                lock (BufferLock)
                {
                    //Buffer.Remove(key);
                }
            }
        }

        public static async Task<HttpResponseMessage> RespondBuffered(Task<HttpResponseMessage> buffered)
        {
            var response = await buffered;
            var request = response.RequestMessage;
            HttpContent content;

            if (request.Method == HttpMethod.Get)
            {
                content = response.Content;
            }
            else
            {
                content = request.Content;
            }

            return new HttpResponseMessage(response.StatusCode)
            {
                Content = content,
            };
        }
    }

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
            }

            // Handle from the cache
            buffered.Add(RespondBuffered(buffer, e));
            Task writeTask = Task.CompletedTask;
            var cacheCall = new TaskCompletionSource<bool>();

            if (eCache.Count > 0)
            {
                SetResultAsync(cacheCall.Task, eCache);
            }

            try
            {
                var nextWrapper = new ProcessorFunc<IEnumerable<TArgs>>(async e1 =>
                {
                    //cacheCall.TrySetResult(true);
                    (e as BulkEventArgs<TArgs>)?.Add(e1);
                    //UpdateBuffer(eCache);

                    var eNext = new BulkEventArgs<TArgs>();
                    Print.Log(Cache.Cache, string.Join(", ", e1));
                    // Check if the request has been fulfilled in the time it took to check the cache. If the task
                    // is not complete, the underlying source has not been transitioned, meaning it's the same
                    // source we added before checking the cache. We can't await that task because it won't
                    // complete (we are expected to complete it with the response from next)
                    lock (BufferLock)
                    {
                        //eNext = new BulkEventArgs<TArgs>(e1.Where(arg => !buffer.TryGetValue(Cache.GetKey(arg), out var value) || !value.Task.IsCompleted));

                        foreach (var arg in e1)
                        {
                            var key = Cache.GetKey(arg);

                            if (!buffer.TryGetValue(key, out var source))
                            {
                                buffer[key] = source = new TaskCompletionSource<Task<TArgs>>();
                            }

                            if (source.Task.IsCompleted)
                            {
                                //UpdateBuffer(arg.AsEnumerable(), source.Task);
                            }
                            else
                            {
                                Print.Log("here");
                                eNext.Add(arg);
                            }
                        }

                        foreach (var arg in eCache)
                        {
                            var key = Cache.GetKey(arg);

                            if (arg.IsHandled)
                            {
                                if (WriteBuffer.TryGetValue(key, out var source))
                                {
                                    Print.Log("\t" + arg);
                                    //source.TrySetResult(Task.FromResult(arg));
                                }
                            }
                        }
                    }
                    Print.Log();
                    try
                    {
                        return await SetResultAsync(() => next.ProcessAsync(eNext), eNext, e1, e);
                    }
                    finally
                    {
                        eCache.Add(e1);
                        writeTask = Safely(Cache.Write(eNext.Where(arg => arg.IsHandled)));
                    }
                });

                return await SetResultAsync(() => Cache.ProcessAsync(eCache, nextWrapper), eCache, e);
            }
            catch (Exception exception)
            {
                cacheCall.TrySetException(exception);
                return false;
            }
            finally
            {
                // Rehandle requests with newer data
                lock (BufferLock)
                {
                    buffered.Add(RespondBuffered(WriteBuffer, e));
                }

                cacheCall.TrySetResult(true);

                // Remove items from the buffer once writing to the cache has completed
                UpdateBufferOnWriteComplete(eCache, writeTask);
            }
        }

        private Task RespondBuffered(Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, IEnumerable<TArgs> e)
        {
            var tasks = new List<Task>();

            foreach (var request in e)
            {
                if (buffer.TryGetValue(Cache.GetKey(request), out var source))
                {
                    tasks.Add(Handle(request, source));
                }
            }

            return Task.WhenAll(tasks);
        }

        private async void SetResultAsync(Task<bool> task, IEnumerable<TArgs> args)
        {
            try
            {
                await task;

                lock (BufferLock)
                {
                    foreach (var arg in args)
                    {
                        if (!arg.IsHandled)
                        {
                            continue;
                        }

                        var key = Cache.GetKey(arg);

                        if (WriteBuffer.TryGetValue(key, out var source))
                        {
                            source.TrySetResult(Task.FromResult(arg));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                lock (BufferLock)
                {
                    foreach (var arg in args)
                    {
                        if (WriteBuffer.TryGetValue(Cache.GetKey(arg), out var source))
                        {
                            source.TrySetException(e);
                        }
                    }
                }
            }
            finally
            {
                lock (BufferLock)
                {
                    foreach (var arg in args)
                    {
                        //WriteBuffer.Remove(Cache.GetKey(arg));
                    }
                }
            }
        }

        private async Task<bool> SetResultAsync(Func<Task<bool>> request, BulkEventArgs<TArgs> args, params IEnumerable<TArgs>[] supersets)
        {
            if (args.Count == 0)
            {
                return true;
            }

            Task<bool> response = Task.FromResult(false);

            try
            {
                response = request();

                if (!response.IsCompleted)
                {
                    // The handler is allowed to add extra requests (representing additional resources that
                    // were retured as a side effect). Make the primary batch request aware of these
                    foreach (var superset in supersets)
                    {
                        (superset as BulkEventArgs<TArgs>)?.Add(args);
                    }

                    // Make sure any extra requests are in the buffer, and alert existing requests that we
                    // are expecting a response. We can choose to update items before we actually get the
                    // response (preemptively) or wait until we have it.
                    UpdateBuffer(args);
                }

                return await response;
            }
            finally
            {
                foreach (var superset in supersets)
                {
                    (superset as BulkEventArgs<TArgs>)?.Add(args);
                }
                UpdateBuffer(args, response);
            }
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
                        else
                        {
                            // We are expecting a response but don't have it yet
                            source.TrySetResult(GetResponseAsync(handling, request));
                        }
                    }
                }
            }
        }

        private List<Task> CheckBuffers(IEnumerable<TArgs> e, Dictionary<object, TaskCompletionSource<Task<TArgs>>> buffer, BulkEventArgs<TArgs> unhandled = null)
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

        private void RefreshBuffers(IEnumerable<TArgs> e, params Dictionary<object, TaskCompletionSource<Task<TArgs>>>[] buffers)
        {
            foreach (var request in e)
            {
                var key = Cache.GetKey(request);
                TaskCompletionSource<Task<TArgs>> source = null;

                foreach (var buffer in buffers)
                {
                    if (buffer.TryGetValue(key, out source))
                    {
                        break;
                    }
                }

                if (source == null)
                {
                    source = new TaskCompletionSource<Task<TArgs>>();

                    foreach (var buffer in buffers)
                    {
                        buffer[key] = source;
                    }
                }

                if (request.IsHandled)
                {
                    source.TrySetResult(Task.FromResult(request));
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

        private static async Task<TArgs> Unwrap(TaskCompletionSource<Task<TArgs>> source) => await (await source.Task);

        private async Task Handle(IEnumerable<KeyValuePair<TArgs, TaskCompletionSource<Task<TArgs>>>> sources)
        {
            await Task.WhenAll(sources.Select(source => source.Value.Task));

            foreach (var source in sources)
            {
                Cache.Process(source.Key, source.Value.Task.Result.Result);
            }
        }

        private async Task Handle(TArgs e, TaskCompletionSource<Task<TArgs>> buffered)
        {
            try
            {
                Cache.Process(e, await (await buffered.Task));
            }
            catch { }
        }

        private static async Task<TArgs> GetResponseAsync(Task task, TArgs e)
        {
            await task;
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