using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TMDbLib.Client;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Reviews;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Trending;
using TMDbLib.Objects.TvShows;

namespace Movies
{
#if DEBUG
    internal class TMDbClient : TMDbLib.Client.TMDbClient
    {
        public static TMDbClient Create() => (TMDbClient)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMDbClient));
        public TMDbClient(string apiKey) : base(apiKey) { }

        new public Task<SearchContainer<SearchMovie>> GetTrendingMoviesAsync(TimeWindow timeWindow, int page = 0, CancellationToken cancellationToken = default)
        {
            //return base.GetTrendingMoviesAsync(timeWindow, page, cancellationToken);
            return Task.FromResult(new SearchContainer<SearchMovie>
            {
                Page = 1,
                TotalPages = 1,
                Results = new List<SearchMovie>
                    {
                        new SearchMovie
                        {
                            Title = "Interstellar",
                            Id = 157336,
                            ReleaseDate = new DateTime(2014, 11, 07),
                            PosterPath = "https://www4.pictures.zimbio.com/mp/-7nXotCKELnx.jpg"
                        }
                    }
            });
        }

        new public Task<Movie> GetMovieAsync(int movieId, MovieMethods extraMethods = MovieMethods.Undefined, CancellationToken cancellationToken = default)
        {
            //return base.GetMovieAsync(movieId, extraMethods, cancellationToken);
            Print.Log(movieId, extraMethods);
            Movie movie;
            movie = new Movie
            {
                Title = "Interstellar",
                Id = movieId,
                ReleaseDate = new DateTime(2014, 11, 07),
                PosterPath = "https://www4.pictures.zimbio.com/mp/-7nXotCKELnx.jpg",
                BackdropPath = "interstellar backdrop.jpg",
                Overview = "Astronauts go to space",
                VoteAverage = 7.8,
                VoteCount = 503,
                Genres = new List<Genre>
                    {
                        new Genre { Name = "Adventure" },
                        new Genre { Name = "Sci-fi" },
                        new Genre { Name = "Drama" },
                    },
                WatchProviders = new SingleResultContainer<Dictionary<string, WatchProviders>>
                {
                    Results = new Dictionary<string, WatchProviders>
                    {
                        ["US"] = new WatchProviders
                        {
                            Link = "test",
                            Free = new List<WatchProviderItem>
                            {
                                new WatchProviderItem
                                {
                                    ProviderName = "Netflix",
                                    LogoPath = "https://vignette2.wikia.nocookie.net/logopedia/images/b/b2/NetflixIcon2016.jpg/revision/latest/scale-to-width-down/2000?cb=20160620223003"
                                }
                            }
                        }
                    }
                },
                Credits = new TMDbLib.Objects.Movies.Credits
                {
                    Cast = new List<TMDbLib.Objects.Movies.Cast>
                        {
                            new TMDbLib.Objects.Movies.Cast
                            {
                                Id = 10297,
                                Name = "Matthew M",
                                Character = "Cooper",
                                ProfilePath = "matthew m profile.jpg"
                            },
                            new TMDbLib.Objects.Movies.Cast
                            {
                                Id = 2,
                                Name = "Jessica Chastain",
                                Character = "Murphy",
                                ProfilePath = "jessica chastain profile.jpg"
                            }
                        }
                },
                BelongsToCollection = new SearchCollection
                {
                    Name = "Good Movies",
                    PosterPath = "belongs to collection poster.jpg"
                }
            };
            //await Task.Delay(5000);
            //Print.Log(movie.Id, movie.Overview);
            return Task.FromResult(movie);
        }

        new public Task<SearchContainer<SearchTv>> GetTrendingTvAsync(TimeWindow timeWindow, int page = 0, CancellationToken cancellationToken = default)
        {
            //return base.GetTrendingTvAsync(timeWindow, page, cancellationToken);
            return Task.FromResult(new SearchContainer<SearchTv>
            {
                Page = 1,
                TotalPages = 1,
                Results = new List<SearchTv>
                    {
                        new SearchTv
                        {
                            Name = "The Office",
                            Id = 2316,
                            FirstAirDate = new DateTime(2005, 1, 24),
                            PosterPath = "the office poster.jpg",
                        }
                    }
            });
        }

        new public Task<TvShow> GetTvShowAsync(int id, TvShowMethods extraMethods = TvShowMethods.Undefined, string language = null, string includeImageLanguage = null, CancellationToken cancellationToken = default)
        {
            //return base.GetTvShowAsync(id, extraMethods, language, includeImageLanguage, cancellationToken);
            return Task.FromResult(new TvShow
            {
                Id = id,
                Name = "The Office",
                Overview = "Very funny show",
                PosterPath = "the office poster.jpg",
                BackdropPath = "the office backdrop.jpg",
                Seasons = new List<SearchTvSeason>
                    {
                        new SearchTvSeason
                        {
                            Name = "Season 1",
                            Id = 0,
                            SeasonNumber = 1
                        },
                        new SearchTvSeason
                        {
                            Name = "Season 2",
                            Id = 1,
                            SeasonNumber = 2
                        },
                        new SearchTvSeason
                        {
                            Name = "Season 3",
                            Id = 2,
                            SeasonNumber = 3
                        },
                        new SearchTvSeason
                        {
                            Name = "Season 4",
                            Id = 3,
                            SeasonNumber = 4
                        },
                        new SearchTvSeason
                        {
                            Name = "Season 5",
                            Id = 4,
                            SeasonNumber = 5
                        },
                        new SearchTvSeason
                        {
                            Name = "Season 6",
                            Id = 5,
                            SeasonNumber = 6
                        }
                    }
            });
        }

        new public Task<TvSeason> GetTvSeasonAsync(int tvShowId, int seasonNumber, TvSeasonMethods extraMethods = TvSeasonMethods.Undefined, string language = null, string includeImageLanguage = null, CancellationToken cancellationToken = default)
        {
            //return base.GetTvSeasonAsync(tvShowId, seasonNumber, extraMethods, language, includeImageLanguage, cancellationToken);
            return Task.FromResult(new TvSeason
            {
                Name = "Season 1",
                PosterPath = "season 1 poster.jpg",
                Overview = "Season 1",
                Episodes = new List<TvSeasonEpisode>
                    {
                        new TvSeasonEpisode
                        {
                            Name = "Episode 1",
                            SeasonNumber = seasonNumber,
                            EpisodeNumber = 1,
                            AirDate = new DateTime(2005, 4, 09),
                            Overview = "Pilot",
                            StillPath = "s1e1 still.jpg"
                        }
                    }
            });
        }

        new public Task<TvEpisode> GetTvEpisodeAsync(int tvShowId, int seasonNumber, int episodeNumber, TvEpisodeMethods extraMethods = TvEpisodeMethods.Undefined, string language = null, string includeImageLanguage = null, CancellationToken cancellationToken = default)
        {
            //return base.GetTvEpisodeAsync(tvShowId, seasonNumber, episodeNumber, extraMethods, language, includeImageLanguage, cancellationToken);
            return Task.FromResult(new TvEpisode
            {
                Name = "Episode 1",
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber,
                AirDate = new DateTime(2005, 4, 09),
                Overview = "Pilot"
            });
        }

        new public Task<SearchContainer<SearchPerson>> GetTrendingPeopleAsync(TimeWindow timeWindow, int page = 0, CancellationToken cancellationToken = default)
        {
            //return base.GetTrendingPeopleAsync(timeWindow, page, cancellationToken);
            return Task.FromResult(new SearchContainer<SearchPerson>
            {
                Page = 1,
                TotalPages = 1,
                Results = new List<SearchPerson>
                {
                    new SearchPerson
                    {
                        Name = "Matthew M",
                        Id = 10297,
                        ProfilePath = "matthew m profile.jpg"
                    }
                }
            });
        }

        new public Task<Person> GetPersonAsync(int personId, PersonMethods extraMethods = PersonMethods.Undefined, CancellationToken cancellationToken = default)
        {
            //return base.GetPersonAsync(personId, extraMethods, cancellationToken);
            return Task.FromResult(new Person
            {
                Id = personId,
                Name = "Matthew M",
                Biography = "Cool guy",
                Birthday = new DateTime(1976, 3, 15),
                PlaceOfBirth = "Texas",
                AlsoKnownAs = new List<string> { "Big M" },
                MovieCredits = new MovieCredits
                {
                    Cast = new List<MovieRole>
                        {
                            new MovieRole
                            {
                                Title = "Interstellar",
                                Id = 157336,
                                ReleaseDate = new DateTime(2014, 11, 07),
                                PosterPath = "https://www4.pictures.zimbio.com/mp/-7nXotCKELnx.jpg"
                            },
                            new MovieRole
                            {
                                Title = "Dallas Buyers Club",
                                Id = 35151
                            }
                        },
                    Crew = new List<MovieJob>
                        {
                            new MovieJob
                            {
                                Title = "Some movie",
                                Id = 948105
                            }
                        }
                },
                TvCredits = new TvCredits
                {
                    Cast = new List<TvRole>
                        {
                            new TvRole
                            {
                                Name = "Some show",
                                Id = 48144
                            }
                        }
                }
            });
        }

        new public Task<Collection> GetCollectionAsync(int collectionId, CollectionMethods extraMethods = CollectionMethods.Undefined, CancellationToken cancellationToken = default)
        {
            //return base.GetCollectionAsync(collectionId, extraMethods, cancellationToken);
            return Task.FromResult(new Collection
            {
                Id = collectionId,
                Name = "test collection",
                Overview = "some collection",
                PosterPath = "collection poster.jpg",
                Parts = new List<SearchMovie>
                    {
                        new SearchMovie { Title = "Movie 1", PosterPath = "Movie 1 poster.jpg" },
                        //new SearchMovie { Title = "Movie 2", PosterPath = "Movie 2 poster.jpg" }
                    }
            });
        }

        new public Task<SearchContainerWithId<ReviewBase>> GetMovieReviewsAsync(int movieId, int page = 0, CancellationToken cancellationToken = default)
        {
            //return base.GetMovieReviewsAsync(movieId, page, cancellationToken);
            return Task.FromResult(new SearchContainerWithId<ReviewBase>
            {
                Id = movieId,
                Page = page,
                TotalPages = 1,
                TotalResults = 3,
                Results = new List<ReviewBase>
                {
                    new ReviewBase
                    {
                        Author = "test",
                        Content = "some review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\n"
                    },
                    new ReviewBase
                    {
                        Author = "test",
                        Content = "some review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\n"
                    },
                    new ReviewBase
                    {
                        Author = "test",
                        Content = "some review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\nsome review content\n"
                    }
                }
            });
        }
    }
#endif
}