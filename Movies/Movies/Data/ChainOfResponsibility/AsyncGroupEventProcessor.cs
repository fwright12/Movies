using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class AsyncGroupEventProcessor<TSingle, TGrouped> : IAsyncEventProcessor<IEnumerable<TSingle>>
    {
        public IAsyncEventProcessor<TGrouped> Processor { get; }
        public bool Flag { get; set; } = true;

        protected AsyncGroupEventProcessor(IAsyncEventProcessor<TGrouped> processor)
        {
            Processor = processor;
        }

        public virtual async Task<bool> ProcessAsync(IEnumerable<TSingle> e)
        {
            var grouped = GroupRequests(e).ToArray();

            if (Flag)
            {
                (e as BulkEventArgs<TSingle>)?.Add(grouped.SelectMany(kvp => kvp.Value));
            }

            var result = (await Task.WhenAll(grouped.Select(ProcessAsync))).All(value => value);

            if (!Flag)
            {
                (e as BulkEventArgs<TSingle>)?.Add(grouped.Where(kvp => false == kvp.Key is EventArgsRequest request || request.IsHandled).SelectMany(kvp => kvp.Value));
            }

            return result;
        }

        private async Task<bool> ProcessAsync(KeyValuePair<TGrouped, IEnumerable<TSingle>> kvp) => await Processor.ProcessAsync(kvp.Key) && Handle(kvp.Key, kvp.Value);

        protected abstract IEnumerable<KeyValuePair<TGrouped, IEnumerable<TSingle>>> GroupRequests(IEnumerable<TSingle> args);

        protected abstract bool Handle(TGrouped grouped, IEnumerable<TSingle> singles);
    }
}