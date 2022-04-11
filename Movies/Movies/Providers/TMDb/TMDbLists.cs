using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Async;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public static class JsonExtensions
    {
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
            node = node[property];

            if (node == null)
            {
                value = default;
                return false;
            }

            return node.TryGetValue(out value);
        }
        public static bool TryGetValue<T>(this JsonNode node, out T value)
        {
            try
            {
                value = node.GetValue<T>();
                return true;
            }
            catch
            {

            }

            value = default;
            return false;
        }

        public static string JsonObject(params string[] items) => "{" + string.Join(", ", items) + "}";

        public static string FormatJson<T>(string name, T value) => string.Format("\"{0}\": {1}", name, JsonSerializer.Serialize(value));
    }

    public partial class TMDB : IListProvider
    {
        public static bool TryParseMovie(JsonNode json, out Models.Movie movie)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("title", out string title) && json.TryGetValue("release_date", out string releaseDate))
            {
                movie = new Models.Movie(title, DateTime.TryParse(releaseDate, out var year) ? (int?)year.Year : null).WithID(IDKey, id);
                return true;
            }

            movie = null;
            return false;
        }

        protected bool TryParseTVShow(JsonNode json, out Models.TVShow show)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("name", out string name) && json.TryGetValue("overview", out string overview) && json.TryGetValue("poster_path", out string poster_path))
            {
                show = GetItem(new TMDbLib.Objects.Search.SearchTv
                {
                    Id = id,
                    Name = name,
                    Overview = overview,
                    PosterPath = poster_path
                });
                return true;
            }

            show = null;
            return false;
        }

        private abstract class BaseList : Models.List
        {
            public override string ID => _ID?.ToString();
            protected HttpClient Client { get; }
            protected TMDB IDSystem { get; }

            protected object _ID;
            private string Token;

            public BaseList(object id, TMDB idSystem, string bearer, Func<IAsyncEnumerable<JsonNode>, IAsyncEnumerable<JsonNode>> cacheWrapper = null)
            {
                _ID = id;
                Client = new HttpClient
                {
                    DefaultRequestHeaders =
                    {
                        Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer)
                    }
                };
                IDSystem = idSystem;
                AllowedTypes = ItemType.Movie | ItemType.TVShow;
                Token = bearer;

                var temp = cacheWrapper == null ? GetItems() : cacheWrapper(GetItems());
                Items = temp.Select(json => TryParse(json, out var item) ? item : null);
            }

            protected abstract IAsyncEnumerable<JsonNode> GetItems();

            protected bool TryParse(JsonNode json, out Models.Item item)
            {
                item = null;
                var type = json["media_type"];

                if (type?.TryGetValue<string>() == "movie")
                {
                    if (TryParseMovie(json, out var movie))
                    {
                        item = movie;
                    }
                }
                else if (type?.TryGetValue<string>() == "tv")
                {
                    if (IDSystem.TryParseTVShow(json, out var show))
                    {
                        item = show;
                    }
                }

                return item != null;
            }

            public IAsyncEnumerable<JsonNode> FlattenPages(string apiCall) => TMDB.FlattenPages(Client, apiCall);

            protected bool TryGetId(Models.Item item, out int id) => IDSystem.TryGetID(item, out id);

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

            public List(string token, TMDB idSystem, Func<IAsyncEnumerable<JsonNode>, IAsyncEnumerable<JsonNode>> cacheWrapper = null) : this(null, token, idSystem, cacheWrapper) { }

            private List(object id, string token, TMDB idSystem, Func<IAsyncEnumerable<JsonNode>, IAsyncEnumerable<JsonNode>> cacheWrapper = null) : base(id, idSystem, token, cacheWrapper)
            {
                Client.BaseAddress = new Uri("https://api.themoviedb.org/4/");
            }

            public static List FromJson(JsonNode json, string token, TMDB idSystem, Func<IAsyncEnumerable<JsonNode>, IAsyncEnumerable<JsonNode>> cacheWrapper = null)
            {
                if (!json.TryGetValue("id", out int id))
                {
                    return null;
                }

                var list = new List(id, token, idSystem, cacheWrapper)
                {
                    Name = json["name"]?.TryGetValue<string>(),
                    PosterPath = (json["poster_path"]?.TryGetValue<string>() ?? json["backdrop_path"]?.TryGetValue<string>()) is string posterPath ? BuildImageURL(posterPath, POSTER_SIZE) : null,
                    Count = json.TryGetValue("number_of_items", out int count) ? count : json["total_results"]?.TryGetValue<int?>(),
                    Author = json["created_by"]?["username"]?.TryGetValue<string>()
                };

                if (json.TryGetValue("description", out string description) && !string.IsNullOrEmpty(description))
                {
                    list.Description = description;
                }

                var node = json["public"];
                list.Public = node != null && node.TryGetValue<int>(out var i) ? (i != 0) : (node?.TryGetValue<bool>() ?? false);

                list.DetailsBackup = list.DetailsAsJson();

                return list;
            }

            protected override async Task<bool> AddAsyncInternal(IEnumerable<Models.Item> items)
            {
                if (!TryFormatItems(items, out string json))
                {
                    return false;
                }

                //var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await Client.TryPostAsync(string.Format("list/{0}/items", ID), json);
                //var response = await Client.PostAsync(string.Format("https://api.themoviedb.org/4/list/{0}/items", ID), content);

                return response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
            }

            protected override async Task<bool?> ContainsAsyncInternal(Models.Item item)
            {
                if (TryGetMediaType(item, out var type) && TryGetId(item, out var itemId))
                {
                    //var response = await Client.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}/item_status?api_key=f0e327cdf818a4e5b6df2cbde7095c60&media_id={1}&media_type={2}", ID, itemId, type));
                    var response = await Client.TryGetAsync(string.Format("list/{0}/item_status?media_id={1}&media_type={2}", ID, itemId, type));

                    if (response != null)
                    {
                        return response.IsSuccessStatusCode && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
                    }
                }

                return null;
            }

            public override async Task<int> CountAsync()
            {
                var response = await Client.TryGetAsync(string.Format("list/{0}", ID));
                //var response = await Client.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}?page=1", ID));
                return response == null ? -1 : (JsonNode.Parse(await response.Content.ReadAsStringAsync())["total_results"]?.GetValue<int>() ?? -1);
            }

            public override async Task Delete()
            {
                var response = await Client.TrySendAsync(string.Format("list/{0}", ID), method: HttpMethod.Delete);
                //var response = await Client.DeleteAsync(string.Format("https://api.themoviedb.org/4/list/{0}", ID));

                //return JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"].GetValue<bool>();
            }

            protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Models.Item> items)
            {
                if (!TryFormatItems(items, out string json))
                {
                    return false;
                }

                var response = await Client.TrySendAsync(string.Format("list/{0}/items", ID), json, HttpMethod.Delete);
                //var response = await Client.SendAsync(request);

                return response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
            }

            private string DetailsAsJson() => JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("name", Name ?? string.Empty),
                JsonExtensions.FormatJson("description", Description ?? string.Empty),
                JsonExtensions.FormatJson("public", Public));

            public override async Task Update()
            {
                string json = DetailsAsJson();
                //return;

                if (ID == null)
                {
                    var iso = JsonExtensions.FormatJson("iso_639_1", LANGUAGE) + ", ";
                    //var response = await Client.TryPostAsync(".", json.Insert(1, iso));
                    var response = await Client.TryPostAsync("list", json.Insert(1, iso));
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
                    var response = await Client.TrySendAsync(string.Format("list/{0}", ID), json, HttpMethod.Put);
                    //var response = await Client.PutAsync(string.Format("https://api.themoviedb.org/4/list/{0}", ID), content);

                    if (!(response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true))
                    {
                        return;
                    }
                }

                DetailsBackup = json;
            }

            protected override async IAsyncEnumerable<JsonNode> GetItems()
            {
                if (ID == null)
                {
                    yield break;
                }

                await foreach (var item in FlattenPages(string.Format("list/{0}?page={{0}}&sort_by={1}", ID, Reverse ? "original_order.desc" : "original_order.asc")))
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

            public NamedList(string itemsPath, string statusPath, string bearer, string accountID, string sessionId, TMDB idSystem, Func<IAsyncEnumerable<JsonNode>, IAsyncEnumerable<JsonNode>> cacheWrapper = null) : base(itemsPath, idSystem, bearer, cacheWrapper)
            {
                AccountID = accountID;
                SessionID = sessionId;

                Client.BaseAddress = new Uri("https://api.themoviedb.org/");

                ItemsPath = itemsPath;
                StatusPath = statusPath;

                Reverse = true;
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
                        await Client.TryPostAsync(string.Format("3/account/{0}/" + StatusPath + "?session_id={1}", AccountID, SessionID), json);
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
                var responses = await Task.WhenAll(Client.TryGetAsync(string.Format("4/account/{0}/movie/" + ID.ToString(), AccountID)), Client.TryGetAsync(string.Format("4/account/{0}/tv/" + ID.ToString(), AccountID)));
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

            protected override async IAsyncEnumerable<JsonNode> GetItems()
            {
#if !DEBUG
                await foreach (var item in FlattenPages(string.Format("4/account/{0}/tv/" + ID + "?page={{0}}", AccountID)))
                {
                    var show = item.AsObject();
                    show.Add("media_type", "tv");
                    yield return show;
                }
#endif

                await foreach (var item in FlattenPages(string.Format("4/account/{0}/movie/" + ID + "?page={{0}}", AccountID)))
                {
                    var movie = item.AsObject();
                    movie.Add("media_type", "movie");
                    yield return movie;
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
            //yield return List.FromJson(JsonNode.Parse("{\"adult\":0,\"average_rating\":8.36,\"backdrop_path\":\"/xJHokMbljvjADYdit5fK5VQsXEG.jpg\",\"created_at\":\"2022-03-08 20:37:35\",\"description\":\"List from TMDb\",\"featured\":0,\"id\":8194544,\"iso_3166_1\":\"US\",\"iso_639_1\":\"en\",\"name\":\"TMDb List\",\"number_of_items\":1,\"public\":0,\"revenue\":\"701729206\",\"runtime\":169,\"sort_by\":1,\"updated_at\":\"2022-03-08 20:38:14\"}"), UserAccessToken, this, CacheItems);
            await Task.CompletedTask;
        }
#endif

        public Models.List CreateList() => new List(UserAccessToken, this, CacheItems);

        public IAsyncEnumerable<Models.List> GetAllListsAsync() => SafeFlattenPages(WebClient, string.Format("https://api.themoviedb.org/4/account/{0}/lists?page={{0}}", AccountID), UserAccessToken).Select(json => ParseList(json, out var value) ? value : default);

        private string GetCacheKey<T>(JsonNode json) => json.TryGetValue<int>("id", out var id) ? GetCacheKey<T>(id) : null;
        private string GetListItemCacheKey(JsonNode json) => json.TryGetValue<int>("id", out var id) ? (json["media_type"]?.TryGetValue<string>() == "tv" ? GetCacheKey<Models.TVShow>(id) : GetCacheKey<Models.Movie>(id)) : null;

        private bool ParseList(JsonNode json, out Models.List list)
        {
            list = List.FromJson(json, UserAccessToken, this, CacheItems);
            return list != null;
        }

        private IAsyncEnumerable<JsonNode> CacheItems(IAsyncEnumerable<JsonNode> items) => CacheStream(items, GetListItemCacheKey);
        private async IAsyncEnumerable<T> CacheStream<T>(IAsyncEnumerable<T> items, Func<T, string> getCacheKey)
        {
            await foreach (var item in items)
            {
                await AddToCacheAsync(getCacheKey(item), item);
                yield return item;
            }
        }

        public delegate bool TryParse<T>(JsonNode json, out T value);

        //public IAsyncEnumerable<T> FlattenPages<T>(string apiCall, Func<JsonNode, T> parse) => FlattenPages<T>(WebClient, apiCall, parse);
        public static async IAsyncEnumerable<JsonNode> SafeFlattenPages(HttpClient client, string apiCall, string bearer = null)
        {
            try
            {
                await foreach (var item in FlattenPages(client, apiCall, bearer))
                {
                    yield return item;
                }
            }
            finally { }
        }

        public static async IAsyncEnumerable<JsonNode> FlattenPages(HttpClient client, string apiCall, string bearer = null)
        {
            for (int page = 1; ; page++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, string.Format(apiCall, page));
                if (bearer != null)
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
                }
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }

                var parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                var results = parsed["results"]?.AsArray();
                //var totalPages = parsed["total_pages"]?.GetValue<int>();

                if (results == null || !parsed.TryGetValue("total_pages", out int totalPages))
                {
                    break;
                }

                foreach (var result in results)
                {
                    yield return result;
                }

                if (page >= totalPages)
                {
                    break;
                }
            }
        }

        public Task<Models.List> GetWatchlist() => Task.FromResult(GetNamedList("watchlist", "watchlist"));

        public Task<Models.List> GetFavorites() => Task.FromResult(GetNamedList("favorites", "favorite"));

        public Task<Models.List> GetHistory() => Task.FromResult<Models.List>(null);

        public async Task<Models.List> GetListAsync(string id)
        {
            //return new DummyList(id) { Name = "Tmdb list" };
            //var response = await WebClient.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}", 
            var response = await WebClient.TrySendAsync(new HttpRequestMessage(HttpMethod.Get, string.Format("4/list/{0}", id))
            {
                Headers =
                {
                    Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserAccessToken)
                }
            });

            return response?.IsSuccessStatusCode == true ? List.FromJson(JsonNode.Parse(await response.Content.ReadAsStringAsync()), UserAccessToken, this) : null;
        }

        private Models.List GetNamedList(string itemsPath, string statusPath) => new NamedList(itemsPath, statusPath, UserAccessToken, AccountID, SessionID, this, CacheItems);
    }

}
