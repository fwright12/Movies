namespace Movies
{
    public abstract class DatastoreWriteArgs
    {
        public object RawValue { get; }

        protected DatastoreWriteArgs(object value)
        {
            RawValue = value;
        }
    }
}
