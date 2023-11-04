using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Movies
{
    public class JsonIndex : IReadOnlyDictionary<string, JsonIndex>
    {
        public JsonIndex this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

        public ArraySegment<byte> Bytes
        {
            get
            {
                if (!IsByteCountComputed)
                {
                    foreach (var kvp in Read()) { }
                }

                return _Bytes;
            }
        }

        private ArraySegment<byte> _Bytes;
        private bool IsByteCountComputed;

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, JsonIndex>)Index).Keys;

        public IEnumerable<JsonIndex> Values => ((IReadOnlyDictionary<string, JsonIndex>)Index).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<string, JsonIndex>>)Index).Count;

        private Dictionary<string, JsonIndex> Index { get; } = new Dictionary<string, JsonIndex>();
        private Enumerator Itr { get; }

        //public JsonIndex(string json) : this(JsonDocument.Parse(json).RootElement) { }

        public JsonIndex(byte[] json) : this(new ArraySegment<byte>(json))
        {
            IsByteCountComputed = true;
        }

        public JsonIndex(ArraySegment<byte> bytes)
        {
            _Bytes = bytes;
            Itr = new Enumerator(this);
        }

        private JsonIndex(ArraySegment<byte> bytes, JsonReaderState state) : this(bytes)
        {
            Itr = new Enumerator(this, state);
        }

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, JsonIndex>)Index).ContainsKey(key);
        }

        public bool TryGetValue(string key, out JsonIndex value)
        {
            if (Index.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var kvp in Read())
            {
                if (kvp.Key == key)
                {
                    value = kvp.Value;
                    return true;
                }
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<string, JsonIndex>> GetEnumerator() => Index.Concat(Read()).GetEnumerator();

        private IEnumerable<KeyValuePair<string, JsonIndex>> Read()
        {
            while (Itr.MoveNext())
            {
                //ICollection<KeyValuePair<string, JsonIndex>> cache = Index;
                //cache.Add(Itr.Current);

                yield return Itr.Current;
            }
        }

        private bool Read(ref Utf8JsonReader reader)
        {
            if (Itr.MoveNext(ref reader))
            {
                ICollection<KeyValuePair<string, JsonIndex>> cache = Index;
                cache.Add(Itr.Current);

                return true;
            }
            else
            {
                _Bytes = new ArraySegment<byte>(_Bytes.Array, _Bytes.Offset, (int)Itr.Consumed);
                IsByteCountComputed = true;

                return false;
            }
        }

        private class Enumerator : IEnumerator<KeyValuePair<string, JsonIndex>>
        {
            public KeyValuePair<string, JsonIndex> Current { get; private set; }
            object IEnumerator.Current => Current;

            public long Consumed { get; private set; }
            private JsonIndex Index { get; }
            private ArraySegment<byte> Bytes { get; }
            private JsonReaderState State;

            public Enumerator(JsonIndex index)
            {
                Index = index;
                Bytes = new ArraySegment<byte>(index._Bytes.Array).Slice(index._Bytes.Offset);

                Reset();
            }

            public Enumerator(JsonIndex index, JsonReaderState state) : this(index)
            {
                State = state;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var start = Consumed;
                if (Current.Value != null)
                {
                    State = Current.Value.Itr.State;
                    start += Current.Value.Itr.Consumed;
                }

                var reader = new Utf8JsonReader(Bytes.Slice((int)start), true, State);
                return Index.Read(ref reader);
                //return MoveNext(ref reader);
            }

            public bool MoveNext(ref Utf8JsonReader reader)
            {
                if (Index.IsByteCountComputed && Consumed >= Index.Bytes.Count)
                {
                    return false;
                }

                if (Current.Value != null)
                {
                    //long before = Current.Value.Itr.Consumed;
                    while (Current.Value.Read(ref reader)) { }
                    Consumed += Current.Value.Bytes.Count;
                    //State = Current.Value.Itr.State;
                }

                var start = reader.BytesConsumed;

                if (reader.Read())
                {
                    //if (reader.TokenType != JsonTokenType.PropertyName && reader.TokenType != JsonTokenType.StartObject)
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        reader.Skip();
                    }

                    Consumed += reader.BytesConsumed - start;
                    State = reader.CurrentState;

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        return MoveNext(ref reader);
                    }
                    else if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        var index = new JsonIndex(Bytes.Slice((int)Consumed), reader.CurrentState);

                        Current = new KeyValuePair<string, JsonIndex>(propertyName, index);
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                Current = default;
                State = default;
                Consumed = 0;
            }
        }
    }
}