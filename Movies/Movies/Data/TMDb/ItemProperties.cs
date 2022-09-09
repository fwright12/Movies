using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        public static Task<JsonNode>[] Request(IEnumerable<TMDbRequest> requests, params object[] parameters)
        {
            var result = new Task<JsonNode>[requests.Count()];
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

                IJsonParser<JsonNode> parser = new JsonNodeParser<JsonNode>();

                if (urls[i] is int pointer)
                {
                    index = pointer;
                    //var property = request.GetURL().Split('?').First().Replace((urls[index] as string).Split('?').First(), string.Empty).TrimStart('/');
                    var property = request.Endpoint.Replace(requests.ElementAt(index).Endpoint, string.Empty).TrimStart('/');
                    parser = new JsonPropertyParser<JsonNode>(property);
                }

                var response = urls[index] as Task<System.Net.Http.HttpResponseMessage>;

                if (response == null)
                {
                    var url = string.Format(urls[index] as string ?? request.GetURL(), parameters);
                    urls[index] = response = WebClient.TryGetAsync(url);
                }

                result[i] = GetAppendedJson(response, parser);
            }

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

        private static async Task<JsonNode> GetAppendedJson(Task<System.Net.Http.HttpResponseMessage> response, IJsonParser<JsonNode> parse) => parse.TryGetValue(JsonNode.Parse(await (await response).Content.ReadAsStringAsync()), out var value) ? value : null;

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
                string changeKey;

                if (property == Media.KEYWORDS) changeKey = "plot_keywords";
                else if (property == Media.TRAILER_PATH) changeKey = "videos";
                else if (property == Movie.CONTENT_RATING || property == TVShow.CONTENT_RATING) changeKey = "releases";
                else if (parser is ParserWrapper wrapper && wrapper.JsonParser is JsonPropertyParser jpp)
                {
                    changeKey = jpp.Property;
                }
                else continue;

                ChangeKeyLookup[property] = changeKey;
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
                        if (property != Movie.WATCH_PROVIDERS && property != TVShow.WATCH_PROVIDERS && !IsCacheValid(request, property))
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

            public void PropertyRequested(PropertyDictionary dict, Dictionary<TMDbRequest, List<Property>> requests) => CacheInMemory(dict, requests.Keys, MakeRequests(dict, requests));

            private void CacheInMemory(PropertyDictionary dict, IEnumerable<TMDbRequest> requests, Task<List<Task<JsonNode>>> responses)
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
                        else
                        {
                            dict.Add(parser.GetPair(response));
                        }
                    }
                }
            }

            private async Task<JsonNode> GetResponse(Task<List<Task<JsonNode>>> responses, int index) => await (await responses)[index];

            private string GetFullURL(TMDbRequest request) => string.Format(request.GetURL(), Parameters);

            private async Task<List<Task<JsonNode>>> MakeRequests(PropertyDictionary dict, Dictionary<TMDbRequest, List<Property>> properties)
            {
                // Virtually guaranteed to execute syncronously
                await Context.ChangeKeysLoaded;
                await Context.CacheSemaphore.WaitAsync();

                var requests = new Dictionary<TMDbRequest, int>();
                var cached = new Dictionary<TMDbRequest, int>();

                var itr = properties.GetEnumerator();

                for (int i = 0; itr.MoveNext(); i++)
                {
                    var request = itr.Current.Key;

                    foreach (var property in itr.Current.Value)
                    {
                        var list = Context.Cache != null && IsCacheValid(request, property) ? cached : requests;

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

                var responses = Enumerable.Repeat<Task<JsonNode>>(null, properties.Count).ToList();// new List<Task<JsonNode>>();

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
                        responses[kvp.Value] = Task.FromResult(JsonNode.Parse(response.Json));
                    }
                }

                if (requests.Count > 0)
                {
                    IEnumerable<TMDbRequest> requesting = requests.Keys;
                    requesting = Context.AllRequests.Keys;
                    var requested = TMDB.Request(requesting, Parameters);

                    var extraRequests = new List<TMDbRequest>();
                    var extraResponses = new List<Task<JsonNode>>();

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
                        await CachePersistent(Context.Cache, Item.ItemType, id, requesting, requested);
                    }
                }

                Context.CacheSemaphore.Release();

                return responses;
            }

            private bool IsCacheValid(TMDbRequest request, Property property) => property == Movie.WATCH_PROVIDERS || property == TVShow.WATCH_PROVIDERS || (Context.ChangeKeyLookup.TryGetValue(property, out var changeKey) && Context.ChangeKeys.Contains(changeKey));

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
                var requests = requests1.ToList();
                var remaining = responses.ToList<Task<JsonNode>>();

                while (remaining.Count > 0)
                {
                    var ready = await Task.WhenAny(remaining);
                    int index = IndexOf(responses, ready);

                    if (index != -1)
                    {
                        var url = GetFullURL(requests[index]);
                        await cache.Expire(url);
                        await cache.AddAsync(type, id, url, new JsonResponse((await ready).ToJsonString()));
                    }

                    remaining.Remove(ready);
                }
            }

            private async Task<IEnumerable<T>> GetTVItems<T>(Task<JsonNode> json, string property, AsyncEnumerable.TryParseFunc<JsonNode, T> parse) => TMDB.TryParseCollection(await json, new JsonPropertyParser<IEnumerable<JsonNode>>(property), out var result, new JsonNodeParser<T>(parse)) ? result : null;

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