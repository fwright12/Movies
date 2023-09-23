using Movies;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, EventArgsAsyncWrapper<TEventArgs> args) where TEventArgs : EventArgs;

    public delegate void ChainLinkEventHandler<in T>(T e);// where T : ChainEventArgs;
    public delegate void ChainEventHandler<T>(T e, ChainLinkEventHandler<T> next);// where T : ChainEventArgs;
    public delegate Task AsyncChainEventHandler<T>(T e, ChainLinkEventHandler<T> next);// where T : ChainEventArgs;

    public interface IEventProcessor<in T> where T : EventArgs
    {
        void Process(T e);
    }

    public interface ICoRProcessor<T> where T : EventArgs
    {
        void Process(T e, IEventProcessor<T> next);
    }

    public interface IAsyncEventProcessor<in T> where T : EventArgs
    {
        Task ProcessAsync(T e);
    }

    public interface IAsyncCoRProcessor<T> where T : EventArgs
    {
        Task ProcessAsync(T e, IAsyncEventProcessor<T> next);
    }

    public interface IRestCoRProcessor : IAsyncCoRProcessor<MultiRestEventArgs> { }
    public interface IRestEventProcessor : IAsyncEventProcessor<MultiRestEventArgs> { }

    public static class ChainExtensions
    {
        public static void Invoke<T>(this EventHandler<EventArgsAsyncWrapper<T>> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static Task InvokeAsync<T>(this AsyncEventHandler<T> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static Task InvokeAsync<T>(this ChainLinkEventHandler<EventArgsAsyncWrapper<T>> handler, T e) where T : EventArgs
        {
            var wrapper = new EventArgsAsyncWrapper<T>(e);
            handler(wrapper);
            return wrapper.Task;
        }

        public static Task ProcessAsync<T>(this ICoRProcessor<EventArgsAsyncWrapper<T>> handler, T e, IEventProcessor<EventArgsAsyncWrapper<T>> next) where T : EventArgs
        {
            var wrapper = new EventArgsAsyncWrapper<T>(e);
            handler.Process(wrapper, next);
            return wrapper.Task;
        }

        public static Task ProcessAsync<T>(this IEventProcessor<EventArgsAsyncWrapper<T>> handler, T e) where T : EventArgs
        {
            var wrapper = new EventArgsAsyncWrapper<T>(e);
            handler.Process(wrapper);
            return wrapper.Task;
        }

        //public static ChainLink<T> SetNext<T>(this ChainLink<T> link, AsyncChainEventHandler<T> handler) where T : AsyncChainEventArgs, IAsyncEventArgsRequest => link.SetNext((e, next) => e.RequestSuspension(handler.Invoke(e, next)));

        //public static ChainLink<T> SetNext<T>(this ChainLink<T> link, ChainLinkEventHandler<T> handler) where T : ChainEventArgs => link.SetNext(Create(handler));
        //public static ChainLink<T> Create<T>(ChainLinkEventHandler<T> handler) where T : ChainEventArgs => new ChainLink<T>((e, next) =>
        //{
        //    handler(e);

        //    if (!e.Handled && next != null)
        //    {
        //        next(e);
        //    }
        //});

        public static ChainLink<EventArgsAsyncWrapper<T>> Create<T>(IAsyncEventProcessor<T> processor) where T : EventArgsRequest => Create<T>((IAsyncCoRProcessor<T>)new AsyncNextProcessor<T>(processor));

        private class AsyncNextProcessor<T> : IAsyncCoRProcessor<T> where T : EventArgsRequest
        {
            public IAsyncEventProcessor<T> Processor { get; }

            public AsyncNextProcessor(IAsyncEventProcessor<T> processor)
            {
                Processor = processor;
            }

            public async Task ProcessAsync(T e, IAsyncEventProcessor<T> next)
            {
                await Processor.ProcessAsync(e);

                if (!e.IsHandled && next != null)
                {
                    await next.ProcessAsync(e);
                }
            }
        }

        public static EventHandler<EventArgsAsyncWrapper<T>> Create<T>(AsyncEventHandler<T> handler) where T : EventArgs => (sender, e) => e.ExecuteInvoke(sender, handler);

        //public static ChainLink<EventArgsAsyncWrapper<T>> Create<T>(AsyncChainEventHandler<T> handler) where T : ChainEventArgs => new ChainLink<EventArgsAsyncWrapper<T>>(new ProcessorFunc<EventArgsAsyncWrapper<T>>((e, next) => e.ExecuteInvoke(null, (sender, e) => handler(e.Args, e => next?.InvokeAsync(e)))));
        public static ChainLink<EventArgsAsyncWrapper<T>> SetNext<T>(this ChainLink<EventArgsAsyncWrapper<T>> link, IAsyncCoRProcessor<T> handler) where T : EventArgs => link.SetNext(Create(handler));
        public static ChainLink<EventArgsAsyncWrapper<T>> Create<T>(IAsyncCoRProcessor<T> handler) where T : EventArgs => new ChainLink<EventArgsAsyncWrapper<T>>(new AsyncCoRProcessor<T>(handler));

        private class AsyncCoRProcessor<T> : ICoRProcessor<EventArgsAsyncWrapper<T>> where T : EventArgs
        {
            public IAsyncCoRProcessor<T> Processor { get; }

            public AsyncCoRProcessor(IAsyncCoRProcessor<T> processor)
            {
                Processor = processor;
            }

            public void Process(EventArgsAsyncWrapper<T> e, IEventProcessor<EventArgsAsyncWrapper<T>> next) => e.ExecuteInvoke(null, (sender, e) => Processor.ProcessAsync(e.Args, next == null ? null : new Wrapper(next)));

            private class Wrapper : IAsyncEventProcessor<T>
            {
                public IEventProcessor<EventArgsAsyncWrapper<T>> Processor { get; }

                public Wrapper(IEventProcessor<EventArgsAsyncWrapper<T>> processor)
                {
                    Processor = processor;
                }

                public Task ProcessAsync(T e) => Processor.ProcessAsync(e);
            }
        }
    }

    public sealed class ChainLink<T> : IEventProcessor<T> where T : EventArgs
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

        public void Process(T e)
        {
            if (Processor == null)
            {
                return;
            }

            Processor.Process(e, Next);
        }
    }
}