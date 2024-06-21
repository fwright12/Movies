using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public sealed class AsyncChainLink<T> : IAsyncEventProcessor<T>
    {
        public AsyncChainLink<T> Next { get; set; }
        public IAsyncCoRProcessor<T> Processor { get; }

        public AsyncChainLink(IAsyncCoRProcessor<T> processor, AsyncChainLink<T> next = null)
        {
            Next = next;
            Processor = processor;
        }

        //public static implicit operator ChainLink<T>(ILinkHandler<T> handler) => new ChainLink<T>(handler);

        public static AsyncChainLink<T> Build(params AsyncChainLink<T>[] links)
        {
            for (int i = 0; i < links.Length - 1; i++)
            {
                links[i].Next = links[i + 1];
            }

            return links.FirstOrDefault();
        }

        public AsyncChainLink<T> SetNext(AsyncChainLink<T> link) => Next = link;
        public AsyncChainLink<T> SetNext(IAsyncCoRProcessor<T> handler) => SetNext(new AsyncChainLink<T>(handler));

        public async Task<bool> ProcessAsync(T e)
        {
            if (Processor == null)
            {
                return false;
            }

            return await Processor.ProcessAsync(e, Next);
        }
    }
}