using System;

namespace Movies
{
    public class KeyValueReadArgs<TKey> : EventArgsRequest
    {
        public TKey Key { get; }
        public object Value { get; private set; }
        public Type Expected { get; set; }

        public KeyValueReadArgs(TKey key, Type expected = null)
        {
            Key = key;
            Expected = expected;
        }

        public bool Handle(object value)
        {
            if (Accept(value))
            {
                Value = value;
                Handle();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual bool Accept(object value)
        {
            if (Expected == null)
            {
                return true;
            }
            else if (value == null)
            {
                return !Expected.IsByRef;
            }
            else
            {
                return Expected.IsAssignableFrom(value.GetType());
            }
        }

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is KeyValueReadArgs<TKey> other && Equals(Key, other.Key);

        public override string ToString() => Key?.ToString();
    }

    public class KeyValueReadArgs<TKey, TValue> : KeyValueReadArgs<TKey>
    {
        new public TValue Value => IsHandled ? (TValue)base.Value : default;

        public KeyValueReadArgs(TKey key) : base(key, typeof(TValue)) { }
    }
}
