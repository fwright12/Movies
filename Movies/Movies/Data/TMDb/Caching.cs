using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        public ItemInfoCache ItemCache { get; private set; }

        public async Task SetItemCache(ItemInfoCache value, DateTime? lastCleaned = null)
        {
            foreach (var properties in ITEM_PROPERTIES.Values)
            {
                properties.Cache = value;
            }

            await CleanCache(value, lastCleaned);
            await CleanWatchProviders(value);
        }

        private const int WATCH_PROVIDERS_EXPIRED_AFTER_DAY_OF_MONTH = 5;

        private async Task CleanWatchProviders(ItemInfoCache cache)
        {
            var requests = new TMDbRequest[] { API.MOVIES.GET_WATCH_PROVIDERS, API.TV.GET_WATCH_PROVIDERS };
            var regexPattern = "{\\d+}";
            var sqlPattern = "%";
            var patterns = requests.Select(request => new Regex(regexPattern).Replace(request.GetURL(), sqlPattern) + "%");

            //var values = await cache.ReadAll();
            var offset = DateTime.Now.AddDays(-WATCH_PROVIDERS_EXPIRED_AFTER_DAY_OF_MONTH + 1);
            var date = new DateTime(offset.Year, offset.Month, WATCH_PROVIDERS_EXPIRED_AFTER_DAY_OF_MONTH);
            //date = DateTime.Now;

            var adfsd = await Task.WhenAll(patterns.Select(pattern => cache.ExpireAll(pattern, date)));
        }

        public IJsonCache ResponseCache { get; set; }

        private readonly IJsonParser<IEnumerable<Genre>> GENRE_VALUES_PARSER = new JsonPropertyParser<IEnumerable<Genre>>("genres", GENRES_PARSER);
        private readonly IJsonParser<IEnumerable<string>> CERTIFICATION_VALUES_PARSER = new JsonNodeParser<IEnumerable<string>>(TryParseCertifications);
        private static readonly IJsonParser<IEnumerable<WatchProvider>> PROVIDER_PARSER = new JsonPropertyParser<IEnumerable<WatchProvider>>("results", new JsonNodeParser<IEnumerable<WatchProvider>>(TryParseWatchProviders));

        public Task GetPropertyValues { get; }
        private Task CleanCacheTask { get; }
        public static readonly TimeSpan PROPERTY_VALUES_CACHE_DURATION = new TimeSpan(7, 0, 0, 0);

        private async Task CleanCache(TimeSpan olderThan)
        {
            if (ResponseCache == null)
            {
                return;
            }

            var cached = await ResponseCache.ReadAll();

            foreach (var kvp in cached)
            {
                if (DateTime.Now - kvp.Value.Timestamp > olderThan)
                {
                    await ResponseCache.Expire(kvp.Key);
                }
            }
        }

        public readonly HashSet<string> ChangeKeys = new HashSet<string>();

        private async Task GetChangeKeys()
        {
            if (ResponseCache == null)
            {
                return;
            }

            await CleanCacheTask;

            var url = API.CONFIGURATION.GET_API_CONFIGURATION.GetURL();
            var cached = ResponseCache.TryGetValueAsync(url);
            JsonDocument json = null;

            if (cached != null)
            {
                var response = await cached;

                if (DateTime.Now - response?.Timestamp <= PROPERTY_VALUES_CACHE_DURATION)
                {
                    json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
                }
            }

            if (json == null)
            {
                var response = await WebClient.TryGetAsync(url);

                if (response?.IsSuccessStatusCode == true)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    await ResponseCache.AddAsync(url, new JsonResponse(content));
                    json = JsonDocument.Parse(content);
                }
            }

            if (json?.RootElement.TryGetProperty("change_keys", out var keysJson) == true && keysJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var key in keysJson.EnumerateArray().Select(element => element.GetString()))
                {
                    ChangeKeys.Add(key);
                }
            }
        }

        private async Task InitValues()
        {
            await CleanCacheTask;

            await Task.WhenAll(
                SetValues(Movie.GENRES, API.GENRES.GET_MOVIE_LIST, GENRE_VALUES_PARSER),
                SetValues(TVShow.GENRES, API.GENRES.GET_TV_LIST, GENRE_VALUES_PARSER),
                SetValues(Movie.CONTENT_RATING, API.CERTIFICATIONS.GET_MOVIE_CERTIFICATIONS, CERTIFICATION_VALUES_PARSER),
                SetValues(TVShow.CONTENT_RATING, API.CERTIFICATIONS.GET_TV_CERTIFICATIONS, CERTIFICATION_VALUES_PARSER),
                SetValues(Movie.WATCH_PROVIDERS, API.WATCH_PROVIDERS.GET_MOVIE_PROVIDERS, PROVIDER_PARSER),
                SetValues(TVShow.WATCH_PROVIDERS, API.WATCH_PROVIDERS.GET_TV_PROVIDERS, PROVIDER_PARSER));
        }

        //private Task SetValues<T>(Property<T> property, TMDbRequest request, IJsonParser<T> parser) => SetValues(property, request, new JsonArrayParser<T>(parser));
        public Task SetValues<T>(Property<T> property, TMDbRequest request, IJsonParser<IEnumerable<T>> parser) => property.Values is ICollection<T> list ? SetValues(list, request, parser) : Task.CompletedTask;

        public async Task SetValues<T>(ICollection<T> list, TMDbRequest request, IJsonParser<IEnumerable<T>> parser)
        {
            //var apiCall = new ApiCall<IEnumerable<T>>(endpoint, new JsonArrayParser<T>(parser));

            var response = await WebClient.TryGetCachedAsync(request.GetURL(), ResponseCache);

            if (JsonParser.TryParse(await response.Content.ReadAsStringAsync(), parser, out var values))
            {
                foreach (var value in values)
                {
                    list.Add(value);
                }
            }
        }

        public static readonly TimeSpan CHANGES_DURATION = new TimeSpan(14, 0, 0, 0);
        public static readonly TimeSpan MAX_TIME_AWAY = new TimeSpan(180, 0, 0, 0);
        public static readonly TimeSpan MIN_TIME_AWAY = new TimeSpan(1, 0, 0);

        public static readonly IReadOnlyDictionary<ItemType, PagedTMDbRequest> CHANGES_ENDPOINTS = new Dictionary<ItemType, PagedTMDbRequest>
        {
            [ItemType.Movie] = API.CHANGES.GET_MOVIE_CHANGE_LIST,
            [ItemType.TVShow] = API.CHANGES.GET_TV_CHANGE_LIST,
            [ItemType.Person] = API.CHANGES.GET_PERSON_CHANGE_LIST
        };

        //public const int MAX_ALLOWED_CHANGES_CALLS = 10;

        public async Task CleanCache(ItemInfoCache cache, DateTime? lastCleaned = null)
        {
            //if (!lastCleaned.HasValue || (DateTime.Now - lastCleaned) / CHANGES_DURATION > MAX_ALLOWED_CHANGES_CALLS)
            if (DateTime.Now - lastCleaned <= MAX_TIME_AWAY == false)
            {
                await cache.Clear();
            }
            else if (DateTime.Now - lastCleaned > MIN_TIME_AWAY)
            {
                await Task.WhenAll(CHANGES_ENDPOINTS.Keys.Select(type => CleanExpired(cache, type, lastCleaned.Value)));
            }
        }

        private static async Task CleanExpired(ItemInfoCache cache, ItemType type, DateTime since)
        {
            if (!CHANGES_ENDPOINTS.TryGetValue(type, out var request))
            {
                return;
            }

            var from = since;
            var to = DateTime.Now;
            var tasks = new List<Task>();

            for (var start = from; start < to; start += CHANGES_DURATION)
            {
                var end = start + CHANGES_DURATION;
                string str(DateTime date) => JsonSerializer.Serialize(date).Trim('\"');
                var args = new List<string> { $"start_date={str(start)}" };

                if (end < to)
                {
                    args.Add($"end_date={str(end)}");
                }

                var ids = await Request<int>(request, new JsonPropertyParser<int>("id").TryGetValue, false, args.ToArray()).ReadAll();
                tasks.Add(cache.Expire(type, ids));
            }

            await Task.WhenAll(tasks);
        }
    }
}
