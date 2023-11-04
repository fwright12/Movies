using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class AsyncBulkEventProcessor<T> : AsyncBulkEventProcessor<T, T> { public AsyncBulkEventProcessor(IAsyncEventProcessor<T> processor) : base(processor) { } }

    public class AsyncBulkEventProcessor<TBase, TDerived> : IAsyncEventProcessor<IEnumerable<TBase>>
    {
        public IAsyncEventProcessor<TDerived> Processor { get; }

        public AsyncBulkEventProcessor(IAsyncEventProcessor<TDerived> processor)
        {
            Processor = processor;
        }

        public async Task<bool> ProcessAsync(IEnumerable<TBase> e)
        {
            var result = true;
            var tasks = new List<Task<bool>>();

            foreach (var request in e)
            {
                if (false == request is TDerived derived)
                {
                    result = false;
                }
                else if (false == derived is EventArgsRequest ear || !ear.IsHandled)
                {
                    tasks.Add(Processor.ProcessAsync(derived));
                }
            }

            var processed = await Task.WhenAll(tasks);
            return result && processed.All(value => value);
        }
    }
}