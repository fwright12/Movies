using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class AsyncCacheAsideProcessor<TArgs> : IAsyncCoRProcessor<IEnumerable<TArgs>> where TArgs : EventArgsRequest
    {
        public BufferedCache<TArgs> Cache { get; }

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

        public Task<bool> ProcessAsync1(IEnumerable<TArgs> args, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            return Cache.ProcessAsync(args, new ProcessorFunc<IEnumerable<TArgs>>(async eNext =>
            {
                var source = new TaskCompletionSource<bool>();

                try
                {
                    Task<bool> response;

                    lock (next)
                    {
                        //Cache.Write(eNext, source.Task);

                        try
                        {
                            response = next.ProcessAsync(eNext);
                            Cache.Write(eNext, source.Task);
                        }
                        finally
                        {
                            (args as BulkEventArgs<TArgs>)?.Add(eNext);
                        }
                    }

                    var result = await response;

                    source.SetResult(result);
                    return result;
                }
                catch (Exception e)
                {
                    source.SetException(e);
                    throw e;
                }
                finally
                {
                    (args as BulkEventArgs<TArgs>)?.Add(eNext);

                    var index = eNext.ToDictionary(arg => arg.Request, arg => arg);
                    foreach (var arg in args)
                    {
                        if (index.TryGetValue(arg.Request, out var next))
                        {
                            Cache.Process(arg, next);
                        }
                    }
                }
            }));
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