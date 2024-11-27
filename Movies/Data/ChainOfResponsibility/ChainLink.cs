using System.Linq;

namespace Movies
{
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