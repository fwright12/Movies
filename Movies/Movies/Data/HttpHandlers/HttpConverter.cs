using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Movies
{
    public static class TaskExtensions
    {
        public static async Task<TOut> TransformAsync<TIn, TOut>(this Task<TIn> value, Func<TIn, TOut> converter) => converter(await value);
    }

    public static class DictionaryHelpers
    {
        public static Task<TValue> GetAsync<TKey, TValue>(this Task<IReadOnlyDictionary<TKey, TValue>> dict, TKey key) => dict.TransformAsync(dict => dict.TryGetValue(key, out var value) ? value : default);

        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            if (kvps is IReadOnlyDictionary<TKey, TValue> roDict)
            {
                return roDict;
            }
            else if (kvps is IDictionary<TKey, TValue> dict)
            {
                return new ReadOnlyDictionaryWrapper<TKey, TValue>(dict);
            }
            else
            {
                return new Dictionary<TKey, TValue>(kvps);
            }
        }

        public abstract class ReadOnlyConverterDictionary<TKey, TInValue, TOutValue> : IReadOnlyDictionary<TKey, TOutValue>
        {
            public TOutValue this[TKey key] => Convert(Inner[key]);
            public IEnumerable<TKey> Keys => Inner.Keys;
            public IEnumerable<TOutValue> Values => Inner.Values.Select(Convert);
            public int Count => Inner.Count;

            private IReadOnlyDictionary<TKey, TInValue> Inner { get; }

            public ReadOnlyConverterDictionary(IReadOnlyDictionary<TKey, TInValue> inner)
            {
                Inner = inner;
            }

            public bool ContainsKey(TKey key) => Inner.ContainsKey(key);

            public bool TryGetValue(TKey key, out TOutValue value)
            {
                if (Inner.TryGetValue(key, out var inValue))
                {
                    value = Convert(inValue);
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            public bool TryGetValue(TKey key, out TInValue value) => Inner.TryGetValue(key, out value);

            protected abstract TOutValue Convert(TInValue value);

            public IEnumerator<KeyValuePair<TKey, TOutValue>> GetEnumerator() => Inner.Select(kvp => new KeyValuePair<TKey, TOutValue>(kvp.Key, Convert(kvp.Value))).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static IReadOnlyDictionary<TKey, TOutValue> ConvertValues<TKey, TInValue, TOutValue>(this IReadOnlyDictionary<TKey, TInValue> dict, Func<TInValue, TOutValue> converter) => new ReadOnlyConverterDictionaryFunc<TKey, TInValue, TOutValue>(dict, converter);

        private class ReadOnlyConverterDictionaryFunc<TKey, TInValue, TOutValue> : ReadOnlyConverterDictionary<TKey, TInValue, TOutValue>
        {
            public Func<TInValue, TOutValue> Converter { get; }

            public ReadOnlyConverterDictionaryFunc(IReadOnlyDictionary<TKey, TInValue> inner, Func<TInValue, TOutValue> converter) : base(inner)
            {
                Converter = converter;
            }

            protected override TOutValue Convert(TInValue value) => Converter(value);
        }

        public class ReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            public IEnumerable<TKey> Keys => Dict.Keys;

            public IEnumerable<TValue> Values => Dict.Values;

            public int Count => Dict.Count;

            public TValue this[TKey key] => Dict[key];

            public IDictionary<TKey, TValue> Dict { get; }

            public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dict)
            {
                Dict = dict;
            }

            public bool ContainsKey(TKey key) => Dict.ContainsKey(key);

            public bool TryGetValue(TKey key, out TValue value) => Dict.TryGetValue(key, out value);

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dict.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    public interface IHttpConverter<T>
    {
        Task<T> Convert(HttpContent content);
    }

    public abstract class HttpResourceCollectionConverter : IHttpConverter<IEnumerable<KeyValuePair<Uri, object>>>, IHttpConverter<object>
    {
        public abstract IEnumerable<Uri> Resources { get; }

        public abstract Task<IEnumerable<KeyValuePair<Uri, object>>> Convert(HttpContent content);

        async Task<object> IHttpConverter<object>.Convert(HttpContent content) => await Convert(content);

        //public IEnumerable<KeyValuePair<Uri, Task<State>>> ConvertAhead(HttpContent content) => new LookaheadDictionary<Uri, State>(Resources, Convert(content).TransformAsync(DictionaryHelpers.ToReadOnlyDictionary));

        public IReadOnlyDictionary<Uri, Task<object>> ConvertAhead(Task<IEnumerable<KeyValuePair<Uri, object>>> values) => new LookaheadDictionary<Uri, object>(Resources, values.TransformAsync(DictionaryHelpers.ToReadOnlyDictionary));

        private class LookaheadDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, Task<TValue>>
        {
            public IEnumerable<TKey> Keys => KeySet;

            public IEnumerable<Task<TValue>> Values => throw new NotImplementedException();

            public int Count => KeySet.Count;

            public Task<TValue> this[TKey key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

            private ISet<TKey> KeySet { get; }
            private Task<IReadOnlyDictionary<TKey, TValue>> Inner { get; }

            public LookaheadDictionary(IEnumerable<TKey> keys, Task<IReadOnlyDictionary<TKey, TValue>> inner)
            {
                KeySet = keys as ISet<TKey> ?? new HashSet<TKey>(keys);
                Inner = inner;
            }

            public bool ContainsKey(TKey key) => KeySet.Contains(key);

            public bool TryGetValue(TKey key, out Task<TValue> value)
            {
                if (KeySet.Contains(key))
                {
                    value = Inner.GetAsync(key);
                    return true;
                }

                value = default;
                return false;
            }

            public IEnumerator<KeyValuePair<TKey, Task<TValue>>> GetEnumerator()
            {
                foreach (var key in KeySet)
                {
                    yield return new KeyValuePair<TKey, Task<TValue>>(key, Inner.GetAsync(key));
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    public class HttpJsonCollectionConverter<TDom> //: HttpResourceCollectionConverter
    {
        public JsonCollectionConverter<TDom> Converter { get; }
        public JsonSerializerOptions Options { get; }

        //public override IEnumerable<Uri> Resources => Converter.Parsers.Keys;

        public HttpJsonCollectionConverter(JsonCollectionConverter<TDom> converter, JsonSerializerOptions options = null)
        {
            Converter = converter;
            Options = options ?? new JsonSerializerOptions();
        }

        //public override async Task<IEnumerable<KeyValuePair<Uri, object>>> Convert(HttpContent content) => Converter.Read(await content.ReadAsByteArrayAsync(), typeof(IReadOnlyDictionary<Uri, object>), Options);
    }
}
