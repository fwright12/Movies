using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public class ItemProperties
    {
        public IReadOnlyDictionary<TMDbRequest, List<Parser>> Info { get; }

        private readonly Dictionary<Property, TMDbRequest> PropertyLookup = new Dictionary<Property, TMDbRequest>();
        private List<TMDbRequest> SupportsAppendToResponse = new List<TMDbRequest>();

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
            public Item Item { get; set; }
            public object[] Parameters { get; set; }

            public RequestHandler(ItemProperties context, Item item)
            {
                Context = context;
                Item = item;
            }

            private async Task<JsonNode> ApiCall(TMDbRequest request, IEnumerable<TMDbRequest> appended)
            {
                var endpoints = appended.Prepend(request).Select(request => string.Format(request.GetURL(), Parameters));
                var endpoint = AppendToResponse(endpoints.FirstOrDefault(), endpoints.Skip(1));

                var response = await TMDB.WebClient.GetAsync(endpoint);
                return JsonNode.Parse(await response.Content.ReadAsStringAsync());
            }

            public void PropertyRequested(object sender, PropertyEventArgs e) => PropertyRequested((PropertyDictionary)sender, e.Properties);
            public void PropertyRequested(PropertyDictionary dict, IEnumerable<Property> properties)
            {
                var requests = new List<TMDbRequest>();

                foreach (var property in properties)
                {
                    if (Context.PropertyLookup.TryGetValue(property, out var request))
                    {
                        requests.Add(request);
                    }
                }

                if (requests.Count == 0)
                {
                    return;
                }

                foreach (var property in Context.Info.Values.SelectMany(parsers => parsers).Select(parser => parser.Property).Except(properties))
                {
                    if (Context.PropertyLookup.TryGetValue(property, out var request))
                    {
                        requests.Add(request);
                    }
                }

                var batched = new List<List<TMDbRequest>>(Context.SupportsAppendToResponse.Select(atr => new List<TMDbRequest>()));
                var parsers = new List<List<IEnumerable<Parser>>>(Context.SupportsAppendToResponse.Select(atr => new List<IEnumerable<Parser>>()));

                for (int i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];

                    if (Context.Info.TryGetValue(request, out var parsers2) && parsers2.Count > 0)
                    {
                        var parsers1 = parsers2;
                        int index = Context.SupportsAppendToResponse.FindIndex(atr => request.Endpoint.StartsWith(atr.Endpoint));

                        if (request is PagedTMDbRequest pagedRequest)
                        {
                            parsers1 = ReplacePagedParsers(pagedRequest, parsers2);
                        }

                        if (index == -1)
                        {
                            index = batched.Count;
                            batched.Add(new List<TMDbRequest>());
                            parsers.Add(new List<IEnumerable<Parser>>());
                        }

                        if (!batched[index].Contains(request))
                        {
                            var batch = batched[index];
                            var atr = index < Context.SupportsAppendToResponse.Count ? Context.SupportsAppendToResponse[index] : request;

                            if (request == atr)
                            {
                                batch.Insert(0, request);
                                parsers[index].Add(parsers1);
                            }
                            else
                            {
                                batch.Add(request);
                                var appended = request.Endpoint.Replace(atr.Endpoint, string.Empty).TrimStart('/');
                                parsers[index].Add(parsers1.Select(parser => new ParserWrapper(parser)
                                {
                                    JsonParser = new JsonPropertyParser<JsonNode>(appended)
                                }));

                                if (batch.Count == 2 && !batch.Contains(atr))
                                {
                                    requests.Add(atr);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < batched.Count; i++)
                {
                    var response = ApiCall(batched[i].FirstOrDefault(), batched[i].Skip(1));

                    foreach (var parser in parsers[i].SelectMany(x => x))
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

            private async Task<IEnumerable<T>> GetTVItems<T>(Task<JsonNode> json, string property, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse) => TMDB.TryParseCollection(await json, new JsonPropertyParser<IEnumerable<JsonNode>>(property), out var result, new JsonNodeParser<T>(parse)) ? result : null;

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

        public static string AppendToResponse(string basePath, IEnumerable<string> endpoints)
        {
            var appendedPaths = new List<string>();
            var parameters = new Dictionary<string, string>();
            var parsers = new List<Parser>();

            foreach (var endpoint in endpoints.Prepend(basePath))
            {
                //var uri = new Uri(call.Endpoint, UriKind.RelativeOrAbsolute);
                var parts = endpoint.Split('?');
                var path = parts.ElementAtOrDefault(0) ?? string.Empty;
                var query = parts.ElementAtOrDefault(1) ?? string.Empty;

                if (endpoint == basePath)
                {
                    basePath = path;
                }
                else if (!path.StartsWith(basePath))
                {
                    continue;
                }
                else
                {
                    path = path.Replace(basePath, string.Empty).TrimStart('/');
                    appendedPaths.Add(path);
                }

                foreach (var parameter in query.Split('&'))
                {
                    var temp = parameter.Split('=');

                    if (temp.Length == 2)
                    {
                        parameters[temp[0]] = temp[1];
                    }
                }
            }

            var parameterList = parameters
                .Select(kvp => string.Join('=', kvp.Key, kvp.Value))
                .Append($"append_to_response={string.Join(',', appendedPaths)}");

            return TMDB.BuildApiCall(basePath, parameterList);
        }
    }
}