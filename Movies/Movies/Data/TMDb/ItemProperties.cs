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
            if (!tmdb.TryGetID(item, out var id))
            {
                return;
            }

            var parameters = new List<object> { id };

            if (item is TVSeason season)
            {
                parameters.Add(season.SeasonNumber);
            }
            else if (item is TVEpisode episode)
            {
                parameters.Add(episode.Season.SeasonNumber, episode.EpisodeNumber);
            }

            var handler = new RequestHandler
            {
                Context = this,
                TMDB = tmdb,
                Parameters = parameters.ToArray(),
            };

            properties.PropertyAdded += handler.PropertyRequested;
        }

        private class RequestHandler
        {
            public ItemProperties Context { get; set; }
            public TMDB TMDB { get; set; }
            public object[] Parameters { get; set; }

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

                    if (Context.Info.TryGetValue(request, out var parsers1) && parsers1.Count > 0)
                    {
                        int index = Context.SupportsAppendToResponse.FindIndex(atr => request.Endpoint.StartsWith(atr.Endpoint));

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
                        dict.Add(parser.GetPair(response));
                    }
                }
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