using System;

namespace Movies
{
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
}