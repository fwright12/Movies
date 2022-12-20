using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Movies
{
    public partial class TMDB
    {
        private abstract class BaseList : Models.List
        {
            public override string ID => _ID?.ToString();
            protected HttpClient Client { get; }
            protected TMDB IDSystem { get; }

            protected object _ID;
            private string Token;

            public BaseList(object id, TMDB idSystem, string bearer)
            {
                _ID = id;
                Client = new HttpClient
                {
                    BaseAddress = new Uri(BASE_ADDRESS),
                    DefaultRequestHeaders =
                    {
                        Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer)
                    }
                };
                IDSystem = idSystem;
                AllowedTypes = ItemType.Movie | ItemType.TVShow;
                Token = bearer;

                Items = GetItems();
            }

            protected abstract IAsyncEnumerable<Models.Item> GetItems();

            public IAsyncEnumerable<T> Request<T>(IPagedRequest request, AsyncEnumerable.TryParseFunc<JsonNode, T> parse, params string[] parameters) => TMDB.Request(Client.GetPagesAsync(request, Helpers.LazyRange(1, 1), default, parameters).SelectAsync(GetJson), new JsonNodeParser<T>(parse));
            //public IAsyncEnumerable<JsonNode> FlattenPages(string apiCall, params string[] parameters) => Request(Client.GetPagesAsync(new PagedTMDbRequest(apiCall), Helpers.LazyRange(1, 1), default, parameters).SelectAsync(GetJson), new JsonNodeParser<JsonNode>());
            //public IAsyncEnumerable<JsonNode> FlattenPages(string apiCall) => TMDB.FlattenPages(Client, apiCall);

            protected bool TryGetId(Models.Item item, out int id) => ((IAssignID<int>)IDSystem).TryGetID(item, out id);

            protected bool TryGetMediaType(Models.Item item, out string type)
            {
                if (item is Models.Movie)
                {
                    type = "movie";
                }
                else if (item is Models.TVShow)
                {
                    type = "tv";
                }
                else
                {
                    type = null;
                    return false;
                }

                return true;
            }

            protected bool TryFormatItems(IEnumerable<Models.Item> items, out string json)
            {
                var kvps = items.Select(item => TryGetMediaType(item, out var type) && TryGetId(item, out var id) ? new { media_type = type, media_id = id } : null).Where(item => item != null);
                //json = string.Join(", ", items.Select(item => TryGetMediaType(item, out var type) && TryGetId(item, out var id) ? (type, id) : (object)false).OfType<(string, int)>().Select(kvp => JsonObject(FormatJson("media_type", kvp.Item1), FormatJson("media_id", kvp.Item2))));

                //if (string.IsNullOrEmpty(json))
                if (!kvps.Any())
                {
                    json = null;
                    return false;
                }

                //json = "{\"items\": [" + json + "]}";
                json = JsonExtensions.JsonObject(JsonExtensions.FormatJson("items", kvps));
                return true;
            }
        }
    }
}
