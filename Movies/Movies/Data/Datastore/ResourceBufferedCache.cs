using System.Threading.Tasks;

namespace Movies
{
    public class ResourceBufferedCache<TKey> : BufferedCache<ResourceReadArgs<TKey>>
    {
        public ResourceBufferedCache(IEventAsyncCache<ResourceReadArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(ResourceReadArgs<TKey> e) => e.Key;

        public override async Task Process(ResourceReadArgs<TKey> e, Task<ResourceReadArgs<TKey>> buffered)
        {
            var args = await buffered;
            e.Handle(args.Response);
        }
    }
}