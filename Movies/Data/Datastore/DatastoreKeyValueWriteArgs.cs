namespace Movies
{
    public class DatastoreKeyValueWriteArgs<TKey, TValue> : DatastoreWriteArgs
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public DatastoreKeyValueWriteArgs(TKey key, TValue value) : base(value)
        {
            Key = key;
            Value = value;
        }
    }

    public class ResourceWriteArgs<TKey> : DatastoreKeyValueWriteArgs<TKey, object>
    {
        public ResourceResponse Response { get; }

        public ResourceWriteArgs(TKey key, object value) : this(key, new ResourceResponse<object>(value)) { }

        public ResourceWriteArgs(TKey key, ResourceResponse response) : base(key, response.TryGetRepresentation<object>(out var value) ? value : null)
        {
            Response = response;
        }
    }
}
