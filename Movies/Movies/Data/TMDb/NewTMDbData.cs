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
#if DEBUG
        private static class Helpers
        {
            public static async IAsyncEnumerable<JsonNode> FlattenPages(HttpClient client, string apiCall, string resultsProperty, Func<JsonNode, int> getTotalPages, AuthenticationHeaderValue auth = null)
            {
                string response;

                if (apiCall.StartsWith("trending/movie"))
                {
                    response = TRENDING_MOVIES_RESPONSE;
                }
                else if (apiCall.StartsWith("trending/tv"))
                {
                    response = TRENDING_TV_RESPONSE;
                }
                else if (apiCall.StartsWith("trending/person"))
                {
                    response = TRENDING_PEOPLE_RESPONSE;
                }
                else
                {
                    yield break;
                }

                await Task.CompletedTask;

                foreach (var item in JsonNode.Parse(response)["results"].AsArray())
                {
                    yield return item;
                }
            }
        }
#endif

        private static string BuildApiCall(string endpoint, params string[] parameters) => BuildApiCall(endpoint, (IEnumerable<string>)parameters);
        private static string BuildApiCall(string endpoint, IEnumerable<string> parameters) => endpoint + "?" + string.Join('&', parameters);

        private static readonly string PageParameter = "page={0}";
        private string AdultParameter => string.Format("adult={0}", false);
        private string LanguageParameter => string.Format("language={0}", LANGUAGE);
        private string RegionParameter => string.Format("region={0}", REGION);

        private string[] LanguageRegionParameters => new string[] { LanguageParameter, RegionParameter };

        public IAsyncEnumerable<T> FlattenPages<T>(string apiCall, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse, params string[] parameters) where T : Item => FlattenPages(apiCall, parse, (IEnumerable<string>)parameters);
        public async IAsyncEnumerable<T> FlattenPages<T>(string apiCall, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse, IEnumerable<string> parameters) where T : Item
        {
            await foreach (var json in Helpers.FlattenPages(WebClient, BuildApiCall(apiCall, parameters.Append(PageParameter)), "results", json => json["total_pages"].TryGetValue<int>()))
            {
                if (parse(json, out var item))
                {
                    CacheItem(item, json);
                    yield return item;
                }
            }
        }

        public IAsyncEnumerable<JsonNode> FlattenPages(string apiCall, params string[] parameters) => Helpers.FlattenPages(WebClient, BuildApiCall(apiCall, parameters.Append(PageParameter)), "results", json => json["total_pages"].TryGetValue<int>());

