using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public partial class Trakt
    {
        public abstract class BaseList : Models.List
        {
            public override string ID => _ID?.ToString();

            protected abstract string ItemsEndpoint { get; }
            protected abstract string AddEndpoint { get; }
            protected abstract string RemoveEndpoint { get; }
            protected virtual string ReorderEndpoint { get; } = null;

            protected Trakt Trakt;
            protected HttpClient Client;
            protected object _ID;

            public BaseList(Trakt trakt, object id, int? pageSize = null)
            {
                Trakt = trakt;
                _ID = id;
                Items = GetItems(pageSize).TrySelect<JsonNode, Item>(TryParseItem);

                Client = new HttpClient
                {
                    DefaultRequestHeaders =
                    {
                        Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Trakt.UserAccessToken)
                    }
                };
                foreach (var header in Trakt.Client.DefaultRequestHeaders)
                {
                    Client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            protected bool TryParseItem(JsonNode json, out Item item) => Trakt.TryParseItem(json, Trakt.TMDb, out item);

            private bool TryFormatItems(IEnumerable<Item> items, out string json, out IEnumerable<int> tmdbIds)
            {
                var ids = items.Select(item => item.TryGetID(TMDB.ID, out int id) ? new Tuple<Item, int>(item, id) : null).Where(item => item != null).ToList();

                if (ids.Count == 0)
                {
                    json = null;
                    tmdbIds = null;
                    return false;
                }

                var values = ids.Select(id => (id.Item1, new { ids = new { tmdb = id.Item2 } })).ToList();
                json = JsonExtensions.JsonObject(
                    JsonExtensions.FormatJson("movies", values.Where(id => id.Item1 is Movie).Select(id => id.Item2)),
                    JsonExtensions.FormatJson("shows", values.Where(id => id.Item1 is TVShow).Select(id => id.Item2)),
                    JsonExtensions.FormatJson("seasons", values.Where(id => id.Item1 is TVSeason).Select(id => id.Item2)),
                    JsonExtensions.FormatJson("episodes", values.Where(id => id.Item1 is TVEpisode).Select(id => id.Item2)),
                    JsonExtensions.FormatJson("people", values.Where(id => id.Item1 is Person).Select(id => id.Item2)));
                tmdbIds = ids.Select(id => id.Item2);
                return true;
            }

            protected override async Task<bool> AddAsyncInternal(IEnumerable<Item> items)
            {
                items = items.Where(item => AllowedTypes.HasFlag(item.ItemType));
                if (!TryFormatItems(items, out string json, out var tmdbIds))
                {
                    return true;
                }

                var response = await Client.TryPostAsync(AddEndpoint, json);
                bool success = response?.IsSuccessStatusCode == true;

                if (ReorderEndpoint != null && success && JsonNode.Parse(await response.Content.ReadAsStringAsync())["added"]?.AsObject().Sum(kvp => kvp.Value.TryGetValue<int>()) > 1)
                {
                    var ranked = new LinkedList<(int rankId, int tmdbId)>();
                    await foreach (var added in FlattenPages(Client, ItemsEndpoint, Trakt.UserAccessToken))
                    {
                        if (added.TryGetValue("id", out int id) && added.TryGetValue("type", out string type) && added[type]?["ids"]?["tmdb"].TryGetValue<int>() is int tmdbId)
                        {
                            ranked.AddLast((id, tmdbId));
                        }
                    }
                    //var ranked = new LinkedList<Item>(all.Select(json => TryParseItem(json, out var item) ? item : null).Where(item => item != null));

                    foreach (var id in tmdbIds)
                    {
                        for (var node = ranked.Last; node != null; node = node.Previous)
                        {
                            if (node.Value.tmdbId == id)
                            {
                                ranked.Remove(node);
                                ranked.AddLast(node);
                                break;
                            }
                        }
                    }

                    var ranks = ranked.Select(item => item.rankId);
                    await Client.TryPostAsync(ReorderEndpoint, JsonExtensions.JsonObject(JsonExtensions.FormatJson("rank", ranks)));
                }

                return success;
            }

            protected override async Task<bool?> ContainsAsyncInternal(Item item)
            {
                try
                {
                    await foreach (var temp in this)
                    {
                        if (Equals(item, temp))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    return null;
                }
            }

            public override async Task<int> CountAsync()
            {
                var response = await Client.TryGetAsync(ItemsEndpoint + "?page=1&limit=1");

                if (response?.IsSuccessStatusCode == true && response.Headers.TryGetValues("x-pagination-item-count", out var values) && int.TryParse(values.FirstOrDefault(), out var count))
                {
                    return count;

                    //return JsonNode.Parse(await response.Content.ReadAsStringAsync())["item_count"].TryGetValue<int>();
                }

                return 0;
            }

            protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Item> items)
            {
                if (!TryFormatItems(items, out string json, out _))
                {
                    return true;
                }

                var response = await Client.TryPostAsync(RemoveEndpoint, json);
                return response?.IsSuccessStatusCode == true;
            }

            protected async IAsyncEnumerable<JsonNode> GetItems(int? pageSize = null)
            {
                if (ID == null)
                {
                    yield break;
                }

                await foreach (var item in FlattenPages(Client, ItemsEndpoint, Trakt.UserAccessToken, pageSize, Reverse))
                {
                    yield return item;
                }
            }
        }
    }
}
