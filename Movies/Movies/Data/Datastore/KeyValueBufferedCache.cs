using System.Threading.Tasks;

namespace Movies
{
    public class KeyValueBufferedCache<TKey> : BufferedCache<KeyValueReadArgs<TKey>>
    {
        public KeyValueBufferedCache(IEventAsyncCache<KeyValueReadArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(KeyValueReadArgs<TKey> e) => e.Key;

        public override void Process(KeyValueReadArgs<TKey> e, KeyValueReadArgs<TKey> buffered) => e.Handle(buffered.Value);
    }
}