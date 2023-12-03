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

        public virtual async Task<bool> ProcessAsync(IEnumerable<TSingle> e) => (await Task.WhenAll(GroupRequests(e).Select(ProcessAsync))).All(value => value);

        private async Task<bool> ProcessAsync(KeyValuePair<TGrouped, IEnumerable<TSingle>> kvp) => await Processor.ProcessAsync(kvp.Key) && Handle(kvp.Key, kvp.Value);

        protected abstract IEnumerable<KeyValuePair<TGrouped, IEnumerable<TSingle>>> GroupRequests(IEnumerable<TSingle> args);

        protected abstract bool Handle(TGrouped grouped, IEnumerable<TSingle> singles);
    }
}