#if DEBUG
        public IAsyncEnumerable<Item> GetRecommendedMoviesAsync() => FlattenPages(string.Format("4/account/{0}/movie/recommendations", AccountID)).TrySelect<JsonNode, Movie>(TryParseMovie);
        public IAsyncEnumerable<Item> GetTrendingAsync<T>(string mediaType, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse, string timeWindow = "week") where T : Item => FlattenPages<T>($"trending/{mediaType}/{timeWindow}", parse);
        public IAsyncEnumerable<Item> GetTrendingMoviesAsync(string timeWindow = "week") => GetTrendingAsync<Movie>("movie", TryParseMovie, timeWindow);
        public IAsyncEnumerable<Item> GetPopularMoviesAsync() => FlattenPages<Movie>("movie/popular", TryParseMovie, LanguageRegionParameters);
        public IAsyncEnumerable<Item> GetTopRatedMoviesAsync() => FlattenPages<Movie>("movie/top_rated", TryParseMovie, LanguageRegionParameters);
        public IAsyncEnumerable<Item> GetNowPlayingMoviesAsync() => FlattenPages<Movie>("movie/now_playing", TryParseMovie, LanguageRegionParameters);
        public IAsyncEnumerable<Item> GetUpcomingMoviesAsync() => FlattenPages<Movie>("movie/upcoming", TryParseMovie, LanguageRegionParameters);

        public IAsyncEnumerable<Item> GetRecommendedTVShowsAsync() => FlattenPages(string.Format("4/account/{0}/tv/recommendations", AccountID)).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTrendingTVShowsAsync(string timeWindow = "week") => GetTrendingAsync<TVShow>("tv", TryParseTVShow2, timeWindow);
        public IAsyncEnumerable<Item> GetPopularTVShowsAsync() => FlattenPages("tv/popular", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTopRatedTVShowsAsync() => FlattenPages("tv/top_rated", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVOnAirAsync() => FlattenPages("tv/on_the_air", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);
        public IAsyncEnumerable<Item> GetTVAiringTodayAsync() => FlattenPages("tv/airing_today", LanguageParameter).TrySelect<JsonNode, TVShow>(TryParseTVShow);

        public IAsyncEnumerable<Item> GetTrendingPeopleAsync(string timeWindow = "week") => GetTrendingAsync<Person>("person", TryParsePerson, timeWindow);
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

        public static JsonExtensions.ICache ResponseCache;

        //public Task<JsonNode> GetCertifications() => GetPropertyValues("certification/{0}/list", "certifications");
        //public Task<JsonNode> GetGenres() => GetPropertyValues("genre/{0}/list", "genres", LanguageParameter);
        //public Task<JsonNode> GetWatchProviders() => GetPropertyValues("watch/providers/{0}", "results", LanguageParameter, RegionParameter);
        //public Task<JsonNode> GetWatchProviderRegions() => GetPropertyValues("watch/providers/regions", "results", LanguageParameter);

        private class TMDbRequest
        {
            public HttpMethod Method { get; }
            public string Endpoint { get; }

            public bool AuthenticationRequired { get; set; }
            public bool HasLanguageParameter { get; set; }
            public bool HasRegionParameter { get; set; }
            public bool HasAdultParameter { get; set; }

            public TMDbRequest(string endpoint) : this(endpoint, HttpMethod.Get) { }
            public TMDbRequest(string endpoint, HttpMethod method)
            {
                Method = method;
                Endpoint = endpoint;
            }

            public static implicit operator TMDbRequest(string url) => new TMDbRequest(url);

            public HttpRequestMessage GetMessage(string language, string region, bool adult, AuthenticationHeaderValue auth)
            {
                var parameters = new List<string>();

                if (HasLanguageParameter) parameters.Add(language);
                if (HasRegionParameter) parameters.Add(region);
                if (HasAdultParameter) parameters.Add(adult.ToString());

                var message = new HttpRequestMessage(Method, BuildApiCall(Endpoint, parameters));

                if (AuthenticationRequired)
                {
                    message.Headers.Authorization = auth;
                }

                return message;
            }
        }

        private static readonly TMDbRequest MOVIE_GENRE_VALUES = new TMDbRequest("genre/movie/list")
        {
            HasLanguageParameter = true,
        };

        private AuthenticationHeaderValue Auth { get; }

        private HttpRequestMessage GetMessage(TMDbRequest request) => request.GetMessage(LanguageParameter, RegionParameter, false, Auth);

        private Task InitValues() => Task.WhenAll(new Task[]
        {
            SetValues(Movie.GENRES, BuildApiCall("genre/movie/list", LanguageParameter), new JsonPropertyParser<string>("genre")),
            SetValues(Movie.GENRES, MOVIE_GENRE_VALUES, new JsonPropertyParser<string>("genre"))
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

        public static bool TryParseTVShow1(JsonNode json, out TVShow show)
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

        protected bool TryParseTVShow2(JsonNode json, out TVShow show)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("name", out string name) && json.TryGetValue("overview", out string overview) && json.TryGetValue("poster_path", out string posterPath))
            {
                show = new TVShow(name)
                {
                    Description = overview,
                    PosterPath = BuildImageURL(posterPath, POSTER_SIZE)
                }.WithID(IDKey, id);

                return true;
            }

            show = null;
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

        private static readonly Parser<string> TITLE_PARSER = Media.TITLE;
        //private static readonly Parser RUNTIME_PARSER = new Parser<TimeSpan>(Media.RUNTIME, json => TimeSpan.FromMinutes(json.GetValue<int>()));
        private static readonly Parser<string> ORIGINAL_TITLE_PARSER = Media.ORIGINAL_LANGUAGE;

        private static readonly List<Parser> CREDITS_PARSERS = new List<Parser>
        {
            Parser.Create(Media.CAST, "cast", ParseCredit),
            Parser.Create(Media.CREW, "crew", ParseCredit)
        };

        private static List<Parser> MEDIA_PROPERTIES = new List<Parser>
        {
            //new Parser<string>(Media.TAGLINE, "tagline"),
            //new Parser<string>(Media.DESCRIPTION, "overview"),
            //new Parser<string>(Media.CONTENT_RATING, "release_dates"),
            //new Parser<string>(Media.ORIGINAL_LANGUAGE, "original_language"),
            //["spoken_languages"] = new SubPropertyParser<string>(Media.LANGUAGES, "name"),
            Parser.Create(Media.POSTER_PATH, "poster_path", path => BuildImageURL(path.TryGetValue<string>(), POSTER_SIZE)),
            //new Parser<string>(Media.BACKDROP_PATH, "backdrop_path", path => BuildImageURL(path.TryGetValue<string>())),
            //new Parser<Company>(Media.PRODUCTION_COMPANIES, "production_companies"),
            //["production_countries"] = new SubPropertyParser<string>(Media.PRODUCTION_COUNTRIES, "name"),
            //new Parser<Rating>(Media.RATING, ParseRating),
        };

        private static readonly Dictionary<string, List<Parser>> MEDIA_APPENDED_PROPERTIES = new Dictionary<string, List<Parser>>
        {
            ["recommendations"] = new List<Parser>
            {
                //["recommendations"] = new Parser<IAsyncEnumerable<Item>>(Media.RECOMMENDED)
            },
            //["reviews"] = new Dictionary<string, Parser>(),
            [BuildApiCall("videos", LANGUAGE_PLACEHOLDER)] = new List<Parser>
            {
                //["results"] = new Parser<string>(Media.TRAILER_PATH)
            },
        };

        private static readonly string DETAILS_ENDPOINT = string.Empty;
        private static readonly string LANGUAGE_PLACEHOLDER = "{0}";

        private static readonly Dictionary<string, List<Parser>> MOVIE_PROPERTIES = new Dictionary<string, List<Parser>>(MEDIA_APPENDED_PROPERTIES)
        {
            [DETAILS_ENDPOINT] = new List<Parser>(MEDIA_PROPERTIES)
            {
                //["title"] = TITLE_PARSER,
                //Parser.Create(Movie.GENRES, "genres", "name"),
                //["runtime"] = RUNTIME_PARSER,
                //["original_title"] = ORIGINAL_TITLE_PARSER,
                //["release_date"] = new Parser<DateTime>(Movie.RELEASE_DATE),
                //["budget"] = new Parser<long>(Movie.BUDGET),
                //["revenue"] = new Parser<long>(Movie.REVENUE),
                //["belongs_to_collection"] = new Parser<Collection>(Movie.PARENT_COLLECTION, TryParseCollection),
            },
            ["release_dates"] = new List<Parser>
            {
                //["results"] = CONTENT_RATING_PARSER
            },
            [BuildApiCall("credits", LANGUAGE_PLACEHOLDER)] = CREDITS_PARSERS,
            ["keywords"] = new List<Parser>
            {
                //["keywords"] = new SubPropertyParser<string>(Movie.KEYWORDS, "name")
            },
            ["watch/providers"] = new List<Parser>
            {
                //["results"] = new Parser<WatchProvider>(Media.WATCH_PROVIDERS, ParseWatchProvider)
            }
        };

        private static readonly Dictionary<string, List<Parser>> TVSHOW_PROPERTIES = new Dictionary<string, List<Parser>>(MEDIA_APPENDED_PROPERTIES)
        {
            [DETAILS_ENDPOINT] = new List<Parser>(MEDIA_PROPERTIES)
            {
                //["name"] = TITLE_PARSER,
                //["episode_run_time"] = RUNTIME_PARSER,
                //["original_name"] = ORIGINAL_TITLE_PARSER,
                //["first_air_date"] = new Parser<DateTime>(TVShow.FIRST_AIR_DATE),
                //["last_air_date"] = new Parser<DateTime>(TVShow.LAST_AIR_DATE),
                //["genres"] = new SubPropertyParser<string>(TVShow.GENRES, "name"),
                //["networks"] = new Parser<Company>(TVShow.NETWORKS, ParseCompany),
            },
            [BuildApiCall("content_ratings", LANGUAGE_PLACEHOLDER)] = new List<Parser>
            {
                //["results"] = CONTENT_RATING_PARSER
            },
            [BuildApiCall("aggregate_credits", LANGUAGE_PLACEHOLDER)] = CREDITS_PARSERS,
            ["keywords"] = new List<Parser>
            {
                //["results"] = new SubPropertyParser<string>(TVShow.KEYWORDS, "name")
            },
            ["watch/providers"] = new List<Parser>
            {
                //["results"] = new Parser<WatchProvider>(Media.WATCH_PROVIDERS, ParseWatchProvider)
            }
        };

        private static readonly Dictionary<string, List<Parser>> TVSEASON_PROPERTIES = new Dictionary<string, List<Parser>>
        {
            [DETAILS_ENDPOINT] = new List<Parser>
            {
                //["air_date"] = new Parser<DateTime>(TVSeason.YEAR),
                //["release_date"] = new Parser<TimeSpan>(TVSeason.AVERAGE_RUNTIME),
            }
        };

        private static readonly Dictionary<string, List<Parser>> TVEPISODE_PROPERTIES = new Dictionary<string, List<Parser>>
        {
            [DETAILS_ENDPOINT] = new List<Parser>
            {
                //["air_date"] = new Parser<DateTime>(TVEpisode.AIR_DATE),
            }
        };

        private static readonly Dictionary<string, List<Parser>> PERSON_PROPERTIES = new Dictionary<string, List<Parser>>
        {
            [DETAILS_ENDPOINT] = new List<Parser>
            {

            }
        };

        private static readonly Dictionary<ItemType, (string BaseURL, Dictionary<string, List<Parser>> Properties)> ITEM_PROPERTIES = new Dictionary<ItemType, (string BaseURL, Dictionary<string, List<Parser>> Properties)>
        {
            [ItemType.Movie] = ("movie", MOVIE_PROPERTIES),
            [ItemType.TVShow] = ("tv", TVSHOW_PROPERTIES),
            [ItemType.TVSeason] = ("tv", TVSEASON_PROPERTIES),
            [ItemType.TVEpisode] = ("tv", TVEPISODE_PROPERTIES),
            [ItemType.Person] = ("person", PERSON_PROPERTIES),
        };

        private void CacheItem(Item item, JsonNode json)
        {
            if (!ITEM_PROPERTIES.TryGetValue(item.ItemType, out var properties))
            {
                return;
            }

            var dict = Data.GetDetails(item);

            foreach (var parsers in properties.Properties.Values)
            {
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
        }

        private DataService Data;

        public void Test(DataService manager)
        {
            Data = manager;

            manager.GetItemDetails += (sender, e) =>
            {
                var item = (Item)sender;
                TryGetDetails(item, e.Properties, out _);
            };
        }

        private bool TryGetDetails(Item item, PropertyDictionary dict, out ItemProperties properties)
        {
            properties = null;

            if (!TryGetID(item, out var id) || !ITEM_PROPERTIES.TryGetValue(item.ItemType, out var detailsProperties))
            {
                return false;
            }

            List<string> urlSegments = new List<string> { detailsProperties.BaseURL, id.ToString() };
            List<ApiCall> appended = new List<ApiCall>();

            if (item is Person)
            {
                return false;
            }
            else if (item is Media)
            {
                //appended.Add(new ApiCall("reviews", "title", new Parser<Rating>(Media.RATING, json => ParseRating(json, Company, GetMovieReviews(id)))));
            }

            ApiCall details = null;
            var detailsURL = string.Join('/', urlSegments);

            int Index(string placeholder) => int.Parse(placeholder.Substring(1, placeholder.Length - 2));
            var placeholderArgs = new object[3];
            placeholderArgs[Index(LANGUAGE_PLACEHOLDER)] = LanguageParameter;

            foreach (var property in detailsProperties.Properties)
            {
                var endpoint = detailsURL + "/" + string.Format(property.Key, placeholderArgs);
                var call = new ApiCall(endpoint.Trim('/'), property.Value);

                if (string.IsNullOrEmpty(property.Key))
                {
                    details = call;
                }
                else
                {
                    appended.Add(call);
                }
            }

            var appendedDetails = ItemProperties.AppendToResponse(details, appended);
            properties = new ItemProperties(appendedDetails);

            dict.PropertyAdded += properties.ValueRequested;
            return true;
        }

        private class ItemProperties
        {
            private Dictionary<Property, ApiCall> ApiCalls;

            public ItemProperties(ApiCall details, params ApiCall[] appended) : this(details, (IEnumerable<ApiCall>)appended) { }
            public ItemProperties(ApiCall details, IEnumerable<ApiCall> appended)
            {
                ApiCalls = new Dictionary<Property, ApiCall>();

                foreach (var apiCall in appended.Prepend(details))
                {
                    foreach (var parser in apiCall.Parsers)
                    {
                        ApiCalls[parser.Property] = apiCall;
                    }
                }
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

            public void ValueRequested(object sender, PropertyEventArgs e)
            {
                var properties = (PropertyDictionary)sender;
                AddValues(properties, e.Properties);
            }

            public void AddValues(PropertyDictionary dict, IEnumerable<Property> properties)
            {
                Task<JsonNode> response = null;

                foreach (var property in properties)
                {
                    if (ApiCalls.TryGetValue(property, out var call))
                    {
                        response ??= ApiCall(call.Endpoint);

                        foreach (var parser in call.Parsers)
                        {
                            dict.Add(parser.GetPair(response));
                        }
                    }
                }
            }

            public static ApiCall AppendToResponse(ApiCall details, IEnumerable<ApiCall> appended)
            {
                string basePath = null;
                var appendedPaths = new List<string>();
                var parameters = new Dictionary<string, string>();
                var parsers = new List<Parser>();

                foreach (var call in appended.Prepend(details))
                {
                    //var uri = new Uri(call.Endpoint, UriKind.RelativeOrAbsolute);
                    var parts = call.Endpoint.Split('?');
                    var path = parts.ElementAtOrDefault(0) ?? string.Empty;
                    var query = parts.ElementAtOrDefault(1) ?? string.Empty;

                    if (call == details)
                    {
                        basePath = path;
                    }

                    if (!path.StartsWith(basePath))
                    {
                        continue;
                    }

                    path = path.Replace(basePath, string.Empty).TrimStart('/');

                    if (string.IsNullOrEmpty(path))
                    {
                        foreach (var parser in call.Parsers)
                        {
                            parsers.Add(parser);
                        }
                    }
                    else
                    {
                        appendedPaths.Add(path);

                        foreach (var parser in call.Parsers)
                        {
                            //parsers.Add(path, new ParserWrapper(parser.Value, parser.Key));
                            
                        }
                    }

                    foreach (var parameter in query.Split('&'))
                    {
                        var temp = parameter.Split('=');

                        if (temp.Length == 2)
                        {
                            parameters[temp[0]] = temp[1];
                        }
                    }
                }

                var parameterList = parameters
                    .Select(kvp => string.Join('=', kvp.Key, kvp.Value))
                    .Append($"append_to_response={string.Join(',', appendedPaths)}");

                return new ApiCall(BuildApiCall(basePath, parameterList), parsers);
            }
        }
    }
}