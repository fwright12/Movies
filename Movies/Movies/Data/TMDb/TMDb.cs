using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB : IAssignID<int>
    {
        public static Language LANGUAGE = CultureInfo.CurrentCulture;
        public static Language FALLBACK_LANGUAGE { get; set; } = new CultureInfo("en");
        public static Region REGION = RegionInfo.CurrentRegion;
        public static Region FALLBACK_REGION { get; set; } = new RegionInfo("US");
        public static bool ADULT { get; set; } = false;

        public static readonly string APPEND_TO_RESPONSE = "append_to_response";

        public static string ISO_639_1 => LANGUAGE.Iso_639;
        public static string ISO_3166_1 => REGION.Iso_3166;

        public static readonly Models.Company TMDb = new Models.Company
        {
            Name = "TMDb"
        };

        public static readonly ID<int>.Key IDKey = new ID<int>.Key();
        public static readonly ID<int> ID = IDKey.ID;

        private AuthenticationHeaderValue Auth { get; }
        private AuthenticationHeaderValue UserAuth { get; set; }

        private string ApiKey;

        private Lazy<Task<List<Models.List>>> LazyAllLists;

        public static HttpClient WebClient { get; private set; }

        public TMDB(string apiKey, string bearer, IJsonCache cache = null)
        {
            ApiKey = apiKey;
            Auth = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
            ResponseCache = cache;

            HandleDataRequests(DataService.Instance);

            //Config = Client.GetConfigAsync();
#if DEBUG && true
            WebClient = new HttpClient(new MockHandler())
            {
                BaseAddress = new Uri("https://mock.themoviedb/"),
#else
            WebClient = new HttpClient
            {
                BaseAddress = new Uri(BASE_ADDRESS),
#endif
                DefaultRequestHeaders =
                {
                    Authorization = Auth
                }
            };

            CleanCacheTask = CleanCache(PROPERTY_VALUES_CACHE_DURATION);
            GetPropertyValues = InitValues();

            LazyAllLists = new Lazy<Task<List<Models.List>>>(GetAllLists);
            LoadChangeKeys = GetChangeKeys();

            foreach (var properties in ITEM_PROPERTIES.Values)
            {
                properties.ChangeKeys = ChangeKeys;
                properties.ChangeKeysLoaded = LoadChangeKeys;
            }
        }

        public Task LoadChangeKeys { get; }

        public IAsyncEnumerable<Item> GetTrendingAsync<T>(string mediaType, AsyncEnumerable.TryParseFunc<JsonNode, T> parse, string timeWindow = "week") where T : Item => FlattenPages(new ParameterizedPagedRequest(API.TRENDING.GET_TRENDING, mediaType, timeWindow), parse);

        public IAsyncEnumerable<Item> GetRecommendedMoviesAsync() => FlattenPages<Movie>(new PagedTMDbRequest($"account/{AccountID}/movie/recommendations") { Version = 4, Authorization = UserAuth }, TryParseMovie);
        public IAsyncEnumerable<Item> GetTrendingMoviesAsync(string timeWindow = "week") => GetTrendingAsync<Movie>("movie", TryParseMovie, timeWindow);
        public IAsyncEnumerable<Item> GetPopularMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_POPULAR, TryParseMovie);
        public IAsyncEnumerable<Item> GetTopRatedMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_TOP_RATED, TryParseMovie);
        public IAsyncEnumerable<Item> GetNowPlayingMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_NOW_PLAYING, TryParseMovie);
        public IAsyncEnumerable<Item> GetUpcomingMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_UPCOMING, TryParseMovie);

        public IAsyncEnumerable<Item> GetRecommendedTVShowsAsync() => FlattenPages<TVShow>(new PagedTMDbRequest($"account/{AccountID}/tv/recommendations") { Version = 4, Authorization = UserAuth }, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTrendingTVShowsAsync(string timeWindow = "week") => GetTrendingAsync<TVShow>("tv", TryParseTVShow, timeWindow);
        public IAsyncEnumerable<Item> GetPopularTVShowsAsync() => FlattenPages<TVShow>(API.TV.GET_POPULAR, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTopRatedTVShowsAsync() => FlattenPages<TVShow>(API.TV.GET_TOP_RATED, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVOnAirAsync() => FlattenPages<TVShow>(API.TV.GET_TV_ON_THE_AIR, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVAiringTodayAsync() => FlattenPages<TVShow>(API.TV.GET_TV_AIRING_TODAY, TryParseTVShow);

        public IAsyncEnumerable<Item> GetTrendingPeopleAsync(string timeWindow = "week") => GetTrendingAsync<Person>("person", TryParsePerson, timeWindow);
        public IAsyncEnumerable<Item> GetPopularPeopleAsync() => FlattenPages<Person>(API.PEOPLE.GET_POPULAR, TryParsePerson);

        private static string BuildImageURL(object path) => BuildImageURL(path?.ToString());
        private static string BuildImageURL(string path, string size = "/original") => path == null ? null : (SECURE_BASE_IMAGE_URL + size + path);
        private static string BuildVideoURL(string site, string key)
        {
            if (site == "YouTube")
            {
                return "https://www.youtube.com/embed/" + key;
            }
            else if (site == "Vimeo")
            {
                return "https://player.vimeo.com/video/" + key;
            }

            return null;
        }

        public static string GetFullSizeImage(string path)
        {
            if (path != null && path.StartsWith(SECURE_BASE_IMAGE_URL) && path.Split('/').LastOrDefault() is string filePath)
            {
                path = SECURE_BASE_IMAGE_URL + "/original/" + filePath;
            }

            return path;
        }

        public static string BuildApiCall(string endpoint, params string[] parameters) => BuildApiCall(endpoint, (IEnumerable<string>)parameters);
        public static string BuildApiCall(string endpoint, IEnumerable<string> parameters)
        {
            var url = endpoint;

            if (parameters.Any())
            {
                url += "?" + string.Join('&', parameters);
            }

            return url;
        }

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }

        public static IAsyncEnumerable<T> Request<T>(IPagedRequest request, AsyncEnumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters) => Request(request, Helpers.LazyRange(1, 1), parse, reverse, parameters);
        public static IAsyncEnumerable<T> Request<T>(IPagedRequest request, IEnumerable<int> pages, AsyncEnumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters)
        {
            var jsonParser = parse == null ? new JsonNodeParser<T>() : new JsonNodeParser<T>(parse);
            return Request(WebClient.GetPagesAsync(request, pages, default, parameters).SelectAsync(GetJson), jsonParser, reverse);
        }
        public static async IAsyncEnumerable<T> Request<T>(IAsyncEnumerable<JsonNode> pages, IJsonParser<T> parse = null, bool reverse = false)
        {
            await foreach (var json in pages)
            {
                var items = ParseCollection(json, PageParser, parse);

                if (!items.Any())
                {
                    break;
                }
                else if (reverse)
                {
                    items = items.Reverse();
                }

                foreach (var item in items)
                {
                    yield return item;
                }
            }
        }

        public static async Task<JsonNode> GetJson(HttpResponseMessage response) => JsonNode.Parse(await response.Content.ReadAsStringAsync());
        public static async Task<JsonNode> GetJson(Task<HttpResponseMessage> response) => JsonNode.Parse(await (await response).Content.ReadAsStringAsync());

        private static readonly IJsonParser<IEnumerable<JsonNode>> PageParser = new JsonPropertyParser<IEnumerable<JsonNode>>("results");

        public static IEnumerable<T> ParseCollection<T>(JsonNode json, IJsonParser<IEnumerable<JsonNode>> parseItems, IJsonParser<T> parse = null) => TryParseCollection(json, parseItems, out var results, parse) ? results : System.Linq.Enumerable.Empty<T>();

        public static bool TryParseCollection<T>(JsonNode json, IJsonParser<IEnumerable<JsonNode>> parseItems, out IEnumerable<T> result, IJsonParser<T> parse = null)
        {
            if (parseItems.TryGetValue(json, out var items))
            {
                result = ParseCollection(items, parse);
                return true;
            }

            result = null;
            return false;
        }

        public static IEnumerable<T> ParseCollection<T>(IEnumerable<JsonNode> items, IJsonParser<T> parse = null, params Parser[] parsers)
        {
            parse ??= new JsonNodeParser<T>();

            foreach (var item in items)
            {
                if (parse.TryGetValue(item, out var parsed))
                {
                    var cache = (parsed as Credit)?.Person ?? parsed as Item;

                    if (cache != null)
                    {
                        CacheItem(cache, item, parsers);
                    }

                    if (NotAdult(item) || cache is Person)
                    {
                        yield return parsed;
                    }
                }
            }
        }

        public static IAsyncEnumerable<T> FlattenPages<T>(IPagedRequest request, AsyncEnumerable.TryParseFunc<JsonNode, T> parse = null, params string[] parameters) => Request(request, parse, false, parameters);// FlattenPages(request, Helpers.LazyRange(0, 1), parse, false, parameters);

        private static bool NotAdult(JsonNode json) => !json.TryGetValue("adult", out bool adult) || !adult;

        private static void CacheItem(Item item, JsonNode json, params Parser[] parsers)
        {
            IEnumerable<Parser> temp = parsers;

            if (parsers.Length == 0)
            {
                if (!ITEM_PROPERTIES.TryGetValue(item.ItemType, out var properties))
                {
                    return;
                }

                temp = properties.Info.Values.SelectMany(parsers => parsers);
            }

            CacheItem(item, json, temp);
        }

        private static void CacheItem(Item item, JsonNode json, IEnumerable<Parser> parsers)
        {
            var dict = Data.GetDetails(item);

            foreach (var parser in parsers)
            {
                if (parser.Property == TVShow.SEASONS)
                {
                    if (item is TVShow show && json["seasons"] is JsonArray array)
                    {
                        var items = ParseCollection(array, new JsonNodeParser<TVSeason>((JsonNode json, out TVSeason season) => TMDB.TryParseTVSeason(json, show, out season)));
                        dict.Add(TVShow.SEASONS, Task.FromResult(items));

                        Data.ResourceCache.Put(new UniformItemIdentifier(item, TVShow.SEASONS), items);
                    }
                }
                else if (parser.Property == TVSeason.EPISODES)
                {
                    if (item is TVSeason season && json["episodes"] is JsonArray array)
                    {
                        var items = ParseCollection(array, new JsonNodeParser<TVEpisode>((JsonNode json, out TVEpisode episode) => TMDB.TryParseTVEpisode(json, season, out episode)));
                        dict.Add(TVSeason.EPISODES, Task.FromResult(items));

                        Data.ResourceCache.Put(new UniformItemIdentifier(item, TVSeason.EPISODES), items);
                    }
                }
                else if (parser is IJsonParser<PropertyValuePair> pvp && pvp.TryGetValue(json, out var pair))
                {
                    dict.Add(pair);
                    //dict.Add(parser.GetPair(Task.FromResult(property.Value)));
                    //break;

                    Data.ResourceCache.Put(new UniformItemIdentifier(item, pair.Property), pair.Value);
                }
            }
        }

        private static DataService Data;

        public void HandleDataRequests(DataService manager)
        {
            Data = manager;

            manager.GetItemDetails += (sender, e) =>
            {
                var item = (Item)sender;

                if (ITEM_PROPERTIES.TryGetValue(item.ItemType, out var properties))
                {
                    properties.HandleRequests(item, e.Properties, this);
                }
            };
        }

        bool IAssignID<int>.TryGetID(Models.Item item, out int id) => TryGetID(item, out id);

        public static bool TryGetID(Models.Item item, out int id)
        {
            // Use search to guess ID if not assigned

            return item.TryGetID(ID, out id);
        }

        Task<Item> IAssignID<int>.GetItem(ItemType type, int id) => GetItem(type, id);

        public static Dictionary<int, Collection> CollectionCache = new Dictionary<int, Collection>();
        private static readonly SemaphoreSlim CollectionCacheSemaphore = new SemaphoreSlim(1, 1);

        public static async Task<Collection> GetCollection(int id)
        {
            Collection result = null;

            await CollectionCacheSemaphore.WaitAsync();
            CollectionCache.TryGetValue(id, out result);
            CollectionCacheSemaphore.Release();

            if (result != null)
            {
                return result;
            }

            var url = string.Format(API.COLLECTIONS.GET_DETAILS.GetURL(), id);
            var response = await WebClient.TryGetAsync(url);

            if (response?.IsSuccessStatusCode == true)
            {
                var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

                if (TryParseCollection(json, out var collection))
                {
                    await CollectionCacheSemaphore.WaitAsync();
                    CollectionCache[id] = collection;
                    CollectionCacheSemaphore.Release();

                    return collection;
                }
            }

            return null;
        }

        private static async Task<JsonNode> Convert(HttpContent content) => JsonNode.Parse(await content.ReadAsByteArrayAsync());

        public static async Task<Item> GetItem(ItemType type, int id)
        {
            if (type == ItemType.Collection)
            {
                return await GetCollection(id);
            }
            else if (ITEM_PROPERTIES.TryGetValue(type, out var properties))
            {
                Item item = null;
                var requests = properties.Info.Keys.ToList();
                var responses = Request(requests, new object[] { id }).Select(Convert).ToArray();

                if (type == ItemType.Movie)
                {
                    int index = requests.IndexOf(API.MOVIES.GET_DETAILS);

                    if (index != -1 && TryParseMovie(await responses[index], out var movie))
                    {
                        item = movie;
                    }
                }
                else if (type == ItemType.TVShow)
                {
                    int index = requests.IndexOf(API.TV.GET_DETAILS);

                    if (index != -1 && TryParseTVShow(await responses[index], out var show))
                    {
                        item = show;
                    }
                }
                else if (type == ItemType.TVSeason)
                {

                }
                else if (type == ItemType.TVEpisode)
                {

                }
                else if (type == ItemType.Person)
                {
                    int index = requests.IndexOf(API.PEOPLE.GET_DETAILS);

                    if (index != -1 && TryParsePerson(await responses[index], out var person))
                    {
                        item = person;
                    }

                    /*if (details.TryGetValue(Person.NAME, out var name))
                    {
                        item = new Person(await name).WithID(IDKey, id);
                    }*/
                }
                else
                {
                    return null;
                }

                if (item != null)
                {
                    item.SetID(IDKey, id);

                    for (int i = 0; i < requests.Count; i++)
                    {
                        if (properties.Info.TryGetValue(requests[i], out var parsers))
                        {
                            CacheItem(item, await responses[i], parsers);
                        }
                    }

                    //var cached = Data.GetDetails(item);
                    //cached.Add(details);

                    //handler.Item = item;
                }

                return item;
            }
            else if (type == ItemType.List)
            {

            }
            else if (type == ItemType.Company)
            {

            }
            else if (type == ItemType.Network)
            {

            }
            else if (type == ItemType.WatchProvider)
            {

            }

            return null;
        }
    }
}