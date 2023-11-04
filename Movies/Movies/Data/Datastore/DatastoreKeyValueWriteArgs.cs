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
}
