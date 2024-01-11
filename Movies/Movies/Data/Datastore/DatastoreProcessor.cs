using REpresentationalStateTransfer;
using System;
using System.Threading.Tasks;

namespace Movies
{
    public class DatastoreProcessor<TKey, TValue> : IAsyncEventProcessor<ResourceReadArgs<TKey>>, IAsyncEventProcessor<DatastoreWriteArgs>, IAsyncEventProcessor<DatastoreKeyValueWriteArgs<TKey, State>>
    {
        public IDataStore<TKey, State> Datastore { get; }

        public DatastoreProcessor(IDataStore<TKey, State> datastore)
        {
            Datastore = datastore;
        }

        public virtual async Task<bool> ProcessAsync(ResourceReadArgs<TKey> e)
        {
            var state = Datastore.ReadAsync(e.Key);

            if (state != null)
            {
                return e.Handle(new RestResponse(await state));
            }
            else
            {
                return false;
            }
        }

        public virtual Task<bool> ProcessAsync(DatastoreWriteArgs e)
        {
            if (e is DatastoreKeyValueWriteArgs<TKey, State> keyValueArgs)
            {
                return ProcessAsync(keyValueArgs);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> ProcessAsync(DatastoreKeyValueWriteArgs<TKey, State> e) => Datastore.CreateAsync(e.Key, e.Value);
    }
}