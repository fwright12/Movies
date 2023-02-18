using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;

namespace Movies
{
    public class TMDbResolver
    {
        private Dictionary<ItemType, Dictionary<Property, Parser>> Index { get; }
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
        private IConverter<object> Converter => DefaultConverter<object>.Instance;

        public TMDbResolver(Dictionary<ItemType, ItemProperties> properties)
        {
            Properties = properties;

            Index = new Dictionary<ItemType, Dictionary<Property, Parser>>();

            foreach (var kvp in Properties)
            {
                var index = new Dictionary<Property, Parser>();

                foreach (var parser in kvp.Value.Info.Values.SelectMany())
                {
                    index[parser.Property] = parser;
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

    public class DefaultConverter<T> : IConverter<T>
    {
        public readonly static DefaultConverter<T> Instance = new DefaultConverter<T>();

        private DefaultConverter() { }

        public bool TryConvert<TTarget>(T source, out TTarget target)
        {
            if (source is TTarget t)
            {
                target = t;
                return true;
            }
            else
            {
                target = default;
                return false;
            }
        }
    }

    public class TMDbResources : ControllerLink
    {
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
            [API.PEOPLE.GET_DETAILS] = new List<TMDbRequest>
            {
                API.PEOPLE.GET_COMBINED_CREDITS,
            }
        };

        public Controller Primary { get; }
        public Controller Greedy { get; }
        public TMDbResolver Resolver { get; }

        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        private void TMDbResources1(IJsonCache cache, IAsyncCollection<string> changeKeys = null)
        {
            //Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            var local = new TMDbLocalResources(cache, Resolver)
            {
                ChangeKeys = changeKeys
            };
            var remote = new TMDbClient(TMDB.WebClient, Resolver);
            var controller = new Controller().SetNext(local).SetNext(remote);
            //var greedy = new GreedyTMDbResources(controller, Resolver, AutoAppend);

            //Controller = new Controller().SetNext(local).SetNext(greedy);
            //new Controller().SetNext(Controller.GetAsync);
        }

        public TMDbResources(ControllerLink local, ControllerLink remote, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new Controller().SetNext(local), new Controller().SetNext(local).SetNext(remote), resolver, autoAppend) { }
        public TMDbResources(Controller primary, Controller greedy, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null)
        {
            Primary = primary;
            Greedy = greedy;
            Resolver = resolver;

            var kvps = autoAppend ?? Enumerable.Empty<KeyValuePair<TMDbRequest, IEnumerable<TMDbRequest>>>();
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

        protected override Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next, async (args) =>
        {
            await Primary.Get(args);

            var unhandled = WhereUnhandled(args).ToArray();
            var greedyUrls = unhandled.SelectMany(arg => GetUrlsGreedy(arg.Uri)).Distinct().ToArray();
            var greedy = greedyUrls.Select(url => new RestRequestArgs(new Uri(url, UriKind.Relative)));

            await Greedy.Get(unhandled.Concat(greedy).ToArray());
        });

        private IEnumerable<string> GetUrlsGreedy(Uri uri)
        {
            if (Resolver.TryGetRequest(uri, out var request) && Resolver.TryResolve(uri, out _, out var args))
            {
                if (AppendsTo.TryGetValue(request, out var temp))
                {
                    yield return Resolver.Resolve(request = temp, args);
                }

                if (Appendable.TryGetValue(request, out var appendable))
                {
                    foreach (var url in appendable.Select(req => Resolver.Resolve(req, args)))
                    {
                        yield return url;
                    }
                }
            }
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

        protected override Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next, async (arg) =>
        {
            if (arg.Uri is UniformItemIdentifier uii && !IsCacheable(uii))
            {
                return;
            }

            var url = Resolver.ResolveUrl(arg.Uri);
            var response = await Cache.TryGetValueAsync(url);

            if (response != null)
            {
                arg.Handle(response.Content);
            }
        });

        public async Task HandleAsync(RestRequestArgs arg)
        {
            if (arg.Uri is UniformItemIdentifier uii && !IsCacheable(uii)) // && uii.Item.TryGetID(TMDB.ID, out var id) && Resolver.TryResolve(uii, out var url) && arg.Response is HttpContent content)// && Converter.TryConvert(resource, out var content))
            {
                return;
            }

            var url = Resolver.ResolveUrl(arg.Uri);

            if (arg.Response.TryGetRepresentation<HttpContent>(out var content))
            {
                await Cache.AddAsync(url, new JsonResponse(content));
            }
        }

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

        public bool IsCacheable(UniformItemIdentifier uii) => CACHEABLE_TYPES.HasFlag(uii.Item.ItemType) && (CHANGES_IGNORED.Contains(uii.Property) || ContainsChangeKey(uii.Property));

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

    public class TMDbClient : ControllerLink//<HttpContent>
    {
        public HttpClient Client { get; }
        private TMDbResolver Resolver { get; }

        public TMDbClient(HttpClient client, TMDbResolver resolver)
        {
            Client = client;
            Resolver = resolver;
        }

        protected override Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next, async (IEnumerable<RestRequestArgs> e) =>
        {
            var trie = new Dictionary<string, List<(string Path, string Query, RestRequestArgs Arg)>>();

            foreach (var arg in e)
            {
                //if (arg.Uri is UniformItemIdentifier uii && Resolver.TryGetRequest(uii, out var request))
                var url = Resolver.ResolveUrl(arg.Uri);
                var parts = url.Split('?');
                AddToTrie(trie, parts[0], (parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg));
            }

            foreach (var kvp in trie)
            {
                var url = kvp.Key;
                var paths = kvp.Value.Select(value => value.Path.Substring(url.Length).Trim('/')).ToArray();
                var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
                IEnumerable<string> queries = kvp.Value.Select(value => value.Query).ToArray();

                if (validPaths.Length > 0)
                {
                    queries = queries.Append($"append_to_response={string.Join(',', validPaths)}");
                }
                var query = CombineQueries(queries);
                if (!string.IsNullOrEmpty(query))
                {
                    url += "?" + query;
                }
                var response = await Client.TrySendAsync(url);

                if (response?.IsSuccessStatusCode == true)
                {
                    var json = Parse(response.Content, validPaths);
                    var itr = kvp.Value.GetEnumerator();

                    for (int i = 0; i < paths.Length && itr.MoveNext(); i++)
                    {
                        var value = itr.Current;
                        var path = paths[i];

                        value.Arg.Handle(new AppendedContent(json, path));
                    }
                }
            }
        });

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
}
