using System;
using System.Threading.Tasks;

namespace Movies
{
    public static class AsyncCoRExtensions
    {
        public static Task ProcessAsync<T>(this ICoRProcessor<EventArgsAsyncWrapper<T>> handler, T e, IEventProcessor<EventArgsAsyncWrapper<T>> next) where T : EventArgs
        {
            var wrapper = new EventArgsAsyncWrapper<T>(e);
            handler.Process(wrapper, next);
            return wrapper.Task;
        }

        public static async Task<bool> ProcessAsync<T>(this IEventProcessor<T> handler, T e) where T : EventArgs, IAsyncResponse
        {
            //var wrapper = new EventArgsAsyncWrapper<T>(e);
            var result = handler.Process(e);
            await e.Response;//.ContinueWith(_ => result);
            return result;
        }

        public static ChainLink<EventArgsAsyncWrapper<TDerived>> Create<TBase, TDerived>(this IAsyncEventProcessor<TBase> processor) where TDerived : TBase => ToChainLink(new AsyncCoRProcessor<TBase, TDerived>(processor));
        public static ChainLink<EventArgsAsyncWrapper<TDerived>> SetNext<TBase, TDerived>(this ChainLink<EventArgsAsyncWrapper<TDerived>> link, IAsyncEventProcessor<TBase> processor) where TDerived : TBase => link.SetNext(Create<TBase, TDerived>(processor));

        public static ChainLink<EventArgsAsyncWrapper<T>> Create<T>(this IAsyncEventProcessor<T> processor) => ToChainLink<T>(new AsyncCoREventProcessor<T>(processor));
        public static ChainLink<EventArgsAsyncWrapper<T>> SetNext<T>(this ChainLink<EventArgsAsyncWrapper<T>> link, IAsyncCoRProcessor<T> handler) => link.SetNext(ToChainLink(handler));

        public static ChainLink<EventArgsAsyncWrapper<T>> ToChainLink<T>(this IAsyncCoRProcessor<T> handler) => new ChainLink<EventArgsAsyncWrapper<T>>(new AsyncCoRProcessor<T>(handler));

        public class AsyncCoREventProcessor<T> : AsyncCoRProcessor<T, T> { public AsyncCoREventProcessor(IAsyncEventProcessor<T> processor) : base(processor) { } }

        public class AsyncCoRProcessor<TBase, TDerived> : IAsyncCoRProcessor<TDerived> where TDerived : TBase
        {
            public IAsyncEventProcessor<TBase> Processor { get; }

            public AsyncCoRProcessor(IAsyncEventProcessor<TBase> processor)
            {
                Processor = processor;
            }

            public async Task<bool> ProcessAsync(TDerived e, IAsyncEventProcessor<TDerived> next)
            {
                if (await Processor.ProcessAsync(e))
                {
                    return true;
                }
                else if (next != null)
                {
                    return await next.ProcessAsync(e);
                }
                else
                {
                    return false;
                }
            }
        }

        private class AsyncCoRProcessor<T> : ICoRProcessor<EventArgsAsyncWrapper<T>>
        {
            public IAsyncCoRProcessor<T> Processor { get; }

            public AsyncCoRProcessor(IAsyncCoRProcessor<T> processor)
            {
                Processor = processor;
            }

            public bool Process(EventArgsAsyncWrapper<T> e, IEventProcessor<EventArgsAsyncWrapper<T>> next)
            {
                IAsyncEventProcessor<T> asyncNext = new AsyncEventProcessorFunc<T>(e => next.ProcessAsync(new EventArgsAsyncWrapper<T>(e)));
                e.ExecuteInvoke(null, (sender, e) => Processor.ProcessAsync(e.Args, next == null ? null : asyncNext));
                return false;
            }
        }

        private class AsyncCoRProcessorFunc<T> : IAsyncCoRProcessor<T>
        {
            public Func<T, IAsyncEventProcessor<T>, Task<bool>> Processor { get; }

            public AsyncCoRProcessorFunc(Func<T, IAsyncEventProcessor<T>, Task<bool>> processor)
            {
                Processor = processor;
            }

            public Task<bool> ProcessAsync(T e, IAsyncEventProcessor<T> next) => Processor(e, next);
        }

        private class AsyncEventProcessorFunc<T> : IAsyncEventProcessor<T>
        {
            public Func<T, Task<bool>> Processor { get; }

            public AsyncEventProcessorFunc(Func<T, Task<bool>> processor)
            {
                Processor = processor;
            }

            public Task<bool> ProcessAsync(T e) => Processor(e);
        }
    }
}