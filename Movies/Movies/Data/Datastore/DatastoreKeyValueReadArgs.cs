using System;

namespace Movies
{
    public class DatastoreKeyValueReadArgs<TKey> : DatastoreReadArgs
    {
        public TKey Key { get; }
        public Type Expected { get; set; }

        public DatastoreKeyValueReadArgs(TKey key)
        {
            Key = key;
        }

        protected override bool Accept(DatastoreResponse response)
        {
            if (Expected == null)
            {
                return base.Accept(response);
            }
            else if (response.RawValue == null)
            {
                return !Expected.IsValueType;
            }
            else
            {
                return Expected.IsAssignableFrom(response.RawValue.GetType());
            }
        }

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is DatastoreKeyValueReadArgs<TKey> other && Equals(Key, other.Key) && Equals(Expected, other.Expected);
    }

    public class DatastoreKeyValueReadArgs<TKey, TValue> : DatastoreKeyValueReadArgs<TKey>
    {
        public virtual TValue Value => Response?.RawValue is TValue value ? value : default;

        public DatastoreKeyValueReadArgs(TKey key) : base(key)
        {
            Expected = typeof(TValue);
        }

        public bool Handle(TValue value) => Handle(new DatastoreResponse<TValue>(value));

        protected override bool Accept(DatastoreResponse response) => response.RawValue is TValue || (response.RawValue == null && default(TValue) == null);
    }
}
