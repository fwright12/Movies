namespace Movies
{
    public abstract class DatastoreReadArgs : EventArgsRequest
    {
        public DatastoreResponse Response { get; private set; }

        public bool Handle(DatastoreResponse response)
        {
            if (Accept(response))
            {
                Response = response;
                Handle();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual bool Accept(DatastoreResponse response) => true;
    }

    public abstract class DatastoreResponse
    {
        public abstract object RawValue { get; }
    }

    public class DatastoreResponse<T> : DatastoreResponse
    {
        public T Value { get; }
        public override object RawValue => Value;

        public DatastoreResponse(T value)
        {
            Value = value;
        }
    }
}
