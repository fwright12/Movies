using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public class Trie<TKey, TValue> : IEnumerable<KeyValuePair<IEnumerable<TKey>, TValue>>
    {
        public TValue Value { get; private set; }
        private Dictionary<TKey, Trie<TKey, TValue>> Children { get; } = new Dictionary<TKey, Trie<TKey, TValue>>();

        public void Add(TValue value, params TKey[] keys)
        {
            var root = this;

            foreach (var key in keys)
            {
                if (!root.Children.TryGetValue(key, out var trie))
                {
                    root.Children.Add(key, trie = new Trie<TKey, TValue>());
                }

                root = trie;
            }

            root.Value = value;
        }

        public IEnumerator<KeyValuePair<IEnumerable<TKey>, TValue>> GetEnumerator()
        {
            if (Children.Count == 0)
            {
                yield return new KeyValuePair<IEnumerable<TKey>, TValue>(Enumerable.Empty<TKey>(), Value);
            }
            else
            {
                foreach (var kvp in Children)
                {
                    foreach (var value in kvp.Value)
                    {
                        yield return new KeyValuePair<IEnumerable<TKey>, TValue>(value.Key.Prepend(kvp.Key), value.Value);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class Enumerator : IEnumerator<KeyValuePair<IEnumerable<TKey>, TValue>>
        {
            public KeyValuePair<IEnumerable<TKey>, TValue> Current { get; private set; }

            object IEnumerator.Current => Current;

            private readonly Stack<Trie<TKey, TValue>> Tries = new Stack<Trie<TKey, TValue>>();

            public Enumerator(Trie<TKey, TValue> root)
            {
                Tries.Push(root);
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                while (Tries.Peek().Children.Count > 0)
                {
                    //Tries.Push(Tries.Peek().Children[0].Value);
                }

                return false;
            }

            public void Reset()
            {
                var root = Tries.First();
                Tries.Clear();
                Tries.Push(root);
            }
        }
    }
}
