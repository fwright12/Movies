using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbResolver
    {
        private Dictionary<ItemType, Dictionary<Property, Parser>> Index { get; }
        private Dictionary<string, KeyValuePair<TMDbRequest, List<Parser>>> Annotations { get; }
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

        public IEnumerable<KeyValuePair<Property, JsonConverterWrapper<string>>> Annotate(string url, params string[] basePath)
        {
            url = url.Split('?')[0];
            url = string.Join('/', url.Split('/').Skip(1));
            var key = RemoveVariables(url, out var args);

            //ItemType type;
            //if (Equals(args[0], "movie")) type = ItemType.Movie;
            //else if (Equals(args[0], "tv")) type = ItemType.TVShow;
            //else if (Equals(args[0], "person")) type = ItemType.Person;
            //else yield break;

            //if (!Properties.TryGetValue(type, out var properties) || !Annotations.TryGetValue(key, out var request) || !properties.Info.TryGetValue(null, out var parsers))
            if (!Annotations.TryGetValue(key, out var info))
            {
                yield break;
            }

            foreach (var parser in info.Value)
            {
                var propertyName = string.Empty;

                if ((parser as ParserWrapper)?.JsonParser is JsonPropertyParser jpp)
                {
                    propertyName = jpp.Property;
                }

                // Should do this for ALL types, but need to work around legacy Parsers
                //
                // Parsers expect full response, and then find the property and then convert
                // So put the full response so the parser works UNLESS the expected type is string,
                // in which case there's no conversion to do and the parser won't run
                var path = parser.Property.FullType == typeof(string) ? new string[] { propertyName } : new string[0];
                var converter = (JsonConverter<string>)StringConverter.Instance;
                path = new string[0];

                if (parser.Property == Movie.CONTENT_RATING)
                {
                    converter = TMDB.MovieCertificationConverter.Instance;
                }
                else if (parser.Property == TVShow.CONTENT_RATING)
                {
                    converter = TMDB.TVCertificationConverter.Instance;
                }

                yield return new KeyValuePair<Property, JsonConverterWrapper<string>>(parser.Property, new JsonConverterWrapper<string>(basePath.Concat(path).ToArray(), converter));
            }
        }

        private async Task<IEnumerable<T>> GetTVItems<T>(Task<ArraySegment<byte>> json, string property, AsyncEnumerable.TryParseFunc<JsonNode, T> parse) => TMDB.TryParseCollection(JsonNode.Parse(await json), new JsonPropertyParser<IEnumerable<JsonNode>>(property), out var result, new JsonNodeParser<T>(parse)) ? result : null;

        public async Task<IConverter<ArraySegment<byte>>> GetConverter(Uri uri, ArraySegment<byte> target)
        {
            if (TryGetParser(uri, out var parser))
            {
                var uii = (UniformItemIdentifier)uri;
                var success = false;
                object converted = null;
                var task = Task.FromResult(target);

                if (parser.Property == TVShow.SEASONS)
                {
                    success = true;
                    converted = await GetTVItems(task, "seasons", (JsonNode json, out TVSeason season) => TMDB.TryParseTVSeason(json, (TVShow)uii.Item, out season));
                }
                else if (parser.Property == TVSeason.EPISODES)
                {
                    if (uii.Item is TVSeason season)
                    {
                        success = true;
                        converted = await GetTVItems(task, "episodes", (JsonNode json, out TVEpisode episode) => TMDB.TryParseTVEpisode(json, season, out episode));
                    }
                }
                //else if (parser.Property.FullType == typeof(string))
                //{
                //    success = true;
                //    converted = Encoding.UTF8.GetString(target);
                //}
                else
                {
                    var response = await parser.TryGetValue(task);
                    success = response.Success;
                    converted = response.Result;
                }

                if (success)
                {
                    return new DummyConverter(target, converted);
                }
            }

            return null;
        }

        private class DummyConverter : IConverter<ArraySegment<byte>>
        {
            private ArraySegment<byte> Original { get; }
            private object Converted { get; }

            public DummyConverter(ArraySegment<byte> original, object converted)
            {
                Original = original;
                Converted = converted;
            }

            public bool TryConvert(ArraySegment<byte> original, Type targetType, out object converted)
            {
                converted = Converted;
                return original == Original;
            }
        }

        private class Converter : IConverter<string>
        {
            public TMDbResolver Resolver { get; }
            public Uri Uri { get; }

            public Converter(TMDbResolver resolver, Uri uri)
            {
                Resolver = resolver;
                Uri = uri;
            }

            public bool TryConvert(string original, Type targetType, out object converted)
            {
                if (Resolver.TryGetParser(Uri, out var parser))
                {
                    converted = parser.GetPair(Convert(original)).Value;
                    return true;
                }
                else
                {
                    converted = default;
                    return false;
                }
            }
        }

        /*public async Task Handle(MultiRestEventArgs<GetEventArgs> e, ChainLinkEventHandler<MultiRestEventArgs<GetEventArgs<HttpContent>>> typedHandler)
        {
            IEnumerable<GetEventArgs<HttpContent>> typedRequests = e.Args.Select(WrapGetEventArgs<HttpContent>).ToArray();
            await typedHandler(new MultiRestEventArgs<GetEventArgs<HttpContent>>(typedRequests));

            var typedItr = typedRequests.GetEnumerator();
            var untypedItr = e.Args.GetEnumerator();

            while (typedItr.MoveNext() && untypedItr.MoveNext())
            {
                var converter = DefaultConverter<HttpContent>.Instance;
                var typed = typedItr.Current;
                var untyped = untypedItr.Current;

                if (typed.Handled && !untyped.Handle(typed.Resource, converter) && TryGetParser(untyped.Uri, out var parser))
                {
                    untyped.Handle(parser.GetPair(Convert(typed.Resource)).Value, Converter);
                }
            }
        }*/

        private bool TryGetParser(Uri uri, out Parser parser)
        {
            parser = null;
            return uri is UniformItemIdentifier uii && Index.TryGetValue(uii.Item.ItemType, out var properties) && properties.TryGetValue(uii.Property, out parser);
        }

        private static Task<ArraySegment<byte>> Convert(string content) => Task.FromResult<ArraySegment<byte>>(Encoding.UTF8.GetBytes(content));
        private static async Task<ArraySegment<byte>> Convert(HttpContent content) => await content.ReadAsByteArrayAsync();

        public bool TryResolveJsonPropertyName(Property property, out string changeKey) => ChangeKeys.TryGetValue(property, out changeKey);

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
                url = string.Format(request.GetURL(), args.ToArray());
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
