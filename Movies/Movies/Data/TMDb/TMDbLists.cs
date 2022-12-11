using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public interface IJsonCache : IAsyncEnumerable<KeyValuePair<string, JsonResponse>>
    {
        Task AddAsync(string url, JsonResponse response);
        Task Clear();
        Task<bool> Expire(string url);
        Task<bool> IsCached(string url);
        Task<JsonResponse> TryGetValueAsync(string url);
    }

    public class JsonResponse
    {
        public HttpContent Content { get; }
        public DateTime Timestamp { get; }

        public JsonResponse(string json) : this(json, DateTime.Now) { }
        public JsonResponse(byte[] bytes) : this(bytes, DateTime.Now) { }

        public JsonResponse(string json, DateTime timeStamp) : this(new StringContent(json), timeStamp) { }
        public JsonResponse(byte[] bytes, DateTime timeStamp) : this(new ByteArrayContent(bytes), timeStamp) { }

        public JsonResponse(HttpContent content, DateTime timeStamp)
        {
            Content = content;
            Timestamp = timeStamp;
        }

        public static async Task<JsonResponse> Create(Func<Task<HttpResponseMessage>> request)
        {
            var response = await request();

            if (response?.IsSuccessStatusCode == true)
            {
                return new JsonResponse(await response.Content.ReadAsStringAsync());
            }

            return null;
        }
    }

    public static class JsonExtensions
    {
        public static Task<JsonResponse> TryGetCachedAsync(this HttpClient client, string url, IJsonCache cache) => TryGetCachedAsync(client, new HttpRequestMessage(HttpMethod.Get, url), cache);
        public static async Task<JsonResponse> TryGetCachedAsync(this HttpClient client, HttpRequestMessage request, IJsonCache cache)
        {
            var url = request.RequestUri.ToString();
            var cached = cache.TryGetValueAsync(url);

            if (cached != null)
            {
                return await cached;
            }

            var content = await TryGetContentAsync(client, request);
            var response = new JsonResponse(content);

            if (content != null)
            {
                await cache.AddAsync(url, response);
            }

            return response;
        }

        public static Task<string> TryGetContentAsync(this HttpClient client, string url) => TryGetContentAsync(client, new HttpRequestMessage(HttpMethod.Get, url));
        public static async Task<string> TryGetContentAsync(this HttpClient client, HttpRequestMessage request)
        {
            var response = await TrySendAsync(client, request);

            if (response?.IsSuccessStatusCode == true)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        public static Task<HttpResponseMessage> TryGetAsync(this HttpClient client, string url) => TrySendAsync(client, url);
        public static Task<HttpResponseMessage> TryPostAsync(this HttpClient client, string url, string content) => TrySendAsync(client, url, content, HttpMethod.Post);

        public static Task<HttpResponseMessage> TrySendAsync(this HttpClient client, string url, string content, HttpMethod method = null) => TrySendAsync(client, url, new StringContent(content, Encoding.UTF8, "application/json"), method);

        public static Task<HttpResponseMessage> TrySendAsync(this HttpClient client, string url, HttpContent content = null, HttpMethod method = null)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url, UriKind.RelativeOrAbsolute)
            };
            if (method != null)
            {
                request.Method = method;
            }
            if (content != null)
            {
                request.Content = content;
            }

            return TrySendAsync(client, request);
        }

        public static async Task<HttpResponseMessage> TrySendAsync(this HttpClient client, HttpRequestMessage request)
        {
            try
            {
                return await client.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public static T TryGetValue<T>(this JsonNode node) => TryGetValue<T>(node, out var value) ? value : default;
        public static bool TryGetValue<T>(this JsonNode node, string property, out T value)
        {
            if ((node as JsonObject)?.TryGetPropertyValue(property, out var temp) == true)
            {
                return TryGetValue(temp, out value);
            }

            value = default;
            return false;
        }
        public static bool TryGetValue<T>(this JsonNode node, out T value)
        {
            if (node is T t)
            {
                value = t;
                return true;
            }

            try
            {
                if (node == null)
                {
                    value = default;
                    return value == null;
                }
                else
                {
                    value = node.GetValue<T>();
                    return true;
                }
            }
            catch { }

            value = default;
            return false;
        }

        public static bool TryGetValue<T>(this JsonElement json, out T value, string path = "", JsonSerializerOptions options = null)
        {
            value = default;

            if (path != string.Empty && json.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in path.Split('.'))
            {
                if (!string.IsNullOrEmpty(property) && !json.TryGetProperty(property, out json))
                {
                    return false;
                }
            }

            if (json is T t)
            {
                value = t;
                return true;
            }

            try
            {
                value = json.Deserialize<T>(options);
                return true;
            }
            catch { }

            return false;
        }

        public static string JsonObject(params string[] items) => "{" + string.Join(", ", items) + "}";

        public static string FormatJson<T>(string name, T value) => string.Format("\"{0}\": {1}", name, JsonSerializer.Serialize(value));
    }

    public partial class TMDB : IListProvider
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

        private class NamedList : BaseList
        {
            public string ItemsPath { get; }
            public string StatusPath { get; }

            private string AccountID;
            private string SessionID;
            private Models.Collection Movies;
            private Models.Collection TV;

            public NamedList(string itemsPath, string statusPath, string bearer, string accountID, string sessionId, TMDB idSystem) : base(itemsPath, idSystem, bearer)
            {
                AccountID = accountID;
                SessionID = sessionId;

                ItemsPath = itemsPath;
                StatusPath = statusPath;

                Reverse = true;
                var sort = $"sort_by={(Reverse ? "created_at.desc" : "created_at.asc")}";

                Movies = new Models.Collection
                {
                    Reverse = true,
                    Items = Request<Models.Movie>(new ParameterizedPagedRequest(API.V4.ACCOUNT.GET_MOVIES, AccountID, ID), TryParseMovie, sort)
                };
                TV = new Models.Collection
                {
                    Reverse = true,
                    Items = Request<Models.TVShow>(new ParameterizedPagedRequest(API.V4.ACCOUNT.GET_TV_SHOWS, AccountID, ID), TryParseTVShow, sort)
                };
            }

            private async Task<bool> UpdateStatus(bool status, IEnumerable<Models.Item> items)
            {
                foreach (var item in items)
                {
                    if (TryGetMediaType(item, out var type) && TryGetId(item, out var id))
                    {
                        var json = JsonExtensions.JsonObject(
                            JsonExtensions.FormatJson("media_type", type),
                            JsonExtensions.FormatJson("media_id", id),
                            JsonExtensions.FormatJson(StatusPath, status));
                        //var content = new StringContent(json, Encoding.UTF8, "application/json");
                        await Client.TryPostAsync(string.Format(API.V3.ACCOUNT.ADD_TO_LIST.GetURL(), AccountID, StatusPath, SessionID), json);
                        //await Client.PostAsync(string.Format("https://api.themoviedb.org/3/account/{0}/" + StatusPath + "?session_id={1}", AccountID, SessionID), content);
                    }
                }

                return true;
            }

            protected override Task<bool> AddAsyncInternal(IEnumerable<Models.Item> items) => UpdateStatus(true, items);

            protected override async Task<bool?> ContainsAsyncInternal(Models.Item item)
            {
                if (TryGetMediaType(item, out var type) && TryGetId(item, out var id))
                {
                    var response = await Client.TryGetAsync(string.Format("3/" + type + "/{0}/account_states?session_id={1}", id, SessionID));
                    //var response = await Client.GetAsync(string.Format("https://api.themoviedb.org/3/" + type + "/{0}/account_states?session_id={1}", id, SessionID));

                    if (response?.IsSuccessStatusCode == true)
                    {
                        return JsonNode.Parse(await response.Content.ReadAsStringAsync())[StatusPath]?.GetValue<bool>() == true;
                    }
                }

                return null;
            }

            public override async Task<int> CountAsync()
            {
                var responses = await Task.WhenAll(Client.TryGetAsync(string.Format(API.V4.ACCOUNT.GET_MOVIES.GetURL(), AccountID, ID)), Client.TryGetAsync(string.Format(API.V4.ACCOUNT.GET_TV_SHOWS.GetURL(), AccountID, ID)));
                //var responses = await Task.WhenAll(Client.GetAsync(string.Format("https://api.themoviedb.org/4/account/{0}/movie/" + ID.ToString() + "?page=1", AccountID)), Client.GetAsync(string.Format("https://api.themoviedb.org/4/account/{0}/tv/" + ID.ToString() + "?page=1", AccountID)));

                if (responses[0]?.IsSuccessStatusCode == true && responses[1]?.IsSuccessStatusCode == true)
                {
                    var parsed = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
                    return parsed.Sum(json => JsonNode.Parse(json).TryGetValue<int>("total_results", out var count) ? count : 0);
                }

                return -1;
                //return JsonNode.Parse(await response.Content.ReadAsStringAsync())["total_results"]?.GetValue<int>() ?? -1;
            }

            public override Task Delete() => Task.CompletedTask;

            protected override Task<bool> RemoveAsyncInternal(IEnumerable<Models.Item> items) => UpdateStatus(false, items);

            public override Task Update() => Task.CompletedTask;

            public override async Task<List<Models.Item>> GetAdditionsAsync(Models.List list)
            {
                var added = new List<Models.Item>();

                if (Reverse)
                {
                    try
                    {
                        await foreach (var item in Movies)
                        {
                            if (await list.ContainsAsync(item) != true)
                            {
                                added.Insert(Reverse ? 0 : added.Count, item);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        added.Clear();
                    }
                }

                return added;
            }

            /*public override async Task<List<Models.Item>> GetAdditionsAsync(Models.List list, bool onlyFromFront = true)
            {
                var items = await GetAdditionsAsync(list);

                if (!(items.FirstOrDefault() is Models.Movie))
                {
                    items.AddRange(await Movies.GetAdditionsAsync(list));
                }

                return items.OfType<Models.TVShow>().Concat<Models.Item>(items.OfType<Models.Movie>()).ToList();
            }*/

            protected override async IAsyncEnumerable<Models.Item> GetItems()
            {
                await foreach (var tv in TV)
                {
                    yield return tv;
                }
                await foreach (var movie in Movies)
                {
                    yield return movie;
                }
            }

            public override IAsyncEnumerable<Models.Item> TryFilter(FilterPredicate filter, out FilterPredicate partial, CancellationToken cancellationToken = default)
            {
                var types = Models.ItemHelpers.RemoveTypes(filter, out partial);
                var movie = types.Contains(typeof(Models.Movie));
                var tv = types.Contains(typeof(Models.TVShow));

                if (movie ^ tv)
                {
                    var items = tv ? TV : Movies;
                    return items;
                    //return ItemHelpers.Filter(items, filter, cancellationToken);
                    //return items.WhereAsync(item => Models.ItemHelpers.Evaluate(item, filter, cancellationToken));
                }
                else
                {
                    return base.TryFilter(filter, out partial, cancellationToken);
                }
            }
        }

#if DEBUG
        private class DummyList : Models.List
        {
            public override string ID { get; }

            public DummyList(string id = null)
            {
                ID = id ?? Guid.NewGuid().ToString();
                //Description = "cool list";
                Items = Stuff();
            }

            private async IAsyncEnumerable<Models.Item> Stuff()
            {
                //yield break;
                Count = 1;
                yield return new Models.Movie("test").WithID(MockData.IDKey, 3);
            }

            protected override Task<bool> AddAsyncInternal(IEnumerable<Models.Item> items)
            {
                return Task.FromResult(true);
            }

            protected override Task<bool?> ContainsAsyncInternal(Models.Item item)
            {
                return Task.FromResult<bool?>(true);
            }

            public override Task<int> CountAsync()
            {
                throw new NotImplementedException();
            }

            public override Task Delete()
            {
                return Task.CompletedTask;
            }

            protected override Task<bool> RemoveAsyncInternal(IEnumerable<Models.Item> items)
            {
                return Task.FromResult(true);
            }

            public override Task Update()
            {
                return Task.CompletedTask;
            }
        }

        public async IAsyncEnumerable<Models.List> GetAllListsAsync1()
        {
            for (int i = 0; i < 20; i++)
                yield return new DummyList();
            await Task.CompletedTask;
        }
#endif

        public Models.List CreateList() => new List(UserAccessToken, this);

        public async Task<List<Models.List>> GetAllLists()
        {
            var lists = new List<Models.List>();
            var request = new PagedTMDbRequest($"account/{AccountID}/lists")
            {
                Version = 4,
                Authorization = UserAuth
            };

            await foreach (var list in Request<Models.List>(request, TryParseList))
            {
                lists.Add(list);
            }

            return lists;
        }

        public async IAsyncEnumerable<Models.List> GetAllListsAsync()
        {
            List<Models.List> lists;

            try
            {
                lists = await LazyAllLists.Value;
            }
            catch
            {
                yield break;
            }

            foreach (var list in lists)
            {
                yield return list;
            }
        }

        private bool TryParseList(JsonNode json, out Models.List list) => List.TryParse(json, UserAccessToken, this, out list);

        public Task<Models.List> GetWatchlist() => Task.FromResult(GetNamedList("watchlist", "watchlist"));

        public Task<Models.List> GetFavorites() => Task.FromResult(GetNamedList("favorites", "favorite"));

        public Task<Models.List> GetHistory() => Task.FromResult<Models.List>(null);

        public async Task<Models.List> GetListAsync(string id)
        {
            //return new DummyList(id) { Name = "Tmdb list" };
            //var response = await WebClient.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}", 

            if ((await LazyAllLists.Value).FirstOrDefault(list => list.ID == id) is Models.List temp)
            {
                return temp;
            }

            var response = await WebClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, string.Format("4/list/{0}", id))
            {
                Headers =
                {
                    Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserAccessToken)
                }
            });

            return response?.IsSuccessStatusCode == true && List.TryParse(JsonNode.Parse(await response.Content.ReadAsStringAsync()), UserAccessToken, this, out var list) ? list : null;
        }

        private Models.List GetNamedList(string itemsPath, string statusPath) => new NamedList(itemsPath, statusPath, UserAccessToken, AccountID, SessionID, this);
    }

}
