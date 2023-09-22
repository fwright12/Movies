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

    public static class ChainExtensions
    {
        public static void Invoke<T>(this EventHandler<EventArgsAsyncWrapper<T>> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static Task InvokeAsync<T>(this AsyncEventHandler<T> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static Task InvokeAsync<T>(this ChainLinkEventHandler<T> handler, T e) where T : AsyncChainEventArgs
        {
            handler(e);
            return e.RequestedSuspension;
        }

        public static Task InvokeAsync<T>(this ChainLinkEventHandler<EventArgsAsyncWrapper<T>> handler, T e) where T : EventArgs
        {
            var wrapper = new EventArgsAsyncWrapper<T>(e);
            handler(wrapper);
            return wrapper.Task;
        }

        //public static ChainLink<T> SetNext<T>(this ChainLink<T> link, AsyncChainEventHandler<T> handler) where T : AsyncChainEventArgs, IAsyncEventArgsRequest => link.SetNext((e, next) => e.RequestSuspension(handler.Invoke(e, next)));

        public static ChainLink<T> SetNext<T>(this ChainLink<T> link, ChainLinkEventHandler<T> handler) where T : ChainEventArgs => link.SetNext(Create(handler));
        public static ChainLink<T> Create<T>(ChainLinkEventHandler<T> handler) where T : ChainEventArgs => new ChainLink<T>((e, next) =>
        {
            handler(e);

            if (!e.Handled && next != null)
            {
                next(e);
            }
        });

        public static ChainLink<T> SetNext<T>(this ChainLink<T> link, AsyncChainLinkEventHandler<T> handler) where T : AsyncChainEventArgs => link.SetNext(Create(handler));
        public static ChainLink<T> Create<T>(AsyncChainLinkEventHandler<T> handler) where T : AsyncChainEventArgs => new ChainLinkAsync<T>(async (e, next) =>
        {
            await handler(e);

            if (!e.Handled && next != null)
            {
                await next.InvokeAsync(e);
            }
        });

        public static EventHandler<EventArgsAsyncWrapper<T>> Create<T>(AsyncEventHandler<T> handler) where T : EventArgs => (sender, e) => e.ExecuteInvoke(sender, handler);

        public static ChainLink<EventArgsAsyncWrapper<T>> Create<T>(AsyncChainEventHandler<T> handler) where T : ChainEventArgs => new ChainLink<EventArgsAsyncWrapper<T>>((e, next) => e.ExecuteInvoke(null, (sender, e) => handler(e.Args, e => next?.InvokeAsync(e))));
    }

    public class ChainLink<T>
    {
        public ChainLink<T> Next { get; set; }
        public ChainEventHandler<T> Handler { get; }

        public ChainLink(ChainEventHandler<T> handler, ChainLink<T> next = null)
        {
            Next = next;
            Handler = handler;
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
        public ChainLink<T> SetNext(ChainEventHandler<T> handler) => SetNext(new ChainLink<T>(handler));

        public void Handle(T e)
        {
            if (Handler == null)
            {
                return;
            }

            Handler.Invoke(e, Next == null ? (ChainLinkEventHandler<T>)null : Next.Handle);
        }
    }

    public class ChainLinkAsync<T> : ChainLink<T> where T : AsyncChainEventArgs
    {
        public ChainLinkAsync(AsyncChainEventHandler<T> handler, ChainLink<T> next = null) : base((e, next) => e.RequestSuspension(handler.Invoke(e, next)), next) { }
    }
}