using System.Threading.Tasks;

namespace Movies
{
    public abstract class CacheAside<TArgs>
    {
        public abstract Task HandleGet(TArgs e);
        public abstract Task HandleSet(TArgs e);
    }

    public abstract class DatastoreCache<TKey, TValue, TArgs> : CacheAside<TArgs>
    {
        public IDataStore<TKey, TValue> Datastore { get; }

        public DatastoreCache(IDataStore<TKey, TValue> datastore)
        {
            Datastore = datastore;
        }
    }

    public class CacheAsideFunc<TArgs> : CacheAside<TArgs>
    {
        public AsyncChainLinkEventHandler<TArgs> Get { get; }
        public AsyncChainLinkEventHandler<TArgs> Set { get; }

        public CacheAsideFunc(AsyncChainLinkEventHandler<TArgs> get, AsyncChainLinkEventHandler<TArgs> set)
        {
            Get = get;
            Set = set;
        }

        public override Task HandleGet(TArgs e) => Get(e);

        public override Task HandleSet(TArgs e) => Set(e);
    }
}