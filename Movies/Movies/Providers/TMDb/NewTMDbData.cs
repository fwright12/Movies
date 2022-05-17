using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq.Async;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Movies
{
    public static class Helpers
    {
        public static async IAsyncEnumerable<JsonNode> FlattenPages(HttpClient client, string apiCall, string resultsProperty, Func<JsonNode, int> getTotalPages, AuthenticationHeaderValue auth = null)
        {
            for (int page = 1; ; page++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, string.Format(apiCall, page));
                if (auth != null)
                {
                    request.Headers.Authorization = auth;
                }
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }

                var parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                var results = parsed[resultsProperty]?.AsArray();
                //var totalPages = parsed["total_pages"]?.GetValue<int>();

                if (results == null)// || !parsed.TryGetValue("total_pages", out int totalPages))
                {
                    break;
                }

                foreach (var result in results)
                {
                    yield return result;
                }

                if (page >= getTotalPages(parsed))
                {
                    break;
                }
            }
        }
    }

    public partial class TMDB
    {
        private static string BuildApiCall(string endpoint, params string[] parameters) => BuildApiCall(endpoint, (IEnumerable<string>)parameters);
        private static string BuildApiCall(string endpoint, IEnumerable<string> parameters) => string.Format(endpoint + "?" + string.Join('&', parameters));

        private static readonly string PageParameter = "page={0}";
        private string AdultParameter => string.Format("adult={0}", false);
        private string LanguageParameter => string.Format("language={0}", LANGUAGE);
        private string RegionParameter => string.Format("region={0}", REGION);

        private string[] LanguageRegionParameters => new string[] { LanguageParameter, RegionParameter };

        public IAsyncEnumerable<JsonNode> FlattenPages(string apiCall, params string[] parameters) => Helpers.FlattenPages(WebClient, BuildApiCall(apiCall, parameters.Append(PageParameter)), "results", json => json["total_pages"].TryGetValue<int>());

#if DEBUG
        public IAsyncEnumerable<Item> GetRecommendedMoviesAsync() => FlattenPages(string.Format("4/account/{0}/movie/recommendations", AccountID)).TrySelect<JsonNode, Movie>(TryParseMovie);
        public IAsyncEnumerable<Item> GetTrendingAsync(string mediaType, string timeWindow = "week") => FlattenPages(string.Format("trending/{0}/{1}", mediaType, timeWindow)).TrySelect<JsonNode, Movie>(TryParseMovie);
        public IAsyncEnumerable<Item> GetTrendingMoviesAsync(string timeWindow = "week") => GetTrendingAsync("movie", timeWindow);
        public IAsyncEnumerable<Item> GetPopularMoviesAsync() => FlattenPages("movie/popular", LanguageRegionParameters).TrySelect<JsonNode, Movie>(TryParseMovie);
        public IAsyncEnumerable<Item> GetTopRatedMoviesAsync() => FlattenPages("movie/top_rated", LanguageRegionParameters).TrySelect<JsonNode, Movie>(TryParseMovie);
        public IAsyncEnumerable<Item> GetNowPlayingMoviesAsync() => FlattenPages("movie/now_playing", LanguageRegionParameters).TrySelect<JsonNode, Movie>(TryParseMovie);
        public IAsyncEnumerable<Item> GetUpcomingMoviesAsync() => FlattenPages("movie/upcoming", LanguageRegionParameters).TrySelect<JsonNode, Movie>(TryParseMovie);

        public IAsyncEnumerable<Item> GetRecommendedTVShowsAsync() => FlattenPages(string.Format("4/account/{0}/tv/recommendations", AccountID)).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTrendingTVShowsAsync(string timeWindow = "week") => GetTrendingAsync("tv", timeWindow);
        public IAsyncEnumerable<Item> GetPopularTVShowsAsync() => FlattenPages("tv/popular", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTopRatedTVShowsAsync() => FlattenPages("tv/top_rated", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVOnAirAsync() => FlattenPages("tv/on_the_air", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVAiringTodayAsync() => FlattenPages("tv/airing_today", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);

        public IAsyncEnumerable<Item> GetTrendingPeopleAsync(string timeWindow = "week") => GetTrendingAsync("person", timeWindow);
        public IAsyncEnumerable<Item> GetPopularPeopleAsync() => FlattenPages("person/popular", LanguageParameter).TrySelect<JsonNode, Person>(TryParsePerson);
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

        public IAsyncEnumerable<Review> GetMovieReviews(int id) => GetReviews($"movie/{id}/reviews");
        public IAsyncEnumerable<Review> GetReviews(TVShow show) => TryGetID(show, out var id) ? GetReviews($"tv/{id}/reviews") : null;
        private IAsyncEnumerable<Review> GetReviews(string endpoint) => FlattenPages(endpoint).Select(ParseReview);

        public static bool TryParseMovie(JsonNode json, out Movie movie)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("title", out string title) && json.TryGetValue("release_date", out string releaseDate))
            {
                movie = new Movie(title, DateTime.TryParse(releaseDate, out var year) ? (int?)year.Year : null).WithID(IDKey, id);
                return true;
            }

            movie = null;
            return false;
        }

        public static bool TryParseTVShow1(JsonNode json, out Models.TVShow show)
        {
            throw new NotImplementedException();
        }

        public static bool TryParsePerson(JsonNode json, out Person person)
        {
            if (json.TryGetValue("name", out string name))
            {
                person = new Person(name);

                if (json.TryGetValue("id", out int id))
                {
                    person.SetID(IDKey, id);
                }

                return true;
            }

            person = null;
            return false;
        }

        protected bool TryParseTVShow(JsonNode json, out TVShow show)
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

        public static bool TryParse(JsonNode json, out Item item)
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
                if (TryParseTVShow1(json, out var show))
                {
                    item = show;
                }
            }

            return item != null;
        }

        public static Rating ParseRating(JsonNode json, Company company, IAsyncEnumerable<Review> reviews = null) => new Rating
        {
            Company = company,
            Score = json["vote_average"]?.TryGetValue<double>(),
            TotalVotes = json["vote_count"]?.TryGetValue<double>() ?? 0,
            Reviews = reviews
        };

        public static Review ParseReview(JsonNode json) => new Review
        {
            Author = json["author"]?.TryGetValue<string>(),
            Content = json["content"]?.TryGetValue<string>(),
        };

        public static Credit ParseCredit(JsonNode json) => new Credit
        {
            Person = TryParsePerson(json, out var person) ? person : null,
            Role = json["character"]?.TryGetValue<string>() ?? json["job"]?.TryGetValue<string>(),
            Department = json["department"]?.TryGetValue<string>(),
        };

        public static Company ParseCompany(JsonNode json)
        {
            throw new NotImplementedException();
        }

        public static WatchProvider ParseWatchProvider(JsonNode json)
        {
            throw new NotImplementedException();
        }

        public static Collection TryParseCollection(JsonNode json)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<T> TryParseArray<T>(JsonNode json, Func<JsonNode, T> parse)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<Parser> PrimaryMediaProperties = new Parser[]
        {
            new Parser<string>(Media.TAGLINE, "tagline"),
            new Parser<string>(Media.DESCRIPTION, "overview"),
            //new Parser<string>(Media.CONTENT_RATING, "release_dates"),
            new Parser<string>(Media.ORIGINAL_LANGUAGE, "original_language"),
            new Parser<IEnumerable<string>>(Media.LANGUAGES, json => json.AsArray().TrySelect((JsonNode node, out string language) => node.TryGetValue("name", out language)), "spoken_languages"),
            new Parser<string>(Media.GENRES, json => json.AsArray().TrySelect((JsonNode node, out string genre) => node.TryGetValue("name", out genre)).First(), "genres"),
            new Parser<string>(Media.BACKDROP_PATH, "backdrop_path"),
            new Parser<IEnumerable<Company>>(Media.PRODUCTION_COMPANIES, "production_companies"),
            new Parser<IEnumerable<string>>(Media.PRODUCTION_COUNTRIES, json => json.AsArray().TrySelect((JsonNode json, out string country) => json.TryGetValue("name", out country)), "production_countries"),
            //new Parser<Rating>(Media.RATING, ParseRating),

            new Parser<DateTime>(TVSeason.YEAR, "air_date"),
            //new Parser<TimeSpan>(TVSeason.AVERAGE_RUNTIME, "release_date"),
            //new Parser<IEnumerable<Credit>>(TVSeason.CAST, TryParseCredits, "title"),
            //new Parser<IEnumerable<Credit>>(TVSeason.CREW, TryParseCredits, "title"),
            new Parser<DateTime>(TVEpisode.AIR_DATE, "air_date"),
        };

        private static readonly Parser CAST_PARSER = new Parser<IEnumerable<Credit>>(Media.CAST, json => json["cast"]?.AsArray().Select(ParseCredit) ?? System.Linq.Enumerable.Empty<Credit>(), "credits");
        private static readonly Parser CREW_PARSER = new Parser<IEnumerable<Credit>>(Media.CREW, json => json["crew"]?.AsArray().Select(ParseCredit) ?? System.Linq.Enumerable.Empty<Credit>(), "credits");

        private static IEnumerable<Parser> PrimaryMovieProperties = PrimaryMediaProperties.Concat(new Parser[]
        {
            new Parser<string>(Media.TITLE, "title"),
            new Parser<TimeSpan>(Media.RUNTIME, json => TimeSpan.FromMinutes(json.GetValue<int>()), "runtime"),
            new Parser<string>(Media.ORIGINAL_TITLE, "original_title"),
            new Parser<string>(Media.POSTER_PATH, "poster_path"),
            new Parser<DateTime>(Movie.RELEASE_DATE, "release_date"),
            new Parser<long>(Movie.BUDGET, "budget"),
            new Parser<long>(Movie.REVENUE, "revenue"),
            new Parser<Collection>(Movie.PARENT_COLLECTION, TryParseCollection, "belongs_to_collection"),
        });

        private static IEnumerable<Parser> PrimaryTVShowProperties = PrimaryMediaProperties.Concat(new Parser[]
        {
            new Parser<string>(Media.TITLE, "name"),
            new Parser<TimeSpan>(Media.RUNTIME, json => TimeSpan.FromMinutes(json.GetValue<int>()), "episode_run_time"),
            new Parser<string>(Media.ORIGINAL_TITLE, "original_name"),
            new Parser<string>(Media.POSTER_PATH, "still_path"),
            new Parser<DateTime>(TVShow.FIRST_AIR_DATE, "first_air_date"),
            new Parser<DateTime>(TVShow.LAST_AIR_DATE, "last_air_date"),
            //new Parser<IEnumerable<Company>>(TVShow.NETWORKS, TryParseCompany, "networks"),
        });

        private static ApiCall AppendToResponse(ApiCall details, IEnumerable<ApiCall> appended)
        {
            var detailsUri = new Uri(details.Endpoint);
            var path = detailsUri.AbsolutePath;
            var uris = appended
                .Select(call => new Uri(call.Endpoint))
                .Where(uri => uri.AbsolutePath.StartsWith(path))
                .ToList();

            var parameters = uris.Append(detailsUri)
                .SelectMany(uri => uri.Query.Split('&'))
                .Aggregate(new Dictionary<string, string>(), (dict, parameter) =>
                {
                    var pair = parameter.Split('=');

                    if (pair.Length == 2)
                    {
                        dict[pair[0]] = pair[0];
                    }

                    return dict;
                })
                .Select(kvp => string.Join('=', kvp.Key, kvp.Value));
            var append = $"append_to_response={uris.Select(uri => uri.AbsolutePath.Replace(path, string.Empty))}";

            return new ApiCall(BuildApiCall(path, parameters.Append(append)), details.Properties.Concat(appended.SelectMany(call => call.Properties)).ToList());

            //return new ApiCall(details.Endpoint + string.Format("append_to_response={0}", string.Join(',', appended.Select(call => call.Endpoint))), details.Properties.Concat(appended.SelectMany(call => call.Properties)));
        }

        public void Test(DataService manager)
        {
            manager.GetItemDetails += (sender, e) =>
            {
                if (TryGetID((Item)sender, out var id))
                {
                    ApiCall details;
                    IEnumerable<ApiCall> appended;

                    if (sender is Movie)
                    {
                        details = new ApiCall(BuildApiCall($"movie/{id}", LanguageParameter), PrimaryMovieProperties);
                        appended = GetMovieProperties(id);
                    }
                    else if (sender is TVShow)
                    {
                        details = new ApiCall(BuildApiCall($"tv/{id}", LanguageParameter), PrimaryTVShowProperties);
                        appended = GetTVShowProperties(id);
                    }
                    else
                    {
                        return;
                    }

                    new ItemProperties(e.Properties, AppendToResponse(details, appended));
                    //new ItemProperties(e.Properties, details, appended);
                }
            };
        }

        private static IEnumerable<T> ParseSubProperty<T>(JsonNode json, string subProperty) => json.AsArray().TrySelect((JsonNode node, out T value) => node.TryGetValue(subProperty, out value));

        private static ApiCall GetKeywords(string type, int id, string property) => ApiCall.Create($"{type}/{id}/keywords", Media.KEYWORDS, property, json => ParseSubProperty<string>(json, "name").First());

        private ApiCall[] GetAppendedMediaProperties(string type, int id) => new ApiCall[]
        {
            //ApiCall.Create($"{type}/{id}/recommendations", Media.RECOMMENDED),
            //ApiCall.Create("reviews", new Parser<Rating>(Media.RATING, json => ParseRating(json, null, getmovie), "title")),
            ApiCall.Create(BuildApiCall($"{type}/{id}/videos", LanguageParameter), Media.TRAILER_PATH, "results"),
            ApiCall.Create($"{type}/{id}/watch/providers", Media.WATCH_PROVIDERS, "results"),
        };

        private IEnumerable<ApiCall> GetMovieProperties(int id) => GetAppendedMediaProperties("movie", id).Concat(new ApiCall[]
        {
            GetKeywords("movie", id, "results"),
            new ApiCall(BuildApiCall($"movie/{id}/credits", LanguageParameter), CAST_PARSER, CREW_PARSER),
            ApiCall.Create($"movie/{id}/release_dates", Media.CONTENT_RATING, "results"),
        });

        private IEnumerable<ApiCall> GetTVShowProperties(int id) => GetAppendedMediaProperties("tv", id).Concat(new ApiCall[]
        {
            GetKeywords("tv", id, "keywords"),
            new ApiCall(BuildApiCall($"tv/{id}/aggregate_credits", LanguageParameter), CAST_PARSER, CREW_PARSER),
            ApiCall.Create(BuildApiCall($"tv/{id}/content_ratings", LanguageParameter), Media.CONTENT_RATING, "results"),
        });

        private class ApiCall
        {
            public string Endpoint { get; }
            public IEnumerable<Parser> Properties { get; }

            public ApiCall(string endpoint, params Parser[] parsers) : this(endpoint, (IEnumerable<Parser>)parsers) { }
            //public ApiCall(string endpoint, Property property, string jsonProperty) { }//: this(endpoint, new KeyValuePair<Property, string>[] { new KeyValuePair<Property, string>(property, jsonProperty) }) { }
            public ApiCall(string endpoint, IEnumerable<Parser> properties)
            {
                Endpoint = endpoint;
                Properties = properties;
            }

            public static ApiCall Create<T>(string endpoint, Property<T> property, string jsonProperty) => new ApiCall(endpoint, new Parser<T>(property, jsonProperty));
            public static ApiCall Create<T>(string endpoint, Property<T> property, string jsonProperty, Func<JsonNode, T> parse) => new ApiCall(endpoint, new Parser<T>(property, parse, jsonProperty));
        }

        private abstract class Parser
        {
            public Property Property { get; }
            public string JsonProperty { get; }

            public Parser(Property property, string jsonProperty = null)
            {
                Property = property;
                JsonProperty = jsonProperty;
            }

            public abstract PropertyValuePair GetPair(Task<JsonNode> node);
        }

        private class Parser<T> : Parser
        {
            public Func<JsonNode, T> Parse { get; }

            public Parser(Property<T> property, string jsonProperty = null) : this(property, node => node.GetValue<T>(), jsonProperty) { }
            public Parser(Property<T> property, Func<JsonNode, T> parse, string jsonProperty = null) : base(property, jsonProperty)
            {
                Parse = parse;
            }

            public override PropertyValuePair GetPair(Task<JsonNode> node) => new PropertyValuePair<T>((Property<T>)Property, Get(node));

            private async Task<T> Get(Task<JsonNode> node)
            {
                var json = await node;

                if (JsonProperty != null && json[JsonProperty] is JsonNode property)
                {
                    json = property;
                }

                return Parse(json);
            }
        }

        private class ItemProperties
        {
            public PropertyDictionary Properties { get; }

            private Dictionary<Property, ApiCall> ApiCalls;

            public ItemProperties(PropertyDictionary properties, ApiCall details, params ApiCall[] appended)
            {
                Properties = properties;
                ApiCalls = new Dictionary<Property, ApiCall>();

                foreach (var apiCall in appended.Prepend(details))
                {
                    foreach (var parser in apiCall.Properties)
                    {
                        ApiCalls[parser.Property] = apiCall;
                    }
                }

                Properties.PropertyAdded += ValueRequested;
            }

            private async Task<JsonNode> ApiCall(string endpoint)
            {
#if DEBUG
                return JsonNode.Parse(await Task.FromResult(INTERSTELLAR_RESPONSE));
#else
                var response = await new TMDB(null, null).WebClient.GetAsync(endpoint);
                return JsonNode.Parse(await response.Content.ReadAsStringAsync());
#endif
            }

            private async Task<JsonNode> AppendedValue(Task<JsonNode> main, string property) => (await main)[property];

            private void ValueRequested(object sender, PropertyEventArgs e)
            {
                if (ApiCalls.TryGetValue(e.Property, out var call))
                {
                    var response = ApiCall(call.Endpoint);

                    foreach (var property in call.Properties)
                    {
                        Properties.Add(property.GetPair(response));
                    }
                }
            }
        }
    }
}