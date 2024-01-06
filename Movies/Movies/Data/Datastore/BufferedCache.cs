using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class BufferedCache<TArgs> : IEventAsyncCache<TArgs>, IAsyncCoRProcessor<IEnumerable<TArgs>>
    {
        public IEventAsyncCache<TArgs> Cache { get; }

        protected BufferedCache(IEventAsyncCache<TArgs> cache)
        {
            Cache = cache;
        }

        public abstract void Process(TArgs e, TArgs buffered);
        public virtual object GetKey(TArgs e) => e;

        public Task<bool> Read(IEnumerable<TArgs> args)
        {
            return Cache.Read(args);
        }

        public Task<bool> Write(IEnumerable<TArgs> args)
        {
            return Cache.Write(args);
        }

        public virtual async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
        {
            if (await Read(e))
            {
                return true;
            }
            else if (next == null)
            {
                return false;
            }
            else
            {
                return await next.ProcessAsync(e);
            }
        }
    }
}