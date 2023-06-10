using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Movies
{
    public class AnnotatedJson : IReadOnlyDictionary<Uri, ArraySegment<byte>>
    {
        public ArraySegment<byte> this[Uri key] => Paths.TryGetValue(key, out var paths) && TryGetJson(paths, out var value) ? value : throw new KeyNotFoundException();

        public IEnumerable<Uri> Keys => this.Select(kvp => kvp.Key);

        public IEnumerable<ArraySegment<byte>> Values => this.Select(kvp => kvp.Value);

        public int Count => this.Count();

        private Dictionary<Uri, JsonConverterWrapper<string>> Paths { get; } = new Dictionary<Uri, JsonConverterWrapper<string>>();
        private JsonIndex Index { get; }
        private byte[] Bytes { get; }
        private string StringValue => _StringValue ??= Encoding.UTF8.GetString(Bytes);
        private string _StringValue;

        public AnnotatedJson(string json)
        {
            Bytes = Encoding.UTF8.GetBytes(json);
            Index = new JsonIndex(Bytes);
        }

        public AnnotatedJson(byte[] bytes)
        {
            Bytes = bytes;
            Index = new JsonIndex(bytes);
        }

        //public bool Add(Uri uri, string value) => Cache.TryAdd(uri, value);
        //public void Add(Uri uri, string path) => Add(uri, path.Split("/").ToArray());
        /*public void Add(Uri uri, params string[] paths)
        {
            Paths.TryAdd(uri, new Location(paths.ToList<string>()));
            return;

            if (!Paths.TryGetValue(uri, out var value))
            {
                //Paths.Add(uri, value = new List<string[]>());
            }

            //value.AddRange(paths);
        }*/

        public void Add(Uri uri, JsonConverterWrapper<string> wrapper) => Paths.TryAdd(uri, wrapper);
        public void Add(Uri uri, params string[] path) => Paths.TryAdd(uri, new JsonConverterWrapper<string>(path, StringConverter.Instance));
        public void Add(Uri uri, string[] path, JsonConverter<string> converter) => Paths.TryAdd(uri, new JsonConverterWrapper<string>(path, converter));

        public bool ContainsKey(Uri key) => Paths.ContainsKey(key);

        public bool TryGetValue(Uri key, out ArraySegment<byte> value)
        {
            if (Paths.TryGetValue(key, out var wrapper))
            {
                return TryGetJson(wrapper, out value);
            }
            else
            {
                value = default;
                return false;
            }
        }

        private bool TryGetJson(JsonConverterWrapper<string> wrapper, out ArraySegment<byte> json)
        {
            //if (paths.Count == 0)
            //{
            //    json = Json.RootElement.GetRawText();
            //    return true;
            //}

            if (TryGetSubProperty(wrapper.Path, out json))
            {
                return true;

                if (wrapper.Converter == StringConverter.Instance)
                {
                    //return true;
                }

                //json = elem.ValueKind == JsonValueKind.String ? elem.GetString() : elem.GetRawText();
                var reader = new Utf8JsonReader(json);
                json = Encoding.UTF8.GetBytes(wrapper.Converter.Read(ref reader, typeof(string), null));
                return true;
            }

            json = default;
            return false;
        }

        private bool TryGetSubProperty(IEnumerable<string> path, out ArraySegment<byte> value)
        {
            var index = Index;

            foreach (var property in path)
            {
                if (!index.TryGetValue(property, out index))
                {
                    value = default;
                    return false;
                }
            }

            value = index.Bytes;
            return true;
        }

        public IEnumerator<KeyValuePair<Uri, ArraySegment<byte>>> GetEnumerator()
        {
            foreach (var kvp in Paths)
            {
                if (TryGetJson(kvp.Value, out var json))
                {
                    yield return new KeyValuePair<Uri, ArraySegment<byte>>(kvp.Key, json);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class StringConverter : JsonConverter<string>
    {
        public static readonly StringConverter Instance = new StringConverter();

        private StringConverter() { }

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var json = JsonDocument.ParseValue(ref reader))
            {
                var root = json.RootElement;
                return root.ValueKind == JsonValueKind.String ? root.GetString() : root.GetRawText();
            }

            var start = reader.BytesConsumed;
            if (reader.TokenType == JsonTokenType.String) return reader.GetString();
            reader.Skip();
            return Encoding.UTF8.GetString(reader.ValueSpan);
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class JsonConverterWrapper<T> : JsonConverter<T>
    {
        public string[] Path { get; }
        public JsonConverter<T> Converter { get; }

        public JsonConverterWrapper(string[] path) : this(path, null) { }

        public JsonConverterWrapper(JsonConverter<T> converter, params string[] path) : this(path, converter) { }

        public JsonConverterWrapper(string[] path, JsonConverter<T> converter)
        {
            Path = path;
            Converter = converter;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}