namespace Movies
{
    public abstract class ResourceResponse
    {
        public abstract object RawValue { get; }

        public virtual bool TryGetRepresentatin<T>(out T value)
        {
            if (RawValue is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }

    public class ResourceResponse<T> : ResourceResponse
    {
        public T Value { get; }
        public override object RawValue => Value;

        public ResourceResponse(T value)
        {
            Value = value;
        }
    }
}
