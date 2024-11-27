using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class AsyncEventProcessorCache<TReadArgs, TWriteArgs> : IEventAsyncCache<TReadArgs> where TReadArgs : EventArgsRequest
    {
        public IAsyncEventProcessor<IEnumerable<TReadArgs>> ReadProcessor { get; }
        public IAsyncEventProcessor<IEnumerable<TWriteArgs>> WriteProcessor { get; }

        protected AsyncEventProcessorCache(IAsyncEventProcessor<IEnumerable<TReadArgs>> readProcessor, IAsyncEventProcessor<IEnumerable<TWriteArgs>> writeProcessor)
        {
            ReadProcessor = readProcessor;
            WriteProcessor = writeProcessor;
        }

        public Task<bool> Read(IEnumerable<TReadArgs> args) => ReadProcessor.ProcessAsync(args);

        public Task<bool> Write(IEnumerable<TReadArgs> args) => WriteProcessor.ProcessAsync(args.Where(arg => arg.IsHandled).Select(Convert));

        protected abstract TWriteArgs Convert(TReadArgs args);
    }
}