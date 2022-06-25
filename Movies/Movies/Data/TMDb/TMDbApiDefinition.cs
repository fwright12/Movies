using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Movies
{
    public static class API
    {
        public static class COLLECTIONS
        {
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("collection/{0}")
            {
                HasLanguageParameter = true,
            };
        }

        public static class DISCOVER
        {
            public static readonly PagedTMDbRequest MOVIE_DISCOVER = new TMDbRequest("discover/movie")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
                HasAdultParameter = true,
            };
            public static readonly PagedTMDbRequest TV_DISCOVER = new TMDbRequest("discover/tv")
            {
                HasLanguageParameter = true,
            };
        }

        public static class GENRES
        {
            public static readonly TMDbRequest GET_MOVIE_LIST = new TMDbRequest("genre/movie/list")
            {
                HasLanguageParameter = true,
            };
        }

        public static class MOVIES
        {
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("movie/{0}")
            {
                SupportsAppendToResponse = true,
                HasLanguageParameter = true,
            };

            public static readonly TMDbRequest GET_CREDITS = new TMDbRequest("movie/{0}/credits")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_KEYWORDS = "movie/{0}/keywords";
            public static readonly TMDbRequest GET_RECOMMENDATIONS = new TMDbRequest("movie/{0}/recommendations")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_RELEASE_DATES = "movie/{0}/release_dates";
            public static readonly TMDbRequest GET_REVIEWS = new TMDbRequest("movie/{0}/reviews")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_VIDEOS = new TMDbRequest("movie/{0}/videos")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_WATCH_PROVIDERS = "movie/{0}/watch/providers";

            public static readonly PagedTMDbRequest GET_NOW_PLAYING = new TMDbRequest("movie/now_playing")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest GET_POPULAR = new TMDbRequest("movie/popular")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest GET_TOP_RATED = new TMDbRequest("movie/top_rated")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest GET_UPCOMING = new TMDbRequest("movie/upcoming")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
        }

        public static class TRENDING
        {
            public static readonly TMDbRequest MOVIES = new TMDbRequest("movie/list");
        }

        public static class PEOPLE
        {
            public static readonly TMDbRequest DETAILS = new TMDbRequest("person/{0}")
            {
                SupportsAppendToResponse = true,
                HasLanguageParameter = true,
            };

            public static readonly TMDbRequest CONTENT_RATINGS = new TMDbRequest("tv/{0}/content_ratings")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest AGGREGATE_CREDITS = new TMDbRequest("tv/{0}/aggregate_credits")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest KEYWORDS = "tv/{0}/keywords";
            public static readonly TMDbRequest WATCH_PROVIDERS = "tv/{0}/watch/providers";
            public static readonly TMDbRequest RECOMMENDED = "tv/{0}/recommendations";
            public static readonly TMDbRequest VIDEOS = new TMDbRequest("tv/{0}/videos")
            {
                HasLanguageParameter = true
            };
        }

        public static class SEARCH
        {
            public static readonly PagedTMDbRequest SEARCH_COLLECTIONS = new TMDbRequest("search/collection")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest SEARCH_KEYWORDS = new TMDbRequest("search/keyword");
            public static readonly PagedTMDbRequest SEARCH_MOVIES = new TMDbRequest("search/movie")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest MULTI_SEARCH = new TMDbRequest("search/multi")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
                HasRegionParameter = true
            };
            public static readonly PagedTMDbRequest SEARCH_PEOPLE = new TMDbRequest("search/person")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
                HasRegionParameter = true
            };
            public static readonly PagedTMDbRequest SEARCH_TV_SHOWS = new TMDbRequest("search/tv")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
            };
        }

        public static class TV
        {
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("tv/{0}")
            {
                SupportsAppendToResponse = true,
                HasLanguageParameter = true,
            };

            public static readonly TMDbRequest GET_AGGREGATE_CREDITS = new TMDbRequest("tv/{0}/aggregate_credits")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_CONTENT_RATINGS = new TMDbRequest("tv/{0}/content_ratings")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_KEYWORDS = "tv/{0}/keywords";
            public static readonly TMDbRequest GET_RECOMMENDATIONS = new TMDbRequest("tv/{0}/recommendations")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_REVIEWS = new TMDbRequest("tv/{0}/reviews")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_VIDEOS = new TMDbRequest("tv/{0}/videos")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_WATCH_PROVIDERS = "tv/{0}/watch/providers";

            public static readonly PagedTMDbRequest GET_TV_AIRING_TODAY = new TMDbRequest("tv/airing_today")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_TV_ON_THE_AIR = new TMDbRequest("tv/on_the_air")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_POPULAR = new TMDbRequest("tv/popular")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_TOP_RATED = new TMDbRequest("tv/top_rated")
            {
                HasLanguageParameter = true,
            };
        }

        public static class TV_SEASONS
        {
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("tv/{0}/season/{1}")
            {
                SupportsAppendToResponse = true,
                HasLanguageParameter = true,
            };

            public static readonly TMDbRequest GET_AGGREGATE_CREDITS = new TMDbRequest("tv/{0}/season/{1}/aggregate_credits")
            {
                HasLanguageParameter = true
            };
        }

        public static class TV_EPISODES
        {
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("tv/{0}/season/{1}/episode/{2}")
            {
                SupportsAppendToResponse = true,
                HasLanguageParameter = true,
            };

            public static readonly TMDbRequest GET_CREDITS = new TMDbRequest("tv/{0}/season/{1}/episode/{2}/credits")
            {
                HasLanguageParameter = true
            };
        }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public bool AllowsMultiple { get; set; }

        public Parameter(string name)
        {
            Name = name;
        }

        public static implicit operator Parameter(string name) => new Parameter(name);
    }

    public partial class TMDB
    {
        public static readonly Property<double> POPULARITY = new Property<double>("Popularity");

        private static readonly Dictionary<Property, Dictionary<Operators, Parameter>> DiscoverMediaParameters = new Dictionary<Property, Dictionary<Operators, Parameter>>
        {
            [Media.RUNTIME] = new Dictionary<Operators, Parameter>
            {
                [Operators.LessThan] = "with_runtime.lte",
                [Operators.GreaterThan] = "with_runtime.gte"
            },
            [Media.ORIGINAL_LANGUAGE] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = "with_original_language",
            },
            [Media.KEYWORDS] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_keywords") { AllowsMultiple = true },
                [Operators.NotEqual] = new Parameter("without_keywords") { AllowsMultiple = true },
            },
            [Media.PRODUCTION_COMPANIES] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_companies") { AllowsMultiple = true },
                [Operators.NotEqual] = new Parameter("without_companies") { AllowsMultiple = true },
            },
            [ViewModels.CollectionViewModel.MonetizationType] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_watch_monetization_types") { AllowsMultiple = true },
            },
        };

        private static readonly Dictionary<Property, Dictionary<Operators, Parameter>> DiscoverMovieParameters = new Dictionary<Property, Dictionary<Operators, Parameter>>(DiscoverMediaParameters)
        {
            [Movie.CONTENT_RATING] = new Dictionary<Operators, Parameter>
            {
                [Operators.LessThan] = "certification.lte",
                [Operators.Equal] = "certification",
                [Operators.GreaterThan] = "certification.gte"
            },
            /*["Primary " + Movie.RELEASE_DATE.Name] = new Dictionary<int, string>
            {
                [-1] = "primary_release_date.lte",
                [1] = "primary_release_date.gte"
            },*/
            [Movie.RELEASE_DATE] = new Dictionary<Operators, Parameter>
            {
                [Operators.LessThan] = "release_date.lte",
                [Operators.GreaterThan] = "release_date.gte"
            },
            /*["Release Type"] = new Dictionary<int, string>
            {
                [0] = "with_release_type"
            },
            ["Vote Count"] = new Dictionary<int, string>
            {
                [-1] = "vote_count.lte",
                [1] = "vote_count.gte"
            },
            ["Vote Average"] = new Dictionary<int, string>
            {
                [-1] = "vote_average.lte",
                [1] = "vote_average.gte"
            },*/
            [Movie.GENRES] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_genres") { AllowsMultiple = true },
                [Operators.NotEqual] = new Parameter("without_genres") { AllowsMultiple = true },
            },
            [Media.CAST] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_cast") { AllowsMultiple = true },
            },
            [Media.CREW] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_crew") { AllowsMultiple = true },
            },
            [ViewModels.CollectionViewModel.People] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_people") { AllowsMultiple = true },
            },
            [Movie.WATCH_PROVIDERS] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_watch_providers") { AllowsMultiple = true },
            },
        };

        private static readonly Dictionary<Property, Dictionary<Operators, Parameter>> DiscoverTVParameters = new Dictionary<Property, Dictionary<Operators, Parameter>>(DiscoverMediaParameters)
        {
            [Movie.RELEASE_DATE] = new Dictionary<Operators, Parameter>
            {
                [Operators.LessThan] = "release_date.lte",
                [Operators.GreaterThan] = "release_date.gte"
            },
            /*["Release Type"] = new Dictionary<int, string>
            {
                [0] = "with_release_type"
            },
            ["Vote Count"] = new Dictionary<int, string>
            {
                [-1] = "vote_count.lte",
                [1] = "vote_count.gte"
            },
            ["Vote Average"] = new Dictionary<int, string>
            {
                [-1] = "vote_average.lte",
                [1] = "vote_average.gte"
            },*/
            [TVShow.GENRES] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_genres") { AllowsMultiple = true },
                [Operators.NotEqual] = new Parameter("without_genres") { AllowsMultiple = true },
            },
            [TVShow.NETWORKS] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_networks") { AllowsMultiple = true },
            },
            [TVShow.WATCH_PROVIDERS] = new Dictionary<Operators, Parameter>
            {
                [Operators.Equal] = new Parameter("with_watch_providers") { AllowsMultiple = true },
            },
        };

        private class ParserList : List<Parser>
        {
            public Parser this[string property]
            {
                set
                {
                    if (string.IsNullOrEmpty(property))
                    {
                        Add(value);
                    }
                    else
                    {
                        Add(new ParserWrapper(value)
                        {
                            JsonParser = new JsonPropertyParser<JsonNode>(property)
                        });
                    }
                }
            }

            public ParserList() { }
            public ParserList(IEnumerable<Parser> collection) : base(collection) { }
        }

        private static readonly Parser<string> TITLE_PARSER = Media.TITLE;
        private static readonly Parser<string> ORIGINAL_TITLE_PARSER = Media.ORIGINAL_TITLE;

        private static readonly IJsonParser<TimeSpan> RUNTIME_PARSER = new JsonNodeParser<TimeSpan>(json => TimeSpan.FromMinutes(json.TryGetValue<int>()));
        private static readonly IJsonParser<Rating> MOVIE_RATING_PARSER = new RatingParser
        {
            ReviewsEndpoint = API.MOVIES.GET_REVIEWS
        };
        private static readonly IJsonParser<Rating> TV_RATING_PARSER = new RatingParser
        {
            ReviewsEndpoint = API.TV.GET_REVIEWS
        };
        private static readonly IJsonParser<IEnumerable<Company>> COMPANIES_PARSER = new JsonArrayParser<Company>(new JsonNodeParser<Company>(ParseCompany));
        private static readonly IJsonParser<IEnumerable<Credit>> CREDITS_PARSER = new JsonArrayParser<Credit>(new JsonNodeParser<Credit>(ParseCredit));

        private static readonly List<Parser> CREDITS_PARSERS = new ParserList
        {
            ["cast"] = new MultiParser<Credit>(Media.CAST, CREDITS_PARSER),
            ["crew"] = new MultiParser<Credit>(Media.CREW, CREDITS_PARSER),
        };

        private static List<Parser> MEDIA_PARSERS = new ParserList
        {
            ["tagline"] = Parser.Create(Media.TAGLINE),
            ["overview"] = Parser.Create(Media.DESCRIPTION),
            ["original_language"] = Parser.Create(Media.ORIGINAL_LANGUAGE),
            ["spoken_languages"] = Parser.Create(Media.LANGUAGES, "name"),
            ["poster_path"] = Parser.Create(Media.POSTER_PATH, path => BuildImageURL(path.TryGetValue<string>(), POSTER_SIZE)),
            ["backdrop_path"] = Parser.Create(Media.BACKDROP_PATH, path => BuildImageURL(path.TryGetValue<string>())),
            ["production_companies"] = new MultiParser<Company>(Media.PRODUCTION_COMPANIES, COMPANIES_PARSER),
            ["production_countries"] = Parser.Create(Media.PRODUCTION_COUNTRIES, "name"),
            ["popularity"] = Parser.Create(POPULARITY),
        };

        private static readonly Dictionary<TMDbRequest, List<Parser>> MEDIA_APPENDED_PARSERS = new Dictionary<TMDbRequest, List<Parser>>
        {
            //["reviews"] = new Dictionary<string, Parser>(),
#if !DEBUG || false
            [API.MOVIES.GET_VIDEOS] = new ParserList
            {
                ["results"] = Parser.Create(Media.TRAILER_PATH, ParseTrailerPath)
            },
#endif
        };

        private static readonly ItemProperties MOVIE_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>(MEDIA_APPENDED_PARSERS)
        {
            [API.MOVIES.GET_DETAILS] = new ParserList(MEDIA_PARSERS)
            {
                ["title"] = TITLE_PARSER,
                ["genres"] = Parser.Create(Movie.GENRES, "name"),
                ["runtime"] = new Parser<TimeSpan>(Media.RUNTIME, RUNTIME_PARSER),
                ["original_title"] = ORIGINAL_TITLE_PARSER,
                ["release_date"] = Parser.Create(Movie.RELEASE_DATE),
                ["budget"] = Parser.Create(Movie.BUDGET),
                ["revenue"] = Parser.Create(Movie.REVENUE),
                ["belongs_to_collection"] = Parser.Create(Movie.PARENT_COLLECTION, json => TryParseCollection(json, out var collection) ? collection : null),
                [""] = new Parser<Rating>(Media.RATING, MOVIE_RATING_PARSER)
            },
            [API.MOVIES.GET_CREDITS] = CREDITS_PARSERS,
            [API.MOVIES.GET_KEYWORDS] = new ParserList
            {
                ["keywords"] = Parser.Create(Media.KEYWORDS, "name")
            },
            [API.MOVIES.GET_RECOMMENDATIONS] = new ParserList
            {
                ["recommendations"] = Parser.Create(Media.RECOMMENDED, ParseMovieRecommended)
            },
            [API.MOVIES.GET_RELEASE_DATES] = new ParserList
            {
                ["results"] = Parser.Create(Movie.CONTENT_RATING, ParseMovieCertification)
            },
            [API.MOVIES.GET_WATCH_PROVIDERS] = new ParserList
            {
                ["results"] = new MultiParser<WatchProvider>(Movie.WATCH_PROVIDERS, new JsonNodeParser<IEnumerable<WatchProvider>>(ParseWatchProviders))
            }
        });

        private static readonly ItemProperties TVSHOW_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>(MEDIA_APPENDED_PARSERS)
        {
            [API.TV.GET_DETAILS] = new ParserList(MEDIA_PARSERS)
            {
                ["name"] = TITLE_PARSER,
                ["episode_run_time"] = Parser.Create(Media.RUNTIME, json => (json as JsonArray)?.FirstOrDefault() is JsonNode first && RUNTIME_PARSER.TryGetValue(first, out var runtime) ? runtime : TimeSpan.Zero),
                ["original_name"] = ORIGINAL_TITLE_PARSER,
                ["first_air_date"] = Parser.Create(TVShow.FIRST_AIR_DATE),
                ["last_air_date"] = Parser.Create(TVShow.LAST_AIR_DATE),
                ["genres"] = Parser.Create(TVShow.GENRES, "name"),
                ["networks"] = new MultiParser<Company>(TVShow.NETWORKS, COMPANIES_PARSER),
                [""] = new Parser<Rating>(Media.RATING, TV_RATING_PARSER)
            },
            [API.TV.GET_CONTENT_RATINGS] = new ParserList
            {
                ["results"] = Parser.Create(TVShow.CONTENT_RATING, ParseTVCertification)
            },
            [API.TV.GET_AGGREGATE_CREDITS] = CREDITS_PARSERS,
            [API.MOVIES.GET_RECOMMENDATIONS] = new ParserList
            {
                //["recommendations"] = Parser.Create(Media.RECOMMENDED)
            },
            [API.TV.GET_KEYWORDS] = new ParserList
            {
                ["results"] = Parser.Create(Media.KEYWORDS, "name")
            },
            [API.TV.GET_WATCH_PROVIDERS] = new ParserList
            {
                ["results"] = new MultiParser<WatchProvider>(TVShow.WATCH_PROVIDERS, new JsonNodeParser<IEnumerable<WatchProvider>>(ParseWatchProviders))
            }
        });

        private static readonly ItemProperties TVSEASON_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>
        {
            [API.TV_SEASONS.GET_DETAILS] = new ParserList
            {
                ["air_date"] = Parser.Create(TVSeason.YEAR),
                //["release_date"] = new Parser<TimeSpan>(TVSeason.AVERAGE_RUNTIME),
            },
            [API.TV_SEASONS.GET_AGGREGATE_CREDITS] = new ParserList
            {
                ["cast"] = new MultiParser<Credit>(TVSeason.CAST, CREDITS_PARSER),
                ["crew"] = new MultiParser<Credit>(TVSeason.CREW, CREDITS_PARSER),
            },
        });

        private static readonly ItemProperties TVEPISODE_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>
        {
            [API.TV_EPISODES.GET_DETAILS] = new ParserList
            {
                ["air_date"] = Parser.Create(TVEpisode.AIR_DATE),
                ["still_path"] = Parser.Create(Media.POSTER_PATH, path => BuildImageURL(path.TryGetValue<string>(), STILL_SIZE)),
                ["overview"] = Parser.Create(Media.DESCRIPTION),
            },
            [API.TV_EPISODES.GET_CREDITS] = new ParserList
            {
                [""] = new MultiParser<Credit>(Media.CAST, new JsonNodeParser<IEnumerable<Credit>>(ParseTVEpisodeCast)),
                ["crew"] = new MultiParser<Credit>(Media.CREW, CREDITS_PARSER),
            },
        });

        private static readonly ItemProperties PERSON_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>
        {
            [API.PEOPLE.DETAILS] = new List<Parser>
            {

            }
        });

        private static readonly Dictionary<ItemType, ItemProperties> ITEM_PROPERTIES = new Dictionary<ItemType, ItemProperties>
        {
            [ItemType.Movie] = MOVIE_PROPERTIES,
            [ItemType.TVShow] = TVSHOW_PROPERTIES,
            [ItemType.TVSeason] = TVSEASON_PROPERTIES,
            [ItemType.TVEpisode] = TVEPISODE_PROPERTIES,
            [ItemType.Person] = PERSON_PROPERTIES,
        };
    }
}