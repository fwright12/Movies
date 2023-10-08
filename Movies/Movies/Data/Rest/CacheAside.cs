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
}