using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
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
    }

    public class ItemProperties
    {
        public IReadOnlyDictionary<TMDbRequest, List<Parser>> Info { get; }
        public IJsonCache Cache { get; set; }
        public Lazy<Task<HashSet<string>>> LazyChangeKeysTask { get; set; }

        private readonly Dictionary<Property, TMDbRequest> PropertyLookup = new Dictionary<Property, TMDbRequest>();
        private List<TMDbRequest> SupportsAppendToResponse = new List<TMDbRequest>();
        private List<TMDbRequest> AllRequests { get; }

        public ItemProperties(Dictionary<TMDbRequest, List<Parser>> info)
        {
            var test = new Dictionary<TMDbRequest, List<Parser>>();

            foreach (var kvp in info)
            {
                if (kvp.Key.SupportsAppendToResponse)
                {
                    SupportsAppendToResponse.Add(kvp.Key);
                }

                foreach (var parser in kvp.Value)
                {
                    PropertyLookup[parser.Property] = kvp.Key;
                }

                test.Add(kvp);
            }

            Info = test;
            AllRequests = Info.Keys.ToList();
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

        public void Add(TMDbRequest request, IEnumerable<Parser> parsers)
        {
            if (request.SupportsAppendToResponse)
            {
                SupportsAppendToResponse.Add(request);
            }

            foreach (var parser in parsers)
            {
                PropertyLookup[parser.Property] = request;
            }

            if (!Info.TryGetValue(request, out var list))
            {
                //Info.Add(request, list = new List<Parser>());
            }

            //list.AddRange(parsers);
        }

        public void HandleRequests(Item item, PropertyDictionary properties, TMDB tmdb)
        {
            var handler = new RequestHandler(this, item);
            var parameters = new List<object>();

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

            if (item == null || !tmdb.TryGetID(item, out var id))
            {
                return;
            }

            parameters.Insert(0, id);
            handler.Parameters = parameters.ToArray();

            properties.PropertyAdded += handler.PropertyRequested;
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

            private string GetChangeKey(Property property)
            {
                if (property == Media.KEYWORDS) return "plot_keywords";
                else if (property == Media.TRAILER_PATH) return "videos";
                else if (property == Movie.CONTENT_RATING || property == TVShow.CONTENT_RATING) return "releases";
                else return null;
            }

            public void PropertyRequested(object sender, PropertyEventArgs e) => PropertyRequested((PropertyDictionary)sender, e.Properties);

            public void PropertyRequested(PropertyDictionary dict, IEnumerable<Property> properties)
            {
                var requests = new List<TMDbRequest>();

                foreach (var property in properties)
                {
                    if (Context.PropertyLookup.TryGetValue(property, out var request) && !requests.Contains(request))
                    {
                        requests.Add(request);
                    }
                }

                if (requests.Count == 0)
                {
                    return;
                }

                //requests = Context.Info.Select(kvp => kvp.Key).ToList();
                requests = Context.AllRequests;

                var requested = Task.Run(() =>
                {
                    if (Context.Cache != null)
                    {
                        //await ExpireResponses(properties);
                    }

                    var responses = new List<Task<JsonNode>>();
                    var atr = new int[Context.SupportsAppendToResponse.Count];

                    for (int i = 0; i < requests.Count; i++)
                    {
                        var request = requests[i];
                        var response = Context.Cache?.TryGetValueAsync(request.GetURL());

                        if (response != null)
                        {
                            if (i != responses.Count)
                            {
                                requests.RemoveAt(i);
                                requests.Insert(responses.Count, request);
                            }

                            responses.Add(GetCachedResponse(response));
                        }
                        else if (!request.SupportsAppendToResponse)
                        {
                            int index = Context.SupportsAppendToResponse.FindIndex(temp => request.Endpoint.StartsWith(temp.Endpoint));

                            if (index != -1)
                            {
                                atr[index]++;
                            }
                        }
                    }

                    for (int i = 0; i < Context.SupportsAppendToResponse.Count; i++)
                    {
                        var request = Context.SupportsAppendToResponse[i];

                        if (atr[i] > 1 && !requests.Contains(request))
                        {
                            requests.Insert(responses.Count, request);
                        }
                    }

                    responses.Clear();

                    var requested = TMDB.Request(requests.Skip(responses.Count), Parameters);
                    _ = CacheResponses(responses.Count, requests, requested);

                    responses.AddRange(requested);
                    return responses;
                });

                //responses.AddRange(requested);

                for (int i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];
                    //var response = responses[i];
                    var response = GetResponse(requested, i);

                    if (!Context.Info.TryGetValue(request, out var parsers))
                    {
                        continue;
                    }

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
            private async Task<JsonNode> GetCachedResponse(Task<JsonResponse> response) => JsonNode.Parse((await response).Json);

            private async Task ExpireResponses(IEnumerable<Property> properties)
            {
                foreach (var property in properties)
                {
                    if (Context.PropertyLookup.TryGetValue(property, out var request) && Context.Info.TryGetValue(request, out var parsers))
                    {
                        int index = parsers.FindIndex(parser => parser.Property == property);

                        if (index != -1 && parsers[index] is ParserWrapper wrapper && wrapper.JsonParser is JsonPropertyParser jpp && !(await Context.LazyChangeKeysTask.Value).Contains(GetChangeKey(property) ?? jpp.Property))
                        {
                            var url = request.GetURL();

                            if (property == Movie.WATCH_PROVIDERS || property == TVShow.WATCH_PROVIDERS)
                            {
                                var response = await Context.Cache.TryGetValueAsync(url);

                                if (DateTime.Now - response?.Timestamp < new TimeSpan(1, 0, 0, 0))
                                {
                                    continue;
                                }
                            }

                            await Context.Cache.Expire(url);
                        }
                    }
                }
            }

            private async Task CacheResponses(int startIndex, List<TMDbRequest> requests, Task<JsonNode>[] responses)
            {
                if (Context.Cache == null)
                {
                    return;
                }

                var remaining = responses.ToList<Task<JsonNode>>();

                while (remaining.Count > 0)
                {
                    var ready = await Task.WhenAny(remaining);
                    int index = Array.IndexOf(responses, ready);

                    if (index != -1)
                    {
                        await Context.Cache.AddAsync(requests[startIndex + index].GetURL(), new JsonResponse((await ready).ToJsonString()));
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