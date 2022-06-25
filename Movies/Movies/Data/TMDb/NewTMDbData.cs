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
    public static class Helpers
    {
        public class PagedRequest
        {
            public string Endpoint { get; set; }
            public IJsonParser<int> ParseTotalPages { get; set; }

            public virtual HttpRequestMessage GetRequest(int page, params string[] parameters) => new HttpRequestMessage(HttpMethod.Get, TMDB.BuildApiCall(Endpoint, parameters.Prepend(page.ToString())));

            public virtual int? GetTotalPages() => null;
            public virtual async Task<int?> GetTotalPages(HttpResponseMessage response) => ParseTotalPages?.TryGetValue(JsonNode.Parse(await response.Content.ReadAsStringAsync()), out var result) == true ? result : (int?)null;
        }

        public static IEnumerable<int> LazyRange(int start, int step = 1)
        {
            while (true)
            {
                yield return (start += step) - step;
            }
        }

        public static async IAsyncEnumerable<HttpResponseMessage> GetAsync(this HttpClient client, PagedRequest request, IEnumerable<int> pages, CancellationToken cancellationToken = default, params string[] parameters)
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

                var pageRequest = request.GetRequest(page, parameters);
                var response = await client.SendAsync(pageRequest, cancellationToken);

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

    public class PagedTMDbRequest : Helpers.PagedRequest
    {
        public TMDbRequest Request { get; }

        public PagedTMDbRequest(TMDbRequest request)
        {
            Request = request;
        }

        public static implicit operator PagedTMDbRequest(TMDbRequest request) => new PagedTMDbRequest(request);

        public override HttpRequestMessage GetRequest(int page, params string[] parameters) => new HttpRequestMessage(HttpMethod.Get, Request.GetURL(null, null, false, parameters.Prepend($"page={page}").ToArray()));
    }

    public partial class TMDB
    {
        public static string BuildApiCall(string endpoint, params string[] parameters) => BuildApiCall(endpoint, (IEnumerable<string>)parameters);
        public static string BuildApiCall(string endpoint, IEnumerable<string> parameters) => endpoint + "?" + string.Join('&', parameters);

        public static async Task<IEnumerable<T>> Request<T>(TMDbRequest request, IJsonParser<IEnumerable<JsonNode>> parseItems, IJsonParser<T> parse = null, params string[] parameters)
        {
            string url = request.GetURL(null, null, false, parameters);
            return await Request(await WebClient.TryGetAsync(url), parseItems, parse);
        }

        public static IAsyncEnumerable<T> Request<T>(PagedTMDbRequest request, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters) => Request(request, Helpers.LazyRange(1, 1), parse, reverse, parameters);
        public static async IAsyncEnumerable<T> Request<T>(PagedTMDbRequest request, IEnumerable<int> pages, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters)
        {
            var items = WebClient.GetAsync(request, pages, default, parameters);
            var jsonParser = parse == null ? new JsonNodeParser<T>() : new JsonNodeParser<T>(parse);

            await foreach (var response in items)
            {
                foreach (var item in await Request(response, PageParser, jsonParser))
                {
                    yield return item;
                }
            }
        }

        public static async Task<IEnumerable<T>> Request<T>(HttpResponseMessage response, IJsonParser<IEnumerable<JsonNode>> parseItems, IJsonParser<T> parse = null)
        {
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            return ParseCollection(json, parseItems, parse);
        }

        public static IEnumerable<T> ParseCollection<T>(JsonNode json, IJsonParser<IEnumerable<JsonNode>> parseItems, IJsonParser<T> parse = null)
        {
            if (parseItems.TryGetValue(json, out var items))
            {
                return ParseCollection(items, parse);
            }
            else
            {
                return System.Linq.Enumerable.Empty<T>();
            }
        }

        private static readonly IJsonParser<IEnumerable<JsonNode>> PageParser = new JsonPropertyParser<IEnumerable<JsonNode>>("results");

        public static IEnumerable<T> ParseCollection<T>(IEnumerable<JsonNode> items, IJsonParser<T> parse = null, params Parser[] parsers)
        {
            foreach (var item in items)
            {
                if (parse.TryGetValue(item, out var parsed))
                {
                    if (parsed is Item temp)
                    {
                        CacheItem(temp, item, parsers);
                    }

                    if (NotAdult(item))
                    {
                        yield return parsed;
                    }
                }
            }
        }

        public static IAsyncEnumerable<T> FlattenPages<T>(PagedTMDbRequest request, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, params string[] parameters) => Request(request, parse, false, parameters);// FlattenPages(request, Helpers.LazyRange(0, 1), parse, false, parameters);
        public static async IAsyncEnumerable<T> FlattenPages<T>(PagedTMDbRequest request, IEnumerable<int> pages, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse = null, bool reverse = false, params string[] parameters)
        {
            var items = WebClient.GetAsync(request, pages, default, parameters);
            var parser = new JsonPropertyParser<IEnumerable<JsonNode>>("results", new JsonArrayParser<JsonNode>());

            await foreach (var json in Helpers.FlattenPages(items, parser, reverse))
            {
                if (parse(json, out var parsed))
                {
                    if (parsed is Item item)
                    {
                        CacheItem(item, json);
                    }

                    yield return parsed;
                }
            }
        }

        private static bool NotAdult(JsonNode json) => !json.TryGetValue("adult", out bool adult) || !adult;

#if DEBUG
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
        //public IAsyncEnumerable<Item> GetPopularPeopleAsync() => FlattenPages("person/popular", LanguageParameter).TrySelect<JsonNode, Person>(TryParsePerson);
#else
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
#endif

        public static JsonExtensions.ICache ResponseCache;

        //public Task<JsonNode> GetCertifications() => GetPropertyValues("certification/{0}/list", "certifications");
        //public Task<JsonNode> GetGenres() => GetPropertyValues("genre/{0}/list", "genres", LanguageParameter);
        //public Task<JsonNode> GetWatchProviders() => GetPropertyValues("watch/providers/{0}", "results", LanguageParameter, RegionParameter);
        //public Task<JsonNode> GetWatchProviderRegions() => GetPropertyValues("watch/providers/regions", "results", LanguageParameter);

        public static string GetURL(TMDbRequest request, params string[] parameters) => request.GetURL(null, null, false, parameters);
        public HttpRequestMessage GetMessage(TMDbRequest request) => request.GetMessage(Auth, LANGUAGE, REGION);

        private AuthenticationHeaderValue Auth { get; }

        private Task InitValues() => Task.WhenAll(new Task[]
        {
            SetValues(Movie.GENRES, API.GENRES.GET_MOVIE_LIST, new JsonPropertyParser<string>("genre"))
            //GetValues(Media.GENRES, new ApiCall(BuildApiCall("genre/{0}/list", LanguageParameter), "genres", new Parser<string>(Media.GENRES)))
        });

        private async Task SetValues<T>(Property<T> property, TMDbRequest request, IJsonParser<T> parser)
        {
            if (property.Values is ICollection<T> list)
            {
                //var apiCall = new ApiCall<IEnumerable<T>>(endpoint, new JsonArrayParser<T>(parser));

                var response = await WebClient.TryGetCachedAsync(GetMessage(request), ResponseCache);

                if (JsonParser.TryParse(response.Json, new JsonArrayParser<T>(parser), out var values))
                {
                    foreach (var value in values)
                    {
                        list.Add(value);
                    }
                }
            }
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

        public static async Task<Item> GetItemJson(ItemType type, int id)
        {
            return null;
        }
    }
}