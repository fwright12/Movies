using System.Threading.Tasks;

namespace Movies
{
    public class KeyValueBufferedCache<TKey> : BufferedCache<KeyValueRequestArgs<TKey>>
    {
        public KeyValueBufferedCache(IEventAsyncCache<KeyValueRequestArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(KeyValueRequestArgs<TKey> e) => e.Request.Key;

        public override void Process(KeyValueRequestArgs<TKey> e, KeyValueRequestArgs<TKey> buffered) => e.Handle(new KeyValueResponse(buffered.Value));
    }
}