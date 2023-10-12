using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Movies.CoRExtensions;

namespace Movies
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, EventArgsAsyncWrapper<TEventArgs> args);

    //public delegate void ChainLinkEventHandler<in T>(T e);// where T : ChainEventArgs;
    //public delegate void ChainEventHandler<T>(T e, ChainLinkEventHandler<T> next);// where T : ChainEventArgs;
    //public delegate Task AsyncChainEventHandler<T>(T e, ChainLinkEventHandler<T> next);// where T : ChainEventArgs;

    public interface IEventProcessor<in T>
    {
        bool Process(T e);
    }

    public interface ICoRProcessor<T>
    {
        bool Process(T e, IEventProcessor<T> next);
    }

    public interface ICoRStrictProcessor<in T>
    {
        bool Process(T e, Action next);
    }

    public interface IAsyncEventProcessor<in T>
    {
        Task<bool> ProcessAsync(T e);
    }

    public interface IAsyncCoRProcessor<T> //: IAsyncEventProcessor<T>
    {
        Task<bool> ProcessAsync(T e, IAsyncEventProcessor<T> next);

        //Task<bool> IAsyncEventProcessor<T>.ProcessAsync(T e) => ProcessAsync(e, null);
    }

    //public interface IRestEventProcessor : IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyArgs<Uri>>>> { }

    public static class AsyncEventHandlerExtensions
    {
        public static void Invoke<T>(this EventHandler<EventArgsAsyncWrapper<T>> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static Task InvokeAsync<T>(this AsyncEventHandler<T> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static EventHandler<EventArgsAsyncWrapper<T>> Create<T>(AsyncEventHandler<T> handler) where T : EventArgs => (sender, e) => e.ExecuteInvoke(sender, handler);
    }

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

    public static class CoRExtensions
    {
        public static ChainLink<TDerived> SetNext<TBase, TDerived>(this ChainLink<TDerived> link, IEventProcessor<TBase> processor) where TDerived : TBase => link.SetNext(new CoREventProcessor<TBase, TDerived>(processor));
        //public static ICoRProcessor<TDerived> Convert<TBase, TDerived>(this ICoRProcessor<TBase> processor) where TBase : EventArgs where TDerived : TBase => new GeneralProcessor<TBase, TDerived>(processor);

        public class CoREventProcessor<T> : CoREventProcessor<T, T> { public CoREventProcessor(IEventProcessor<T> processor) : base(processor) { } }

        public class CoREventProcessor<TBase, TDerived> : ICoRProcessor<TDerived> where TDerived : TBase
        {
            public IEventProcessor<TBase> Processor { get; }

            public CoREventProcessor(IEventProcessor<TBase> processor)
            {
                Processor = processor;
            }

            public bool Process(TDerived e, IEventProcessor<TDerived> next)
            {
                if (Processor.Process(e))
                {
                    return true;
                }
                else if (next != null)
                {
                    return next.Process(e);
                }
                else
                {
                    return false;
                }
            }
        }

        private class CoRProcessorFunc<T> : ICoRProcessor<T>
        {
            public Func<T, IEventProcessor<T>, bool> Processor { get; }

            public CoRProcessorFunc(Func<T, IEventProcessor<T>, bool> processor)
            {
                Processor = processor;
            }

            public bool Process(T e, IEventProcessor<T> next) => Processor(e, next);
        }

        private class EventProcessorFunc<T> : IEventProcessor<T>
        {
            public Func<T, bool> Processor { get; }

            public EventProcessorFunc(Func<T, bool> processor)
            {
                Processor = processor;
            }

            public bool Process(T e) => Processor(e);
        }

        /*private class GeneralProcessor<TBase, TDerived> : ICoRProcessor<TDerived> where TBase : EventArgs where TDerived : TBase
        {
            public ICoRProcessor<TBase> Processor { get; }

            public GeneralProcessor(ICoRProcessor<TBase> processor)
            {
                Processor = processor;
            }

            public void Process(TDerived e, IEventProcessor<TDerived> next) => Processor.Process(e, new FilterProcessor(next));

            private class FilterProcessor : IEventProcessor<TBase>
            {
                public IEventProcessor<TDerived> Processor { get; }

                public FilterProcessor(IEventProcessor<TDerived> processor)
                {
                    Processor = processor;
                }

                public void Process(TBase e)
                {
                    if (e is TDerived derived)
                    {
                        Processor.Process(derived);
                    }
                }
            }
        }*/
    }

    public sealed class ChainLink<T> : IEventProcessor<T>
    {
        public ChainLink<T> Next { get; set; }
        public ICoRProcessor<T> Processor { get; }

        public ChainLink(ICoRProcessor<T> processor, ChainLink<T> next = null)
        {
            Next = next;
            Processor = processor;
        }

        //public static implicit operator ChainLink<T>(ILinkHandler<T> handler) => new ChainLink<T>(handler);

        public static ChainLink<T> Build(params ChainLink<T>[] links)
        {
            for (int i = 0; i < links.Length - 1; i++)
            {
                links[i].Next = links[i + 1];
            }

            return links.FirstOrDefault();
        }

        public ChainLink<T> SetNext(ChainLink<T> link) => Next = link;
        public ChainLink<T> SetNext(ICoRProcessor<T> handler) => SetNext(new ChainLink<T>(handler));

        public bool Process(T e)
        {
            if (Processor == null)
            {
                return false;
            }

            return Processor.Process(e, Next);
        }
    }
}