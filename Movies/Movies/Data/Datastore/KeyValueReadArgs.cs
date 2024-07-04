using System;

namespace Movies
{
    public abstract class KeyValueReadEventArgs : EventArgs
    {
        public Type Expected { get; }

        protected KeyValueReadEventArgs(Type expected)
        {
            Expected = expected;
        }
    }

    public class KeyValueReadEventArgs<TKey> : KeyValueReadEventArgs
    {
        public TKey Key { get; }

        public KeyValueReadEventArgs(TKey key, Type expected = null) : base(expected)
        {
            Key = key;
        }

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is KeyValueReadEventArgs<TKey> other && Equals(Key, other.Key);

        public override string ToString() => Key?.ToString();
    }

    public class KeyValueRequestArgs<TKey> : EventArgsRequest<KeyValueReadEventArgs<TKey>, KeyValueResponse>
    {
        public object Value => Response?.Value;

        public KeyValueRequestArgs(KeyValueReadEventArgs<TKey> request) : base(request) { }
        public KeyValueRequestArgs(TKey key, Type expected = null) : this(new KeyValueReadEventArgs<TKey>(key, expected)) { }

        protected override bool Accept(KeyValueResponse response)
        {
            if (response is ResourceResponse resourceResponse && resourceResponse.Count == 0)
            {
                return false;
            }

            if (Request.Expected == null)
            {
                return true;
            }
            else if (response.Value == null)
            {
                return !Request.Expected.IsByRef;
            }
            else
            {
                return Request.Expected.IsAssignableFrom(response.Value.GetType());
            }
        }
    }

    public class KeyValueRequestArgs<TKey, TValue> : KeyValueRequestArgs<TKey>
    {
        public new TValue Value => base.Value == null ? default : (TValue)base.Value;

        public KeyValueRequestArgs(TKey key) : base(key, typeof(TValue)) { }
    }

    public class KeyValueResponse
    {
        public object Value { get; }

        public KeyValueResponse(object value)
        {
            Value = value;
        }
    }

    public class KeyValueResponseArgs<T> : KeyValueResponse
    {
        public new T Value => base.Value is T t ? t : default;
        public KeyValueResponseArgs(object value) : base(value) { }
    }
}
