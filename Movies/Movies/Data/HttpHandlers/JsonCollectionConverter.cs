using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Movies
{
    public class JsonCollectionConverter<TDom> : JsonConverter<IReadOnlyDictionary<Uri, object>>
    {
        public JsonDomType<TDom> DomType { get; }

        public Dictionary<Uri, IJsonDomExtractor<TDom, object>> Parsers { get; }

        public JsonCollectionConverter(JsonDomType<TDom> domType)
        {
            DomType = domType;
        }

        public virtual IReadOnlyDictionary<Uri, object> Read(ReadOnlySpan<byte> json, Type typeToConvert, JsonSerializerOptions options) => Convert(DomType.Parse(json));

        public override IReadOnlyDictionary<Uri, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Convert(DomType.Parse(ref reader));

        protected virtual IReadOnlyDictionary<Uri, object> Convert(TDom dom) => Parsers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Extract(dom));

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<Uri, object> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}