using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Async;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public interface IPagedRequest
    {
        public string Endpoint { get; }

        public string GetURL(int page, params string[] parameters) => TMDB.BuildApiCall(Endpoint, parameters.Prepend($"page={page}"));
        public int? GetTotalPages() => null;
        public Task<int?> GetTotalPages(HttpResponseMessage response);
    }

    public static class Helpers
    {
        public static async Task<int?> GetTotalPages(HttpResponseMessage response, IJsonParser<int> parse) => parse?.TryGetValue(JsonNode.Parse(await response.Content.ReadAsStringAsync()), out var result) == true ? result : (int?)null;

        public static IEnumerable<int> LazyRange(int start, int step = 1)
        {
            while (true)
            {
                yield return (start += step) - step;
            }
        }

        public static async IAsyncEnumerable<HttpResponseMessage> GetPagesAsync(this HttpClient client, IPagedRequest request, IEnumerable<int> pages, CancellationToken cancellationToken = default, params string[] parameters)
        {
            int? totalPages = null;
            var pageItr = pages.GetEnumerator();
            int page;

            do
            {
                if (!pageItr.MoveNext())
                {
                    break;
                }

                if (pageItr.Current < 0)
                {
                    if (totalPages.HasValue)
                    {
                        page = totalPages.Value + pageItr.Current;
                    }
                    else
                    {
                        // Cause a query of page 0 so we can get total pages
                        page = 0;
                    }
                }
                else
                {
                    page = pageItr.Current;
                }

                var pageRequest = request.GetURL(page, parameters);
                var response = await client.GetAsync(pageRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }

                totalPages = await request.GetTotalPages(response);

                if (page == pageItr.Current || page == totalPages + pageItr.Current)
                {
                    yield return response;
                }
            }
            while (page >= 0 && page < totalPages);
        }

        public static async IAsyncEnumerable<T> FlattenPages<T>(IAsyncEnumerable<HttpResponseMessage> pages, IJsonParser<IEnumerable<T>> parse, bool reverse = false)
        {
            await foreach (var page in pages)
            {
                var json = JsonNode.Parse(await page.Content.ReadAsStringAsync());

                if (parse.TryGetValue(json, out var items))
                {
                    if (reverse)
                    {
                        items = items.Reverse();
                    }

                    foreach (var item in items)
                    {
                        yield return item;
                    }
                }
            }
        }
    }

    public class ParameterizedPagedRequest : IPagedRequest
    {
        public IPagedRequest Request { get; }
        public string Endpoint => Request.Endpoint;

        private object[] Parameters;

        public ParameterizedPagedRequest(PagedTMDbRequest request, params object[] parameters)
        {
            Request = request;
            Parameters = parameters;
        }

        public string GetURL(int page, params string[] parameters) => string.Format(Request.GetURL(page, parameters), Parameters);

        public Task<int?> GetTotalPages(HttpResponseMessage response) => Request.GetTotalPages(response);
    }

    public partial class TMDB : IDataProvider, IAccount, IAssignID<int>
    {
        public static string LANGUAGE { get; set; } = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        public static string REGION { get; set; } = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
        public static bool ADULT { get; set; } = false;

        public static string ISO_639_1 => LANGUAGE;
        public static string ISO_3166_1 => REGION;

        public static readonly Models.Company TMDb = new Models.Company
        {
            Name = "TMDb"
        };

        public static readonly ID<int>.Key IDKey = new ID<int>.Key();
        public static readonly ID<int> ID = IDKey.ID;

        private static readonly string POSTER_SIZE = "/w342";
        private static readonly string PROFILE_SIZE = "/original";
        private static readonly string STILL_SIZE = "/original";
        private static readonly string STREAMING_LOGO_SIZE = "/w45";
        private static readonly string LOGO_SIZE = "/w300";

        public Models.Company Company { get; } = TMDb;
        public string Name => Company.Name;
        public string Username { get; private set; }

        private AuthenticationHeaderValue Auth { get; }

        private string UserAccessToken;
        private string AccountID;
        private string SessionID;
        private string ApiKey;

        private Lazy<Task<List<Models.List>>> LazyAllLists;

#if DEBUG
        private TMDbClient Client
        {
            get
            {
                Print.Log("accessing Client");
                return _Client;
            }
            set => _Client = value;
        }
        private TMDbClient _Client = TMDbClient.Create();

        private static readonly string SECURE_BASE_IMAGE_URL = string.Empty;// = "https://image.tmdb.org/t/p";
#else
        private TMDbClient Client;
        private static readonly string SECURE_BASE_IMAGE_URL = "https://image.tmdb.org/t/p";
#endif
        private DataManager DataManager;
        public static HttpClient WebClient { get; private set; }

        public TMDB(string apiKey, string bearer)
        {
            ApiKey = apiKey;
            Auth = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);

            RatingParser.TMDb = this;

            Test(ViewModels.ItemViewModel.Data);
            //Config = Client.GetConfigAsync();
            WebClient = new HttpClient
            {
#if DEBUG
                BaseAddress = new Uri("https://mockTMDb"),
#else
                BaseAddress = new Uri("https://api.themoviedb.org/"),
#endif
                DefaultRequestHeaders =
                {
                    Authorization = Auth
                }
            };

            LazyAllLists = new Lazy<Task<List<Models.List>>>(() => GetAllLists());
        }

        private string RequestToken;

        public async Task<string> GetOAuthURL(Uri redirectUri)
        {
            var response = await WebClient.TrySendAsync("4/auth/request_token", JsonExtensions.JsonObject(JsonExtensions.FormatJson("redirect_to", redirectUri)), HttpMethod.Post);

            if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode json && json["success"]?.GetValue<bool>() == true)
            {
                RequestToken = json["request_token"]?.GetValue<string>();
                return string.Format("https://www.themoviedb.org/auth/access?request_token={0}", RequestToken);
            }

            return null;
        }

        public async Task<object> Login(object credentials)
        {
            JsonNode json = null;

            if (RequestToken != null)
            {
                var response = await WebClient.TryPostAsync("4/auth/access_token", JsonExtensions.JsonObject(JsonExtensions.FormatJson("request_token", RequestToken)));

                if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && parsed["success"]?.GetValue<bool>() == true)
                {
                    json = parsed;
                    RequestToken = null;
                }
            }
            else if (credentials != null)
            {
                try
                {
                    json = JsonNode.Parse(credentials.ToString());
                }
                catch (JsonException) { }
            }

            if (json == null)
            {
                return credentials;
            }

            UserAccessToken = json["access_token"]?.TryGetValue<string>();
            AccountID = json["account_id"]?.TryGetValue<string>();
            SessionID = json["session_id"]?.TryGetValue<string>();
            Username = json["username"]?.TryGetValue<string>();

            if (SessionID == null)
            {
                //var content = new StringContent(JsonObject(FormatJson("access_token", UserAccessToken)), Encoding.UTF8, "application/json");
                //var response = await WebClient.PostAsync(string.Format("https://api.themoviedb.org/3/authentication/session/convert/4"), content);
                var response = await WebClient.TryPostAsync(string.Format("3/authentication/session/convert/4"), JsonExtensions.JsonObject(JsonExtensions.FormatJson("access_token", UserAccessToken)));

                if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && parsed["success"]?.GetValue<bool>() == true)
                {
                    SessionID = parsed["session_id"]?.TryGetValue<string>();
                }
            }

            if (Username == null)
            {
                //var response = await WebClient.GetAsync(string.Format("https://api.themoviedb.org/3/account?session_id={0}", SessionID));
                var response = await WebClient.TryGetAsync(string.Format("3/account?session_id={0}", SessionID));

                if (response?.IsSuccessStatusCode == true)
                {
                    var parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    Username = parsed["username"]?.TryGetValue<string>();// ?? parsed["name"]?.GetValue<string>();
                    //AccountID = parsed["id"]?.TryGetValue<int>().ToString();
                }
            }

            return JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("access_token", UserAccessToken),
                JsonExtensions.FormatJson("account_id", AccountID),
                JsonExtensions.FormatJson("session_id", SessionID),
                JsonExtensions.FormatJson("username", Username));
        }

        public async Task<bool> Logout()
        {
            /*HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonObject(FormatJson("access_token", UserAccessToken)), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("https://api.themoviedb.org/4/auth/access_token"),
            };*/

            //var response = await WebClient.SendAsync(request);
            var response = await WebClient.TrySendAsync("4/auth/access_token", JsonExtensions.JsonObject(JsonExtensions.FormatJson("access_token", UserAccessToken)), HttpMethod.Delete);

            if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true)
            {
                UserAccessToken = Username = AccountID = SessionID = null;
                return true;
            }

            return false;
        }

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
        public static string BuildApiCall(string endpoint, IEnumerable<string> parameters) => endpoint + "?" + string.Join('&', parameters);

        public static async Task<IEnumerable<T>> Request<T>(TMDbRequest request, IJsonParser<IEnumerable<JsonNode>> parseItems, IJsonParser<T> parse = null, params string[] parameters)
        {
            string url = request.GetURL(null, null, false, parameters);
            return ParseCollection(await GetJson(await WebClient.TryGetAsync(url)), parseItems, parse);
        }

        public static IAsyncEnumerable<T> Request<T>(IPagedRequest request, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters) => Request(request, Helpers.LazyRange(1, 1), parse, reverse, parameters);
        public static IAsyncEnumerable<T> Request<T>(IPagedRequest request, IEnumerable<int> pages, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters)
        {
            var jsonParser = parse == null ? new JsonNodeParser<T>() : new JsonNodeParser<T>(parse);
            return Request(WebClient.GetPagesAsync(request, pages, default, parameters).SelectAsync(GetJson), jsonParser, reverse);
        }
        public static async IAsyncEnumerable<T> Request<T>(IAsyncEnumerable<JsonNode> pages, IJsonParser<T> parse = null, bool reverse = false)
        {
            await foreach (var json in pages)
            {
                foreach (var item in ParseCollection(json, PageParser, parse))
                {
                    yield return item;
                }
            }
        }

        public static async Task<JsonNode> GetJson(HttpResponseMessage response) => JsonNode.Parse(await response.Content.ReadAsStringAsync());

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

                    if (NotAdult(item))
                    {
                        yield return parsed;
                    }
                }
            }
        }

        public static IAsyncEnumerable<T> FlattenPages<T>(IPagedRequest request, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, params string[] parameters) => Request(request, parse, false, parameters);// FlattenPages(request, Helpers.LazyRange(0, 1), parse, false, parameters);

        private static bool NotAdult(JsonNode json) => !json.TryGetValue("adult", out bool adult) || !adult;

        public IAsyncEnumerable<Item> GetTrendingAsync<T>(string mediaType, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse, string timeWindow = "week") where T : Item => FlattenPages(new PagedTMDbRequest($"trending/{mediaType}/{timeWindow}"), parse);

        public IAsyncEnumerable<Item> GetRecommendedMoviesAsync() => FlattenPages<Movie>(new PagedTMDbRequest($"4/account/{AccountID}/movie/recommendations"), TryParseMovie);
        public IAsyncEnumerable<Item> GetTrendingMoviesAsync(string timeWindow = "week") => GetTrendingAsync<Movie>("movie", TryParseMovie, timeWindow);
        public IAsyncEnumerable<Item> GetPopularMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_POPULAR, TryParseMovie);
        public IAsyncEnumerable<Item> GetTopRatedMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_TOP_RATED, TryParseMovie);
        public IAsyncEnumerable<Item> GetNowPlayingMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_NOW_PLAYING, TryParseMovie);
        public IAsyncEnumerable<Item> GetUpcomingMoviesAsync() => FlattenPages<Movie>(API.MOVIES.GET_UPCOMING, TryParseMovie);

        public IAsyncEnumerable<Item> GetRecommendedTVShowsAsync() => FlattenPages<TVShow>(new PagedTMDbRequest($"4/account/{AccountID}/tv/recommendations"), TryParseTVShow);
        public IAsyncEnumerable<Item> GetTrendingTVShowsAsync(string timeWindow = "week") => GetTrendingAsync<TVShow>("tv", TryParseTVShow, timeWindow);
        public IAsyncEnumerable<Item> GetPopularTVShowsAsync() => FlattenPages<TVShow>(API.TV.GET_POPULAR, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTopRatedTVShowsAsync() => FlattenPages<TVShow>(API.TV.GET_TOP_RATED, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVOnAirAsync() => FlattenPages<TVShow>(API.TV.GET_TV_ON_THE_AIR, TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVAiringTodayAsync() => FlattenPages<TVShow>(API.TV.GET_TV_AIRING_TODAY, TryParseTVShow);

        public IAsyncEnumerable<Item> GetTrendingPeopleAsync(string timeWindow = "week") => GetTrendingAsync<Person>("person", TryParsePerson, timeWindow);
        public IAsyncEnumerable<Item> GetPopularPeopleAsync() => FlattenPages<Person>(API.PEOPLE.GET_POPULAR, TryParsePerson);

        public static JsonExtensions.ICache ResponseCache;

        public static string GetURL(TMDbRequest request, params string[] parameters) => request.GetURL(null, null, false, parameters);
        public HttpRequestMessage GetMessage(TMDbRequest request) => request.GetMessage(Auth, LANGUAGE, REGION);

        private static readonly IJsonParser<WatchProvider> PROVIDER_PARSER = new JsonNodeParser<WatchProvider>(TryParseWatchProvider);

        private Task InitValues() => Task.WhenAll(new Task[]
        {
            SetValues(Movie.GENRES, API.GENRES.GET_MOVIE_LIST, GENRES_PARSER),
            SetValues(TVShow.GENRES, API.GENRES.GET_TV_LIST, GENRES_PARSER),
            SetValues(Movie.CONTENT_RATING, API.CERTIFICATIONS.GET_MOVIE_CERTIFICATIONS, new JsonNodeParser<IEnumerable<string>>(TryParseCertifications)),
            SetValues(Movie.WATCH_PROVIDERS, API.WATCH_PROVIDERS.GET_MOVIE_PROVIDERS, PROVIDER_PARSER),
            SetValues(TVShow.WATCH_PROVIDERS, API.WATCH_PROVIDERS.GET_TV_PROVIDERS, PROVIDER_PARSER),
        });

        private Task SetValues<T>(Property<T> property, TMDbRequest request, IJsonParser<T> parser) => SetValues(property, request, new JsonArrayParser<T>(parser));
        private async Task SetValues<T>(Property<T> property, TMDbRequest request, IJsonParser<IEnumerable<T>> parser)
        {
            if (property.Values is ICollection<T> list)
            {
                //var apiCall = new ApiCall<IEnumerable<T>>(endpoint, new JsonArrayParser<T>(parser));

                var response = await WebClient.TryGetCachedAsync(GetMessage(request), ResponseCache);

                if (JsonParser.TryParse(response.Json, parser, out var values))
                {
                    foreach (var value in values)
                    {
                        list.Add(value);
                    }
                }
            }
        }

        private static bool TryParseCertifications(JsonNode json, out IEnumerable<string> certifications) => TryParseCertifications(json, REGION, out certifications);
        private static bool TryParseCertifications(JsonNode json, string region, out IEnumerable<string> certifications)
        {
            if (json.TryGetValue("certifications", out json))
            {
                if (json.TryGetValue(region, out JsonArray array) && new JsonArrayParser<string>(new JsonPropertyParser<string>("certification")).TryGetValue(array, out certifications))
                {
                    return true;
                }
                else
                {
                    return TryParseCertifications(json, "US", out certifications);
                }
            }

            certifications = null;
            return false;
        }

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
                if (parser is IJsonParser<PropertyValuePair> pvp && pvp.TryGetValue(json, out var pair))
                {
                    dict.Add(pair);
                    //dict.Add(parser.GetPair(Task.FromResult(property.Value)));
                    //break;
                }
            }
        }

        private static DataService Data;

        public void Test(DataService manager)
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

        public bool TryGetID(Models.Item item, out int id)
        {
            // Use search to guess ID if not assigned

            return item.TryGetID(ID, out id);
        }

        Task<Item> IAssignID<int>.GetItem(ItemType type, int id) => GetItem(type, id);

        public static async Task<Item> GetItem(ItemType type, int id)
        {
            if (type == ItemType.Collection)
            {
                var url = string.Format(API.COLLECTIONS.GET_DETAILS.GetURL(), id);
                var response = await WebClient.TryGetAsync(url);

                if (response?.IsSuccessStatusCode == true)
                {
                    var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

                    if (TryParseCollection(json, out var collection))
                    {
                        return collection;
                    }
                }
            }
            else if (ITEM_PROPERTIES.TryGetValue(type, out var properties))
            {
                var details = new PropertyDictionary();
                var handler = new ItemProperties.RequestHandler(properties, null)
                {
                    Parameters = new object[] { id }
                };

                details.PropertyAdded += handler.PropertyRequested;

                Item item = null;

                if (type == ItemType.Movie)
                {
                    if (details.TryGetValue(Media.TITLE, out var title) && details.TryGetValue(Movie.RELEASE_DATE, out var year))
                    {
                        item = new Movie(await title, (await year).Year).WithID(IDKey, id);
                    }
                }
                else if (type == ItemType.TVShow)
                {
                    if (details.TryGetValue(Media.TITLE, out var title))
                    {
                        item = new TVShow(await title).WithID(IDKey, id);
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

                }
                else
                {
                    return null;
                }

                var cached = Data.GetDetails(item);
                cached.Add(details);

                handler.Item = item;
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