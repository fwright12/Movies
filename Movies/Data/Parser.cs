using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public class JsonArrayParser<T> : IJsonParser<IEnumerable<T>>
    {
        private Func<JsonNode, IEnumerable<T>> Parse;
        private IJsonParser<T> Parser;

        public JsonArrayParser() : this(new JsonNodeParser<T>()) { }
        public JsonArrayParser(IJsonParser<T> parser)
        {
            Parser = parser;
        }
        public JsonArrayParser(Func<JsonNode, IEnumerable<T>> parse)
        {
            Parse = parse;
        }

        public bool TryGetValue(JsonNode json, out IEnumerable<T> value)
        {
            if (json is JsonArray array)
            {
                if (Parse != null)
                {
                    value = Parse(array);
                }
                else
                {
                    value = array.TrySelect<JsonNode, T>(Parser.TryGetValue);
                }

                return true;
            }

            value = null;
            return false;
        }
    }

    public static class JsonParser
    {
        public static bool TryParse<T>(string json, out T value) => TryParse(json, new JsonNodeParser<T>(), out value);
        public static bool TryParse<T>(string json, IJsonParser<T> parser, out T value) => parser.TryGetValue(JsonNode.Parse(json), out value);

        public static bool PeelProperty(string propertyName, ArraySegment<byte> bytes, out ArraySegment<byte> value)
        {
            var reader = new Utf8JsonReader(bytes);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var property = reader.GetString();
                    var offset = reader.BytesConsumed;

                    reader.Skip();

                    if (property == propertyName)
                    {
                        value = bytes.Slice((int)offset, (int)(reader.BytesConsumed - offset));
                        //Print.Log(System.Text.Encoding.UTF8.GetString(bytes));
                        //var json = JsonNode.Parse(bytes);
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        public static bool PeelProperties(ArraySegment<byte> bytes, out ArraySegment<byte> value, params string[] properties)
        {
            var reader = new Utf8JsonReader(bytes);
            int index = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    bool match = reader.ValueTextEquals(properties[index]);
                    var offset = reader.BytesConsumed;

                    if (!match || ++index == properties.Length)
                    {
                        reader.Skip();

                        if (match)
                        {
                            value = bytes.Slice((int)offset, (int)(reader.BytesConsumed - offset));
                            //Print.Log(System.Text.Encoding.UTF8.GetString(bytes));
                            //var json = JsonNode.Parse(bytes);
                            return true;
                        }
                    }
                }
            }

            value = default;
            return false;
        }
    }

    public interface IJsonParser<T>
    {
        bool TryGetValue(JsonNode json, out T value);
        bool TryGetValue(ArraySegment<byte> bytes, out T value) => TryGetValue(JsonNode.Parse(bytes), out value);
    }

    public class JsonNodeParser<T> : IJsonParser<T>
    {
        private AsyncEnumerable.TryParseFunc<JsonNode, T> Parse;
        private Func<ArraySegment<byte>, T> RawParse;

        public JsonNodeParser() { }
        public JsonNodeParser(AsyncEnumerable.TryParseFunc<JsonNode, T> parse)
        {
            Parse = parse;
        }
        public JsonNodeParser(Func<JsonNode, T> parse)
        {
            Parse = (JsonNode json, out T value) =>
            {
                value = parse(json);
                return true;
            };
        }
        public JsonNodeParser(Func<ArraySegment<byte>, T> parse)
        {
            RawParse = parse;
        }

        public bool TryGetValue(JsonNode json, out T value)
        {
            if (Parse != null)
            {
                return Parse(json, out value);
            }
            else
            {
                return json.TryGetValue(out value);
            }
        }

        public bool TryGetValue(ArraySegment<byte> bytes, out T value)
        {
            if (RawParse != null)
            {
                value = RawParse(bytes);
                return true;
            }
            else
            {
                return TryGetValue(JsonNode.Parse(bytes), out value);
            }
        }
    }

    public abstract class JsonPropertyParser : IJsonParser<JsonNode>
    {
        public string Property { get; }

        public JsonPropertyParser(string property)
        {
            Property = property;
        }

        public bool TryGetValue(JsonNode json, out JsonNode value) => json.TryGetValue(Property, out value);
        public bool TryGetValue(ArraySegment<byte> bytes, out ArraySegment<byte> value) => JsonParser.PeelProperty(Property, bytes, out value);
    }

    public class JsonPropertyParser<T> : JsonPropertyParser, IJsonParser<T>
    {
        public IJsonParser<T> Parser { get; }

        public JsonPropertyParser(string property) : this(property, new JsonNodeParser<T>()) { }

        public JsonPropertyParser(string property, IJsonParser<T> parser) : base(property)
        {
            Parser = parser;
        }

        public bool TryGetValue(JsonNode json, out T value)
        {
            value = default;
            return TryGetValue(json, out json) && Parser.TryGetValue(json, out value);
        }

        public bool TryGetValue(ArraySegment<byte> bytes, out T value)
        {
            value = default;
            return TryGetValue(bytes, out bytes) && Parser.TryGetValue(bytes, out value);
        }
    }

    public abstract class Parser : IJsonParser<PropertyValuePair>
    {
        public Property Property { get; }

        protected Parser(Property property)
        {
            Property = property;
        }

        public static Parser<T> Create<T>(Property<T> property) => new Parser<T>(property, new JsonNodeParser<T>());
        public static Parser<T> Create<T>(Property<T> property, Func<JsonNode, T> parse) => new Parser<T>(property, new JsonNodeParser<T>(parse));

        public static Parser<IEnumerable<T>> Create<T>(MultiProperty<T> property, string subProperty) => new MultiParser<T>(property, new JsonArrayParser<T>(new JsonPropertyParser<T>(subProperty)));
        public static Parser<IEnumerable<T>> Create<T>(MultiProperty<T> property, Func<JsonNode, IEnumerable<T>> parse) => new MultiParser<T>(property, new JsonNodeParser<IEnumerable<T>>(parse));

        public abstract bool TryGetValue(JsonNode json, out PropertyValuePair value);

        public abstract PropertyValuePair GetPair(JsonNode node);
        public virtual PropertyValuePair GetPair(ArraySegment<byte> bytes) => GetPair(JsonNode.Parse(bytes));

        protected async Task<JsonNode> Convert(Task<ArraySegment<byte>> bytes) => JsonNode.Parse(await bytes);
    }

    public abstract class IParser<T> : Parser
    {
        public IJsonParser<T> JsonParser { get; }

        public IParser(Property property, IJsonParser<T> jsonParser) : base(property)
        {
            JsonParser = jsonParser;
        }

        public override PropertyValuePair GetPair(JsonNode node) => GetPairInternal(Unwrap(node));

        public override PropertyValuePair GetPair(ArraySegment<byte> bytes) => GetPairInternal(Unwrap(bytes));

        protected abstract PropertyValuePair GetPairInternal(T value);

        protected T Unwrap(JsonNode json) => JsonParser.TryGetValue(json, out var value) ? value : throw new FormatException();
        protected T Unwrap(ArraySegment<byte> bytes)
        {
            if (JsonParser.TryGetValue(bytes, out var value))
            {
                return value;
            }
            else
            {
#if DEBUG
                throw new FormatException($"Could not parse to type {typeof(T)}: \"{System.Text.Encoding.UTF8.GetString(bytes)}\"");
#else
                throw new FormatException($"Could not parse to type {typeof(T)}");
#endif
            }
        }
    }

    public class ParserWrapper : IParser<ArraySegment<byte>>
    {
        public Parser Parser { get; }

        public ParserWrapper(Parser parser, IJsonParser<ArraySegment<byte>> jsonParser) : base(parser.Property, jsonParser)
        {
            Parser = parser;
        }

        public override bool TryGetValue(JsonNode json, out PropertyValuePair value)
        {
            value = null;

            if (JsonParser != null)
            {
                if (JsonParser is IJsonParser<JsonNode> jsonNodeParser && jsonNodeParser.TryGetValue(json, out json))
                {

                }
                else
                {
                    return false;
                }
            }

            return Parser.TryGetValue(json, out value);
        }

        protected override PropertyValuePair GetPairInternal(ArraySegment<byte> value) => Parser.GetPair(value);

        //public override PropertyValuePair GetPair(Task<JsonNode> node) => Parser.GetPair(Unwrap(node));

        //private async Task<JsonNode> Unwrap(Task<JsonNode> json) => JsonParser?.TryGetValue(await json, out var unwrapped) == true ? unwrapped : new JsonObject();
    }

    public class Parser<T> : IParser<T>, IJsonParser<PropertyValuePair>
    {
        public Parser(Property<T> property, IJsonParser<T> jsonParser) : this((Property)property, jsonParser) { }
        protected Parser(Property property, IJsonParser<T> jsonParser) : base(property, jsonParser) { }

        public static implicit operator Parser<T>(Property<T> property) => Create(property);

        protected override PropertyValuePair GetPairInternal(T value) => new PropertyValuePair<T>((Property<T>)Property, value);

        public override bool TryGetValue(JsonNode json, out PropertyValuePair value)
        {
            if (JsonParser.TryGetValue(json, out var value1))
            {
                value = new PropertyValuePair<T>((Property<T>)Property, value1);
                return true;
            }

            value = null;
            return false;
        }
    }

    public class MultiParser<T> : Parser<IEnumerable<T>>
    {
        public MultiParser(MultiProperty<T> property, IJsonParser<IEnumerable<T>> jsonParser) : base(property, jsonParser) { }

        protected override PropertyValuePair GetPairInternal(IEnumerable<T> value) => new PropertyValuePair<T>((MultiProperty<T>)Property, value);

        public override bool TryGetValue(JsonNode json, out PropertyValuePair value)
        {
            if (JsonParser.TryGetValue(json, out var value1))
            {
                value = new PropertyValuePair<T>((MultiProperty<T>)Property, value1);
                return true;
            }

            value = null;
            return false;
        }
    }
}