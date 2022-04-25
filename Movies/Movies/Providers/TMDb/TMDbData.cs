using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Async;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Reviews;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Trending;
using TMDbLib.Objects.TvShows;

namespace System.Linq.Async
{
    public static class Enumerable
    {
        public static async Task<List<T>> ReadAll<T>(this IAsyncEnumerable<T> items)
        {
            var result = new List<T>();

            await foreach (var item in items)
            {
                result.Add(item);
            }

            return result;
        }

        public static async IAsyncEnumerable<TResult> OfType<TSource, TResult>(this IAsyncEnumerable<TSource> source)
        {
            await foreach (var item in source)
            {
                if (item is TResult result)
                {
                    yield return result;
                }
            }
        }

        public delegate bool TryParse<TSource, TParsed>(TSource source, out TParsed parsed);

        public static async IAsyncEnumerable<TResult> TrySelect<TSource, TResult>(this IAsyncEnumerable<TSource> source, TryParse<TSource, TResult> selector)
        {
            await foreach (var item in source)
            {
                if (selector(item, out var selected))
                {
                    yield return selected;
                }
            }
        }

        public static async IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            await foreach (var item in source)
            {
                yield return selector(item);
            }
        }
    }
}

namespace Movies
{
    public partial class TMDB : IDataProvider, IAccount, IAssignID<int>
    {
        public static readonly string LANGUAGE = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        public static readonly string REGION = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;

        public static readonly ID<int>.Key IDKey = new ID<int>.Key();
        public static readonly ID<int> ID = IDKey.ID;

        private static readonly string POSTER_SIZE = "/w342";
        private static readonly string PROFILE_SIZE = "/original";
        private static readonly string STILL_SIZE = "/original";
        private static readonly string STREAMING_LOGO_SIZE = "/w45";
        private static readonly string LOGO_SIZE = "/w300";

        public Models.Company Company { get; } = new Models.Company
        {
            Name = "TMDb"
        };
        public string Name => Company.Name;
        public string Username { get; private set; }

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
        private HttpClient WebClient;

