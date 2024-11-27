using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO;

namespace Movies
{
    public abstract class JsonDomType<T>
    {
        public abstract T Parse(ReadOnlySpan<byte> json);
        public abstract T Parse(Stream json);
        public abstract T Parse(string json);
        public abstract T Parse(ref Utf8JsonReader reader);
        public abstract bool TryGetValue(T dom, string propertyName, out T value);
    }

    public static class JsonDom
    {
        public static readonly JsonDomType<JsonIndex> JsonIndex = new JsonIndexDom();

        private class JsonIndexDom : JsonDomType<JsonIndex>
        {
            public override JsonIndex Parse(ReadOnlySpan<byte> json) => new JsonIndex(json.ToArray());

            public override JsonIndex Parse(Stream json)
            {
                throw new NotImplementedException();
            }

            public override JsonIndex Parse(string json) => new JsonIndex(Encoding.UTF8.GetBytes(json));

            public override JsonIndex Parse(ref Utf8JsonReader reader)
            {
                reader.Read();
                reader.Skip();
                return new JsonIndex(reader.ValueSpan.ToArray());
            }

            public override bool TryGetValue(JsonIndex dom, string propertyName, out JsonIndex value) => dom.TryGetValue(propertyName, out value);
        }
    }

    public interface IJsonDomExtractor<TDom, out TValue>
    {
        TValue Extract(TDom dom);
    }

    public class JsonDomExtractorFunc<TDom, TValue> : IJsonDomExtractor<TDom, TValue>
    {
        public Func<TDom, TValue> Func { get; }

        public JsonDomExtractorFunc(Func<TDom, TValue> func)
        {
            Func = func;
        }

        public TValue Extract(TDom dom) => Func.Invoke(dom);
    }

    public class JsonDomConverterExtractor<T> : IJsonDomExtractor<JsonIndex, T>
    {
        public JsonConverter<T> Converter { get; }
        public JsonSerializerOptions Options { get; }

        public JsonDomConverterExtractor(JsonConverter<T> converter, JsonSerializerOptions options = null)
        {
            Converter = converter;
            Options = options;
        }

        public T Extract(JsonIndex dom)
        {
            var reader = new Utf8JsonReader(dom.Bytes);
            return Converter.Read(ref reader, typeof(T), Options);
        }
    }

    public class JsonDomPropertyExtractor<TDom, TValue> : IJsonDomExtractor<TDom, TValue>
    {
        public JsonDomType<TDom> DomType { get; }
        public IJsonDomExtractor<TDom, TValue> Inner { get; }
        public string PropertyName { get; }

        public TValue Extract(TDom dom)
        {
            if (DomType.TryGetValue(dom, PropertyName, out var inner))
            {
                return Inner.Extract(inner);
            }
            else
            {
                return default;
            }
        }
    }
}
