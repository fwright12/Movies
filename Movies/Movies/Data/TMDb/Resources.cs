using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TrieValue = System.Collections.Generic.List<(string Url, string Path, string Query, Movies.RestRequestArgs Arg)>;

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

        private string RemoveVariables(string url, out List<object> args)
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
            var url = ResolveUrl(uri);
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

            if (uri is UniformItemIdentifier uii && TryGetRequest(uii, out request))
            {
                if (TMDB.TryGetParameters(uii.Item, out var temp))
                {
                    args = temp.ToArray();
                }
                return true;
            }

            request = null;
            return false;
        }
    }

    public class TMDbLocalResources : ControllerLink//<HttpContent>
    {
        public IAsyncCollection<string> ChangeKeys { get; set; }

        // There are no change keys for these properties but we're going to ignore that and cache them anyway
        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType CACHEABLE_TYPES = ItemType.Movie | ItemType.TVShow | ItemType.Person;

        private IJsonCache Cache { get; }
        private TMDbResolver Resolver { get; }

        public TMDbLocalResources(IJsonCache cache, TMDbResolver resolver) : base()
        {
            Cache = cache;
            Resolver = resolver;
        }

        protected override async Task GetInternalAsync(MultiRestEventArgs args, ChainLinkEventHandler<MultiRestEventArgs> next) //=> HandleAsync(e, next, async e =>
        {
            foreach (var e in args.Args)
            {
                if (!IsCacheable(e.Uri))
                {
                    return;
                }

                var url = Resolver.ResolveUrl(e.Uri);
                var response = await Cache.TryGetValueAsync(url);

                if (response != null)
                {
                    var json = new AnnotatedJson(await response.Content.ReadAsByteArrayAsync());

                    if (e.Uri is UniformItemIdentifier uii)
                    {
                        foreach (var annotation in Resolver.Annotate(url))
                        {
                            json.Add(new UniformItemIdentifier(uii.Item, annotation.Key), annotation.Value);
                        }
                    }

                    args.HandleMany(json);

                    if (!e.Handled && e.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(e.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                    {
                        e.Handle(converter);
                    }
                }
            }
        }//);

        protected override Task PutAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next, async e =>
        {
            //if (e.Uri is UniformItemIdentifier uii && !IsCacheable(uii)) // && uii.Item.TryGetID(TMDB.ID, out var id) && Resolver.TryResolve(uii, out var url) && arg.Response is HttpContent content)// && Converter.TryConvert(resource, out var content))
            if (e.Uri is UniformItemIdentifier || !IsCacheable(e.Uri))
            {
                return;
            }

            var url = Resolver.ResolveUrl(e.Uri);

            if (e.Body?.TryGetRepresentation<ArraySegment<byte>>(out var content) == true)
            {
                await Cache.AddAsync(url, new JsonResponse(content.ToArray()));
            }
        });

        /*public async Task HandleAsync1(GetEventArgs<HttpContent> arg)
        {
            var url = DeconstructUrl(arg.Uri.ToString(), out var query, out var paths);
            var urls = ConstructUrls(url, query, paths);
            var responses = (await Task.WhenAll(urls.Select(url => Cache.TryGetValueAsync(url))))
                .OfType<JsonResponse>()
                .ToArray();

            if (responses.All(response => response != null))
            {
                var json = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
                var properties = paths.Zip(json, JsonProperty);
                arg.Handle(new StringContent(string.Join(',', properties)));
            }
        }

        public async Task HandleAsync(PostEventArgs<HttpContent> arg)
        {
            var url = DeconstructUrl(arg.Uri.ToString(), out var query, out var paths);
            //var urls = ConstructUrls(url, query, paths);
            var json = new LazyJson(await arg.Resource.ReadAsByteArrayAsync(), paths);

            foreach (var path in paths.Prepend(""))
            {
                if (json.TryGetValue(path, out var bytes))
                {
                    var temp = url;

                    if (!string.IsNullOrEmpty(path))
                    {
                        temp += "/" + path;
                    }

                    //await Cache.AddAsync(ItemType.Movie, 0, $"{temp}?{query}", new JsonResponse(bytes.ToArray()));
                }
            }
        }

        private const string APPEND_TO_RESPONSE = "append_to_response";

        private string JsonProperty(string propertyName, string json) => $"[{propertyName}] = {json}";

        private IEnumerable<string> SeparateAppendedRequests(string url) => ConstructUrls(DeconstructUrl(url, out var query, out var appended), query, appended);

        private IEnumerable<string> ConstructUrls(string baseUrl, string query, IEnumerable<string> appended) => appended.Select(path => $"{baseUrl}/{path}?{query}");

        private string DeconstructUrl(string url, out string query, out IEnumerable<string> appended)
        {
            var parts = url.Split('?');
            appended = new string[] { "" };

            if (parts.Length > 1)
            {
                query = parts[1];
                var args = parts[1]
                    .Split('&')
                    .Select(arg => arg.Split('='))
                    .ToList();
                var i = args.FindIndex(arg => arg.FirstOrDefault() == APPEND_TO_RESPONSE);

                if (args[i].Length > 1)
                {
                    var atr = args[i];
                    args.RemoveAt(i);

                    var temp = query = string.Join('&', args.Select(arg => string.Join('=', arg)));
                    appended = atr[1].Split(',').ToArray();
                }
            }
            else
            {
                query = string.Empty;
            }

            return parts[0];
        }*/

        public static async Task<string> GetContent(JsonResponse json) => await json.Content.ReadAsStringAsync();

        public bool IsCacheable(Uri uri)
        {
            if (uri is UniformItemIdentifier uii)
            {
                return CACHEABLE_TYPES.HasFlag(uii.Item.ItemType) && IsCacheable(uii.Property);
            }
            else
            {
                var url = Resolver.ResolveUrl(uri);

                if (!url.StartsWith("3/movie") && !url.StartsWith("3/tv") && !url.StartsWith("3/person"))
                {
                    return false;
                }

                if (Resolver.TryGetAnnotations(uri, out var annotations) && annotations.Value.All(parser => !IsCacheable(parser.Property)))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsCacheable(Property property) => CHANGES_IGNORED.Contains(property) || ContainsChangeKey(property);
        private bool ContainsChangeKey(Property property) => Resolver.TryResolveJsonPropertyName(property, out string changeKey) && ChangeKeys.Contains(changeKey);
    }

    public interface IAsyncCollection<T> : ICollection<T>, IAsyncEnumerable<T>
    {
        Task<int> CountAsync { get; }

        bool IsReadOnly { get; }

        Task AddAsync(T item);

        Task ClearAsync();

        Task<bool> ContainsAsync(T item);

        Task CopyToAsync(T[] array, int arrayIndex);

        Task<bool> RemoveAsync(T item);
    }

    public class HashSetAsyncWrapper<T> : AsyncCollection<HashSet<T>, T> { public HashSetAsyncWrapper(Task<IEnumerable<T>> items) : base(items) { } }
    public class ListAsyncWrapper<T> : AsyncCollection<List<T>, T> { public ListAsyncWrapper(Task<IEnumerable<T>> items) : base(items) { } }

    public class AsyncCollection<TCollection, T> : IAsyncCollection<T>
        where TCollection : ICollection<T>, new()
    {
        public int Count => Collection.Count;
        public bool IsReadOnly => Collection.IsReadOnly;
        public Task<int> CountAsync => throw new NotImplementedException();

        public TCollection Collection { get; }
        public Task Load { get; }

        public AsyncCollection(Task<IEnumerable<T>> items)
        {
            Collection = new TCollection();
            Load = AddWhenReady(items);
        }

        private async Task AddWhenReady(Task<IEnumerable<T>> items)
        {
            foreach (var item in await items)
            {
                Collection.Add(item);
            }
        }

        public void Add(T item) => Collection.Add(item);

        public void Clear() => Collection.Clear();

        public bool Contains(T item) => Collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => Collection.CopyTo(array, arrayIndex);

        public bool Remove(T item) => Collection.Remove(item);

        public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Collection).GetEnumerator();

        public async Task AddAsync(T item)
        {
            await Load;
            Add(item);
        }

        public async Task ClearAsync()
        {
            await Load;
            Clear();
        }

        public async Task<bool> ContainsAsync(T item)
        {
            await Load;
            return Contains(item);
        }

        public async Task CopyToAsync(T[] array, int arrayIndex)
        {
            await Load;
            CopyTo(array, arrayIndex);
        }

        public async Task<bool> RemoveAsync(T item)
        {
            await Load;
            return Remove(item);
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Load;

            foreach (var item in this)
            {
                yield return item;
            }
        }
    }

    public class ByteConverter : IConverter<string>, IConverter<byte[]>
    {
        public Encoding Encoding { get; }

        public ByteConverter(Encoding encoding)
        {
            Encoding = encoding;
        }

        public bool TryConvert(string original, Type targetType, out object converted)
        {
            if (targetType == typeof(byte[]))
            {
                converted = Encoding.GetBytes(original);
                return true;
            }
            else
            {
                converted = default;
                return false;
            }
        }

        public bool TryConvert(byte[] original, Type targetType, out object converted)
        {
            if (targetType == typeof(string))
            {
                converted = Encoding.GetString(original);
                return true;
            }
            else
            {
                converted = default;
                return false;
            }
        }
    }

    public class TMDbClient : ControllerLink//<HttpContent>
    {
        //public HttpClient Client { get; }
        private TMDbResolver Resolver { get; }

        public static readonly Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> AutoAppend = new Dictionary<TMDbRequest, IEnumerable<TMDbRequest>>
        {
            [API.MOVIES.GET_DETAILS] = new List<TMDbRequest>
            {
                API.MOVIES.GET_CREDITS,
                API.MOVIES.GET_KEYWORDS,
                API.MOVIES.GET_RECOMMENDATIONS,
                API.MOVIES.GET_RELEASE_DATES,
                API.MOVIES.GET_VIDEOS,
                API.MOVIES.GET_WATCH_PROVIDERS
            },
            [API.TV.GET_DETAILS] = new List<TMDbRequest>
            {
                API.TV.GET_AGGREGATE_CREDITS,
                API.TV.GET_CONTENT_RATINGS,
                API.TV.GET_KEYWORDS,
                API.TV.GET_RECOMMENDATIONS,
                API.TV.GET_VIDEOS,
                API.TV.GET_WATCH_PROVIDERS
            },
            [API.TV_SEASONS.GET_DETAILS] = new List<TMDbRequest>
            {
                API.TV_SEASONS.GET_AGGREGATE_CREDITS,
            },
            [API.TV_EPISODES.GET_DETAILS] = new List<TMDbRequest>
            {
                API.TV_EPISODES.GET_CREDITS,
            },
            [API.PEOPLE.GET_DETAILS] = new List<TMDbRequest>
            {
                API.PEOPLE.GET_COMBINED_CREDITS,
            }
        };

        private static readonly ByteConverter ByteConverter = new ByteConverter(Encoding.UTF8);

        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbClient(HttpClient client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(new BufferedHandler { InnerHandler = new MockHandler() })
        {
            Resolver = resolver;

            var kvps = autoAppend ?? AutoAppend;// Enumerable.Empty<KeyValuePair<TMDbRequest, IEnumerable<TMDbRequest>>>();
            Appendable = new Dictionary<TMDbRequest, IEnumerable<TMDbRequest>>(kvps.Where(kvp => kvp.Key.SupportsAppendToResponse));
            AppendsTo = new Dictionary<TMDbRequest, TMDbRequest>();

            foreach (var kvp in kvps)
            {
                foreach (var request in kvp.Value)
                {
                    AppendsTo.Add(request, kvp.Key);
                }
            }
        }

        public override void Handle(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            //var parentCollectionWasRequested = new Lazy<bool>(() => e.Args.OfType<UniformItemIdentifier>().Any(uii => uii.Property == Movie.PARENT_COLLECTION));
            var greedyUrls = e.Args.SelectMany(arg => GetUrlsGreedy(arg.Uri)).Distinct().ToArray();
            var greedy = greedyUrls.Select(url => new RestRequestArgs(new Uri(url, UriKind.Relative)));
            var args = e.Args.Concat(greedy);

            var trie = new Dictionary<string, TrieValue>();
            //var trie1 = new Trie<string, (string Url, string Path, string Query, RestRequestArgs Arg)>();

            foreach (var arg in args)
            {
                //if (arg.Uri is UniformItemIdentifier uii && Resolver.TryGetRequest(uii, out var request))
                var url = Resolver.ResolveUrl(arg.Uri);
                var parts = url.Split('?');
                AddToTrie(trie, parts[0], (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg));

                //IEnumerable<string> keys = parts[0].Split('/');

                //if (parts.Length > 1)
                //{
                //    keys = keys.Append(parts[1]);
                //}

                //var value = (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg);
                //trie1.Add(value, keys.ToArray());
            }

            //IEnumerable<KeyValuePair<IEnumerable<string>, (string, string, string, RestRequestArgs)>> asdfasd = trie1;

            var tasks = new List<Task>();

            foreach (var kvp in trie)
            {
                var url = kvp.Key;
                var paths = kvp.Value.Select(value => value.Path.Substring(url.Length).Trim('/')).ToArray();
                var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
                var queries = kvp.Value.Select(value => value.Query).ToArray();

                var query = CombineQueries(queries);
                if (validPaths.Length > 0)
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        query += "&";
                    }

                    query += $"append_to_response={string.Join(',', validPaths)}";
                }

                if (!string.IsNullOrEmpty(query))
                {
                    url += "?" + query;
                }

                var json = GetResponse(e, url, kvp, paths);
                tasks.Add(json);
                e.Handle(kvp.Value.ToDictionary(info => info.Arg.Uri, info => Handle(json, info)));
            }

            //return Task.WhenAll(tasks);
        }

        private async Task<AnnotatedJson> GetResponse(MultiRestEventArgs e, string url, KeyValuePair<string, TrieValue> kvp, string[] paths)
        {
            var response = await Client.TrySendAsync(url);

            if (response?.IsSuccessStatusCode == false)
            {
                return null;
            }

            var json = new AnnotatedJson(await response.Content.ReadAsByteArrayAsync());

            if (e.Args.Select(arg => arg.Uri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item is Item item)
            {
                var values = Enumerable.Range(0, kvp.Value.Count).Select(i => (kvp.Value[i].Url, paths[i])).Distinct();

                foreach (var value in values)
                {
                    var uri = value.Url;
                    var path = value.Item2;
                    var temp = string.IsNullOrEmpty(path) ? new string[0] : new string[] { path };

                    json.Add(new Uri(uri, UriKind.Relative), temp);
                    //json.Add(kvp.Value[i].Arg.Uri, temp);

                    foreach (var annotation in Resolver.Annotate(uri, temp))
                    {
                        var uii = new UniformItemIdentifier(item, annotation.Key);
                        json.Add(uii, annotation.Value);
                    }
                }
            }

            return json;
        }

        private async Task<ArraySegment<byte>> Handle(Task<AnnotatedJson> jsonTask, (string Url, string Path, string Query, Movies.RestRequestArgs Arg) value)
        {
            var json = await jsonTask;

            if (json.TryGetValue(value.Arg.Uri, out var bytes))
            {
                if (value.Arg.Response == null)
                {
                    value.Arg.Handle(bytes);
                }

                if (!value.Arg.Handled && value.Arg.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(value.Arg.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                {
                    //value.Arg.Handle(new AppendedContent(json, path));
                    //value.Arg.Response.Add<string, byte[]>(ByteConverter);
                    value.Arg.Handle(converter);
                }

                return bytes;
            }

            return default;
        }

        protected override async Task GetInternalAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) //=> HandleAsync(e, next, async (IEnumerable<RestRequestArgs> e) =>
        {
            //var parentCollectionWasRequested = new Lazy<bool>(() => e.Args.OfType<UniformItemIdentifier>().Any(uii => uii.Property == Movie.PARENT_COLLECTION));
            var greedyUrls = e.Args.SelectMany(arg => GetUrlsGreedy(arg.Uri)).Distinct().ToArray();
            var greedy = greedyUrls.Select(url => new RestRequestArgs(new Uri(url, UriKind.Relative)));
            var args = e.Args.Concat(greedy);

            var trie = new Dictionary<string, List<(string Url, string Path, string Query, RestRequestArgs Arg)>>();
            //var trie1 = new Trie<string, (string Url, string Path, string Query, RestRequestArgs Arg)>();

            foreach (var arg in args)
            {
                //if (arg.Uri is UniformItemIdentifier uii && Resolver.TryGetRequest(uii, out var request))
                var url = Resolver.ResolveUrl(arg.Uri);
                var parts = url.Split('?');
                AddToTrie(trie, parts[0], (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg));

                //IEnumerable<string> keys = parts[0].Split('/');

                //if (parts.Length > 1)
                //{
                //    keys = keys.Append(parts[1]);
                //}

                //var value = (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg);
                //trie1.Add(value, keys.ToArray());
            }

            //IEnumerable<KeyValuePair<IEnumerable<string>, (string, string, string, RestRequestArgs)>> asdfasd = trie1;

            foreach (var kvp in trie)
            {
                var url = kvp.Key;
                var paths = kvp.Value.Select(value => value.Path.Substring(url.Length).Trim('/')).ToArray();
                var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
                var queries = kvp.Value.Select(value => value.Query).ToArray();

                var query = CombineQueries(queries);
                if (validPaths.Length > 0)
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        query += "&";
                    }

                    query += $"append_to_response={string.Join(',', validPaths)}";
                }

                if (!string.IsNullOrEmpty(query))
                {
                    url += "?" + query;
                }
                var response = await Client.TrySendAsync(url);

                if (response?.IsSuccessStatusCode == true)
                {
                    //var json = Parse(response.Content, validPaths);
                    var json = new AnnotatedJson(await response.Content.ReadAsByteArrayAsync());

                    if (e.Args.Select(arg => arg.Uri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item is Item item)
                    {
                        var values = Enumerable.Range(0, kvp.Value.Count).Select(i => (kvp.Value[i].Url, paths[i])).Distinct();

                        foreach (var value in values)
                        {
                            var uri = value.Url;
                            var path = value.Item2;
                            var temp = string.IsNullOrEmpty(path) ? new string[0] : new string[] { path };

                            json.Add(new Uri(uri, UriKind.Relative), temp);
                            //json.Add(kvp.Value[i].Arg.Uri, temp);

                            foreach (var annotation in Resolver.Annotate(uri, temp))
                            {
                                var uii = new UniformItemIdentifier(item, annotation.Key);
                                json.Add(uii, annotation.Value);
                            }
                        }
                    }

                    //var itr = kvp.Value.GetEnumerator();

                    e.HandleMany(json);

                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        var value = kvp.Value[i];
                        var path = paths[i];

                        if (value.Arg.Response == null && json.TryGetValue(value.Arg.Uri, out var bytes))
                        {
                            value.Arg.Handle(bytes);
                        }

                        if (!value.Arg.Handled && value.Arg.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(value.Arg.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                        {
                            //value.Arg.Handle(new AppendedContent(json, path));
                            //value.Arg.Response.Add<string, byte[]>(ByteConverter);
                            value.Arg.Handle(converter);
                        }
                    }
                }
            }
        }

        private IEnumerable<string> GetUrlsGreedy(Uri uri)
        {
            if (Resolver.TryGetRequest(uri, out var request) && Resolver.TryResolve(uri, out _, out var args))
            {
                var original = request;

                if (AppendsTo.TryGetValue(request, out var temp))
                {
                    yield return Resolver.Resolve(request = temp, args);
                }

                if (Appendable.TryGetValue(request, out var appendable))
                {
                    foreach (var url in appendable.Where(req => req != original).Select(req => Resolver.Resolve(req, args)))
                    {
                        yield return url;
                    }
                }
            }
        }

        private void AddToTrie<TValue>(Dictionary<string, List<TValue>> trie, string key, TValue value)
        {
            foreach (var kvp in trie)
            {
                if (key.StartsWith(kvp.Key))
                {
                    kvp.Value.Add(value);
                    return;
                }
            }

            var list = new List<TValue> { value };
            var kvps = trie.Where(kvp => kvp.Key.StartsWith(key)).ToArray();

            trie[key] = list;

            foreach (var kvp in kvps)
            {
                trie.Remove(kvp.Key);
                list.AddRange(kvp.Value);
            }
        }

        private static async Task<LazyJson> Parse(HttpContent response, IEnumerable<string> properties)
        {
            //var response = await responseTask;
            return new LazyJson(await response.ReadAsByteArrayAsync(), properties);
        }

        private static async Task<LazyJson> Parse(Task<HttpResponseMessage> responseTask, IEnumerable<string> properties)
        {
            var response = await responseTask;
            return response?.IsSuccessStatusCode == true ? new LazyJson(await response.Content.ReadAsByteArrayAsync(), properties) : null;
        }

        private static string CombineQueries(IEnumerable<string> queries)
        {
            var result = new Dictionary<string, string>();

            foreach (var query in queries)
            {
                var args = query.Split('&');

                foreach (var arg in args)
                {
                    var kvp = arg.Split('=');

                    if (kvp.Length == 2)
                    {
                        result.TryAdd(kvp[0], kvp[1]);
                    }
                }
            }

            return string.Join('&', result.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
    }

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
