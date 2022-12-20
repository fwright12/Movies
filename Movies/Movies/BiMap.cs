using System.Collections.Generic;

namespace Movies
{
    public class BiMap<TKey, TValue> //: Dictionary<TKey, TValue>, IDictionary<TValue, TKey>
    {
        public Dictionary<TKey, TValue> Keys { get; } = new Dictionary<TKey, TValue>();
        public Dictionary<TValue, TKey> Values { get; } = new Dictionary<TValue, TKey>();

        public TValue this[TKey key]
        {
            get => Keys[key];
            set
            {
                Keys[key] = value;
                Values[value] = key;
            }
        }

        public TKey this[TValue val]
        {
            get => Values[val];
            set
            {
                Keys[value] = val;
                Values[val] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value) => Keys.TryGetValue(key, out value);
        public bool TryGetValue(TValue value, out TKey key) => Values.TryGetValue(value, out key);
    }
}
