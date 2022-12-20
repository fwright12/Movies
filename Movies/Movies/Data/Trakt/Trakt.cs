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
    public partial class Trakt : IListProvider
    {
        public static readonly ID<int>.Key IDKey = new ID<int>.Key();
        public static readonly ID<int> ID = IDKey.ID;

        public TMDB TMDb { get; }
        
        private HttpClient Client;

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

        public IAsyncEnumerable<Item> GetAnticpatedMoviesAsync() => FlattenPages(Client, "movies/anticipated").TrySelect<JsonNode, Movie>(TryParseListMovie);
        public IAsyncEnumerable<Item> GetRecommendedMoviesAsync() => FlattenPages(Client, "recommendations/movies").TrySelect<JsonNode, Movie>(TryParseMovie);

        public IAsyncEnumerable<Item> GetAnticipatedTVAsync() => FlattenPages(Client, "shows/anticipated").TrySelect<JsonNode, TVShow>(TryParseListShow);
        public IAsyncEnumerable<Item> GetRecommendedTVAsync() => FlattenPages(Client, "recommendations/shows").TrySelect<JsonNode, TVShow>((JsonNode json, out TVShow show) => TryParseTVShow(json, TMDb, out show));

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

                    if (response.RequestMessage?.RequestUri.ToString().StartsWith("https://api.trakt.tv/sync/history") == true)
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
