using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public static class AsyncCacheAsideProcessor
    {
        public static AsyncCacheAsideProcessor<T> Create<T>(IAsyncCacheAsideProcessor<T> cache) where T : EventArgsRequest => new AsyncCacheAsideProcessor<T>(cache);

        public static AsyncChainLink<IEnumerable<TArgs>> SetNextCacheAside<TArgs>(this AsyncChainLink<IEnumerable<TArgs>> link, IAsyncCoRProcessor<IEnumerable<TArgs>> next)
            where TArgs : EventArgsRequest
        {
            var nextLink = new AsyncChainLink<IEnumerable<TArgs>>(next);

            if (link.Processor is IAsyncCacheAsideProcessor<TArgs> cacheAside)
            {
                //nextLink = new AsyncChainLink<IEnumerable<TArgs>>(new WriteBackProcessor<TArgs>(cacheAside, null, link));
                nextLink = new AsyncChainLink<IEnumerable<TArgs>>(new AsyncCacheAsideProcessor<TArgs>(cacheAside));
            }

            return link.SetNext(nextLink);
        }
    }

    public interface IAsyncCacheAsideProcessor<TArgs> : IWriteBackProcessor<TArgs>, IAsyncCoRProcessor<IEnumerable<TArgs>> { }

    public interface IWriteBackProcessor<TArgs>
    {
        void Write(IEnumerable<TArgs> args, Task task);
        void Process(TArgs args, TArgs buffered);
    }

    public abstract class CacheAside<TArgs> : IAsyncEventProcessor<IEnumerable<TArgs>>
    {
        public IEventAsyncCache<TArgs> Cache { get; }
        public IAsyncEventProcessor<IEnumerable<TArgs>> Datastore { get; }
        public IAsyncCoRProcessor<IEnumerable<TArgs>> Datastore1 { get; }

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> e)
        {
            if (await Cache.Read(e))
            {
                return true;
            }

            //return await Datastore1.ProcessAsync(e, new CoRProcessor(Datastore, Cache));
            var result = await Datastore.ProcessAsync(e);
            await Cache.Write(e.Select(Convert));

            return result;
        }

        protected abstract TArgs Convert(TArgs args);

        private class CoRProcessor : IAsyncEventProcessor<IEnumerable<TArgs>>
        {
            public IAsyncEventProcessor<IEnumerable<TArgs>> Processor { get; }
            public IEventAsyncCache<TArgs> Cache { get; }

            public CoRProcessor(IAsyncEventProcessor<IEnumerable<TArgs>> processor, IEventAsyncCache<TArgs> cache)
            {
                Processor = processor;
                Cache = cache;
            }

            public async Task<bool> ProcessAsync(IEnumerable<TArgs> e)
            {
                var result = await Processor.ProcessAsync(e);
                await Cache.Write(e);
                return result;
            }
        }
    }

    public class WriteBackProcessor<TArgs> : IAsyncEventProcessor<IEnumerable<TArgs>>
        where TArgs : EventArgsRequest
    {
        public IWriteBackProcessor<TArgs> Processor { get; }
        public IEnumerable<TArgs> args { get; }
        public IAsyncEventProcessor<IEnumerable<TArgs>> next { get; }

        public WriteBackProcessor(IWriteBackProcessor<TArgs> processor, IEnumerable<TArgs> args, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            Processor = processor;
            this.args = args;
            this.next = next;
        }

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> eNext)
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
                        Processor.Write(eNext, source.Task);
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
                    if (index.TryGetValue(arg.Request, out var newer))
                    {
                        Processor.Process(arg, newer);
                    }
                }
            }
        }
    }

    public interface ICacheAsideProcessor<TArgs>
    {
        Task<bool> ProcessAsync(IEnumerable<TArgs> args, Task task);
    }

    public class AsyncCacheAsideProcessor<TArgs> : IAsyncCoRProcessor<IEnumerable<TArgs>> where TArgs : EventArgsRequest
    {
        public IAsyncCacheAsideProcessor<TArgs> Cache { get; }

        public AsyncCacheAsideProcessor(IAsyncCacheAsideProcessor<TArgs> cache)
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