using Movies.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        public static Task<ArraySegment<byte>>[] Request(IEnumerable<TMDbRequest> requests, params object[] parameters)
        {
            //return Enumerable.Repeat(GetAppendedJson(WebClient.TryGetAsync(string.Format(requests.First().GetURL(), parameters)), new JsonNodeParser<JsonNode>()), requests.Count()).ToArray();

            var result = new Task<ArraySegment<byte>>[requests.Count()];
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
                var atrQuery = $"append_to_response={string.Join(',', appended.Select(uri => uri.OriginalString.Split('?').First()))}";

                urls[i] = BuildApiCall(basePath, query, atrQuery);
            }

            itr = requests.GetEnumerator();

            for (int i = 0; itr.MoveNext(); i++)
            {
                var request = itr.Current;
                var index = i;

                IJsonParser<ArraySegment<byte>> parser = new JsonNodeParser<ArraySegment<byte>>();

                if (urls[i] is int pointer)
                {
                    index = pointer;
                    //var property = request.GetURL().Split('?').First().Replace((urls[index] as string).Split('?').First(), string.Empty).TrimStart('/');
                    var property = request.Endpoint.Replace(requests.ElementAt(index).Endpoint, string.Empty).TrimStart('/');
                    //if (property == "watch/providers")
                    parser = new JsonPropertyParser<ArraySegment<byte>>(property);
                }

                var response = urls[index] as Task<byte[]>;

                if (response == null)
                {
                    var url = string.Format(urls[index] as string ?? request.GetURL(), parameters);
                    urls[index] = response = ToBytes(WebClient.TryGetAsync(url));
                }

                result[i] = GetAppendedJson(response, parser);
            }

            //return Enumerable.Repeat(Task.FromResult(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes("{}"))), requests.Count()).ToArray();

            return result;
        }

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

        private static async Task<byte[]> ToBytes(Task<System.Net.Http.HttpResponseMessage> response) => await (await response).Content.ReadAsByteArrayAsync();

        private static async Task<ArraySegment<byte>> GetAppendedJson(Task<byte[]> bytes, IJsonParser<ArraySegment<byte>> parser)
        {
            //JsonDocument.Parse(await (await response).Content.ReadAsStreamAsync());
            //return parse.TryGetValue(JsonNode.Parse("{}"), out var empty) ? empty : null;
            //var json = await JsonDocument.ParseAsync(await (await response).Content.ReadAsStreamAsync());
            //parser.TryGetValue(await bytes, out var temp);
            //return System.Text.Encoding.UTF8.GetBytes("{}");
            //var content = await (await response).Content.ReadAsByteArrayAsync();
            //foreach(var c in content){ }
            return parser.TryGetValue(await bytes, out var value) ? value : null;
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
            [TVShow.LAST_AIR_DATE] = "status",
            [TVShow.SEASONS] = "season",
            [Person.PROFILE_PATH] = "images",
        };

        // These are properties that TMDb does not monitor for changes. As a result they won't be cached across sessions, and will request data once EVERY session
        public static HashSet<Property> NO_CHANGE_KEY = new HashSet<Property>
        {
            Media.ORIGINAL_LANGUAGE,
            Media.RECOMMENDED,
            Movie.PARENT_COLLECTION,
            TVShow.FIRST_AIR_DATE,
            //TVShow.LAST_AIR_DATE,
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

        private List<TMDbRequest> SupportsAppendToResponse = new List<TMDbRequest>();
        private Dictionary<TMDbRequest, List<Property>> AllRequests { get; }
        private Dictionary<Property, string> ChangeKeyLookup { get; }

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

            private void CacheInMemory(PropertyDictionary dict, IEnumerable<TMDbRequest> requests, Task<List<Task<ArraySegment<byte>>>> responses, bool parentCollectionWasRequested = false)
            {
                var itr = requests.GetEnumerator();

                for (int i = 0; itr.MoveNext(); i++)
                {
                    var request = itr.Current;

                    if (!Context.Info.TryGetValue(request, out var parsers))
                    {
                        continue;
                    }

                    //var response = responses[i];
                    var response = GetResponse(responses, i);

                    if (request is PagedTMDbRequest pagedRequest)
                    {
                        parsers = ReplacePagedParsers(pagedRequest, parsers);
                    }

                    foreach (var parser in parsers)
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
                }
            }

            private async Task<T> GetResponse<T>(Task<List<Task<T>>> responses, int index) => await (await responses)[index];

            private string GetFullURL(TMDbRequest request) => string.Format(request.GetURL(), Parameters);

            private async Task<List<Task<ArraySegment<byte>>>> MakeRequests(PropertyDictionary dict, Dictionary<TMDbRequest, List<Property>> properties)
            {
                // Virtually guaranteed to execute syncronously
                await Context.ChangeKeysLoaded;
                //await Context.CacheSemaphore.WaitAsync();

                var requests = new Dictionary<TMDbRequest, int>();
                var cached = new Dictionary<TMDbRequest, int>();

                var itr = properties.GetEnumerator();

                for (int i = 0; itr.MoveNext(); i++)
                {
                    var request = itr.Current.Key;

                    foreach (var property in itr.Current.Value)
                    {
                        var list = Context.Cache != null && IsCacheValid(property) ? cached : requests;

                        if (!list.ContainsKey(request))
                        {
                            list.Add(request, i);
                        }
                    }
                }

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

                var responses = Enumerable.Repeat<Task<ArraySegment<byte>>>(null, properties.Count).ToList();// new List<Task<JsonNode>>();
                
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
                        //responses[kvp.Value] = Task.Run(() => JsonNode.Parse(response.Json));
                    }
                }
                
                if (requests.Count > 0)
                {
                    IEnumerable<TMDbRequest> requesting = requests.Keys;
                    requesting = Context.AllRequests.Keys;
                    // This method is pretty inefficient
                    var requested = await Task.Run(() => TMDB.Request(requesting, Parameters));

                    var extraRequests = new List<TMDbRequest>();
                    var extraResponses = new List<Task<ArraySegment<byte>>>();

                    var itr1 = requesting.GetEnumerator();
                    for (int i = 0; itr1.MoveNext(); i++)
                    {
                        var request = itr1.Current;

                        if (requests.TryGetValue(request, out var index) && index != -1)
                        {
                            responses[index] = requested[i];
                        }
                        else
                        {
                            extraRequests.Add(request);
                            extraResponses.Add(requested[i]);
                        }
                    }

                    CacheInMemory(dict, extraRequests, Task.FromResult(extraResponses));

                    if (Context.Cache != null && Item != null && Parameters.Length > 0 && Parameters[0] is int id)
                    {
                        //await CachePersistent(Context.Cache, Item.ItemType, id, requesting, requested);
                    }
                }

                //Context.CacheSemaphore.Release();
                //return Enumerable.Repeat<Task<ArraySegment<byte>>>(Task.FromResult(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes("{}"))), properties.Count).ToList();
                return responses;
            }

            public bool IsCacheValid(Property property) => CHANGES_IGNORED.Contains(property) || (Context.ChangeKeyLookup.TryGetValue(property, out var changeKey) && Context.ChangeKeys.Contains(changeKey));

            private static int IndexOf<T>(IEnumerable<T> source, T value)
            {
                var itr = source.GetEnumerator();

                for (int i = 0; itr.MoveNext(); i++)
                {
                    if (Equals(itr.Current, value))
                    {
                        return i;
                    }
                }

                return -1;
            }

            private async Task CachePersistent(ItemInfoCache cache, ItemType type, int id, IEnumerable<TMDbRequest> requests1, IEnumerable<Task<JsonNode>> responses)
            {
                var temp = requests1.Zip(responses, (request, response) =>
                {
                    var parsers = Context.Info.TryGetValue(request, out var temp) ? temp : null;
                    var invalid = parsers?.Where(parser => NO_CHANGE_KEY.Contains(parser.Property)).ToList();
                    return (request, response, parsers, invalid);
                });
                var info = temp.Where(item => item.invalid?.Count < item.parsers?.Count).ToList();
                var remaining = info.Select(item => item.response).ToList();

                while (remaining.Count > 0)
                {
                    var ready = await Task.WhenAny(remaining);
                    int index = remaining.IndexOf(ready);

                    if (index != -1)
                    {
                        var request = info[index].request;
                        var invalid = info[index].invalid;

                        var url = GetFullURL(request);
                        await cache.Expire(url);

                        var node = await ready;
                        var json = node as JsonObject ?? JsonObject.Create(System.Text.Json.JsonDocument.Parse(node.ToJsonString()).RootElement);

                        foreach (var parser in invalid)
                        {
                            if ((parser as ParserWrapper)?.JsonParser is JsonPropertyParser jpp)
                            {
                                json.Remove(jpp.Property);
                            }
                        }

                        await cache.AddAsync(type, id, url, new JsonResponse(json.ToJsonString()));
                    }

                    remaining.RemoveAt(index);
                    info.RemoveAt(index);
                }
            }

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