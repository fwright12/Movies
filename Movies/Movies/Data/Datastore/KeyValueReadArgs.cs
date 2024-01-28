using System;

namespace Movies
{
    public class KeyValueReadEventArgs<TKey> : EventArgs
    {
        public TKey Key { get; }
        public Type Expected { get; }

        public KeyValueReadEventArgs(TKey key, Type expected = null)
        {
            Key = key;
            Expected = expected;
        }

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is KeyValueReadEventArgs<TKey> other && Equals(Key, other.Key);

        public override string ToString() => Key?.ToString();
    }

    public class KeyValueRequestArgs<TKey> : KeyValueRequestArgs<KeyValueReadEventArgs<TKey>, TKey, KeyValueResponse>
    {
        public KeyValueRequestArgs(KeyValueReadEventArgs<TKey> request) : base(request) { }
        public KeyValueRequestArgs(TKey key, Type expected = null) : this(new KeyValueReadEventArgs<TKey>(key, expected)) { }
    }

    public class KeyValueRequestArgs<TRequest, TKey, TResponse> : EventArgsRequest<TRequest, TResponse>
        where TRequest : KeyValueReadEventArgs<TKey>
        where TResponse : KeyValueResponse
    {
        public object Value => Response?.Value;

        public KeyValueRequestArgs(TRequest request) : base(request) { }

        protected override bool Accept(TResponse response)
        {
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

    public class KeyValueRequestArgs<TKey, TValue> : KeyValueRequestArgs<KeyValueReadEventArgs<TKey>, TKey, KeyValueResponseArgs<TValue>>
    {
        public KeyValueRequestArgs(KeyValueReadEventArgs<TKey> request) : base(request) { }
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
