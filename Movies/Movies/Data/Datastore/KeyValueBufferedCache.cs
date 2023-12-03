using System.Threading.Tasks;

namespace Movies
{
    public class KeyValueBufferedCache<TKey> : BufferedCache<KeyValueReadArgs<TKey>>
    {
        public KeyValueBufferedCache(IEventAsyncCache<KeyValueReadArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(KeyValueReadArgs<TKey> e) => e.Key;

        public override async Task Process(KeyValueReadArgs<TKey> e, Task<KeyValueReadArgs<TKey>> buffered) => e.Handle((await buffered).Value);
    }
}