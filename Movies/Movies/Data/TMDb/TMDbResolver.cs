using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace Movies
{
    public static class HttpUtilityExtensions
    {
        public static string GetAndRemove(this NameValueCollection collection, string name)
        {
            var value = collection.Get(name);
            collection.Remove(name);
            return value;
        }
    }

    public class TMDbResolver
    {
        public static readonly string APPEND_TO_RESPONSE = "append_to_response";

        private Dictionary<ItemType, Dictionary<Property, Parser>> Index { get; }
        private Dictionary<string, KeyValuePair<TMDbRequest, List<Parser>>> Annotations { get; }
        private Dictionary<ItemType, IReadOnlyDictionary<Property, TMDbRequest>> Lookup { get; }
        private Dictionary<Property, string> ChangeKeys { get; } = new Dictionary<Property, string>
        {
            [Media.KEYWORDS] = "plot_keywords",
            [Media.POSTER_PATH] = "images",
            [Media.BACKDROP_PATH] = "images",
            [Media.TRAILER_PATH] = "videos",
            [Movie.CONTENT_RATING] = "releases",
            [Movie.RELEASE_DATE] = "releases",
            [TVShow.CONTENT_RATING] = "releases",
            [TVShow.FIRST_AIR_DATE] = "episode",
            [TVShow.LAST_AIR_DATE] = "episode",
            //[TVShow.SEASONS] = "season",
            [Person.PROFILE_PATH] = "images",
        };

        private Dictionary<ItemType, ItemProperties> Properties { get; }

        public TMDbResolver(Dictionary<ItemType, ItemProperties> properties)
        {
            Properties = properties;

            Index = new Dictionary<ItemType, Dictionary<Property, Parser>>();
            Annotations = new Dictionary<string, KeyValuePair<TMDbRequest, List<Parser>>>();
            Lookup = new Dictionary<ItemType, IReadOnlyDictionary<Property, TMDbRequest>>();

            foreach (var kvp in Properties)
            {
                var index = new Dictionary<Property, Parser>();

                foreach (var kvp1 in kvp.Value.Info)
                {
                    var path = RemoveVariables(string.Format(kvp1.Key.Endpoint, Enumerable.Repeat<object>(0, 3).ToArray()), out _);
                    Annotations[path] = kvp1;

                    foreach (var parser in kvp1.Value)
                    {
                        index[parser.Property] = parser;
                    }
                }

                Index[kvp.Key] = index;
                Lookup[kvp.Key] = kvp.Value.PropertyLookup;

                foreach (var parser in kvp.Value.Info.SelectMany(kvp => kvp.Value))
                {
                    var property = parser.Property;

                    //if (CHANGE_KEY_PROPERTY_MAP.TryGetValue(property, out var changeKey) || (changeKey = ((parser as ParserWrapper)?.JsonParser as JsonPropertyParser)?.Property) != null)
                    if (((parser as ParserWrapper)?.JsonParser as JsonPropertyParser)?.Property is string changeKey)
                    {
                        ChangeKeys.TryAdd(property, changeKey);
                    }
                }
            }
        }

        public bool TryGetParser(ItemType type, Property property, out Parser parser)
        {
            if (Index.TryGetValue(type, out var map) && map.TryGetValue(property, out var temp))
            {
                parser = temp;
                return true;
            }
            else
            {
                parser = default;
                return false;
            }
        }

        public string RemoveVariables(Uri uri, out List<object> args)
        {
            var url = ResolveUrl(uri);

            url = url.Split('?')[0];
            url = string.Join('/', url.Split('/').Skip(1));
            url = RemoveVariables(url, out args);

            return url;
        }

        public string RemoveVariables(string url, out List<object> args)
        {
            args = new List<object>();
            var parts = url.Split('/').Select(part => part.ToLower().Trim()).ToList();

            if (parts.Count > 0)
            {
                if (parts[0] == "movie" || parts[0] == "person")
                {
                    RemoveVariables(parts, out args, 1);
                }
                else if (parts[0] == "tv")
                {
                    RemoveVariables(parts, out args, 1, 3, 5);
                }

                url = string.Join('/', parts);
            }

            return url;
        }

        private void RemoveVariables(List<string> parts, out List<object> args, params int[] indices)
        {
            args = new List<object>();

            foreach (var index in indices.OrderByDescending(index => index))
            {
                if (parts.Count > index && int.TryParse(parts[index], out var id))
                {
                    args.Insert(0, id);
                    parts.RemoveAt(index);
                }
            }
        }

        public bool TryGetAnnotations(Uri uri, out KeyValuePair<TMDbRequest, List<Parser>> annotations)
        {
            var url = RemoveVariables(uri, out _);
            return Annotations.TryGetValue(url, out annotations);
        }

        public bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
        {
            var url = ResolveUrl(uri);
            var rawQuery = url.Split('?').ElementAtOrDefault(1) ?? string.Empty;
            //var rawQuery = new Uri(url, UriKind.Relative).Query;
            var query = HttpUtility.ParseQueryString(rawQuery);

            var language = query.GetAndRemove("language");// ?? TMDB.LANGUAGE.Iso_639;
            var region = query.GetAndRemove("region");// ?? TMDB.REGION.Iso_3166;
            var adult = (query.GetAndRemove("adult") ?? TMDB.ADULT.ToString().ToLower()) == "true";
            adult = query.GetAndRemove("adult") == "true";
            var append = query.GetAndRemove(APPEND_TO_RESPONSE)?.Split(',') ?? new string[0];

            var basePath = uri.ToString().Split('?')[0];
            var queryString = query.ToString();
            var data = new Dictionary<TMDbRequest, (Uri Uri, string Path, List<Parser> Parsers, IEnumerable<object> Args)>();
            var partial = (uri as TrojanTMDbUri)?.RequestedProperties != null;

            foreach (var appended in append.Prepend(string.Empty))
            {
                var fullUrl = basePath;
                if (!string.IsNullOrEmpty(appended))
                {
                    fullUrl += "/" + appended;
                }
                fullUrl += "?" + queryString;

                var fullUri = new Uri(fullUrl, UriKind.RelativeOrAbsolute);
                var deparameterizedUrl = RemoveVariables(fullUri, out var args);

                if (Annotations.TryGetValue(deparameterizedUrl, out var annotation))
                {
                    var childUrl = string.Format(annotation.Key.GetURL(language, region, adult, queryString), args.ToArray());
                    fullUri = new Uri(childUrl, UriKind.Relative);

                    data.Add(annotation.Key, (fullUri, appended, partial ? new List<Parser>() : annotation.Value, args));
                }
            }

            if (data.Count > 0 && uri is TrojanTMDbUri dummyUri)
            {
                if (dummyUri.Item != null && dummyUri.RequestedProperties != null && Lookup.TryGetValue(dummyUri.Item.ItemType, out var lookup))
                {
                    foreach (var property in dummyUri.RequestedProperties)
                    {
                        if (lookup.TryGetValue(property, out var request) &&
                            data.TryGetValue(request, out var value) &&
                            TryGetParser(dummyUri.Item.ItemType, property, out var parser))
                        {
                            if (request is PagedTMDbRequest pagedRequest)
                            {
                                parser = ReplacePagedParsers(pagedRequest, parser, value.Args);
                            }

                            value.Parsers.Add(parser);
                        }
                    }
                }
                else
                {
                    foreach (var kvp in data)
                    {
                        if (kvp.Key is PagedTMDbRequest pagedRequest)
                        {
                            var value = kvp.Value;

                            for (int i = 0; i < kvp.Value.Parsers.Count; i++)
                            {
                                value.Parsers[i] = ReplacePagedParsers(pagedRequest, value.Parsers[i], value.Args);
                            }
                        }
                    }
                }

                resource = new HttpConverter(dummyUri.Item, data.Select(kvp => kvp.Value), dummyUri.ParentCollectionWasRequested);
                return true;
            }

            resource = default;
            return false;
        }

        private Parser ReplacePagedParsers(PagedTMDbRequest request, Parser parser, IEnumerable<object> args)
        {
            if (parser.Property is Property<IAsyncEnumerable<Item>> property == false)
            {
                return parser;
            }

            var pageParser = ParserToJsonParser<IAsyncEnumerable<Item>>(parser);
            var pagedRequest = new ParameterizedPagedRequest(request, args.ToArray());
            var pagedParser = new TMDB.PagedParser<Item>(pagedRequest, pageParser);
            var listParser = new Parser<IAsyncEnumerable<Item>>(property, pagedParser);

            return listParser;
        }

        private static IJsonParser<T> ParserToJsonParser<T>(Parser parser) => new JsonNodeParser<T>((JsonNode json, out T items) =>
        {
            if (parser.TryGetValue(json, out var pair) && pair.Value is T task)// && task.IsCompletedSuccessfully)
            {
                items = task;
                return true;
            }

            items = default;
            return true;
        });

        public abstract class LazyBytes : IEnumerable<byte>
        {
            public JsonIndex Index { get; }
            public ArraySegment<byte> Bytes => _Bytes ??= GetBytes();

            private ArraySegment<byte>? _Bytes = null;

            public LazyBytes(JsonIndex index)
            {
                Index = index;
            }

            protected abstract ArraySegment<byte> GetBytes();

            public IEnumerator<byte> GetEnumerator() => Bytes.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class PropertyBytes : LazyBytes
        {
            public string Path { get; }

            public PropertyBytes(JsonIndex index, string path) : base(index)
            {
                Path = path;
            }

            protected override ArraySegment<byte> GetBytes() => (Index.TryGetValue(Path, out var index) ? index : Index).Bytes;
        }

        private class ExceptBytes : LazyBytes
        {
            public IEnumerable<string> Paths { get; }

            private static readonly IEnumerable<byte> OPEN = Encoding.UTF8.GetBytes("{\r\n  ");
            private static readonly IEnumerable<byte> DELIMITER = Encoding.UTF8.GetBytes(",\r\n  ");
            private static readonly IEnumerable<byte> CLOSE = Encoding.UTF8.GetBytes("\r\n}");

            public ExceptBytes(JsonIndex index, IEnumerable<string> paths) : base(index)
            {
                Paths = paths;
            }

            protected override ArraySegment<byte> GetBytes() => OPEN.Concat(GetBytesEnumerable()
                .Aggregate((first, second) => first.Concat(DELIMITER).Concat(second)))
                .Concat(CLOSE)
                .ToArray();

            private IEnumerable<IEnumerable<byte>> GetBytesEnumerable()
            {
                var set = Paths.ToHashSet();
                return Index
                    .Where(kvp => !set.Contains(kvp.Key))
                    .Select(kvp => Encoding.UTF8.GetBytes($"\"{kvp.Key}\":").Concat((IEnumerable<byte>)kvp.Value.Bytes));
            }
        }

        public class HttpConverter : HttpResourceCollectionConverter
        {
            public Item Item { get; }
            public bool ParentCollectionWasRequested { get; }
            public IEnumerable<(Uri Uri, string Path, List<Parser> Parsers, IEnumerable<object> Args)> Annotations { get; }
            public override IEnumerable<Uri> Resources { get; }

            public HttpConverter(Item item, IEnumerable<(Uri, string, List<Parser>, IEnumerable<object>)> annotations, bool parentCollectionWasRequested)
            {
                Item = item;
                Resources = annotations.SelectMany(temp => (Item == null ? Enumerable.Empty<Uri>() : temp.Item3.Select(parser => GetUii(Item, temp.Item1, parser))).Prepend(temp.Item1)).ToArray();
                Annotations = annotations;
                ParentCollectionWasRequested = parentCollectionWasRequested;
            }

            public override async Task<IEnumerable<KeyValuePair<Uri, object>>> Convert(HttpContent content)
            {
                var result = new Dictionary<Uri, object>();
                var bytes = content.ReadAsByteArrayAsync();
                var json = new JsonIndex(await bytes);

                foreach (var annotation in Annotations)
                {
                    var lazyJson = string.IsNullOrEmpty(annotation.Path) ? new ExceptBytes(json, Annotations.Select(annotation => annotation.Path).Where(path => !string.IsNullOrEmpty(path))) : (LazyBytes)new PropertyBytes(json, annotation.Path);
                    //JsonIndex index;

                    //if (string.IsNullOrEmpty(annotation.Path))
                    //{
                    //    index = json;
                    //}
                    //else if (!json.TryGetValue(annotation.Path, out index))
                    //{
                    //    continue;
                    //}

                    var state1 = State.Create(lazyJson);
                    result.Add(annotation.Uri, state1);

                    var inner = new Dictionary<Uri, State>();

                    foreach (var parser in annotation.Parsers)
                    {
                        Func<IEnumerable<byte>, object> converter;

                        if (parser.Property == TVShow.SEASONS)
                        {
                            //var response = await bytes;
                            converter = source => GetTVItems(source as byte[] ?? source.ToArray(), "seasons", (JsonNode json, out TVSeason season) => TMDB.TryParseTVSeason(json, (TVShow)Item, out season));
                        }
                        else if (parser.Property == TVSeason.EPISODES)
                        {
                            if (Item is TVSeason season)
                            {
                                //var response = await bytes;
                                converter = source => GetTVItems(source as byte[] ?? source.ToArray(), "episodes", (JsonNode json, out TVEpisode episode) => TMDB.TryParseTVEpisode(json, season, out episode));
                            }
                            else
                            {
                                converter = null;
                            }
                        }
                        else if (parser.Property == Movie.PARENT_COLLECTION)
                        {
                            if (!ParentCollectionWasRequested)
                            {
                                converter = null;
                            }
                            else //if (parser.GetPair(lazyJson.Bytes).Value is Task<Collection> response)
                            {
                                try
                                {
                                    var collection = parser.GetPair(lazyJson.Bytes).Value is int id && id != -1 ? await TMDB.GetCollection(id) : null;
                                    converter = source => collection;
                                }
                                catch
                                {
                                    converter = null;
                                }
                            }
                        }
                        else
                        {
                            converter = source => parser.GetPair(source is ArraySegment<byte> segment ? segment : new ArraySegment<byte>(source.ToArray())).Value;
                        }

                        try
                        {
                            var bytes1 = lazyJson.Bytes;
                            if (parser is IParser<ArraySegment<byte>> parser1 && parser1.JsonParser is JsonPropertyParser jpp && (string.IsNullOrEmpty(annotation.Path) ? json : (json.TryGetValue(annotation.Path, out var temp) ? temp : null)) is JsonIndex index)
                            {
                                bytes1 = new PropertyBytes(index, jpp.Property).Bytes;
                            }

                            bytes1 = TrimWhitespace(bytes1);
                            var byteRepresentation = new ObjectRepresentation<IEnumerable<byte>>(bytes1);
                            var state = new State(byteRepresentation);

                            if (converter != null)
                            {
                                state.AddRepresentation(parser.Property.FullType, new LazilyConvertedRepresentation<IEnumerable<byte>, object>(new ObjectRepresentation<IEnumerable<byte>>(lazyJson.Bytes), converter));
                            }
                            //else if (success)
                            //{
                            //    //var state = converted == null ? State.Null(parser.Property.FullType) : new State(converted);
                            //    state.Add(parser.Property.FullType, converted);
                            //}

                            var uii = GetUii(Item, annotation.Uri, parser);
                            inner.Add(uii, state);
                        }
                        catch { }
                    }

                    if (inner.Count > 0)
                    {
                        state1.Add(inner);

                        foreach (var kvp in inner)
                        {
                            result.Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                return result;
            }

            private static UniformItemIdentifier GetUii(Item item, Uri uri, Parser parser)
            {
                var query = uri.ToString().Split("?").ElementAtOrDefault(1);
                return new UniformItemIdentifier(item, parser.Property, query);
            }

            private static ArraySegment<byte> TrimWhitespace(ArraySegment<byte> bytes)
            {
                int offset = 0;
                int count = bytes.Count;

                for (; char.IsWhiteSpace((char)bytes[offset]); offset++) { }
                for (; char.IsWhiteSpace((char)bytes[count - 1]); count--) { }

                return new ArraySegment<byte>(bytes.Array, bytes.Offset + offset, count - offset);
            }
        }

        public class TrojanTMDbUri : Uri
        {
            public Item Item { get; }
            public bool ParentCollectionWasRequested { get; }
            public IEnumerable<Property> RequestedProperties { get; set; }
            public IHttpConverter<object> Converter { get; set; }

            public TrojanTMDbUri(string url, Item item, bool parentCollectionWasRequested) : base(url, UriKind.RelativeOrAbsolute)
            {
                Item = item;
                ParentCollectionWasRequested = parentCollectionWasRequested;
            }
        }

        private static IEnumerable<T> GetTVItems<T>(byte[] json, string property, AsyncEnumerable.TryParseFunc<JsonNode, T> parse) => TMDB.TryParseCollection(JsonNode.Parse(json), new JsonPropertyParser<IEnumerable<JsonNode>>(property), out var result, new JsonNodeParser<T>(parse)) ? result : null;

        private static async Task<IEnumerable<T>> GetTVItems<T>(Task<ArraySegment<byte>> json, string property, AsyncEnumerable.TryParseFunc<JsonNode, T> parse) => TMDB.TryParseCollection(JsonNode.Parse(await json), new JsonPropertyParser<IEnumerable<JsonNode>>(property), out var result, new JsonNodeParser<T>(parse)) ? result : null;

        public bool TryGetChangeKey(Property property, out string changeKey) => ChangeKeys.TryGetValue(property, out changeKey);

        public string ResolveUrl(Uri uri)
        {
            string url;

            if (uri is UniformItemIdentifier uii == false || !TryResolve(uii, out url))
            {
                url = uri.ToString();
            }

            return url;
        }

        public bool TryResolve(UniformItemIdentifier uii, out string url)
        {
            if (Properties.TryGetValue(uii.Item.ItemType, out var properties) && properties.PropertyLookup.TryGetValue(uii.Property, out var request) && TMDB.TryGetParameters(uii.Item, out var args))
            {
                var language = uii.Language ?? TMDB.LANGUAGE;
                var region = uii.Region ?? TMDB.REGION;
                var adult = uii.IncludeAdult ?? false;

                url = string.Format(request.GetURL(language.Iso_639, region.Iso_3166, adult), args.ToArray());
                url = string.Format(request.GetURL(uii.Language?.Iso_639, uii.Region?.Iso_3166, uii.IncludeAdult), args.ToArray());
                return true;
            }
            else
            {
                url = null;
                return false;
            }
        }

        public bool TryGetRequest(Uri uri, out TMDbRequest request)
        {
            if (uri is UniformItemIdentifier uii && TryGetRequest(uii, out request))
            {
                return true;
            }
            else if (TryGetAnnotations(uri, out var annotations))
            {
                request = annotations.Key;
                return true;
            }

            request = null;
            return false;
        }

        public bool TryGetRequest(UniformItemIdentifier uii, out TMDbRequest request)
        {
            request = null;
            return Properties.TryGetValue(uii.Item.ItemType, out var properties) && properties.PropertyLookup.TryGetValue(uii.Property, out request);
        }

        public string Resolve(TMDbRequest request, params object[] args) => string.Format(request.GetURL(), args);

        public bool TryResolve(Uri uri, out TMDbRequest request, out object[] args)
        {
            args = new object[0];

            //if (uri is UniformItemIdentifier uii && TryGetRequest(uii, out request))
            if (TryGetRequest(uri, out request))
            {
                if (uri is UniformItemIdentifier uii && TMDB.TryGetParameters(uii.Item, out var temp))
                {
                    args = temp.ToArray();
                }
                else
                {
                    RemoveVariables(uri, out var args1);
                    args = args1.ToArray();
                }

                return true;
            }

            request = null;
            return false;
        }
    }
}
