using Movies.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        public static HttpContent[] Request(IEnumerable<TMDbRequest> requests, params object[] parameters)
        {
            //return Enumerable.Repeat(Task.FromResult<ArraySegment<byte>>(Encoding.UTF8.GetBytes("{}")), requests.Count()).ToArray();

            var result = new HttpContent[requests.Count()];
            var urls = new object[result.Length];
            var itr = requests.GetEnumerator();

            for (int i = 0; itr.MoveNext(); i++)
            {
                var request = itr.Current;
                result[i] = null;

                if (!request.SupportsAppendToResponse)
                {
                    continue;
                }

                var appended = new List<Uri>();
                var baseUrl = new Uri(request.GetURL(), UriKind.Relative);
                var basePath = baseUrl.OriginalString.Split('?').First();

                var itr1 = requests.GetEnumerator();
                for (int j = 0; itr1.MoveNext(); j++)
                {
                    var append = itr1.Current;
                    var url = append.GetURL();

                    if (request != append && url.StartsWith(basePath))
                    {
                        appended.Add(new Uri(url.Replace(basePath, string.Empty).TrimStart('/'), UriKind.Relative));
                        urls[j] = i;
                    }
                }

                //var atr = appended.Select(other => other.Endpoint.Replace(request.Endpoint, string.Empty));

                var query = CombineQueries(appended.Prepend(baseUrl));
                var paths = appended.Select(uri => uri.OriginalString.Split('?').First()).ToArray();
                var atrQuery = $"append_to_response={string.Join(',', paths)}";

                var endpoint = urls[i] = TMDB.BuildApiCall(basePath, query, atrQuery);

                var fullUrl = string.Format(endpoint as string, parameters);
                urls[i] = Parse(WebClient.TrySendAsync(fullUrl), paths);
            }

            itr = requests.GetEnumerator();

            for (int i = 0; itr.MoveNext(); i++)
            {
                var request = itr.Current;
                var index = i;

                //IJsonParser<ArraySegment<byte>> parser = null;// new JsonNodeParser<ArraySegment<byte>>();
                string property = "";

                if (urls[i] is int pointer)
                {
                    index = pointer;
                    //var property = request.GetURL().Split('?').First().Replace((urls[index] as string).Split('?').First(), string.Empty).TrimStart('/');
                    property = request.Endpoint.Replace(requests.ElementAt(index).Endpoint, string.Empty).TrimStart('/');
                    //if (property == "watch/providers")
                    //parser = new JsonPropertyParser<ArraySegment<byte>>(property);
                }

                var response = urls[index] as Task<LazyJson>;

                if (response == null)
                {
                    var url = string.Format(urls[index] as string ?? request.GetURL(), parameters);
                    //urls[index] = response = ToBytes(WebClient.TryGetAsync(url));
                }

                result[i] = new AppendedContent(response, property);
            }

            //return Enumerable.Repeat(Task.FromResult(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes("{}"))), requests.Count()).ToArray();

            return result;
        }

        private static async Task<LazyJson> Parse(Task<System.Net.Http.HttpResponseMessage> response, IEnumerable<string> properties) => new LazyJson(await (await response).Content.ReadAsByteArrayAsync(), properties);

        public static string CombineQueries(IEnumerable<Uri> endpoints)
        {
            var parameters = new Dictionary<string, string>();

            foreach (var endpoint in endpoints)
            {
                //var uri = new Uri(call.Endpoint, UriKind.RelativeOrAbsolute);
                var parts = endpoint.OriginalString.Split('?');
                var path = parts.ElementAtOrDefault(0) ?? string.Empty;
                var query = parts.ElementAtOrDefault(1) ?? string.Empty;

                foreach (var parameter in query.Split('&'))
                {
                    var temp = parameter.Split('=');

                    if (temp.Length == 2)
                    {
                        parameters[temp[0]] = temp[1];
                    }
                }
            }

            return string.Join('&', parameters.Select(kvp => string.Join('=', kvp.Key, kvp.Value)));
        }

        private static async Task<ArraySegment<byte>> GetAppendedJson(Task<LazyJson> jsonTask, string property)
        {
            var json = await jsonTask;

            await json.Semaphore.WaitAsync();
            var result = json.TryGetValue(property, out var value) ? value : Encoding.UTF8.GetBytes("{}");
            json.Semaphore.Release();

            return result;
        }

        public static bool TryGetParameters(Item item, out List<object> parameters)
        {
            parameters = new List<object>();

            if (item is TVSeason season)
            {
                parameters.Add(season.SeasonNumber);
                item = season.TVShow;
            }
            else if (item is TVEpisode episode)
            {
                parameters.Add(episode.Season.SeasonNumber, episode.EpisodeNumber);
                item = episode.Season?.TVShow;
            }

            if (item == null || !TryGetID(item, out var id))
            {
                return false;
            }

            parameters.Insert(0, id);
            return true;
        }
    }

    public class ItemProperties
    {
        public IReadOnlyDictionary<TMDbRequest, List<Parser>> Info { get; }
        public IReadOnlyDictionary<Property, TMDbRequest> PropertyLookup { get; }

        public ItemInfoCache Cache { get; set; }
        public HashSet<string> ChangeKeys { get; set; }
        public Task ChangeKeysLoaded { get; set; }

        private static Dictionary<Property, string> CHANGE_KEY_PROPERTY_MAP = new Dictionary<Property, string>
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

        // These are properties that TMDb does not monitor for changes. As a result they won't be cached across sessions, and will request data once EVERY session
        public static HashSet<Property> NO_CHANGE_KEY = new HashSet<Property>
        {
            Media.ORIGINAL_LANGUAGE,
            Media.RECOMMENDED,
            Movie.PARENT_COLLECTION,
            TVShow.SEASONS, // There is a change key for this but I'm not tracking it at this point
            TVShow.NETWORKS,
            Person.GENDER,
            Person.CREDITS,
            TMDB.POPULARITY
        };

        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType UNCACHED_TYPES = ~(ItemType.Movie | ItemType.TVShow | ItemType.Person);

        private List<TMDbRequest> SupportsAppendToResponse = new List<TMDbRequest>();
        private Dictionary<TMDbRequest, List<Property>> AllRequests { get; }
        private Dictionary<Property, string> ChangeKeyLookup { get; }
        private Dictionary<TMDbRequest, List<Parser>> Uncacheable { get; }

        private SemaphoreSlim CacheSemaphore = new SemaphoreSlim(1, 1);

        public ItemProperties(Dictionary<TMDbRequest, List<Parser>> requests)
        {
            var info = new Dictionary<TMDbRequest, List<Parser>>();
            var propertyLookup = new Dictionary<Property, TMDbRequest>();

            foreach (var kvp in requests)
            {
                if (kvp.Key.SupportsAppendToResponse)
                {
                    SupportsAppendToResponse.Add(kvp.Key);
                }

                foreach (var parser in kvp.Value)
                {
                    propertyLookup[parser.Property] = kvp.Key;
                }

                info.Add(kvp);
            }

            Info = info;
            PropertyLookup = propertyLookup;
            //AllRequests = Info.Keys.ToList();
            AllRequests = new Dictionary<TMDbRequest, List<Property>>(Info.Select(kvp => new KeyValuePair<TMDbRequest, List<Property>>(kvp.Key, kvp.Value.Select(parser => parser.Property).ToList())));
            ChangeKeyLookup = new Dictionary<Property, string>();
            Uncacheable = new Dictionary<TMDbRequest, List<Parser>>(Info.Select(kvp => new KeyValuePair<TMDbRequest, List<Parser>>(kvp.Key, kvp.Value.Where(parser => NO_CHANGE_KEY.Contains(parser.Property)).ToList())));

            foreach (var parser in Info.SelectMany(kvp => kvp.Value))
            {
                var property = parser.Property;

                if (CHANGE_KEY_PROPERTY_MAP.TryGetValue(property, out var changeKey) || (changeKey = ((parser as ParserWrapper)?.JsonParser as JsonPropertyParser)?.Property) != null)
                {
                    ChangeKeyLookup[property] = changeKey;
                    //changeKey = jpp.Property;
                }
            }
        }

        public bool HasProperty(Property property)
        {
            for (; property != null; property = property.Parent)
            {
                if (PropertyLookup.ContainsKey(property))
                {
                    return true;
                }
            }

            return false;
        }

        public void HandleRequests(Item item, PropertyDictionary properties, TMDB tmdb)
        {
            if (TMDB.TryGetParameters(item, out var parameters))
            {
                var handler = new RequestHandler(this, item)
                {
                    Parameters = parameters.ToArray()
                };
                properties.PropertyAdded += handler.PropertyRequested;
            }
        }

        public class RequestHandler
        {
            public ItemProperties Context { get; }
            public Item Item { get; private set; }
            public object[] Parameters { get; set; }

            private Dictionary<string, HttpContent> Responses = new Dictionary<string, HttpContent>();
            private SemaphoreSlim ResponseCacheSemaphore = new SemaphoreSlim(1, 1);

            public RequestHandler(ItemProperties context, Item item)
            {
                Context = context;
                Item = item;
            }

            public void PropertyRequested(object sender, PropertyEventArgs e) => PropertyRequested((PropertyDictionary)sender, e.Properties);

            public void PropertyRequested(PropertyDictionary dict, IEnumerable<Property> properties)
            {
                var requests = new Dictionary<TMDbRequest, List<Property>>();

                foreach (var property in properties)
                {
                    if (Context.PropertyLookup.TryGetValue(property, out var request))
                    {
                        // If this property can't be retrieved from the cache (because TMDb doesn't monitor it for changes), might as well request all new data
                        if (!IsCacheValid(property))
                        {
                            PropertyRequested(dict, Context.AllRequests);
                            return;
                        }
                        if (!requests.TryGetValue(request, out var list))
                        {
                            requests[request] = list = new List<Property>();
                        }

                        list.Add(property);
                    }
                }

                if (requests.Count > 0)
                {
                    PropertyRequested(dict, requests);
                }
            }

            public void PropertyRequested(PropertyDictionary dict, Dictionary<TMDbRequest, List<Property>> requests) => CacheInMemory(dict, requests.Keys, MakeRequests(dict, requests), requests.Values.SelectMany().Contains(Movie.PARENT_COLLECTION));

            private void CacheInMemory(PropertyDictionary dict, IEnumerable<TMDbRequest> requests, Task<List<HttpContent>> responses, bool parentCollectionWasRequested = false)
            {
                var itr = requests.GetEnumerator();

                for (int i = 0; itr.MoveNext() && Context.Info.TryGetValue(itr.Current, out var parsers); i++)
                {
                    var request = itr.Current;
                    var response = GetResponse(responses, i);

                    if (request is PagedTMDbRequest pagedRequest)
                    {
                        parsers = ReplacePagedParsers(pagedRequest, parsers);
                    }

                    foreach (var parser in parsers)
                    {
                        CacheInMemory(dict, parser, GetBytes(response), parentCollectionWasRequested);
                    }
                }
            }

            private void CacheInMemory(PropertyDictionary dict, Parser parser, Task<ArraySegment<byte>> response, bool parentCollectionWasRequested = false)
            {
                if (parser.Property == TVShow.SEASONS)
                {
                    dict.Add(TVShow.SEASONS, GetTVItems(response, "seasons", (JsonNode json, out TVSeason season) => TMDB.TryParseTVSeason(json, (TVShow)Item, out season)));
                }
                else if (parser.Property == TVSeason.EPISODES)
                {
                    if (Item is TVSeason season)
                    {
                        dict.Add(TVSeason.EPISODES, GetTVItems(response, "episodes", (JsonNode json, out TVEpisode episode) => TMDB.TryParseTVEpisode(json, season, out episode)));
                    }
                }
                // Avoid parsing a movie's parent collection unless it was specifically requested because it requires an additional api call
                else if (parser.Property != Movie.PARENT_COLLECTION || parentCollectionWasRequested)
                {
                    dict.Add(parser.GetPair(response));
                }
            }

            private async Task<ArraySegment<byte>> GetBytes(Task<HttpContent> content) => await (await content).ReadAsByteArrayAsync();

            private async Task<T> GetResponse<T>(Task<List<T>> responses, int index) => (await responses)[index];

            private string GetFullURL(TMDbRequest request) => string.Format(request.GetURL(), Parameters);

            private async Task<List<HttpContent>> MakeRequests(PropertyDictionary dict, Dictionary<TMDbRequest, List<Property>> properties)
            {
                // Virtually guaranteed to execute syncronously
                await Context.ChangeKeysLoaded;
                //await Context.CacheSemaphore.WaitAsync();

                var requests = new Dictionary<TMDbRequest, int>();
                var cached = new Dictionary<TMDbRequest, int>();

                var responses = Enumerable.Repeat<HttpContent>(null, properties.Count).ToList();// new List<Task<JsonNode>>();

                var itr = properties.GetEnumerator();

                for (int i = 0; itr.MoveNext(); i++)
                {
                    var request = itr.Current.Key;

                    // Ok to remove here because the result will be cached in the associated PropertyDictionary
                    if (Responses.TryGetValue(GetFullURL(request), out var content))
                    {
                        responses[i] = content;
                    }
                    else
                    {
                        foreach (var property in itr.Current.Value)
                        {
                            var list = Context.Cache != null && IsCacheValid(property) ? cached : requests;

                            if (!list.ContainsKey(request))
                            {
                                list.Add(request, i);
                            }
                        }
                    }
                }

                TryGetAppendToResponse(requests);

                foreach (var kvp in cached)
                {
                    var request = kvp.Key;

                    // We have to make the request again anyway, get the newer data
                    if (requests.ContainsKey(request))
                    {
                        continue;
                    }

                    var response = await Context.Cache.TryGetValueAsync(GetFullURL(request));

                    // not cached, we'll have to make the request
                    if (response == null)
                    {
                        requests.Add(request, kvp.Value);
                    }
                    else
                    {
                        //var json = JsonNode.Parse(response.Json);
                        //Print.Log(json);
                        //responses[kvp.Value] = Task.FromResult<ArraySegment<byte>>(response.Json);
                        responses[kvp.Value] = response.Content;
                    }
                }

                if (requests.Count > 0)
                {
                    IEnumerable<TMDbRequest> requesting = requests.Keys;
                    // keywords,recommendations,watch/providers,release_dates,credits
                    // request in this order for better performance
                    //requesting = new List<TMDbRequest> { API.MOVIES.GET_DETAILS, API.MOVIES.GET_WATCH_PROVIDERS, API.MOVIES.GET_RELEASE_DATES };
                    requesting = Context.AllRequests.Keys;
                    // This method is pretty inefficient
                    var requested = TMDB.Request(requesting, Parameters);
                    //var requested = await Task.Run(() => TMDB.Request(requesting, Parameters));

                    //var extraRequests = new List<TMDbRequest>();
                    //var extraResponses = new List<Task<ArraySegment<byte>>>();

                    //var extraRequests = new List<TMDbRequest>();
                    //var extraResponses = new List<HttpContent>();

                    var itr1 = requesting.GetEnumerator();
                    for (int i = 0; itr1.MoveNext(); i++)
                    {
                        var request = itr1.Current;
                        var response = requested[i];

                        if (requests.TryGetValue(request, out var index) && index != -1)
                        {
                            responses[index] = response;
                        }
                        /*else if (Context.Uncacheable.TryGetValue(request, out var parsers))
                        {
                            foreach (var parser in parsers)
                            {
                                CacheInMemory(dict, parser, GetBytes(Task.FromResult(response)));
                            }
                        }*/
                        else
                        {
                            //extraRequests.Add(request);
                            //extraResponses.Add(response);
                        }

                        await ResponseCacheSemaphore.WaitAsync();

                        try
                        {
                            Responses.TryAdd(GetFullURL(request), response);
                        }
                        finally
                        {
                            ResponseCacheSemaphore.Release();
                        }
                    }

                    //CacheInMemory(dict, extraRequests, Task.FromResult(extraResponses));

                    if (Context.Cache != null && Item != null && Parameters.Length > 0 && Parameters[0] is int id)
                    {
                        await Task.WhenAll(requesting.Zip(requested, (request, response) => CachePersistentBatched(request, response)));
                    }
                }

                //Context.CacheSemaphore.Release();
                //return Enumerable.Repeat<Task<ArraySegment<byte>>>(Task.FromResult(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes("{}"))), properties.Count).ToList();
                return responses;
            }

            private void TryGetAppendToResponse(Dictionary<TMDbRequest, int> requests)
            {
                var atr = new int[Context.SupportsAppendToResponse.Count];

                // Check if we can make fewer requests by appending to some (not already included) request
                foreach (var request in requests.Keys.Where(request => !request.SupportsAppendToResponse))
                {
                    int index = Context.SupportsAppendToResponse.FindIndex(temp => request.Endpoint.StartsWith(temp.Endpoint));

                    if (index != -1)
                    {
                        atr[index]++;
                    }
                }

                for (int i = 0; i < Context.SupportsAppendToResponse.Count; i++)
                {
                    var request = Context.SupportsAppendToResponse[i];

                    if (atr[i] > 1 && !requests.ContainsKey(request))
                    {
                        requests.Add(request, -1);
                    }
                }
            }

            public bool IsCacheValid(Property property) => CHANGES_IGNORED.Contains(property) || (Context.ChangeKeyLookup.TryGetValue(property, out var changeKey) && Context.ChangeKeys.Contains(changeKey));

            private const int BATCH_DELAY = 1000;
            private static List<(string URL, HttpContent Response, ItemType Type, int ID)> ResponseBatch = new List<(string URL, HttpContent Response, ItemType Type, int ID)>();
            private static SemaphoreSlim BatchResponseSemaphore = new SemaphoreSlim(1, 1);
            private static CancellationTokenSource CancelBatch;
            private static Task BatchCache;

            private async Task CachePersistentBatched(TMDbRequest request, HttpContent content)
            {
                if (Context.Cache == null || Item == null || Parameters.Length == 0 || Parameters[0] is int id == false)
                {
                    return;
                }

                var parsers = Context.Info.TryGetValue(request, out var temp) ? temp : null;
                var invalid = parsers?.Where(parser => NO_CHANGE_KEY.Contains(parser.Property)).ToList();

                // We can't cache this request (probably no change key)
                if (UNCACHED_TYPES.HasFlag(Item.ItemType) || invalid?.Count < parsers?.Count == false)
                {
                    return;
                }

                await BatchResponseSemaphore.WaitAsync();

                try
                {
                    ResponseBatch.Add((GetFullURL(request), content, Item.ItemType, id));
                }
                finally
                {
                    BatchResponseSemaphore.Release();
                }

                CancelBatch?.Cancel();
                CancelBatch = new CancellationTokenSource();

                BatchCache = CachePersistentDelayed(Context.Cache, CancelBatch.Token);
            }

            private static async Task CachePersistentDelayed(ItemInfoCache cache, CancellationToken cancellationToken = default)
            {
                await Task.Delay(BATCH_DELAY, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    CancelBatch?.Cancel();

                    await BatchResponseSemaphore.WaitAsync();
                    (string URL, HttpContent Response, ItemType Type, int ID)[] rows = new (string, HttpContent, ItemType, int)[0];

                    try
                    {
                        rows = ResponseBatch.ToArray();// await Task.Run(() => ResponseBatch.Select(info => new KeyValuePair<string, Task<JsonResponse>>(info.URL, GetJson(info.Response))).ToList());
                        ResponseBatch.Clear();
                    }
                    finally
                    {
                        BatchResponseSemaphore.Release();
                    }

                    foreach (var row in rows)
                    {
                        await cache.AddAsync(row.Type, row.ID, row.URL, GetJson(row.Response));
                    }
                }
            }

            private static async Task<JsonResponse> GetJson(HttpContent content) => new JsonResponse(await content.ReadAsStringAsync());

            private async Task<IEnumerable<T>> GetTVItems<T>(Task<ArraySegment<byte>> json, string property, AsyncEnumerable.TryParseFunc<JsonNode, T> parse) => TMDB.TryParseCollection(JsonNode.Parse(await json), new JsonPropertyParser<IEnumerable<JsonNode>>(property), out var result, new JsonNodeParser<T>(parse)) ? result : null;

            private List<Parser> ReplacePagedParsers(PagedTMDbRequest request, IEnumerable<Parser> parsers)
            {
                var result = new List<Parser>();

                foreach (var parser in parsers)
                {
                    if (parser.Property is Property<IAsyncEnumerable<Item>> property)
                    {
                        var pageParser = ParserToJsonParser<IAsyncEnumerable<Item>>(parser);

                        var pagedRequest = new ParameterizedPagedRequest(request, Parameters);
                        var pagedParser = new TMDB.PagedParser<Item>(pagedRequest, pageParser);
                        var listParser = new Parser<IAsyncEnumerable<Item>>(property, pagedParser);

                        result.Add(listParser);
                    }
                    else
                    {
                        result.Add(parser);
                    }
                }

                return result;
            }
        }

        private static IJsonParser<T> ParserToJsonParser<T>(Parser parser) => new JsonNodeParser<T>((JsonNode json, out T items) =>
        {
            if (parser.TryGetValue(json, out var pair) && pair.Value is Task<T> task && task.IsCompletedSuccessfully)
            {
                items = task.Result;
                return true;
            }

            items = default;
            return true;
        });

        private static async IAsyncEnumerable<T> Concat<T>(IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            await foreach (var item in first)
            {
                yield return item;
            }

            await foreach (var item in first)
            {
                yield return item;
            }
        }
    }
}