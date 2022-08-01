using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    public interface IJsonParser<T>
    {
        bool TryGetValue(JsonNode json, out T value);
    }

    public class JsonNodeParser<T> : IJsonParser<T>
    {
        private AsyncEnumerable.TryParseFunc<JsonNode, T> Parse;

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
    }

    public class JsonPropertyParser<T> : IJsonParser<T>
    {
        public string Property { get; }
        public IJsonParser<T> Parser { get; }

        public JsonPropertyParser(string property) : this(property, new JsonNodeParser<T>()) { }

        public JsonPropertyParser(string property, IJsonParser<T> parser)
        {
            Property = property;
            Parser = parser;
        }

        public bool TryGetValue(JsonNode json, out T value)
        {
            if (json.TryGetValue(Property, out JsonNode sub))
            {
                return Parser.TryGetValue(sub, out value);
            }

            value = default;
            return false;
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

        public abstract PropertyValuePair GetPair(Task<JsonNode> node);
        public abstract bool TryGetValue(JsonNode json, out PropertyValuePair value);
    }

    public class ParserWrapper : Parser
    {
        public Parser Parser { get; set; }
        public IJsonParser<JsonNode> JsonParser { get; set; }

        public ParserWrapper(Parser parser) : base(parser.Property)
        {
            Parser = parser;
        }

        public override bool TryGetValue(JsonNode json, out PropertyValuePair value)
        {
            value = null;

            if (JsonParser != null)
            {
                if (JsonParser.TryGetValue(json, out var parsed))
                {
                    json = parsed;
                }
                else
                {
                    return false;
                }
            }

            return Parser.TryGetValue(json, out value);
        }

        public override PropertyValuePair GetPair(Task<JsonNode> node) => Parser.GetPair(Unwrap(node));

        private async Task<JsonNode> Unwrap(Task<JsonNode> json) => JsonParser?.TryGetValue(await json, out var unwrapped) == true ? unwrapped : new JsonObject();
    }

    public class Parser<T> : Parser, IJsonParser<PropertyValuePair>
    {
        public IJsonParser<T> JsonParser { get; }

        public Parser(Property<T> property, IJsonParser<T> jsonParser) : this((Property)property, jsonParser) { }
        protected Parser(Property property, IJsonParser<T> jsonParser) : base(property)
        {
            JsonParser = jsonParser;
        }

        public static implicit operator Parser<T>(Property<T> property) => Create(property);

        public override PropertyValuePair GetPair(Task<JsonNode> json) => new PropertyValuePair<T>((Property<T>)Property, Parse(json));

        public override bool TryGetValue(JsonNode json, out PropertyValuePair value)
        {
            if (JsonParser.TryGetValue(json, out var value1))
            {
                value = new PropertyValuePair<T>((Property<T>)Property, Task.FromResult(value1));
                return true;
            }

            value = null;
            return false;
        }

        protected async Task<T> Parse(Task<JsonNode> json) => JsonParser.TryGetValue(await json, out var value) ? value : throw new FormatException();
    }

    public class MultiParser<T> : Parser<IEnumerable<T>>
    {
        public MultiParser(MultiProperty<T> property, IJsonParser<IEnumerable<T>> jsonParser) : base(property, jsonParser) { }

        public override PropertyValuePair GetPair(Task<JsonNode> json) => new PropertyValuePair<T>((MultiProperty<T>)Property, Parse(json));

        public override bool TryGetValue(JsonNode json, out PropertyValuePair value)
        {
            if (JsonParser.TryGetValue(json, out var value1))
            {
                value = new PropertyValuePair<T>((MultiProperty<T>)Property, Task.FromResult(value1));
                return true;
            }

            value = null;
            return false;
        }
    }
}