        public TMDB(string apiKey, string bearer)
        {
            ApiKey = apiKey;
#if !DEBUG || false
            Client = new TMDbClient(apiKey);
#endif
            //Config = Client.GetConfigAsync();
            WebClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.themoviedb.org/"),
                DefaultRequestHeaders =
                {
                    Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer)
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
        private static string BuildVideoURL(Video video)
        {
            if (video?.Site == "YouTube")
            {
                return "https://www.youtube.com/embed/" + video.Key;
            }
            else if (video?.Site == "Vimeo")
            {
                return "https://player.vimeo.com/video/" + video.Key;
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

        private static string GetCacheKey(SearchMovie movie) => GetCacheKey<Models.Movie>(movie.Id);
        private static string GetCacheKey(SearchTv show) => GetCacheKey<Models.TVShow>(show.Id);
        private static string GetCacheKey(SearchPerson person) => GetCacheKey<Models.Person>(person.Id);
        private static string GetCacheKey(PersonResult person) => GetCacheKey<Models.Person>(person.Id);
        private static string GetCacheKey(SearchCollection collection) => GetCacheKey<Models.Collection>(collection.Id);
        private static string GetCacheKey(SearchCompany company) => GetCacheKey<Models.Company>(company.Id);
        private static string TryGetCacheKey(object item) => TryGetCacheKey(item, out var key) ? key : null;
        private static bool TryGetCacheKey(object item, out string key)
        {
            try
            {
                key = GetCacheKey((dynamic)item);
                return true;
            }
            catch
            {
                key = null;
                return false;
            }
        }

        public IAsyncEnumerable<Models.Item> GetRecommendedMoviesAsync() => CacheStream(FlattenPages(WebClient, string.Format("4/account/{0}/movie/recommendations?page={{0}}", AccountID), UserAccessToken), json => GetCacheKey<Models.Movie>(json)).TrySelect<JsonNode, Models.Movie>(TryParseMovie);
        public IAsyncEnumerable<Models.Item> GetTrendingMoviesAsync(TimeWindow timeWindow = TimeWindow.Week) => FlattenPages(page => Client.GetTrendingMoviesAsync(timeWindow, page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetPopularMoviesAsync() => FlattenPages(page => Client.GetMoviePopularListAsync(page: page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetTopRatedMoviesAsync() => FlattenPages(page => Client.GetMovieTopRatedListAsync(page: page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetNowPlayingMoviesAsync() => FlattenPages<SearchMovie>(async page => await Client.GetMovieNowPlayingListAsync(page: page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetUpcomingMoviesAsync() => FlattenPages<SearchMovie>(async page => await Client.GetMovieUpcomingListAsync(page: page), GetCacheKey).Select(GetItem);

        public IAsyncEnumerable<Models.Item> GetRecommendedTVShowsAsync() => CacheStream(FlattenPages(WebClient, string.Format("4/account/{0}/tv/recommendations?page={{0}}", AccountID), UserAccessToken), json => GetCacheKey<Models.TVShow>(json)).TrySelect<JsonNode, Models.TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Models.Item> GetTrendingTVShowsAsync(TimeWindow timeWindow = TimeWindow.Week) => FlattenPages(page => Client.GetTrendingTvAsync(timeWindow, page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetPopularTVShowsAsync() => FlattenPages(page => Client.GetTvShowPopularAsync(page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetTopRatedTVShowsAsync() => FlattenPages(page => Client.GetTvShowTopRatedAsync(page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetTVOnAirAsync() => FlattenPages(page => Client.GetTvShowListAsync(TvShowListType.OnTheAir, page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetTVAiringTodayAsync() => FlattenPages(page => Client.GetTvShowListAsync(TvShowListType.AiringToday, page), GetCacheKey).Select(GetItem);

        public IAsyncEnumerable<Models.Item> GetTrendingPeopleAsync(TimeWindow timeWindow = TimeWindow.Week) => FlattenPages(page => Client.GetTrendingPeopleAsync(timeWindow, page), GetCacheKey).Select(GetItem);
        public IAsyncEnumerable<Models.Item> GetPopularPeopleAsync() => FlattenPages(page => Client.GetPersonListAsync(PersonListType.Popular, page), GetCacheKey).Select(GetItem);

        public void HandleInfoRequests(DataManager manager)
        {
            DataManager = manager;

            manager.Searched += Search;
            //manager.ItemRated += HandleItemRated;

            HandleMovieRequests(manager.MovieService);
            HandleTVShowRequests(manager.TVShowService);
            HandleTVSeasonRequests(manager.TVSeasonService);
            HandleTVEpisodeRequests(manager.TVEpisodeService);
            HandlePersonRequests(manager.PersonService);
        }

        private async IAsyncEnumerable<(SearchCollection, string, int)> CollectionWithOverview(IAsyncEnumerable<SearchCollection> collections)
        {
            await foreach (var collection in collections)
            {
                var response = await Client.GetCollectionAsync(collection.Id);
                yield return (collection, response.Overview, response.Parts.Count);
            }
        }

        private void Search(object sender, DataManager.SearchEventArgs e)
        {
#if DEBUG
            async IAsyncEnumerable<Models.Item> SearchResults(DataManager.SearchEventArgs e)
            {
                yield return GetItem(new SearchCollection
                {
                    Name = "Harry Potter",
                    PosterPath = "harry potter poster"
                }, "harry potter movies", 8);
                //yield break;
                await foreach (var movie in GetTrendingMoviesAsync(TimeWindow.Day))
                {
                    yield return movie;
                }

                await foreach (var movie in GetTrendingTVShowsAsync(TimeWindow.Day))
                {
                    yield return movie;
                }

                await foreach (var movie in GetTrendingPeopleAsync(TimeWindow.Day))
                {
                    yield return movie;
                }
            }

            e.Results = SearchResults(e);
            return;
            ;
#endif

            IAsyncEnumerable<Models.Item> results = null;
            var filters = e.Filters == null ? new Dictionary<string, object>() : new Dictionary<string, object>(e.Filters);

            if (!string.IsNullOrEmpty(e.Query))
            {
                if (filters.Remove(nameof(ItemType), out var rawType) && rawType is ItemType type)
                {
                    if (type.HasFlag(ItemType.Collection)) results = CollectionWithOverview(FlattenPages(page => Client.SearchCollectionAsync(e.Query, page), GetCacheKey)).Select(value => GetItem(value.Item1, value.Item2, value.Item3));
                    else if (type.HasFlag(ItemType.Company)) results = FlattenPages(page => Client.SearchCompanyAsync(e.Query, page), GetCacheKey).Select(GetItem);
                    else if (type == ItemType.Movie) results = FlattenPages(page => Client.SearchMovieAsync(e.Query, page), GetCacheKey).Select(GetItem);
                    else if (type == ItemType.TVShow) results = FlattenPages(page => Client.SearchTvShowAsync(e.Query, page), GetCacheKey).Select(GetItem);
                    else if (type == ItemType.Person) results = FlattenPages(page => Client.SearchPersonAsync(e.Query, page), GetCacheKey).Select(GetItem);
                }

                if (results == null)
                {
                    results = FlattenPages(page => Client.SearchMultiAsync(e.Query, page), TryGetCacheKey).TrySelect<object, Models.Item>(TryGetItem);
                }
            }
            else
            {
                DiscoverMovieSortBy movieSort = DiscoverMovieSortBy.Undefined;
                DiscoverTvShowSortBy tvSort = DiscoverTvShowSortBy.Undefined;
                bool personSort = false;
                Func<object, IComparable> compare = null;

                if (e.SortBy == "Popularity" || e.SortBy == null)
                {
                    movieSort = e.SortAscending ? DiscoverMovieSortBy.Popularity : DiscoverMovieSortBy.PopularityDesc;
                    tvSort = e.SortAscending ? DiscoverTvShowSortBy.Popularity : DiscoverTvShowSortBy.PopularityDesc;
                    personSort = true;
                    compare = item => (item as SearchMovieTvBase)?.Popularity ?? (item as PersonResult)?.Popularity;
                }
                else if (e.SortBy == "Release Date")
                {
                    movieSort = e.SortAscending ? DiscoverMovieSortBy.ReleaseDate : DiscoverMovieSortBy.ReleaseDateDesc;
                    tvSort = e.SortAscending ? DiscoverTvShowSortBy.FirstAirDate : DiscoverTvShowSortBy.FirstAirDateDesc;
                    compare = item => item is SearchMovie movie ? movie.ReleaseDate : ((SearchTv)item).FirstAirDate;
                }
                else if (e.SortBy == "Revenue")
                {
                    movieSort = e.SortAscending ? DiscoverMovieSortBy.Revenue : DiscoverMovieSortBy.RevenueDesc;
                }
                else if (e.SortBy == "Original Title")
                {
                    movieSort = e.SortAscending ? DiscoverMovieSortBy.OriginalTitle : DiscoverMovieSortBy.OriginalTitleDesc;
                }
                else if (e.SortBy == "Vote Average")
                {
                    movieSort = e.SortAscending ? DiscoverMovieSortBy.VoteAverage : DiscoverMovieSortBy.VoteAverageDesc;
                    tvSort = e.SortAscending ? DiscoverTvShowSortBy.VoteAverage : DiscoverTvShowSortBy.VoteAverageDesc;
                    compare = item => ((SearchMovieTvBase)item).VoteAverage;
                }
                else if (e.SortBy == "Vote Count")
                {
                    movieSort = e.SortAscending ? DiscoverMovieSortBy.VoteCount : DiscoverMovieSortBy.VoteCountDesc;
                    compare = item => ((SearchMovieTvBase)item).VoteCount;
                }

                var filterType = filters.Remove(nameof(ItemType), out var rawType) && rawType is ItemType;
                var types = (filterType ? (ItemType)rawType : ItemType.Movie | ItemType.TVShow | ItemType.Person).ToString();

                var sources = new List<IAsyncEnumerable<object>>();
                foreach (var type1 in types.Split(","))
                {
                    var type = type1.Trim();
                    if (type == ItemType.Movie.ToString() && movieSort != DiscoverMovieSortBy.Undefined)
                    {
                        var search = Client.DiscoverMoviesAsync().OrderBy(movieSort);
                        sources.Add(FlattenPages(page => search.Query(page), GetCacheKey));
                    }
                    else if (type == ItemType.TVShow.ToString() && tvSort != DiscoverTvShowSortBy.Undefined)
                    {
                        var search = Client.DiscoverTvShowsAsync().OrderBy(tvSort);
                        sources.Add(FlattenPages(page => search.Query(page), GetCacheKey));
                    }
                    else if (type == ItemType.Person.ToString() && personSort)
                    {
                        sources.Add(FlattenPages(page => Client.GetPersonListAsync(PersonListType.Popular, page), GetCacheKey, e.SortAscending));
                    }
                    else if (filterType)
                    {
                        sources.Clear();
                        break;
                    }
                }

                var sorted = sources.Count == 1 ? sources[0] : Merge(sources, compare, e.SortAscending ? -1 : 1);
                results = sorted.TrySelect<object, Models.Item>((object item, out Models.Item result) =>
                {
                    if (item is SearchMovie movie) result = GetItem(movie);
                    else if (item is SearchTv show) result = GetItem(show);
                    else if (item is PersonResult person) result = GetItem(person);
                    else
                    {
                        result = null;
                        return false;
                    }

                    return true;
                });
            }

            // apply any remaining filters

            e.Results = results;
        }

        private async IAsyncEnumerable<T> Merge<T>(IEnumerable<IAsyncEnumerable<T>> sources, Func<T, IComparable> property, int order = -1)
        {
            var itrs = sources.Select(source => source.GetAsyncEnumerator()).ToList();
            await Task.WhenAll(itrs.Select(itr => itr.MoveNextAsync().AsTask()));
            //IComparable[] values = await Task.WhenAll(itrs.Select(itr => byValue(itr.Current)));

            while (itrs.Count > 0)
            {
                int index = 0;
                for (int i = 1; i < itrs.Count; i++)
                {
                    if (property(itrs[i].Current).CompareTo(property(itrs[index].Current)) == order)
                    {
                        index = i;
                    }
                }

                yield return itrs[index].Current;

                if (!await itrs[index].MoveNextAsync())
                {
                    itrs.RemoveAt(index);
                }
            }
        }

        private async void HandleItemRated(object sender, DataManager.RatingEventArgs e)
        {
            if (e.Item is Models.Movie movie && movie.TryGetID(ID, out var id))
            {
                if (e.Rating == null)
                {
                    await Client.MovieRemoveRatingAsync(id);
                }
                else if (e.Rating.Score.HasValue)
                {
                    await Client.MovieSetRatingAsync(id, e.Rating.Score.Value);
                }
            }
        }

        private Dictionary<string, BatchRequest> BatchedRequests = new Dictionary<string, BatchRequest>();
        private SemaphoreSlim BatchSemaphore = new SemaphoreSlim(1, 1);

        private Dictionary<string, Task<object>> BatchedApiCalls = new Dictionary<string, Task<object>>();

#if false
        private class ApiCall
        {
            public string URL { get; }

            public ApiCall(string url)
            {
                URL = url;
            }

            public static implicit operator ApiCall(string url) => new ApiCall(url);

            public override bool Equals(object obj) => obj is ApiCall apiCall && apiCall.URL == URL;

            public override int GetHashCode() => URL.GetHashCode();
        }

        private class TMDBCall : ApiCall
        {
            public static readonly string BaseURL = "https://api.themoviedb.org/3";

            public TMDBCall(string endpoint) : base(BaseURL + endpoint)
            {

            }
        }

        private class SmartHttpClient : System.Net.Http.HttpClient
        {
            private Dictionary<string, object> Cache = new Dictionary<string, object>();
            private Dictionary<string, Task<object>> PendingRequests = new Dictionary<string, Task<object>>();

            public Task<object> GetAsync(string url, bool cacheResult = false)
            {
                if (Cache.TryGetValue(url, out var cached))
                {
                    return Task.FromResult(cached);
                }
                else if (PendingRequests.TryGetValue(url, out var pending))
                {
                    return pending;
                }

                Task<object> response = null;// new System.Net.Http.HttpClient().GetAsync("test");
                response = response.ContinueWith(task =>
                {
                    if (cacheResult)
                    {
                        Cache[url] = new List<object> { task.Result };
                    }
                    PendingRequests.Remove(url);

                    return task.Result;
                });

                PendingRequests.Add(url, response);
                return response;
            }
        }
#endif

        private class BatchRequest { }

        private class BatchRequest<T> : BatchRequest
        {
            private Func<int, Task<T>> DetailsCall;
            private readonly Task<T> Task;

            private int Methods;

            public BatchRequest(Func<bool> batched, Func<int, Task<T>> details)
            {
                DetailsCall = details;
                Task = ApiCall(batched);
            }

            public Task<T> GetDetails()
            {
                return Task;
            }

            public Task<T> GetMethod(int method)
            {
                Methods |= method;
                return Task;
            }

            private int PollFrequency = 100;
            private int MaxPollCount = 50;

            private async Task<T> ApiCall(Func<bool> batched, CancellationToken token = default)
            {
                //await batchEnd;
                int pollCount = 0;
                while (batched())
                {
                    await System.Threading.Tasks.Task.Delay(PollFrequency);

                    if (pollCount > MaxPollCount)
                    {
                        throw new OperationCanceledException();
                    }
                    token.ThrowIfCancellationRequested();

                    pollCount++;
                }
                //Print.Log(Methods, string.Join(", ", Enum.GetValues(typeof(MovieMethods)).Cast<MovieMethods>().Where(v => ((MovieMethods)Methods).HasFlag(v))));
                return await DetailsCall(Methods);
            }
        }

        private async Task<TValue> Parse<TValue>(Task<object> raw, Func<object, TValue> parse) => parse == null ? (TValue)await raw : parse(await raw);

        private Func<object[], Task<object>> ConstructInfoRequest<TItem, TMethods, TValue>(
            Func<int, TMethods, CancellationToken, Task<TItem>> details,
            Func<TItem, TValue> property,
            TMethods method,
            Func<int, CancellationToken, Task<TValue>> standalone = null) => parameters => ConstructApiCall(parameters, extraMethods => details((int)parameters[0], (TMethods)(object)extraMethods, default), property, (int)(object)method, standalone == null ? (Func<Task<object>>)null : async () => await standalone((int)parameters[0], new CancellationToken()));

        private Func<object[], Task<object>> ConstructInfoRequest<TItem, TMethods, TValue>(
            Func<int, int, TMethods, CancellationToken, Task<TItem>> details,
            Func<TItem, TValue> property,
            TMethods method,
            Func<int, int, CancellationToken, Task<TValue>> standalone = null) => parameters => ConstructApiCall(parameters, extraMethods => details((int)parameters[0], (int)parameters[1], (TMethods)(object)extraMethods, default), property, (int)(object)method, standalone == null ? (Func<Task<object>>)null : async () => await standalone((int)parameters[0], (int)parameters[1], new CancellationToken()));

        private Func<object[], Task<object>> ConstructInfoRequest<TItem, TMethods, TValue>(
            Func<int, int, int, TMethods, CancellationToken, Task<TItem>> details,
            Func<TItem, TValue> property,
            TMethods method,
            Func<int, int, int, CancellationToken, Task<TValue>> standalone = null) => parameters => ConstructApiCall(parameters, extraMethods => details((int)parameters[0], (int)parameters[1], (int)parameters[2], (TMethods)(object)extraMethods, default), property, (int)(object)method, standalone == null ? (Func<Task<object>>)null : async () => await standalone((int)parameters[0], (int)parameters[1], (int)parameters[2], new CancellationToken()));

        private Func<object[], Task<object>> ConstructMovieInfoRequest<TValue>(Func<Movie, TValue> property, MovieMethods method = MovieMethods.Undefined, Func<int, CancellationToken, Task<TValue>> standalone = null) => ConstructInfoRequest(Client.GetMovieAsync, property, method, standalone);

        private Func<object[], Task<object>> ConstructTVShowInfoRequest<TValue>(Func<TvShow, TValue> property, TvShowMethods method = TvShowMethods.Undefined, Func<int, CancellationToken, Task<TValue>> standalone = null) => ConstructInfoRequest((id, extraMethods, token) => Client.GetTvShowAsync(id, extraMethods, cancellationToken: token), property, method, standalone);

        private Func<object[], Task<object>> ConstructTVSeasonInfoRequest<TValue>(Func<TvSeason, TValue> property, TvSeasonMethods method = TvSeasonMethods.Undefined, Func<int, int, CancellationToken, Task<TValue>> standalone = null) => ConstructInfoRequest((id, season, extraMethods, token) => Client.GetTvSeasonAsync(id, season, extraMethods, cancellationToken: token), property, method, standalone);

        private Func<object[], Task<object>> ConstructTVEpisodeInfoRequest<TValue>(Func<TvEpisode, TValue> property, TvEpisodeMethods method = TvEpisodeMethods.Undefined, Func<int, int, int, CancellationToken, Task<TValue>> standalone = null) => ConstructInfoRequest((id, season, episode, extraMethods, token) => Client.GetTvEpisodeAsync(id, season, episode, extraMethods, cancellationToken: token), property, method, standalone);

        private Func<object[], Task<object>> ConstructPersonInfoRequest<TValue>(Func<Person, TValue> property, PersonMethods method = PersonMethods.Undefined, Func<int, CancellationToken, Task<TValue>> standalone = null) => ConstructInfoRequest(Client.GetPersonAsync, property, method, standalone);

        private static string GetCacheKey<T>(int id) => GetCacheKey(typeof(T), id);
        private static string GetCacheKey(Type type, int id) => type.ToString() + id;

        private T JsonFromCache<T>(object cached, string propertyName)
        {
            try
            {
                var node = (cached as System.Text.Json.Nodes.JsonNode)?[propertyName];

                if (node != null)
                {
                    return node.GetValue<T>();
                }
            }
            catch { }

            return default;
        }

        private Dictionary<string, List<object>> Cache = new Dictionary<string, List<object>>();
        private SemaphoreSlim CacheSemaphore = new SemaphoreSlim(1, 1);

        private async Task AddToCacheAsync(string key, object value)
        {
            await CacheSemaphore.WaitAsync();
            AddToCache(key, value);
            CacheSemaphore.Release();
        }

        private void AddToCache(string key, object value)
        {
            if (key == null)
            {
                return;
            }

            //string key = GetCacheKey(type, id);
            if (!Cache.TryGetValue(key, out var cached))
            {
                Cache[key] = cached = new List<object>();
            }

            cached.Add(value);
        }

        private bool CheckCache<T>(string key, string[] properties, out object value)
        {
            bool result = false;

            if (properties.Length > 0 && Cache.TryGetValue(key, out var list))
            {
                foreach (var item in list)
                {
                    foreach (var property in properties)
                    {
                        if (item is JsonNode json)
                        {
                            var node = json[property];

                            if (node != null)
                            {
                                try
                                {
                                    value = node.Deserialize<T>();
                                    return true;
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            var info = item.GetType().GetProperty(property);

                            if (info != null)
                            {
                                value = info.GetValue(item);

                                if (value != null)
                                {
                                    return true;
                                }

                                result = true;
                            }
                        }
                    }
                }
            }

            value = null;
            return result;
        }

        private delegate bool ExtractParams(Models.Item item, out object[] parameters);

        private bool ExtractID(Models.Item item, out object[] parameters) => (parameters = TryGetID(item, out var id) ? new object[] { id } : null) != null;

        /*private void HandleInfoRequest<TItem, TValue>(InfoRequestHandler<TItem, TValue> handler, string url, Func<object, TValue> parse = null, Func<List<object>, object> cacheProperty = null)
            where TItem : Models.Item
        {
            handler.AddHandler((sender, e) =>
            {
                if (ExtractID(e.Item, out var parameters))
                {
                    Task<object> result;

                    if (CheckCache(GetCacheKey<TItem>((int)parameters[0]), cacheProperty, out var value))
                    {
                        result = Task.FromResult(value);
                    }
                    else if (DataManager.Batched)
                    {
                        //result = MakeApiCall(string.Format(url, parameters));
                    }
                    else
                    {

                    }

                    //e.SetValue(Parse(result, parse));
                }
            });
        }*/

        private void HandleInfoRequest<TItem, TValue>(InfoRequestHandler<TItem, TValue> handler, Func<object[], Task<object>> apiCall, Func<object, TValue> parse = null, params string[] cacheProperty)
            where TItem : Models.Item
        {
            handler.AddHandler((sender, e) =>
            {
                if (ExtractID(e.Item, out var parameters))
                {
                    var result = CheckCache<TValue>(GetCacheKey<TItem>((int)parameters[0]), cacheProperty, out var value) ? Task.FromResult(value) : apiCall(parameters);
                    e.SetValue(Parse(result, parse));
                }
            });
        }

        private void HandleTVSeasonInfoRequest<TValue>(
            InfoRequestHandler<Models.TVSeason, TValue> handler,
            Func<object[], Task<object>> apiCall,
            Func<object, TValue> parse = null)
        {
            handler.AddHandler((sender, e) =>
            {
                if (e.Item.TVShow != null && TryGetID(e.Item.TVShow, out var id))
                {
                    //var result = CheckCache<Models.TVSeason>(id, cacheProperty, out var value) ? Task.FromResult(value) : apiCall(parameters);
                    e.SetValue(Parse(apiCall(new object[] { id, e.Item.SeasonNumber }), parse));
                }
            });
        }

        private void HandleTVEpisodeInfoRequest<TValue>(InfoRequestHandler<Models.TVEpisode, TValue> handler, Func<object[], Task<object>> apiCall, Func<object, TValue> parse = null, params string[] cacheProperty)
        {
            handler.AddHandler((sender, e) =>
            {
                if (e.Item.Season?.TVShow != null && TryGetID(e.Item.Season.TVShow, out var id))
                {
                    string key = TryGetID(e.Item, out var episodeID) ? GetCacheKey<Models.TVEpisode>(episodeID) + "s" + e.Item.Season.SeasonNumber + "e" + e.Item.EpisodeNumber : string.Empty;
                    var result = CheckCache<TValue>(key, cacheProperty, out var value) ? Task.FromResult(value) : apiCall(new object[] { id, e.Item.Season.SeasonNumber, e.Item.EpisodeNumber });
                    e.SetValue(Parse(result, parse));
                }
            });
        }

        private void HandleInfoRequest<TItem, TValue>(InfoRequestHandler<TItem, TValue> handler, Func<int, Task<TValue>> getter) where TItem : Models.Item => handler.AddHandler((sender, e) =>
        {
            if (TryGetID(e.Item, out var id))
            {
                e.SetValue(getter(id));
            }
        });

        private async Task<object> ConstructApiCall<TItem, TValue>(object[] parameters, Func<int, Task<TItem>> detailsCall, Func<TItem, TValue> property, int extraMethods, Func<Task<object>> standalone = null)
        {
            //Print.Log("constucting api call", DataManager.Batched, string.Join(", ", parameters), (TvSeasonMethods)extraMethods);
            if (DataManager.Batched)
            {
                string key = typeof(TItem).ToString() + string.Join(", ", parameters);

                BatchRequest batchedRequest;
                lock (BatchSemaphore)
                {
                    if (!BatchedRequests.TryGetValue(key, out batchedRequest))
                    {
                        batchedRequest = new BatchRequest<TItem>(() => DataManager.Batched, detailsCall);
                        BatchedRequests.Add(key, batchedRequest);
                    }

                }

                var request = (BatchRequest<TItem>)batchedRequest;
                var result = property(standalone == null ? (await request.GetDetails()) : (await request.GetMethod(extraMethods)));

                BatchedRequests.Remove(key);
                return result;

                //return property(await ((BatchRequest<TItem>)batchedRequest).GetDetails(detailsCall, extraMethods));
                //(bool Details, int Methods) info = await (standalone == null ? batchedRequest.GetDetails() : batchedRequest.GetMethod(extraMethods));

                /*extraMethods = info.Methods;
                if (info.Details)
                {
                    standalone = null;
                }
                Print.Log(extraMethods, info.Details);*/
            }

            if (standalone != null)
            {
                return await standalone();
            }
            else
            {
                return property(await detailsCall(extraMethods));
            }
        }

        private IEnumerable<Models.WatchProvider> ParseWatchProviders(object response) => response is SingleResultContainer<Dictionary<string, WatchProviders>> wps && (wps.Results.TryGetValue(REGION, out var providers) || wps.Results.TryGetValue("US", out providers)) ? GetItem(providers) : null;

        private void HandleMovieRequests(MovieService service)
        {
            HandleInfoRequest(service.PosterPathRequested, ConstructMovieInfoRequest(movie => movie.PosterPath), response => BuildImageURL(response.ToString(), POSTER_SIZE), "PosterPath", "poster_path");
            HandleInfoRequest(service.TrailerPathRequested, ConstructMovieInfoRequest(movie => movie, MovieMethods.Videos, async (id, ct) => new Movie { Videos = await Client.GetMovieVideosAsync(id, ct) }), movie => BuildVideoURL(((Movie)movie).Videos.Results.FirstOrDefault(video => video.Type.ToLower() == "trailer")) ?? BuildImageURL(((Movie)movie).BackdropPath));
            //HandleInfoRequest(service.TrailerPathRequested, ConstructMovieInfoRequest(movie => movie.Videos, MovieMethods.Videos, (id, token) => Client.GetMovieVideosAsync(id, token)), videos => ((ResultContainer<Video>)videos).Results[0].Site);

            HandleInfoRequest(service.ReleaseDateRequested, ConstructMovieInfoRequest(movie => movie.ReleaseDate));
            HandleInfoRequest(service.TaglineRequested, ConstructMovieInfoRequest(movie => movie.Tagline));
            HandleInfoRequest(service.DescriptionRequested, ConstructMovieInfoRequest(movie => movie.Overview));
            HandleInfoRequest(service.ContentRatingRequested, ConstructMovieInfoRequest(movie => movie.ReleaseDates, MovieMethods.ReleaseDates, (id, token) => Client.GetMovieReleaseDatesAsync(id, token)), releases => ((releases as ResultContainer<ReleaseDatesContainer>)?.Results.FirstOrDefault(release => release.Iso_3166_1 == REGION) ?? (releases as ResultContainer<ReleaseDatesContainer>)?.Results.FirstOrDefault(release => release.Iso_3166_1 == "US"))?.ReleaseDates.FirstOrDefault(item => !string.IsNullOrEmpty(item.Certification))?.Certification);
            HandleInfoRequest(service.RuntimeRequested, ConstructMovieInfoRequest(movie => movie.Runtime), value => value is int runtime ? TimeSpan.FromMinutes(runtime) : (TimeSpan?)null);
            HandleInfoRequest(service.BudgetRequested, ConstructMovieInfoRequest(movie => movie.Budget), budget => (long)budget == 0 ? null : (long?)budget);
            HandleInfoRequest(service.RevenueRequested, ConstructMovieInfoRequest(movie => movie.Revenue), revenue => (long)revenue == 0 ? null : (long?)revenue);
            HandleInfoRequest(service.OriginalTitleRequested, ConstructMovieInfoRequest(movie => movie.OriginalTitle));
            HandleInfoRequest(service.OriginalLanguageRequested, ConstructMovieInfoRequest(movie => movie.OriginalLanguage));
            HandleInfoRequest(service.LanguagesRequested, ConstructMovieInfoRequest(movie => movie.SpokenLanguages), languages => (languages as List<SpokenLanguage>)?.Select(language => language.Name));
            HandleInfoRequest(service.GenresRequested, ConstructMovieInfoRequest(movie => movie.Genres), genres => (genres as List<Genre>)?.Select(genre => genre.Name));

            HandleInfoRequest(service.RatingRequested, ConstructMovieInfoRequest(movie => movie), response => response is Movie movie ? GetRating(movie) : null);
            HandleInfoRequest(service.CrewRequested, ConstructMovieInfoRequest(movie => movie.Credits.Crew, MovieMethods.Credits, async (id, ct) => (await Client.GetMovieCreditsAsync(id, ct)).Crew), credits => (credits as List<Crew>)?.Select(GetItem));
            HandleInfoRequest(service.CastRequested, ConstructMovieInfoRequest(movie => movie.Credits.Cast, MovieMethods.Credits, async (id, ct) => (await Client.GetMovieCreditsAsync(id, ct)).Cast), cast => (cast as List<TMDbLib.Objects.Movies.Cast>)?.Select(GetItem));
            HandleInfoRequest(service.ProductionCompaniesRequested, ConstructMovieInfoRequest(movie => movie.ProductionCompanies), companies => (companies as List<ProductionCompany>)?.Select(GetItem));
            HandleInfoRequest(service.ProductionCountriesRequested, ConstructMovieInfoRequest(movie => movie.ProductionCountries), countries => (countries as List<ProductionCountry>)?.Select(country => country.Name));
            HandleInfoRequest(service.WatchProvidersRequested, ConstructMovieInfoRequest(movie => movie.WatchProviders, MovieMethods.WatchProviders, (id, token) => Client.GetMovieWatchProvidersAsync(id, token)), ParseWatchProviders);
            HandleInfoRequest(service.KeywordsRequested, ConstructMovieInfoRequest(movie => movie.Keywords, MovieMethods.Keywords, (id, token) => Client.GetMovieKeywordsAsync(id, token)), keywords => (keywords as KeywordsContainer)?.Keywords.Select(keyword => keyword.Name));
            HandleInfoRequest(service.RecommendedRequested, async (int id) => (IAsyncEnumerable<Models.Item>)await new Items<Models.Movie>(FlattenPages(async page => await ConstructMovieInfoRequest(movie => movie.Recommendations, MovieMethods.Recommendations, async (id, ct) => await Client.GetMovieRecommendationsAsync(id, page, ct))(new object[] { id }) as SearchContainer<SearchMovie>, GetCacheKey).Select(GetItem)).Load(1));
            HandleInfoRequest(service.ParentCollectionRequested, async parameters =>
            {
                var collection = await ConstructMovieInfoRequest(movie => movie.BelongsToCollection)(parameters) as SearchCollection;

                if (collection == null)
                {
                    return (collection, string.Empty, 0);
                }

                var response = await Client.GetCollectionAsync(collection.Id);
                return (collection, response.Overview, response.Parts.Count);
            }, collection =>
            {
                var response = ((SearchCollection, string, int))collection;
                return response.Item1 == null ? null : GetItem(response.Item1, response.Item2, response.Item3);
            });
        }

        private void HandleTVShowRequests(TVShowService service)
        {
            HandleInfoRequest(service.FirstAirDateRequested, ConstructTVShowInfoRequest(show => show.FirstAirDate));
            HandleInfoRequest(service.LastAirDateRequested, ConstructTVShowInfoRequest(show => show.InProduction ? null : show.LastAirDate));
            HandleInfoRequest(service.TaglineRequested, ConstructTVShowInfoRequest(show => show.Tagline));
            HandleInfoRequest(service.DescriptionRequested, ConstructTVShowInfoRequest(show => show.Overview));
            HandleInfoRequest(service.ContentRatingRequested, ConstructTVShowInfoRequest(show => show.ContentRatings, TvShowMethods.ContentRatings, (id, token) => Client.GetTvShowContentRatingsAsync(id, token)), releases => ((releases as ResultContainer<ContentRating>)?.Results.FirstOrDefault(release => release.Iso_3166_1 == REGION) ?? (releases as ResultContainer<ContentRating>)?.Results.FirstOrDefault(release => release.Iso_3166_1 == "US"))?.Rating);
            HandleInfoRequest(service.RuntimeRequested, ConstructTVShowInfoRequest(show => show.EpisodeRunTime.Count > 0 ? show.EpisodeRunTime[0] : (int?)null), value => value is int runtime ? TimeSpan.FromMinutes(runtime) : (TimeSpan?)null);
            HandleInfoRequest(service.OriginalTitleRequested, ConstructTVShowInfoRequest(show => show.OriginalName));
            HandleInfoRequest(service.OriginalLanguageRequested, ConstructTVShowInfoRequest(show => show.OriginalLanguage));
            HandleInfoRequest(service.LanguagesRequested, ConstructTVShowInfoRequest(show => show.SpokenLanguages), languages => (languages as List<SpokenLanguage>)?.Select(language => language.Name));
            HandleInfoRequest(service.GenresRequested, ConstructTVShowInfoRequest(show => show.Genres), genres => (genres as List<Genre>)?.Select(genre => genre.Name));

            HandleInfoRequest(service.PosterPathRequested, ConstructTVShowInfoRequest(show => show.PosterPath), response => BuildImageURL(response.ToString(), POSTER_SIZE), "PosterPath", "poster_path");
            HandleInfoRequest(service.TrailerPathRequested, ConstructTVShowInfoRequest(show => show, TvShowMethods.Videos, async (id, ct) => new TvShow { Videos = await Client.GetTvShowVideosAsync(id, ct) }), show => BuildVideoURL(((TvShow)show).Videos.Results.FirstOrDefault(video => video.Type.ToLower() == "trailer")) ?? BuildImageURL(((TvShow)show).BackdropPath));
            //HandleInfoRequest(service.TrailerPathRequested, ConstructTVShowInfoRequest(show => show.BackdropPath), BuildImageURL, cacheProperty: "BackdropPath");

            HandleInfoRequest(service.RatingRequested, ConstructTVShowInfoRequest(show => show), value => value is TvShow show ? GetRating(show) : null);
            HandleInfoRequest(service.CrewRequested, ConstructTVShowInfoRequest(show => show.AggregateCredits.Crew, TvShowMethods.CreditsAggregate, async (id, ct) => (await Client.GetAggregateCredits(id, cancellationToken: ct)).Crew), crew => (crew as List<Crew>)?.Select(GetItem));
            HandleInfoRequest(service.CastRequested, ConstructTVShowInfoRequest(show => show.AggregateCredits.Cast, TvShowMethods.CreditsAggregate, async (id, ct) => (await Client.GetAggregateCredits(id, cancellationToken: ct)).Cast), cast => (cast as List<TMDbLib.Objects.TvShows.Cast>)?.Select(GetItem));
            HandleInfoRequest(service.ProductionCompaniesRequested, ConstructTVShowInfoRequest(show => show.ProductionCompanies), companies => (companies as List<ProductionCompany>)?.Select(GetItem));
            HandleInfoRequest(service.ProductionCountriesRequested, ConstructTVShowInfoRequest(show => show.ProductionCountries), countries => (countries as List<ProductionCountry>)?.Select(country => country.Name));
            HandleInfoRequest(service.NetworksRequested, ConstructTVShowInfoRequest(show => show.Networks), networks => (networks as List<NetworkWithLogo>)?.Select(GetItem));
            HandleInfoRequest(service.WatchProvidersRequested, ConstructTVShowInfoRequest(show => show.WatchProviders, TvShowMethods.WatchProviders, (id, token) => Client.GetTvShowWatchProvidersAsync(id, token)), ParseWatchProviders);
            HandleInfoRequest(service.KeywordsRequested, ConstructTVShowInfoRequest(show => show.Keywords, TvShowMethods.Keywords, (id, token) => Client.GetTvShowKeywordsAsync(id, token)), keywords => (keywords as ResultContainer<Keyword>)?.Results.Select(keyword => keyword.Name));
            HandleInfoRequest(service.RecommendedRequested, async (int id) => (IAsyncEnumerable<Models.Item>)await new Items<Models.TVShow>(FlattenPages(async page => await ConstructTVShowInfoRequest(show => SearchContainerFromList(show.Recommendations?.Results?.Select(TvShowToSearchTv)), TvShowMethods.Recommendations, async (id, ct) => await Client.GetTvShowRecommendationsAsync(id, page, ct))(new object[] { id }) as SearchContainer<SearchTv>, GetCacheKey).Select(GetItem)).Load(1));
        }

        private static SearchTv TvShowToSearchTv(TvShow show) => new SearchTv { Id = show.Id, Name = show.Name, PosterPath = show.PosterPath, Overview = show.Overview };

        private static SearchContainer<T> SearchContainerFromList<T>(IEnumerable<T> results) => new SearchContainer<T>
        {
            Page = 1,
            TotalPages = 1,
            Results = new List<T>(results)
        };

        private void HandleTVSeasonRequests(TVSeasonService service)
        {
            HandleTVSeasonInfoRequest(service.YearRequested, ConstructTVSeasonInfoRequest(season => season.AirDate));
            //HandleInfoRequest(service.AvgRuntimeRequested, ConstructTVSeasonInfoRequest(season => season.);
            HandleTVSeasonInfoRequest(service.CrewRequested, ConstructTVSeasonInfoRequest(season => season.Credits.Crew, TvSeasonMethods.Credits, async (id, s, token) => (await Client.GetTvSeasonCreditsAsync(id, s, cancellationToken: token)).Crew), crew => (crew as List<Crew>)?.Select(GetItem));
            HandleTVSeasonInfoRequest(service.CastRequested, ConstructTVSeasonInfoRequest(season => season.Credits.Cast, TvSeasonMethods.Credits, async (id, s, token) => (await Client.GetTvSeasonCreditsAsync(id, s, cancellationToken: token)).Cast), cast => (cast as List<TMDbLib.Objects.TvShows.Cast>)?.Select(GetItem));
        }

        private void HandleTVEpisodeRequests(TVEpisodeService service)
        {
            HandleTVEpisodeInfoRequest(service.PosterPathRequested, ConstructTVEpisodeInfoRequest(episode => episode.StillPath), response => BuildImageURL(response.ToString(), STILL_SIZE), cacheProperty: "StillPath");
            //HandleInfoRequest(service.TrailerPathRequested, ConstructMovieInfoRequest(movie => movie.BackdropPath));
            //HandleInfoRequest(service.TrailerPathRequested, ConstructMovieInfoRequest(movie => movie.Videos, MovieMethods.Videos, (id, token) => Client.GetMovieVideosAsync(id, token)), videos => ((ResultContainer<Video>)videos).Results[0].Site);

            HandleTVEpisodeInfoRequest(service.AirDateRequested, ConstructTVEpisodeInfoRequest(episode => episode.AirDate), cacheProperty: "AirDate");
            //HandleInfoRequest(service.TaglineRequested, ConstructMovieInfoRequest(movie => movie.Tagline));
            HandleTVEpisodeInfoRequest(service.DescriptionRequested, ConstructTVEpisodeInfoRequest(episode => episode.Overview), cacheProperty: "Overview");
            //return;
            //HandleInfoRequest(service.ContentRatingRequested, ConstructMovieInfoRequest(movie => movie.ReleaseDates, MovieMethods.ReleaseDates, (id, token) => Client.GetMovieReleaseDatesAsync(id, token)), releases => ((ResultContainer<ReleaseDatesContainer>)releases).Results.First(release => release.Iso_3166_1 == "US").ReleaseDates.FirstOrDefault()?.Certification);
            //HandleInfoRequest(service.RuntimeRequested, ConstructMovieInfoRequest(movie => movie.Runtime), value => value is int runtime ? TimeSpan.FromMinutes(runtime) : (TimeSpan?)null);
            //HandleInfoRequest(service.BudgetRequested, ConstructMovieInfoRequest(movie => movie.Budget));
            //HandleInfoRequest(service.RevenueRequested, ConstructMovieInfoRequest(movie => movie.Revenue));
            //HandleInfoRequest(service.OriginalTitleRequested, ConstructMovieInfoRequest(movie => movie.OriginalTitle));
            //HandleInfoRequest(service.OriginalLanguageRequested, ConstructMovieInfoRequest(movie => movie.OriginalLanguage));
            //HandleInfoRequest(service.LanguagesRequested, ConstructMovieInfoRequest(movie => movie.SpokenLanguages), languages => ((List<SpokenLanguage>)languages).Select(language => language.Name));
            //HandleInfoRequest(service.GenresRequested, ConstructMovieInfoRequest(movie => movie.Genres), genres => ((List<Genre>)genres).Select(genre => genre.Name));

            //HandleInfoRequest(service.RatingRequested, id => GetRatings((movieID, extraMethods, token) => Client.GetMovieAsync(movieID, (MovieMethods)extraMethods, token), id, movie => movie.VoteAverage, movie => movie.VoteCount, page => Client.GetMovieReviewsAsync(id, page)));
            HandleTVEpisodeInfoRequest(service.CrewRequested, ConstructTVEpisodeInfoRequest(episode => episode.Credits.Crew, TvEpisodeMethods.Credits, async (id, s, e, token) => (await Client.GetTvEpisodeCreditsAsync(id, s, e, cancellationToken: token)).Crew), crew => (crew as List<Crew>)?.Select(GetItem));
            HandleTVEpisodeInfoRequest(service.CastRequested, ConstructTVEpisodeInfoRequest(episode => episode.Credits.Cast, TvEpisodeMethods.Credits, async (id, s, e, token) => (await Client.GetTvEpisodeCreditsAsync(id, s, e, cancellationToken: token)).Cast), cast => (cast as List<TMDbLib.Objects.TvShows.Cast>)?.Select(GetItem));
            //HandleInfoRequest(service.ProductionCompaniesRequested, ConstructMovieInfoRequest(movie => movie.ProductionCompanies), companies => ((List<ProductionCompany>)companies).Select(GetProductionCompany));
            //HandleInfoRequest(service.ProductionCountriesRequested, ConstructMovieInfoRequest(movie => movie.ProductionCountries), countries => ((List<ProductionCountry>)countries).Select(country => country.Name));
            //HandleInfoRequest(service.WatchProvidersRequested, ConstructMovieInfoRequest(movie => movie.WatchProviders, MovieMethods.WatchProviders, (id, token) => Client.GetMovieWatchProvidersAsync(id, token)), wps => GetWatchProviders(((SingleResultContainer<Dictionary<string, WatchProviders>>)wps).Results["US"]));
            //HandleInfoRequest(service.KeywordsRequested, ConstructMovieInfoRequest(movie => movie.Keywords, MovieMethods.Keywords, (id, token) => Client.GetMovieKeywordsAsync(id, token)), keywords => ((KeywordsContainer)keywords).Keywords.Select(keyword => keyword.Name));
            //HandleInfoRequest(service.RecommendedRequested, id => Task.FromResult(GetListItems(page => Client.GetMovieRecommendationsAsync(id, page))));
        }

        private void HandlePersonRequests(PersonService service)
        {
            HandleInfoRequest(service.BirthdayRequested, ConstructPersonInfoRequest(person => person.Birthday));
            HandleInfoRequest(service.BirthplaceRequested, ConstructPersonInfoRequest(person => person.PlaceOfBirth));
            HandleInfoRequest(service.DeathdayRequested, ConstructPersonInfoRequest(person => person.Deathday));
            HandleInfoRequest(service.AlsoKnownAsRequested, ConstructPersonInfoRequest(person => person.AlsoKnownAs));
            HandleInfoRequest(service.GenderRequested, ConstructPersonInfoRequest(person => person.Gender.ToString()));
            HandleInfoRequest(service.BioRequested, ConstructPersonInfoRequest(person => person.Biography));
            HandleInfoRequest(service.ProfilePathRequested, ConstructPersonInfoRequest(person => person.ProfilePath), response => BuildImageURL(response.ToString(), PROFILE_SIZE), cacheProperty: "ProfilePath");
            HandleInfoRequest(service.CreditsRequested, id => GetPersonCredits(id));
        }

        private async Task<IEnumerable<Models.Item>> GetPersonCredits(int id)
        {
            var parameters = new object[] { id };
            var movies = ConstructPersonInfoRequest(person => person.MovieCredits, PersonMethods.MovieCredits, async (id, ct) => await Client.GetPersonMovieCreditsAsync(id, ct))(parameters);
            var shows = ConstructPersonInfoRequest(person => person.TvCredits, PersonMethods.TvCredits, async (id, ct) => await Client.GetPersonTvCreditsAsync(id, ct))(parameters);

            var credits = await Task.WhenAll(movies, shows);
            var result = System.Linq.Enumerable.Empty<Models.Item>();

            T Cache<T>(string key, T value)
            {
                AddToCache(key, value);
                return value;
            }

            foreach (var type in credits)
            {
                if (type is MovieCredits movie)
                {
                    if (movie.Cast != null)
                    {
                        result = result.Concat(movie.Cast.Where(role => !role.Adult).Select(role => new Models.Movie(Cache(GetCacheKey<Models.Movie>(role.Id), role).Title, role.ReleaseDate?.Year).WithID(IDKey, role.Id)));
                    }
                    if (movie.Crew != null)
                    {
                        result = result.Concat(movie.Crew.Where(job => !job.Adult).Select(job => new Models.Movie(Cache(GetCacheKey<Models.Movie>(job.Id), job).Title, job.ReleaseDate?.Year).WithID(IDKey, job.Id)));
                    }
                }
                else if (type is TvCredits tv)
                {
                    if (tv.Cast != null)
                    {
                        result = result.Concat(tv.Cast.Select(role => new Models.TVShow(Cache(GetCacheKey<Models.TVShow>(role.Id), role).Name).WithID(IDKey, role.Id)));
                    }
                    if (tv.Crew != null)
                    {
                        result = result.Concat(tv.Crew.Select(job => new Models.TVShow(Cache(GetCacheKey<Models.TVShow>(job.Id), job).Name).WithID(IDKey, job.Id)));
                    }
                }
            }

            return result;
        }

#if false
        private async Task<IEnumerable<Models.Item>> GetPersonCreditsJson(int id)
        {
            var results = new List<Models.Item>();

            var data = System.Text.Json.Nodes.JsonNode.Parse(Models.Credit.CreditsJSON);
            var cast = data["cast"].AsArray();
            var crew = data["crew"].AsArray();

            foreach (var node in cast.Concat(crew))
            {
                if (node["adult"]?.GetValue<bool>() == true)
                {
                    continue;
                }

                var type = node["media_type"]?.GetValue<string>().ToLower();
                Models.Item item = null;

                if (type == "movie")
                {
                    var title = node["title"]?.GetValue<string>();

                    if (title != null)
                    {
                        var releaseDate = node["release_date"]?.GetValue<DateTime?>();
                        item = new Models.Movie(title, releaseDate?.Year);
                    }
                }
                else if (type == "tv")
                {
                    var name = node["name"]?.GetValue<string>();

                    if (name != null)
                    {
                        item = new Models.TVShow(name)
                        {
                            PosterPath = node["poster_path"]?.GetValue<string>(),
                            Description = node["overview"]?.GetValue<string>(),
                            //Items = GetTVSeasons(node["id"].GetValue<int>())
                        };
                    }
                }

                if (item != null)
                {
                    if (node["id"]?.GetValue<int>() is int itemID)
                    {
                        item.SetID(IDKey, itemID);
                        await AddToCacheAsync(GetCacheKey(item.GetType(), itemID), node);
                    }

                    results.Add(item);
                }
            }

            return results;
        }
#endif

        private Models.Rating GetRating(Movie movie) => GetRating(movie.VoteAverage, movie.VoteCount, page => Client.GetMovieReviewsAsync(movie.Id, page));
        private Models.Rating GetRating(TvShow show) => GetRating(show.VoteAverage, show.VoteCount, page => Client.GetTvShowReviewsAsync(show.Id, page: page));
        private Models.Rating GetRating(double score, double total, Func<int, Task<SearchContainerWithId<ReviewBase>>> getReviews) => new Models.Rating
        {
            Company = Company,
            Score = score,
            TotalVotes = total,
            Reviews = FlattenPages<ReviewBase>(async page => await getReviews(page))?.Select(GetItem)
        };

        //private IAsyncEnumerable<T> FlattenPages<T>(Func<int, Task<SearchContainerWithId<T>>> apiCall) => FlattenPages((Func<int, Task<SearchContainer<T>>>)(async page => await apiCall(page)));
        private async IAsyncEnumerable<T> FlattenPages<T>(Func<int, Task<SearchContainer<T>>> apiCall, Func<T, string> getCacheKey = null, bool reverse = false)
        {
            int page = reverse ? (await apiCall(1)).TotalPages : 1;
            int sign = reverse ? -1 : 1;

            for (; ; page += sign)
            {
                var results = await apiCall(page);

                if (results == null)
                {
                    break;
                }
                else if (reverse)
                {
                    results.Results.Reverse();
                }

                foreach (var result in results.Results)
                {
                    if (getCacheKey != null)
                    {
                        await AddToCacheAsync(getCacheKey(result), result);
                    }

                    yield return result;
                }

                if ((reverse && page <= 1) || (!reverse && page >= results.TotalPages))
                {
                    break;
                }
            }
        }

        private Models.Item TryGetItem(object item) => TryGetItem(item, out var key) ? key : null;
        private bool TryGetItem(object item, out Models.Item result)
        {
            try
            {
                result = GetItem((dynamic)item);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public IAsyncEnumerable<Models.Item> GetCollectionItems(Models.Item item, int id)
        {
            if (item is Models.TVShow show)
            {
                return new Items<Models.Item>(GetCollectionItems(async () => (TvShow)await ConstructTVShowInfoRequest(show => show)(new object[] { id }), show => show.Seasons, season => GetItem(season, show), GetCacheKey<Models.TVShow>(id), season => GetCacheKey<Models.TVSeason>(season.Id)));
            }
            else if (item is Models.Collection collection)
            {
                return new Items<Models.Item>(GetCollectionItems(() => Client.GetCollectionAsync(id), collection1 =>
                {
                    collection.PosterPath = BuildImageURL(collection1.PosterPath, POSTER_SIZE);
                    return collection1.Parts;
                }, GetItem, GetCacheKey<Models.Collection>(id), GetCacheKey));
            }
            else
            {
                return Empty<Models.Item>();
            }
        }

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }

        private static Models.Movie GetItem(SearchMovie movie) => new Models.Movie(movie.Title, movie.ReleaseDate?.Year).WithID(IDKey, movie.Id);
        private Models.TVShow GetItem(SearchTv show)
        {
            var result = new Models.TVShow(show.Name)
            {
                Description = show.Overview,
                PosterPath = BuildImageURL(show.PosterPath, POSTER_SIZE),
            }.WithID(IDKey, show.Id);
            result.Items = GetCollectionItems(result, show.Id);

            return result;
        }
        private Models.TVSeason GetItem(SearchTvSeason season, Models.TVShow show)
        {
            var result = new Models.TVSeason(show, season.SeasonNumber)
            {
                Description = season.Overview,
                PosterPath = BuildImageURL(season.PosterPath, POSTER_SIZE),
                Count = season.EpisodeCount
            }.WithID(IDKey, season.Id);
            result.Items = new Items<Models.Item>(GetCollectionItems(async () => TryGetID(show, out var showID) ? (TvSeason)await ConstructTVSeasonInfoRequest(season => season)(new object[] { showID, season.SeasonNumber }) : null, season1 => season1.Episodes, episode => GetItem(episode, result), GetCacheKey<Models.TVSeason>(season.Id) + "s" + season.SeasonNumber, episode => GetCacheKey<Models.TVEpisode>(episode.Id) + "s" + season.SeasonNumber + "e" + episode.EpisodeNumber), season.EpisodeCount);

            return result;
        }
        private static Models.TVEpisode GetItem(SearchTvEpisode episode, Models.TVSeason season) => new Models.TVEpisode(season, episode.Name, episode.EpisodeNumber).WithID(IDKey, episode.Id);
        private static Models.TVEpisode GetItem(TvSeasonEpisode episode, Models.TVSeason season) => new Models.TVEpisode(season, episode.Name, episode.EpisodeNumber).WithID(IDKey, episode.Id);
        private static Models.Person GetItem(SearchPerson person) => new Models.Person(person.Name).WithID(IDKey, person.Id);
        private static Models.Person GetItem(PersonResult person) => new Models.Person(person.Name).WithID(IDKey, person.Id);
        private Models.Collection GetItem(SearchCollection collection, string overview, int count)
        {
            var result = new Models.Collection
            {
                Name = collection.Name,
                PosterPath = BuildImageURL(collection.PosterPath, POSTER_SIZE),
                Description = overview,
                Count = count
            }.WithID(IDKey, collection.Id);
            result.Items = GetCollectionItems(result, collection.Id);

            return result;
        }

        private class Items<T> : IAsyncEnumerable<T>
        {
            public int? Count { get; }

            private IAsyncEnumerator<T> Itr;
            private SemaphoreSlim LoadedSemaphore = new SemaphoreSlim(1, 1);
            private List<T> Loaded = new List<T>();

            public Items(IAsyncEnumerable<T> items, int? count = null)
            {
                Itr = items.GetAsyncEnumerator();
                Count = count;
            }

            public async Task<Items<T>> Load(int count = 0)
            {
                var itr = GetAsyncEnumerator();
                for (int i = 0; i < count; i++)
                {
                    await itr.MoveNextAsync();
                }

                return this;
            }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                for (int i = 0; ; i++)
                {
                    T item;

                    await LoadedSemaphore.WaitAsync();

                    if (i < Loaded.Count)
                    {
                        item = Loaded[i];
                    }
                    else if (await Itr.MoveNextAsync())
                    {
                        Loaded.Add(item = Itr.Current);
                    }
                    else
                    {
                        LoadedSemaphore.Release();
                        break;
                    }

                    LoadedSemaphore.Release();

                    yield return item;
                }
            }
        }

        private async IAsyncEnumerable<Models.Item> GetCollectionItems<TCollection, TItem>(Func<Task<TCollection>> apiCall, Func<TCollection, IEnumerable<TItem>> items, Func<TItem, Models.Item> parse, string cacheCollection = null, Func<TItem, string> cacheItem = null)
        {
            TCollection response = default;

            if (cacheCollection != null && Cache.TryGetValue(cacheCollection, out var cached))
            {
                foreach (var item in cached)
                {
                    if (item is Task<TCollection> t)
                    {
                        response = await t;
                        apiCall = null;
                        break;
                    }
                }
            }

            if (apiCall != null)
            {
                var task = apiCall();

                if (cacheCollection != null)
                {
                    await AddToCacheAsync(cacheCollection, task);
                }

                response = await task;
            }

            foreach (var item in items(response))
            {
                if (cacheItem != null)
                {
                    await AddToCacheAsync(cacheItem(item), item);
                }

                yield return parse(item);
            }
        }

        private T CacheCredit<T>(T credit, int id)
        {
            AddToCache(GetCacheKey<Models.Person>(id), credit);
            return credit;
        }

        private Models.Credit GetItem(Crew crew) => new Models.Credit
        {
            Person = new Models.Person(CacheCredit(crew, crew.Id).Name).WithID(IDKey, crew.Id),
            Role = crew.Job,
            Department = crew.Department
        };

        private Models.Credit GetItem(TMDbLib.Objects.Movies.Cast cast) => new Models.Credit
        {
            Person = new Models.Person(CacheCredit(cast, cast.Id).Name).WithID(IDKey, cast.Id),
            Role = cast.Character
        };

        private Models.Credit GetItem(TMDbLib.Objects.TvShows.Cast cast) => new Models.Credit
        {
            Person = new Models.Person(CacheCredit(cast, cast.Id).Name).WithID(IDKey, cast.Id),
            Role = cast.Character
        };

        private static Models.Company GetItem(SearchCompany company) => new Models.Company
        {
            Name = company.Name,
            LogoPath = BuildImageURL(company.LogoPath, LOGO_SIZE)
        }.WithID(IDKey, company.Id);

        private static Models.Company GetItem(ProductionCompany company) => new Models.Company
        {
            Name = company.Name,
            LogoPath = BuildImageURL(company.LogoPath, LOGO_SIZE)
        }.WithID(IDKey, company.Id);

        private static Models.Company GetItem(NetworkWithLogo network) => new Models.Company
        {
            Name = network.Name,
            LogoPath = BuildImageURL(network.LogoPath, LOGO_SIZE)
        }.WithID(IDKey, network.Id);

        private static Models.WatchProvider GetWatchProvider(Models.MonetizationType type, WatchProviderItem watchProvider) => new Models.WatchProvider
        {
            Company = new Models.Company
            {
                Name = watchProvider.ProviderName,
                LogoPath = BuildImageURL(watchProvider.LogoPath, STREAMING_LOGO_SIZE)
            },
            Type = type
        };

        private static IEnumerable<Models.WatchProvider> GetItem(WatchProviders watchProviders)
        {
            if (watchProviders.Free != null) foreach (var item in watchProviders.Free) yield return GetWatchProvider(Models.MonetizationType.Free, item);
            if (watchProviders.Ads != null) foreach (var item in watchProviders.Ads) yield return GetWatchProvider(Models.MonetizationType.Ads, item);
            if (watchProviders.FlatRate != null) foreach (var item in watchProviders.FlatRate) yield return GetWatchProvider(Models.MonetizationType.Subscription, item);
            if (watchProviders.Rent != null) foreach (var item in watchProviders.Rent) yield return GetWatchProvider(Models.MonetizationType.Rent, item);
            if (watchProviders.Buy != null) foreach (var item in watchProviders.Buy) yield return GetWatchProvider(Models.MonetizationType.Buy, item);
        }

        private static Models.Review GetItem(ReviewBase review) => new Models.Review
        {
            Author = review.Author,
            Content = review.Content
        };

        public bool TryGetID(Models.Item item, out int id)
        {
            // Use search to guess ID if not assigned

            return item.TryGetID(ID, out id);
        }

        private async Task<T> GetDetails<T>(string cacheKey, Func<object[], Task<object>> apiCall, params object[] parameters)
        {
            if (Cache.TryGetValue(cacheKey, out var details))
            {
                foreach (var item in details)
                {
                    if (item is T t)
                    {
                        return t;
                    }
                }
            }

            var result = (T)await apiCall(parameters);
            await AddToCacheAsync(cacheKey, result);
            return result;
        }

        public async Task<Models.Item> GetItem(ItemType type, int id)
        {
            if (type == ItemType.Movie)
            {
                var movie = await GetDetails<Movie>(GetCacheKey<Models.Movie>(id), ConstructMovieInfoRequest(movie => movie), id);
                return new Models.Movie(movie.Title, movie.ReleaseDate?.Year).WithID(IDKey, movie.Id);
            }
            else if (type == ItemType.TVShow)
            {
                var show = await GetDetails<TvShow>(GetCacheKey<Models.TVShow>(id), ConstructTVShowInfoRequest(show => show), id);
                return GetItem(new SearchTv
                {
                    Id = show.Id,
                    Name = show.Name,
                    PosterPath = show.PosterPath,
                    Overview = show.Overview,
                });
            }
            else if (type == ItemType.TVSeason)
            {
                var show = await GetDetails<TvShow>(GetCacheKey<Models.TVShow>(id), ConstructTVShowInfoRequest(show => show), id);
                return GetItem(new SearchTvSeason
                {
                    Id = show.Id,
                    Name = show.Name,
                    PosterPath = show.PosterPath,
                    Overview = show.Overview,
                }, GetItem(new SearchTv
                {
                }));
            }
            else if (type == ItemType.TVEpisode)
            {

            }
            else if (type == ItemType.Person)
            {
                var person = await GetDetails<Person>(GetCacheKey<Models.Person>(id), ConstructPersonInfoRequest(person => person), id);
                return new Models.Person(person.Name, person.Birthday?.Year).WithID(IDKey, person.Id);
            }
            else if (type == ItemType.Collection)
            {
                var collection = await GetDetails<Collection>(GetCacheKey<Models.Collection>(id), async parameters => await Client.GetCollectionAsync((int)parameters[0]), id);
                return GetItem(new SearchCollection
                {
                    Id = collection.Id,
                    Name = collection.Name,
                    PosterPath = collection.PosterPath,
                }, collection.Overview, collection.Parts.Count);
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
