using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class BufferedCache<TArgs> : IEventAsyncCache<TArgs>, IAsyncEventProcessor<IEnumerable<TArgs>>
    {
        public IEventAsyncCache<TArgs> Cache { get; }

        protected BufferedCache(IEventAsyncCache<TArgs> cache)
        {
            Cache = cache;
        }

        public abstract Task Process(TArgs e, Task<TArgs> buffered);
        public virtual object GetKey(TArgs e) => e;

        public Task<bool> Read(IEnumerable<TArgs> args)
        {
            return Cache.Read(args);
        }

        public Task<bool> Write(IEnumerable<TArgs> args)
        {
            return Cache.Write(args);
        }

        public Task<bool> ProcessAsync(IEnumerable<TArgs> e) => Read(e);
    }
}