using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Movies
{
    public static class API
    {
        /*
         * v4
         */

        public static class V4
        {
            public static class ACCOUNT
            {
                public static readonly PagedTMDbRequest GET_MOVIES = new PagedTMDbRequest("account/{0}/movie/{1}")
                {
                    Version = 4,
                };
                public static readonly PagedTMDbRequest GET_TV_SHOWS = new PagedTMDbRequest("account/{0}/tv/{1}")
                {
                    Version = 4
                };
            }
        }

        public static class LIST
        {
            public static readonly PagedTMDbRequest GET_LIST = new PagedTMDbRequest("list/{0}")
            {
                Version = 4
            };
            public static readonly TMDbRequest CREATE_LIST = new TMDbRequest("list")
            {
                Version = 4
            };
            public static readonly TMDbRequest UPDATE_LIST = new TMDbRequest("list/{0}")
            {
                Version = 4
            };
            public static readonly TMDbRequest DELETE_LIST = new TMDbRequest("list/{0}")
            {
                Version = 4
            };
            public static readonly TMDbRequest ADD_ITEMS = new TMDbRequest("list/{0}/items")
            {
                Version = 4
            };
            public static readonly TMDbRequest REMOVE_ITEMS = new TMDbRequest("list/{0}/items")
            {
                Version = 4
            };
            public static readonly TMDbRequest CHECK_ITEM_STATUS = new TMDbRequest("list/{0}/item_status?media_id={1}&media_type={2}")
            {
                Version = 4
            };
        }

        /*
         * v3
         */

        public static class V3
        {
            public static class ACCOUNT
            {
                public static readonly TMDbRequest ADD_TO_LIST = "account/{0}/{1}?session_id={2}";
            }
        }

        public static class CERTIFICATIONS
        {
            public static readonly TMDbRequest GET_MOVIE_CERTIFICATIONS = "certification/movie/list";
            public static readonly TMDbRequest GET_TV_CERTIFICATIONS = "certification/tv/list";
        }

        public static class CHANGES
        {
            public static readonly PagedTMDbRequest GET_MOVIE_CHANGE_LIST = "movie/changes";
            public static readonly PagedTMDbRequest GET_TV_CHANGE_LIST = "tv/changes";
            public static readonly PagedTMDbRequest GET_PERSON_CHANGE_LIST = "person/changes";
        }

        public static class COLLECTIONS
        {
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("collection/{0}")
            {
                HasLanguageParameter = true,
            };
        }

        public static class CONFIGURATION
        {
            public static readonly TMDbRequest GET_COUNTRIES = "configuration/countries";
            public static readonly TMDbRequest GET_API_CONFIGURATION = "configuration";
        }

        public static class DISCOVER
        {
            public static readonly PagedTMDbRequest MOVIE_DISCOVER = new PagedTMDbRequest("discover/movie")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
                HasAdultParameter = true,
            };
            public static readonly PagedTMDbRequest TV_DISCOVER = new PagedTMDbRequest("discover/tv")
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
            public static readonly TMDbRequest GET_TV_LIST = new TMDbRequest("genre/tv/list")
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
            public static readonly PagedTMDbRequest GET_RECOMMENDATIONS = new PagedTMDbRequest("movie/{0}/recommendations")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_RELEASE_DATES = "movie/{0}/release_dates";
            public static readonly PagedTMDbRequest GET_REVIEWS = new PagedTMDbRequest("movie/{0}/reviews")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_VIDEOS = new TMDbRequest("movie/{0}/videos")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_WATCH_PROVIDERS = "movie/{0}/watch/providers";

            public static readonly PagedTMDbRequest GET_NOW_PLAYING = new PagedTMDbRequest("movie/now_playing")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest GET_POPULAR = new PagedTMDbRequest("movie/popular")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest GET_TOP_RATED = new PagedTMDbRequest("movie/top_rated")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest GET_UPCOMING = new PagedTMDbRequest("movie/upcoming")
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
            public static readonly TMDbRequest GET_DETAILS = new TMDbRequest("person/{0}")
            {
                SupportsAppendToResponse = true,
                HasLanguageParameter = true,
            };

            public static readonly TMDbRequest GET_COMBINED_CREDITS = new TMDbRequest("person/{0}/combined_credits")
            {
                HasLanguageParameter = true
            };

            public static readonly PagedTMDbRequest GET_POPULAR = new PagedTMDbRequest("person/popular")
            {
                HasLanguageParameter = true,
            };
        }

        public static class SEARCH
        {
            public static readonly PagedTMDbRequest SEARCH_COLLECTIONS = new PagedTMDbRequest("search/collection")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest SEARCH_KEYWORDS = new PagedTMDbRequest("search/keyword");
            public static readonly PagedTMDbRequest SEARCH_MOVIES = new PagedTMDbRequest("search/movie")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
                HasRegionParameter = true,
            };
            public static readonly PagedTMDbRequest MULTI_SEARCH = new PagedTMDbRequest("search/multi")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
                HasRegionParameter = true
            };
            public static readonly PagedTMDbRequest SEARCH_PEOPLE = new PagedTMDbRequest("search/person")
            {
                HasLanguageParameter = true,
                HasAdultParameter = true,
                HasRegionParameter = true
            };
            public static readonly PagedTMDbRequest SEARCH_TV_SHOWS = new PagedTMDbRequest("search/tv")
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
            public static readonly PagedTMDbRequest GET_RECOMMENDATIONS = new PagedTMDbRequest("tv/{0}/recommendations")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_REVIEWS = new PagedTMDbRequest("tv/{0}/reviews")
            {
                HasLanguageParameter = true,
            };
            public static readonly TMDbRequest GET_VIDEOS = new TMDbRequest("tv/{0}/videos")
            {
                HasLanguageParameter = true
            };
            public static readonly TMDbRequest GET_WATCH_PROVIDERS = "tv/{0}/watch/providers";

            public static readonly PagedTMDbRequest GET_TV_AIRING_TODAY = new PagedTMDbRequest("tv/airing_today")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_TV_ON_THE_AIR = new PagedTMDbRequest("tv/on_the_air")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_POPULAR = new PagedTMDbRequest("tv/popular")
            {
                HasLanguageParameter = true,
            };
            public static readonly PagedTMDbRequest GET_TOP_RATED = new PagedTMDbRequest("tv/top_rated")
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

        public static class WATCH_PROVIDERS
        {
            public static readonly TMDbRequest GET_MOVIE_PROVIDERS = new TMDbRequest("watch/providers/movie")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
            };
            public static readonly TMDbRequest GET_TV_PROVIDERS = new TMDbRequest("watch/providers/tv")
            {
                HasLanguageParameter = true,
                HasRegionParameter = true,
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
        public static readonly Property<double> POPULARITY = new Property<double>("TMDb Popularity");
        public static readonly Property SCORE = new Property<double>("TMDb Score", new SteppedValueRange
        {
            First = 0.0,
            Last = 10.0,
            Step = 0.1
        }) // typeof(Rating).GetProperty(nameof(Rating.Score)))
        {
            Parent = Media.RATING
        };
        public static readonly Property VOTE_COUNT = new ReflectedProperty("Vote Count", typeof(Rating).GetProperty(nameof(Rating.TotalVotes)))
        {
            Parent = Media.RATING
        };

        private static readonly Dictionary<Property, Dictionary<Operators, Parameter>> DiscoverMediaParameters = new Dictionary<Property, Dictionary<Operators, Parameter>>
        {
            /*["Vote Count"] = new Dictionary<int, string>
            {
                [-1] = "vote_count.lte",
                [1] = "vote_count.gte"
            },*/
            [SCORE] = new Dictionary<Operators, Parameter>
            {
                [Operators.LessThan] = "vote_average.lte",
                [Operators.GreaterThan] = "vote_average.gte"
            },
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
                [Operators.LessThan] = "air_date.lte",
                [Operators.GreaterThan] = "air_date.gte"
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
                        Add(new ParserWrapper(value, new JsonPropertyParser<ArraySegment<byte>>(property)));
                    }
                }
            }

            public ParserList() { }
            public ParserList(IEnumerable<Parser> collection) : base(collection) { }
        }

        private static readonly Parser<string> TITLE_PARSER = Media.TITLE;
        private static readonly Parser<string> ORIGINAL_TITLE_PARSER = Media.ORIGINAL_TITLE;

        private static readonly JsonNodeParser<TimeSpan> RUNTIME_PARSER = new JsonNodeParser<TimeSpan>(json => TimeSpan.FromMinutes(json.TryGetValue<int>()));
        private static readonly JsonNodeParser<long> MONEY_PARSER = new JsonNodeParser<long>((JsonNode json, out long value) => json.TryGetValue<long>(out value) && value > 0);
        private static readonly JsonNodeParser<Language> LANGUAGE_PARSER = new JsonNodeParser<Language>((JsonNode json, out Language lang) =>
        {
            if (json.TryGetValue(out string iso_639))
            {
                lang = new Language(iso_639);
                return true;
            }

            lang = null;
            return false;
        });
        private static readonly IJsonParser<Rating> MOVIE_RATING_PARSER = new RatingParser
        {
            ReviewsEndpoint = API.MOVIES.GET_REVIEWS
        };
        private static readonly IJsonParser<Rating> TV_RATING_PARSER = new RatingParser
        {
            ReviewsEndpoint = API.TV.GET_REVIEWS
        };
        private static readonly IJsonParser<IEnumerable<Company>> COMPANIES_PARSER = new JsonArrayParser<Company>(new JsonNodeParser<Company>(ParseCompany));
        private static readonly IJsonParser<IEnumerable<Genre>> GENRES_PARSER = new JsonArrayParser<Genre>(new JsonNodeParser<Genre>(TryParseGenre));
        private static readonly MultiParser<Keyword> KEYWORDS_PARSER = new MultiParser<Keyword>(Media.KEYWORDS, new JsonArrayParser<Keyword>(new JsonNodeParser<Keyword>(TryParseKeyword)));

        private static readonly List<Parser> CREDITS_PARSERS = new ParserList
        {
            ["cast"] = new MultiParser<Credit>(Media.CAST, new JsonNodeParser<IEnumerable<Credit>>(TryParseCredits)),
            ["crew"] = new MultiParser<Credit>(Media.CREW, new JsonNodeParser<IEnumerable<Credit>>(TryParseCredits))
        };
        private static readonly List<Parser> TV_CREDITS_PARSERS = new ParserList
        {
            ["cast"] = new MultiParser<Credit>(Media.CAST, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVCredits)),
            ["crew"] = new MultiParser<Credit>(Media.CREW, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVCredits))
        };
        private static readonly List<Parser> CREDITS_PARSERS1 = new List<Parser>
        {
            //new MultiParser<Credit>(Media.CAST, new JsonNodeParser<IEnumerable<Credit>>(TryParseCast)),
            //new MultiParser<Credit>(Media.CREW, new JsonNodeParser<IEnumerable<Credit>>(TryParseCrew))
        };
        private static readonly List<Parser> TV_CREDITS_PARSERS1 = new List<Parser>
        {
            //new MultiParser<Credit>(Media.CAST, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVCast)),
            //new MultiParser<Credit>(Media.CREW, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVCrew))
        };

        private static List<Parser> MEDIA_PARSERS = new ParserList
        {
            ["tagline"] = Parser.Create(Media.TAGLINE),
            ["overview"] = Parser.Create(Media.DESCRIPTION),
            ["original_language"] = new Parser<Language>(Media.ORIGINAL_LANGUAGE, LANGUAGE_PARSER),
            ["spoken_languages"] = new MultiParser<Language>(Media.LANGUAGES, new JsonArrayParser<Language>(new JsonPropertyParser<Language>("iso_639_1", LANGUAGE_PARSER))),
            ["poster_path"] = Parser.Create(Media.POSTER_PATH, path => BuildImageURL(path.TryGetValue<string>(), POSTER_SIZE)),
            ["backdrop_path"] = Parser.Create(Media.BACKDROP_PATH, path => BuildImageURL(path.TryGetValue<string>())),
            ["production_companies"] = new MultiParser<Company>(Media.PRODUCTION_COMPANIES, COMPANIES_PARSER),
            ["production_countries"] = Parser.Create(Media.PRODUCTION_COUNTRIES, "name"),
            ["popularity"] = Parser.Create(POPULARITY),
        };

        private static readonly ItemProperties MOVIE_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>
        {
            [API.MOVIES.GET_DETAILS] = new ParserList(MEDIA_PARSERS)
            {
                ["title"] = TITLE_PARSER,
                ["genres"] = new MultiParser<Genre>(Movie.GENRES, GENRES_PARSER),
                ["runtime"] = new Parser<TimeSpan>(Media.RUNTIME, RUNTIME_PARSER),
                ["original_title"] = ORIGINAL_TITLE_PARSER,
                ["release_date"] = Parser.Create(Movie.RELEASE_DATE),
                ["budget"] = new Parser<long>(Movie.BUDGET, MONEY_PARSER),
                ["revenue"] = new Parser<long>(Movie.REVENUE, MONEY_PARSER),
                //["budget"] = Parser.Create(Movie.BUDGET),
                //["revenue"] = Parser.Create(Movie.REVENUE),
                ["belongs_to_collection"] = new CollectionParser(Movie.PARENT_COLLECTION),
                [""] = new Parser<Rating>(Media.RATING, MOVIE_RATING_PARSER)
            },
            [API.MOVIES.GET_CREDITS] = CREDITS_PARSERS,
            [API.MOVIES.GET_KEYWORDS] = new ParserList
            {
                ["keywords"] = KEYWORDS_PARSER
            },
            [API.MOVIES.GET_RECOMMENDATIONS] = new ParserList
            {
                ["results"] = Parser.Create(Media.RECOMMENDED, json => ParseRecommended<Movie>(json, TryParseMovie))
            },
            [API.MOVIES.GET_RELEASE_DATES] = new ParserList
            {
                ["results"] = Parser.Create(Movie.CONTENT_RATING, ParseMovieCertification)
            },
#if !DEBUG || false
            [API.MOVIES.GET_VIDEOS] = new ParserList
            {
                ["results"] = Parser.Create(Media.TRAILER_PATH, ParseTrailerPath)
            },
#endif
            [API.MOVIES.GET_WATCH_PROVIDERS] = new ParserList
            {
                ["results"] = new MultiParser<WatchProvider>(Movie.WATCH_PROVIDERS, new JsonNodeParser<IEnumerable<WatchProvider>>(ParseWatchProviders))
            }
        });

        private static readonly ItemProperties TVSHOW_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>
        {
            [API.TV.GET_DETAILS] = new ParserList(MEDIA_PARSERS)
            {
                ["name"] = TITLE_PARSER,
                ["episode_run_time"] = Parser.Create(Media.RUNTIME, json => (json as JsonArray)?.FirstOrDefault() is JsonNode first && RUNTIME_PARSER.TryGetValue(first, out var runtime) ? runtime : TimeSpan.Zero),
                ["original_name"] = ORIGINAL_TITLE_PARSER,
                ["first_air_date"] = Parser.Create(TVShow.FIRST_AIR_DATE),
                ["last_air_date"] = new Parser<DateTime?>(TVShow.LAST_AIR_DATE, new JsonNodeParser<DateTime?>(TryParseLastAirDate)),
                ["genres"] = new MultiParser<Genre>(TVShow.GENRES, GENRES_PARSER),
                ["networks"] = new MultiParser<Company>(TVShow.NETWORKS, COMPANIES_PARSER),
                ["seasons"] = new MultiParser<TVSeason>(TVShow.SEASONS, null),
                [""] = new Parser<Rating>(Media.RATING, TV_RATING_PARSER)
            },
            [API.TV.GET_CONTENT_RATINGS] = new ParserList
            {
                ["results"] = Parser.Create(TVShow.CONTENT_RATING, ParseTVCertification)
            },
            [API.TV.GET_AGGREGATE_CREDITS] = TV_CREDITS_PARSERS,
            [API.TV.GET_RECOMMENDATIONS] = new ParserList
            {
                ["results"] = Parser.Create(Media.RECOMMENDED, json => ParseRecommended<TVShow>(json, TryParseTVShow))
            },
            [API.TV.GET_KEYWORDS] = new ParserList
            {
                ["results"] = KEYWORDS_PARSER
            },
#if !DEBUG || false
            [API.TV.GET_VIDEOS] = new ParserList
            {
                ["results"] = Parser.Create(Media.TRAILER_PATH, ParseTrailerPath)
            },
#endif
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
                ["episodes"] = new MultiParser<TVEpisode>(TVSeason.EPISODES, null),
                //[""] = new MultiParser<TVEpisode>(TVSeason.EPISODES, new TVItemsParser<TVSeason, TVEpisode>("episodes", TryParseTVEpisode)),
            },
            [API.TV_SEASONS.GET_AGGREGATE_CREDITS] = new ParserList
            {
                ["cast"] = new MultiParser<Credit>(TVSeason.CAST, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVCredits)),
                ["crew"] = new MultiParser<Credit>(TVSeason.CREW, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVCredits))
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
                [""] = new MultiParser<Credit>(Media.CAST, new JsonNodeParser<IEnumerable<Credit>>(TryParseTVEpisodeCast)),
                ["crew"] = new MultiParser<Credit>(Media.CREW, new JsonNodeParser<IEnumerable<Credit>>(TryParseCredits))
            },
        });

        private static readonly ItemProperties PERSON_PROPERTIES = new ItemProperties(new Dictionary<TMDbRequest, List<Parser>>
        {
            [API.PEOPLE.GET_DETAILS] = new ParserList
            {
                ["name"] = Parser.Create(Person.NAME),
                ["birthday"] = Parser.Create(Person.BIRTHDAY),
                ["deathday"] = Parser.Create(Person.DEATHDAY),
                ["also_known_as"] = new MultiParser<string>(Person.ALSO_KNOWN_AS, new JsonArrayParser<string>()),
                ["gender"] = Parser.Create(Person.GENDER),
                ["biography"] = Parser.Create(Person.BIO),
                ["popularity"] = Parser.Create(POPULARITY),
                ["place_of_birth"] = Parser.Create(Person.BIRTHPLACE),
                ["profile_path"] = Parser.Create(Person.PROFILE_PATH, path => BuildImageURL(path.TryGetValue<string>(), PROFILE_SIZE))
            },
            [API.PEOPLE.GET_COMBINED_CREDITS] = new List<Parser>
            {
                new MultiParser<Item>(Person.CREDITS, new JsonNodeParser<IEnumerable<Item>>(TryParsePersonCredits))
            }
        });

        public static readonly Dictionary<ItemType, ItemProperties> ITEM_PROPERTIES = new Dictionary<ItemType, ItemProperties>
        {
            [ItemType.Movie] = MOVIE_PROPERTIES,
            [ItemType.TVShow] = TVSHOW_PROPERTIES,
            [ItemType.TVSeason] = TVSEASON_PROPERTIES,
            [ItemType.TVEpisode] = TVEPISODE_PROPERTIES,
            [ItemType.Person] = PERSON_PROPERTIES,
        };
    }
}