using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class AsyncGroupEventProcessor<TSingle, TGrouped> : IAsyncEventProcessor<IEnumerable<TSingle>>
    {
        public IAsyncEventProcessor<TGrouped> Processor { get; }

        protected AsyncGroupEventProcessor(IAsyncEventProcessor<TGrouped> processor)
        {
            Processor = processor;
        }

        public virtual async Task<bool> ProcessAsync(IEnumerable<TSingle> e)
        {
            var batch = e as BulkEventArgs<TSingle>;
            var results = new List<Task<bool>>();

            foreach (var kvp in GroupRequests(e))
            {
                results.Add(ProcessAsync(kvp));
                batch?.Add(kvp.Value);
            }

            return (await Task.WhenAll(results)).All(value => value);
        }

        private async Task<bool> ProcessAsync(KeyValuePair<TGrouped, IEnumerable<TSingle>> kvp) => await Processor.ProcessAsync(kvp.Key) && Handle(kvp.Key, kvp.Value);

        protected abstract IEnumerable<KeyValuePair<TGrouped, IEnumerable<TSingle>>> GroupRequests(IEnumerable<TSingle> args);

        protected abstract bool Handle(TGrouped grouped, IEnumerable<TSingle> singles);
    }
}