using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        private class List : BaseList
        {
            private string DetailsBackup;

            public List(string token, TMDB idSystem) : this(null, token, idSystem) { }

            private List(object id, string token, TMDB idSystem) : base(id, idSystem, token) { }

            public static bool TryParse(JsonNode json, string token, TMDB idSystem, out Models.List list)
            {
                list = null;

                if (!json.TryGetValue("id", out int id))
                {
                    return false;
                }

                var localList = new List(id, token, idSystem)
                {
                    Name = json["name"]?.TryGetValue<string>(),
                    Description = json["description"]?.TryGetValue<string>(),
                    PosterPath = (json["poster_path"]?.TryGetValue<string>() ?? json["backdrop_path"]?.TryGetValue<string>()) is string posterPath ? BuildImageURL(posterPath, POSTER_SIZE) : null,
                    Count = json.TryGetValue("number_of_items", out int count) ? count : json["total_results"]?.TryGetValue<int?>(),
                    Author = json["created_by"]?["username"]?.TryGetValue<string>(),
                };

                if (DateTime.TryParse(json["updated_at"]?.TryGetValue<string>(), out DateTime date))
                {
                    localList.LastUpdated = date;
                }

                var node = json["public"];
                localList.Public = node != null && node.TryGetValue<int>(out var i) ? (i != 0) : (node?.TryGetValue<bool>() ?? false);

                localList.DetailsBackup = localList.DetailsAsJson();

                list = localList;
                return true;
            }

            protected override async Task<bool> AddAsyncInternal(IEnumerable<Models.Item> items)
            {
                if (!TryFormatItems(items, out string json))
                {
                    return false;
                }

                //var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await Client.TryPostAsync(string.Format(API.LIST.ADD_ITEMS.GetURL(), ID), json);
                //var response = await Client.PostAsync(string.Format("https://api.themoviedb.org/4/list/{0}/items", ID), content);

                return response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
            }

            protected override async Task<bool?> ContainsAsyncInternal(Models.Item item)
            {
                if (TryGetMediaType(item, out var type) && TryGetId(item, out var itemId))
                {
                    //var response = await Client.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}/item_status?api_key=f0e327cdf818a4e5b6df2cbde7095c60&media_id={1}&media_type={2}", ID, itemId, type));
                    var response = await Client.TryGetAsync(string.Format(API.LIST.CHECK_ITEM_STATUS.GetURL(), ID, itemId, type));

                    if (response != null)
                    {
                        return response.IsSuccessStatusCode && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
                    }
                }

                return null;
            }

            public override async Task<int> CountAsync()
            {
                var response = await Client.TryGetAsync(string.Format(API.LIST.GET_LIST.GetURL(), ID));
                //var response = await Client.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}?page=1", ID));
                return response == null ? -1 : (JsonNode.Parse(await response.Content.ReadAsStringAsync())["total_results"]?.GetValue<int>() ?? -1);
            }

            public override async Task Delete()
            {
                var response = await Client.TrySendAsync(string.Format(API.LIST.DELETE_LIST.GetURL(), ID), method: HttpMethod.Delete);
                //var response = await Client.DeleteAsync(string.Format("https://api.themoviedb.org/4/list/{0}", ID));

                //return JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"].GetValue<bool>();
            }

            protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Models.Item> items)
            {
                if (!TryFormatItems(items, out string json))
                {
                    return false;
                }

                var response = await Client.TrySendAsync(string.Format(API.LIST.REMOVE_ITEMS.GetURL(), ID), json, HttpMethod.Delete);
                //var response = await Client.SendAsync(request);

                return response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
            }

            private string CleanString(string value) => (value ?? string.Empty).Trim();

            private string DetailsAsJson() => JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("name", CleanString(Name)),
                JsonExtensions.FormatJson("description", CleanString(Description)),
                JsonExtensions.FormatJson("public", Public));

            public override async Task Update()
            {
                string json = DetailsAsJson();
                //return;

                if (ID == null)
                {
                    var iso = JsonExtensions.FormatJson("iso_639_1", ISO_639_1) + ", ";
                    //var response = await Client.TryPostAsync(".", json.Insert(1, iso));
                    var response = await Client.TryPostAsync(API.LIST.CREATE_LIST.GetURL(), json.Insert(1, iso));
                    //var response = await Client.PostAsync("https://api.themoviedb.org/4/list", new StringContent(json.Insert(1, iso), Encoding.UTF8, "application/json"));

                    if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && parsed["success"]?.GetValue<bool>() == true && parsed["id"]?.GetValue<int>() is int id)
                    {
                        _ID = id;
                    }
                    else
                    {
                        return;
                    }
                }
                else if (json != DetailsBackup)
                {
                    //var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await Client.TrySendAsync(string.Format(API.LIST.UPDATE_LIST.GetURL(), ID), json, HttpMethod.Put);
                    //var response = await Client.PutAsync(string.Format("https://api.themoviedb.org/4/list/{0}", ID), content);

                    if (!(response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true))
                    {
                        return;
                    }
                }

                DetailsBackup = json;
            }

            /*private async IAsyncEnumerable<Models.Item> ReadAll()
            {
                var items = await this.ReadAll<Models.Item>();

                if (!Reverse)
                {
                    items.Reverse();
                }

                foreach (var item in items)
                {
                    yield return item;
                }
            }

            public override async Task<IEnumerable<Models.Item>> GetAddedAsync(Models.List list)
            {
                if (Reverse)
                {
                    return await base.GetAddedAsync(list);
                }

                var added = new List<Models.Item>();
                var items = Count.HasValue && Count <= 20 ? ReadAll() : FlattenPages(string.Format("list/{0}?page={{0}}&sort_by=original_order.desc", ID)).TrySelect<JsonNode, Models.Item>(TryParse);

                await foreach (var item in items)
                {
                    if (await list.ContainsAsync(item) == true)
                    {
                        break;
                    }

                    added.Insert(0, item);
                }

                return added;
            }*/

            protected override async IAsyncEnumerable<Models.Item> GetItems()
            {
                if (ID == null)
                {
                    yield break;
                }

                await foreach (var item in Request<Models.Item>(new ParameterizedPagedRequest(API.LIST.GET_LIST, ID), TMDB.TryParse, $"sort_by=original_order.{(Reverse ? "desc" : "asc")}"))
                {
                    yield return item;
                }
            }
        }
    }
}
