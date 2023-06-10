using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public delegate void ChainLinkEventHandler<in T>(T e) where T : ChainEventArgs;
    public delegate void ChainEventHandler<T>(T e, ChainLinkEventHandler<T> next) where T : ChainEventArgs;
    public delegate Task AsyncChainEventHandler<T>(T e, ChainLinkEventHandler<T> next) where T : ChainEventArgs;

    public class ChainLink<T> where T : ChainEventArgs
    {
        public ChainLink<T> Next { get; set; }
        public ChainEventHandler<T> Handler { get; }

        public ChainLink(ChainEventHandler<T> handler, ChainLink<T> next = null)
        {
            Next = next;
            Handler = handler;
        }

        public ChainLink(ChainLinkEventHandler<T> handler, ChainLink<T> next = null) : this((e, next) =>
        {
            handler(e);

            if (!e.Handled)
            {
                next(e);
            }
        }, next)
        { }

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

        public ChainLinkAsync(AsyncChainLinkEventHandler<T> handler, ChainLink<T> next = null) : this(async (e, next) =>
        {
            await handler(e);

            if (!e.Handled && next != null)
            {
                await next.InvokeAsync(e);
            }
        }, next) { }
    }
}