using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public class Trakt : IAccount, IListProvider
    {
        public static readonly ID<int>.Key IDKey = new ID<int>.Key();
        public static readonly ID<int> ID = IDKey.ID;

        public TMDB TMDb { get; }
        public string Username { get; private set; }
        public string Name => Company.Name;
        public Company Company { get; } = new Company
        {
            Name = "Trakt"
        };
        public Uri RedirectUri { get; set; }

        private HttpClient Client;
        private string ClientID;
        private string ClientSecret;
        private string UserAccessToken;
        private string UserSlug;

        private Lazy<Task<List<Models.List>>> LazyAllLists;
        private Lazy<Task<JsonNode>> LastActivities;

        public Trakt(TMDB tmdb, string clientID, string clientSecret = null)
        {
            TMDb = tmdb;
            ClientID = clientID;
            ClientSecret = clientSecret;

            Client = new HttpClient
            {
                BaseAddress = new Uri("https://api.trakt.tv/")
            };

            Client.DefaultRequestHeaders.Add("trakt-api-key", ClientID);
            Client.DefaultRequestHeaders.Add("trakt-api-version", "2");

            LazyAllLists = new Lazy<Task<List<Models.List>>>(() => GetAllLists());
            LastActivities = new Lazy<Task<JsonNode>>(() => GetLastActivities());
        }

        private bool TryParseListMovie(JsonNode json, out Movie movie)
        {
            if (json["movie"] is JsonNode value && TryParseMovie(value, out movie))
            {
                return true;
            }

            movie = null;
            return false;
        }

        private bool TryParseListShow(JsonNode json, out TVShow show)
        {
            if (json["show"] is JsonNode value && TryParseTVShow(value, TMDb, out show))
            {
                return true;
            }

            show = null;
            return false;
        }

        public IAsyncEnumerable<Item> GetAnticpatedMoviesAsync() => FlattenPages(Client, "movies/anticipated").TrySelect<JsonNode, Movie>(TryParseListMovie);
        public IAsyncEnumerable<Item> GetRecommendedMoviesAsync() => FlattenPages(Client, "recommendations/movies").TrySelect<JsonNode, Movie>(TryParseListMovie);

        public IAsyncEnumerable<Item> GetAnticipatedTVAsync() => FlattenPages(Client, "shows/anticipated").TrySelect<JsonNode, TVShow>(TryParseListShow);
        public IAsyncEnumerable<Item> GetRecommendedTVAsync() => FlattenPages(Client, "recommendations/shows").TrySelect<JsonNode, TVShow>(TryParseListShow);

        private static async IAsyncEnumerable<JsonNode> FlattenPages(HttpClient client, string apiCall, string accessToken = null, int? pageSize = null, bool reverse = false)
        {
            int sign = 0;
            for (int page = 1; ; page += sign)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiCall + (pageSize.HasValue ? string.Format("?page={0}&limit={1}", page, pageSize.Value) : string.Empty));
                if (accessToken != null)
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }
                var response = await client.SendAsync(request);
                
                if (response?.IsSuccessStatusCode != true)
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }

                var totalPages = response.Headers.TryGetValues("x-pagination-page-count", out var pageCountHeaders) && int.TryParse(pageCountHeaders.FirstOrDefault(), out var temp) ? temp : 1;
                if (!pageSize.HasValue && response.Headers.TryGetValues("x-pagination-limit", out var pageLimitHeaders) && int.TryParse(pageLimitHeaders.FirstOrDefault(), out var limit))
                {
                    pageSize = limit;
                }

                if (sign == 0)
                {
                    string sortedHow = null;

                    if (response.RequestMessage.RequestUri.ToString().StartsWith("https://api.trakt.tv/sync/history"))
                    {
                        sortedHow = "desc";
                    }
                    else if (response.Headers.TryGetValues("x-applied-sort-how", out var sortHeaders))
                    {
                        sortedHow = sortHeaders.FirstOrDefault();
                    }
                    
                    if (sortedHow != null)
                    {
                        reverse = (sortedHow.ToLower() == "desc") != reverse;
                    }

                    if (reverse)
                    {
                        sign = -1;

                        if (totalPages > 1)
                        {
                            page = totalPages + 1;
                            continue;
                        }
                    }
                    else
                    {
                        sign = 1;
                    }
                }

                var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                var array = json.AsArray();

                foreach (var item in reverse ? array.Reverse() : array)
                {
                    yield return item;
                }

                if ((reverse && page <= 1) || (!reverse && page >= totalPages))
                {
                    break;
                }
            }
        }

        private static bool TryParseMovie(JsonNode json, out Movie movie)
        {
            if (json.TryGetValue("title", out string title))
            {
                movie = new Movie(title, json["year"]?.TryGetValue<int>());
                TryAddID(movie, json);
                return true;
            }

            movie = null;
            return false;
        }

        private static bool TryParseTVShow(JsonNode json, TMDB tmdb, out TVShow show)
        {
            if (json["ids"] is JsonNode temp && temp.TryGetValue("tmdb", out int id) && json.TryGetValue("title", out string title))
            {
                show = new TVShow(title)
                {
                    Description = json["description"]?.TryGetValue<string>()
                };
                TryAddID(show, json);
                show.Items = tmdb.GetCollectionItems(show, id);
                return true;
            }

            show = null;
            return false;
        }

        private static bool TryParsePerson(JsonNode json, out Person person)
        {
            if (json.TryGetValue("name", out string name))
            {
                person = new Person(name);
                TryAddID(person, json);
                return true;
            }

            person = null;
            return false;
        }

        private static bool TryParseItem(JsonNode json, TMDB tmdb, out Item item)
        {
            item = null;
            var type = json["type"]?.TryGetValue<string>();

            if (type == "movie")
            {
                if (json[type] is JsonNode movie && TryParseMovie(movie, out var temp))
                {
                    item = temp;
                }
            }
            else if (type == "show")
            {
                if (json[type] is JsonNode show && TryParseTVShow(show, tmdb, out var temp))
                {
                    item = temp;
                }
            }
            else if (type == "person")
            {
                if (json["person"] is JsonNode person && TryParsePerson(person, out var temp))
                {
                    item = temp;
                }
            }

            return item != null;
        }

        private static void TryAddID(Item item, JsonNode json)
        {
            if (json["ids"] is JsonNode ids)
            {
                if (ids.TryGetValue("tmdb", out int tmdbID))
                {
                    item.SetID(TMDB.IDKey, tmdbID);
                }
                if (ids.TryGetValue("trakt", out int traktID))
                {
                    item.SetID(IDKey, traktID);
                }
            }
        }

        public Task<string> GetOAuthURL(Uri redirectUri) => Task.FromResult(string.Format("https://trakt.tv/oauth/authorize?response_type=code&client_id={0}&redirect_uri={1}", ClientID, RedirectUri = redirectUri));

        public async Task<object> Login(object credentials)
        {
            JsonObject json = null;

            if (credentials != null)
            {
                string tokenProperty = "code";
                string grantType = "authorization_code";

                try
                {
                    json = JsonNode.Parse(credentials.ToString()).AsObject();

                    if (json.TryGetValue("refresh_token", out string refreshToken) && json.TryGetValue("expires_in", out int expiresIn) && json.TryGetValue("created_at", out long createdAt) && DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime + TimeSpan.FromSeconds(expiresIn) < DateTime.UtcNow)
                    {
                        credentials = "code=" + refreshToken;
                        tokenProperty = "refresh_token";
                        grantType = "refresh_token";
                        throw new JsonException("Access token expired");
                    }
                }
                catch (JsonException)
                {
                    if (credentials is Uri uri)
                    {
                        credentials = uri.Query;
                    }

                    var parameters = credentials.ToString().TrimStart('?').Split('&');
                    var code = parameters.Select(parameter => parameter.Split('=')).FirstOrDefault(parts => parts.FirstOrDefault() == "code")?.LastOrDefault();

                    if (code != null)
                    {
                        var response = await Client.TryPostAsync("oauth/token", JsonExtensions.JsonObject(
                            JsonExtensions.FormatJson(tokenProperty, code),
                            JsonExtensions.FormatJson("client_id", ClientID),
                            JsonExtensions.FormatJson("client_secret", ClientSecret),
                            JsonExtensions.FormatJson("redirect_uri", RedirectUri),
                            JsonExtensions.FormatJson("grant_type", grantType)));

                        if (response?.IsSuccessStatusCode == true)
                        {
                            json = JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsObject();
                        }
                    }
                }
            }

            if (json == null)
            {
                return credentials;
            }

            UserAccessToken = json["access_token"]?.TryGetValue<string>();
            Username = json["username"]?.TryGetValue<string>();
            UserSlug = json["user_slug"]?.TryGetValue<string>();

            if (Username == null || UserSlug == null)
            {
                var response = await Client.TrySendAsync(AuthedRequest(HttpMethod.Get, "users/settings"));

                if (response?.IsSuccessStatusCode == true)
                {
                    var user = JsonNode.Parse(await response.Content.ReadAsStringAsync())["user"];

                    if (user?.TryGetValue("username", out string username) == true)
                    {
                        Username = username;
                    }
                    if (user?["ids"]?.TryGetValue("slug", out string userSlug) == true)
                    {
                        UserSlug = userSlug;
                    }
                }
            }

            if (!json.ContainsKey("username"))
            {
                json.Add("username", Username);
            }
            if (!json.ContainsKey("user_slug"))
            {
                json.Add("user_slug", Username);
            }

            return json.ToJsonString();
        }

        public async Task<bool> Logout()
        {
            var response = await Client.TryPostAsync("oauth/revoke", JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("token", UserAccessToken),
                JsonExtensions.FormatJson("client_id", ClientID),
                JsonExtensions.FormatJson("client_secret", ClientSecret)));

            if (response?.IsSuccessStatusCode == true)
            {
                UserAccessToken = Username = UserSlug = null;
                return true;
            }

            return false;
        }

        private async Task<int?> TryGetId(Item item)
        {
            if (item.TryGetID(IDKey.ID, out int traktId))
            {
                return traktId;
            }
            else if (item.TryGetID(TMDB.IDKey.ID, out int tmdbId))
            {
                string type;
                if (item is Movie)
                {
                    type = "movie";
                }
                else if (item is TVShow)
                {
                    type = "show";
                }
                else if (item is TVEpisode)
                {
                    type = "episode";
                }
                else if (item is Person)
                {
                    type = "person";
                }
                else if (item is List)
                {
                    type = "list";
                }
                else
                {
                    return null;
                }

                var response = await Client.TrySendAsync(new HttpRequestMessage(HttpMethod.Get, string.Format("https://api.trakt.tv/search/{0}/{1}?type={2}", "tmdb", tmdbId, type)));

                if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsArray().FirstOrDefault() is JsonNode json && TryParseItem(json, TMDb, out var temp) && temp.TryGetID(IDKey.ID, out int id))
                {
                    return id;
                }
            }

            return null;
        }

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

        public class List : BaseList
        {
            protected override string ItemsEndpoint => string.Format("{0}/items", ID);
            protected override string AddEndpoint => string.Format("{0}/items", ID);
            protected override string RemoveEndpoint => string.Format("{0}/items/remove", ID);
            protected override string ReorderEndpoint => string.Format("{0}/items/reorder", ID);

            public List(Trakt trakt) : this(trakt, null) { }

            private List(Trakt trakt, object id) : base(trakt, id)
            {
                AllowedTypes = ItemType.AllMedia | ItemType.Person;
                Client.BaseAddress = new Uri("https://api.trakt.tv/users/me/lists/");
            }

            private static bool TryParseListID(JsonNode json, out int id)
            {
                if (json["ids"]?.TryGetValue("trakt", out id) == true)
                {
                    return true;
                }

                id = -1;
                return false;
            }

            public static bool TryParse(JsonNode json, Trakt trakt, out Models.List list)
            {
                list = null;

                if (!TryParseListID(json, out int id))
                {
                    return false;
                }

                var localList = new List(trakt, id)
                {
                    Name = json["name"]?.TryGetValue<string>(),
                    Description = json["description"]?.TryGetValue<string>(),
                    Count = json["item_count"]?.TryGetValue<int?>(),
                    Public = json["privacy"]?.TryGetValue<string>()?.ToLower() == "public"
                };

                if (DateTime.TryParse(json["updated_at"]?.TryGetValue<string>(), out DateTime date))
                {
                    localList.LastUpdated = date.ToUniversalTime();
                }

                localList.DetailsBackup = localList.DetailsAsJson();

                list = localList;
                return true;
            }

            public override Task Delete() => Client.TrySendAsync(ID, method: HttpMethod.Delete);

            private string DetailsBackup;
            private string DetailsAsJson() => JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("name", CleanString(Name)),
                JsonExtensions.FormatJson("description", CleanString(Description)),
                JsonExtensions.FormatJson("privacy", Public ? "public" : "private"));

            private string CleanString(string value) => (value ?? string.Empty).Trim();

            public override async Task Update()
            {
                string json = DetailsAsJson();

                if (ID == null)
                {
                    var response = await Client.TrySendAsync(new HttpRequestMessage(HttpMethod.Post, Client.BaseAddress.ToString())
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    });

                    if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && TryParseListID(parsed, out int id))
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
                    var response = await Client.TrySendAsync(ID, json, HttpMethod.Put);

                    if (response?.IsSuccessStatusCode != true)
                    {
                        return;
                    }
                }

                DetailsBackup = json;
            }
        }

        public abstract class BaseNamedList : BaseList
        {
            protected override string ItemsEndpoint => ID;
            protected override string AddEndpoint => ID;
            protected override string RemoveEndpoint => string.Format("{0}/remove", ID);
            protected override string ReorderEndpoint => string.Format("{0}/reorder", ID);

            public BaseNamedList(Trakt trakt, string endpoint, int? pageSize = null) : base(trakt, endpoint, pageSize)
            {
                AllowedTypes = ItemType.AllMedia;
                Client.BaseAddress = new Uri("https://api.trakt.tv/sync/");
            }

            public override Task Delete() => Task.CompletedTask;

            public override Task Update() => Task.CompletedTask;
        }

        public class NamedList : BaseNamedList
        {
            public NamedList(Trakt trakt, string endpoint, int? pageSize = null) : base(trakt, endpoint, pageSize) { }
        }

        public class History : BaseNamedList
        {
            protected override string ItemsEndpoint => string.Format("{0}/movies", ID);
            protected override string ReorderEndpoint => null;

            public History(Trakt trakt) : base(trakt, "history", 20)
            {
                AllowedTypes = ItemType.Movie | ItemType.TVEpisode;
            }

            protected override async Task<bool?> ContainsAsyncInternal(Item item)
            {
                if (await Trakt.TryGetId(item) is int id)
                {
                    string type;
                    if (item is Movie)
                    {
                        type = "movies";
                    }
                    else if (item is TVShow)
                    {
                        type = "shows";
                    }
                    else if (item is TVSeason)
                    {
                        type = "seasons";
                    }
                    else if (item is TVEpisode)
                    {
                        type = "episodes";
                    }
                    else
                    {
                        return false;
                    }

                    var response = await Client.TryGetAsync(string.Format("history/{0}/{1}", type, id));

                    if (response?.IsSuccessStatusCode == true)
                    {
                        return JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsArray().Count > 0;
                    }
                }

                return null;
            }

            public override async Task<int> CountAsync()
            {
                var response = await Client.TrySendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.trakt.tv/users/me/stats"));

                if (response?.IsSuccessStatusCode == true)
                {
                    var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    int total = 0;

                    foreach (var kvp in json.AsObject())
                    {
                        if ((kvp.Key.ToLower() == "movies" || kvp.Key.ToLower() == "shows") && kvp.Value?.TryGetValue("watched", out int count) == true)
                        {
                            total += count;
                        }
                    }

                    return total;
                }

                return 0;
            }
        }

        public Models.List CreateList() => new List(this);

        private HttpRequestMessage AuthedRequest(HttpMethod method, string requestUri) => AuthedRequest(method, requestUri, UserAccessToken);
        private static HttpRequestMessage AuthedRequest(HttpMethod method, string requestUri, string accessToken) => new HttpRequestMessage(method, requestUri)
        {
            Headers =
            {
                Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken)
            }
        };

        public async Task<Models.List> GetListAsync(string id)
        {
            if ((await LazyAllLists.Value).FirstOrDefault(list => list.ID == id) is Models.List temp)
            {
                return temp;
            }

            //return List.FromJson(JsonNode.Parse("{\"name\":\"Trakt List\",\"description\":\"List from Trakt\",\"privacy\":\"private\",\"display_numbers\":false,\"allow_comments\":true,\"sort_by\":\"rank\",\"sort_how\":\"asc\",\"created_at\":\"2022-04-03T17:38:23.000Z\",\"updated_at\":\"2022-04-03T17:57:47.000Z\",\"item_count\":2,\"comment_count\":0,\"likes\":0,\"ids\":{\"trakt\":23304032,\"slug\":\"trakt-list\"},\"user\":{\"username\":\"GreenMountainLabs\",\"private\":false,\"name\":\"\",\"vip\":false,\"vip_ep\":false,\"ids\":{\"slug\":\"greenmountainlabs\"}}}"), this);
            var response = await Client.SendAsync(AuthedRequest(HttpMethod.Get, string.Format("users/me/lists/{0}", id)));

            return response?.IsSuccessStatusCode == true && List.TryParse(JsonNode.Parse(await response.Content.ReadAsStringAsync()), this, out var list) ? list : null;
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

        public async Task<List<Models.List>> GetAllLists()
        {
            var lists = new List<Models.List>();
            var response = await Client.SendAsync(AuthedRequest(HttpMethod.Get, "users/me/lists"));

            if (response?.IsSuccessStatusCode == true)
            {
                var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

                foreach (var item in json.AsArray())
                {
                    if (List.TryParse(item, this, out var list))
                    {
                        lists.Add(list);
                    }
                }
            }

            return lists;
        }

        private async Task<JsonNode> GetLastActivities()
        {
            var response = await Client.TrySendAsync(AuthedRequest(HttpMethod.Get, "https://api.trakt.tv/sync/last_activities"));

            if (response?.IsSuccessStatusCode == true)
            {
                return JsonNode.Parse(await response.Content.ReadAsStringAsync());
            }
            else
            {
                return null;
            }
        }

        public async Task<Models.List> GetWatchlist()
        {
            var list = new NamedList(this, "watchlist");
            var json = await LastActivities.Value;

            if (DateTime.TryParse(json?["watchlist"]?["updated_at"]?.TryGetValue<string>(), out DateTime date) == true)
            {
                list.LastUpdated = date.ToUniversalTime();
            }

            return list;
        }

        public Task<Models.List> GetFavorites() => Task.FromResult<Models.List>(null);

        public async Task<Models.List> GetHistory()
        {
            var list = new History(this);
            return list;
            var json = await LastActivities.Value;

            if (DateTime.TryParse(json?["movies"]?["watched_at"].TryGetValue<string>(), out DateTime date) == true)
            {
                list.LastUpdated = date.ToUniversalTime();
            }

            return list;
        }
    }
}
