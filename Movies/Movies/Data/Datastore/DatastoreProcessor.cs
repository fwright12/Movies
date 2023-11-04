using REpresentationalStateTransfer;
using System;
using System.Threading.Tasks;

namespace Movies
{
    public class DatastoreProcessor<TKey, TValue> : IAsyncEventProcessor<DatastoreKeyValueReadArgs<TKey>>, IAsyncEventProcessor<DatastoreWriteArgs>
    {
        public IDataStore<TKey, State> Datastore { get; }

        public DatastoreProcessor(IDataStore<TKey, State> datastore)
        {
            Datastore = datastore;
        }

        public virtual async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<TKey> e)
        {
            var state = Datastore.ReadAsync(e.Key);

            if (state != null)
            {
                return e.Handle(new RestResponse(await state)
                {
                    Expected = e.Expected
                });
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
                return Datastore.CreateAsync(keyValueArgs.Key, keyValueArgs.Value);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}