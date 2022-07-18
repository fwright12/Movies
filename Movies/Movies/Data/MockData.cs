using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Movies.Models;
using Movies.ViewModels;

namespace Movies
{
    public class MockData : IAssignID<int>
    {
        public static readonly ID<int>.Key IDKey = new ID<int>.Key();
        public static readonly ID<int> ID = IDKey.ID;

        public static readonly MockData Instance = new MockData();
        private readonly DataManager DataManager = new DataManager();

        public static Company TMDb { get; } = new Company
        {
            Name = "TMDb",
            LogoPath = "file://Movies.Logos.TMDbLogo.png"
        };

        public static Company Netflix { get; } = new Company
        {
            Name = "Netflix",
            LogoPath = "https://vignette2.wikia.nocookie.net/logopedia/images/b/b2/NetflixIcon2016.jpg/revision/latest/scale-to-width-down/2000?cb=20160620223003"
        };

        public static WatchProvider NetflixStreaming = new WatchProvider
        {
            Company = Netflix,
            Type = MonetizationType.Subscription
        };

        private static Dictionary<int, Dictionary<string, object>> MovieInfo = new Dictionary<int, Dictionary<string, object>>
        {
            [0] = new Dictionary<string, object>()
            {
                [nameof(MovieViewModel.Year)] = new DateTime(2014, 1, 1),
                [nameof(MovieViewModel.Tagline)] = "Between the stars",
                [nameof(MovieViewModel.Description)] = "Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie",
                [nameof(MovieViewModel.PosterPath)] = "https://www4.pictures.zimbio.com/mp/-7nXotCKELnx.jpg",
                [nameof(MovieViewModel.TrailerPath)] = "https://www.youtube.com/embed/zSWdZVtXT7E",
                [nameof(MovieViewModel.ContentRating)] = "PG-13",
                [nameof(MovieViewModel.Runtime)] = TimeSpan.FromMinutes(168),
                [nameof(MovieViewModel.Genres)] = new List<string> { "Adventure", "Sci-Fi" },
                [nameof(MovieViewModel.Ratings)] = new Rating
                {
                    Company = TMDb,
                    Score = 10.0,
                    Reviews = new AsyncList<Review>
                    {
                        new Review
                        {
                            Author = "Author",
                            Content = "What a good movie"
                        },
                        new Review{ },
                        new Review{ },
                        new Review{ },
                    }
                },
                [nameof(MovieViewModel.Crew)] = new List<Credit>
                {
                    new Credit
                    {
                        Role = "Director",
                        Person = new Person("Christopher Nolan")
                    },
                    new Credit
                    {
                        Role = "Composer",
                        Person = new Person("Hans Zimmer")
                    }
                },
                [nameof(MovieViewModel.Cast)] = new List<Credit>
                {
                    new Credit
                    {
                        Role = "Cooper",
                        Person = Instance.MatthewM
                    },
                    new Credit
                    {
                        Role = "Adult Murphy Cooper",
                        Person = new Person("Jessica Chastain").WithID(IDKey, 1)
                    },
                    new Credit
                    {
                        Role = "Astronaut Brand",
                        Person = new Person("Anne Hathaway").WithID(IDKey, 2)
                    },
                    new Credit
                    {
                        Role = "Dr. Brand",
                        Person = new Person("Michael Caine").WithID(IDKey, 3)
                    },
                    new Credit
                    {
                        Role = "Young Murhpy Cooper",
                        Person = new Person("Mackenzie Foy").WithID(IDKey, 4)
                    },
                    new Credit
                    {
                        Role = "Tom",
                        Person = new Person("Casey Affleck").WithID(IDKey, 5)
                    }
                },
                [nameof(MovieViewModel.WatchProviders)] = new List<WatchProvider>
                {
                    NetflixStreaming,
                    new WatchProvider
                    {
                        Company = new Company { Name = "HBO Max" },
                        Type = MonetizationType.Subscription
                    },
                    new WatchProvider
                    {
                        Company = new Company { Name = "Crackle" },
                        Type = MonetizationType.Ads
                    },
                    new WatchProvider
                    {
                        Company = new Company { Name = "iTunes" },
                        Type = MonetizationType.Buy
                    }
                },
                [nameof(MovieViewModel.ProductionCompanies)] = new List<Company>
                {
                    Netflix,
                    new Company
                    {
                        Name = "Syncopy"
                    }
                },
                [nameof(MovieViewModel.Keywords)] = new List<string> { "Space travel", "Time", "Black holes", "Wormhole", "Astronaut", "Farming", "Interdimensional" },
                [nameof(MovieViewModel.Recommended)] = new AsyncList<Item> { Instance.Arrival }
            },
            [1] = new Dictionary<string, object>
            {
                [nameof(MovieViewModel.Description)] = "Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie",
                [nameof(MovieViewModel.Ratings)] = new Rating
                {
                    Company = TMDb,
                    Score = 0.0,
                    Reviews = new AsyncList<Review>
                    {
                        new Review
                        {
                            Author = "Author",
                            Content = "What a good movie"
                        },
                        new Review{ },
                        new Review{ },
                        new Review{ },
                    }
                },
            }
        };

        private static Dictionary<int, Dictionary<string, object>> TVShowInfo = new Dictionary<int, Dictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                [nameof(TVShowViewModel.Description)] = "Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny",
                [nameof(TVShowViewModel.PosterPath)] = "https://upload.wikimedia.org/wikipedia/en/b/b6/TheOfficeUSSeason1Cover.jpg",
                [nameof(TVShowViewModel.FirstAirDate)] = new DateTime(2005, 1, 1),
                [nameof(TVShowViewModel.LastAirDate)] = new DateTime(2013, 1, 1),
                [nameof(TVShowViewModel.ContentRating)] = "TV-14",
                [nameof(TVShowViewModel.Runtime)] = TimeSpan.FromMinutes(21),
                [nameof(TVShowViewModel.Genres)] = new List<string> { "Comedy", "Mockumentary" },
                [nameof(MovieViewModel.Ratings)] = new Rating
                {
                    Company = TMDb,
                    Score = 9.0,
                    Reviews = new AsyncList<Review>
                    {
                        new Review
                        {
                            Author = "Author",
                            Content = "What a good movie"
                        },
                        new Review{ },
                        new Review{ },
                        new Review{ },
                    }
                },
            }
        };

        private static Dictionary<int, Dictionary<string, object>> TVSeasonsInfo = new Dictionary<int, Dictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                [nameof(TVSeasonViewModel.Year)] = new DateTime(2005, 1, 1),
                [nameof(TVSeasonViewModel.AvgRuntime)] = TimeSpan.FromMinutes(22),
                [nameof(TVSeasonViewModel.Cast)] = new List<Credit>
                {
                    new Credit
                    {
                        Role = "Dwight Shrute",
                        Person = new Person("Rainn Wilson").WithID(IDKey, 6)
                    },
                    new Credit
                    {
                        Role = "Michael Scott",
                        Person = new Person("Steve Carrell").WithID(IDKey, 7)
                    }
                }
            },
            [1] = new Dictionary<string, object>
            {
                [nameof(TVSeasonViewModel.AvgRuntime)] = TimeSpan.FromMinutes(22)
            },
            [2] = new Dictionary<string, object>
            {
                [nameof(TVSeasonViewModel.AvgRuntime)] = TimeSpan.FromMinutes(22)
            }
        };

        private static Dictionary<int, Dictionary<string, object>> TVEpisodeInfo = new Dictionary<int, Dictionary<string, object>>
        {
            [10] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "Michael does diversity training\nLine1\nLine2\nLine3\nLine4\nLine5\nLine6\nLine7\nLine8",
                [nameof(TVEpisodeViewModel.AirDate)] = new DateTime(2005, 1, 1),
                [nameof(TVEpisodeViewModel.ContentRating)] = "TV-14",
                [nameof(TVEpisodeViewModel.Ratings)] = new Rating
                {
                    Company = TMDb,
                    Score = 10.0,
                    Reviews = new AsyncList<Review>
                    {
                        new Review{ },
                        new Review{ },
                        new Review{ },
                        new Review{ },
                    }
                }
            },
            [11] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "Michael lets Dwight pick the health care plan"
            },
            [12] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "The upstairs workers play the warehouse in basketball"
            },
            [13] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "Dwight forms and alliance with Jim to protect his job"
            },
            [14] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "A purse saleswoman visits the office"
            },
            [20] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "Michael hosts the annual Dundies awards show"
            },
            [30] = new Dictionary<string, object>
            {
                [nameof(TVEpisodeViewModel.Runtime)] = TimeSpan.FromMinutes(22),
                [nameof(TVEpisodeViewModel.Description)] = "Michael outs Oscar"
            }
        };

        private static Dictionary<int, Dictionary<string, object>> PersonInfo = new Dictionary<int, Dictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                /*[nameof(PersonService.ProfilePathRequested)] = "https://upload.wikimedia.org/wikipedia/commons/3/3b/Matthew_McConaughey_2011.jpg",
                [nameof(PersonService.BirthdayRequested)] = new DateTime(1971, 3, 13),
                [nameof(PersonService.BirthplaceRequested)] = "Texas",
                [nameof(PersonService.AlsoKnownAsRequested)] = new List<string> { "Big M" },
                [nameof(PersonService.GenderRequested)] = "Male",
                [nameof(PersonService.BioRequested)] = "Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy",
                [nameof(PersonService.CreditsRequested)] = new List<Media>
                {
                    Instance.Interstellar,
                    new Movie("Dallas Buyers Club", 2007).WithID(IDKey, 2)
                },*/
            }
        };

        public readonly Movie Interstellar;
        public readonly Movie Arrival;
        public readonly TVShow TheOffice;
        public readonly IList<TVSeason> TheOfficeSeasons;
        public readonly Person MatthewM;

        public string Name { get; } = "Mock";

        private class AsyncList<T> : List<T>, IAsyncEnumerable<T>
        {
            public AsyncList() { }
            public AsyncList(IEnumerable<T> items) : base(items) { }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                foreach (var item in this)
                {
                    yield return item;
                }

                await Task.CompletedTask;
            }
        }

        public class DB : Collection, IAsyncFilterable<Item>
        {
            public IAsyncEnumerable<Item> GetItems(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                return Instance.GetTrending();
            }
        }

        private MockData()
        {
            //DataManager.AddDataSource(this);

            //InterstellarModel = new Movie("Interstellar", ((DateTime)MovieInfo[0][nameof(MovieViewModel.Year)]).Year, new ID(this, "0"));
            Interstellar = new Movie("Interstellar", 2014).WithID(IDKey, 0);
            Arrival = new Movie("Arrival", 2016).WithID(IDKey, 1);

            TheOffice = new TVShow("The Office").WithID(IDKey, 0);
            TheOfficeSeasons = new List<TVSeason>
            {
                new TVSeason(TheOffice, 1)
                {
                    Description = "First season",
                    PosterPath = "https://upload.wikimedia.org/wikipedia/en/b/b6/TheOfficeUSSeason1Cover.jpg"
                }.WithID(IDKey, 0),
                new TVSeason(TheOffice, 2).WithID(IDKey, 1),
                new TVSeason(TheOffice, 3).WithID(IDKey, 2),
            };
            TheOffice.Items = new AsyncList<TVSeason>(TheOfficeSeasons);
            TheOfficeSeasons[0].Items = new AsyncList<TVEpisode>
            {
                new TVEpisode(TheOfficeSeasons[0], "Diversity Day", 1).WithID(IDKey, 10),
                new TVEpisode(TheOfficeSeasons[0], "Health Care", 2).WithID(IDKey, 11),
                new TVEpisode(TheOfficeSeasons[0], "Basketball", 3).WithID(IDKey, 12),
                new TVEpisode(TheOfficeSeasons[0], "The Alliance", 4).WithID(IDKey, 13),
                new TVEpisode(TheOfficeSeasons[0], "Hot Girl", 5).WithID(IDKey, 14)
            };
            TheOfficeSeasons[1].Items = new AsyncList<TVEpisode>
            {
                new TVEpisode(TheOfficeSeasons[1], "The Dundies", 1).WithID(IDKey, 20),
            };
            TheOfficeSeasons[2].Items = new AsyncList<TVEpisode>
            {
                new TVEpisode(TheOfficeSeasons[2], "Gay Witch Hunt", 1).WithID(IDKey, 30),
            };

            MatthewM = new Person("Matthew M").WithID(IDKey, 0);
        }

        public async IAsyncEnumerable<Item> GetTrending()
        {
            yield return Interstellar;
            yield return TheOffice;
            yield return MatthewM;
            yield return Arrival;
            yield return new Collection
            {
                Name = "Recommended",
                Items = GetRecommended()
            }.WithID(IDKey, 0);
            for (int i = 0; i < 100; i++)
                yield return Arrival;

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<Item> GetRecommended()
        {
            yield return Arrival;
            yield return TheOffice;
            yield return Interstellar;
            yield return new Collection
            {
                Name = "Trending",
                Items = GetTrending()
            };
            yield return MatthewM;

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<Item> GetInTheaters()
        {
            if (!HandleAnyInfoRequests(MovieInfo, 0, nameof(MovieViewModel.Cast), out IEnumerable<Credit> credits))
            {
                yield break;
            }

            foreach (var person in credits.Select(value => value.Person))
            {
                yield return person;
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<Item> GetUpcoming()
        {
            await Task.CompletedTask;
            yield break;
        }

        public bool TryGetID(Item item, out int id) => item.TryGetID(ID, out id);

        public Task<Item> GetItem(ItemType type, int id)
        {
            Item item = null;

            if (type == ItemType.Movie)
            {
                if (id == 0) item = Interstellar;
                else if (id == 1) item = Arrival;
                else item = new Movie("Movie (" + id + ")").WithID(IDKey, id);
            }
            else if (type == ItemType.TVShow)
            {
                if (id == 0) item = TheOffice;
                else item = new TVShow("TV Show (" + id + ")").WithID(IDKey, id);
            }
            else if (type == ItemType.Person)
            {
                item = new Person("Person (" + id + ")").WithID(IDKey, id);
            }
            else if (type == ItemType.Collection)
            {
                item = new Collection { Name = "Collection (" + id + ")" }.WithID(IDKey, id);
            }

            return Task.FromResult(item);
        }

        private bool HandleAnyInfoRequests<T>(Dictionary<int, Dictionary<string, object>> info, Item item, string property, out T result)
        {
            result = default;
            return item.TryGetID(ID, out var id) && HandleAnyInfoRequests(info, id, property, out result);
        }

        private bool HandleAnyInfoRequests<T>(Dictionary<int, Dictionary<string, object>> info, int id, string property, out T result)
        {
            if (info.TryGetValue(id, out var values) && values.TryGetValue(property, out var rawValue) && rawValue is T value)
            {
                result = value;
                return true;
            }

            result = default;
            return false;
        }

        /*private void HandleMediaInfoRequests(object sender, AsyncEventArgs<object> e)
        {
            Dictionary<string, object> info = null;

            if (sender is MediaViewModel media)// && int.TryParse(media.ID, out int id))
            {
                int id = media.ID;
                bool movie = sender is MovieViewModel && MovieInfo.TryGetValue(id, out info);
                bool tvShow = sender is TVShowViewModel && TVShowInfo.TryGetValue(id, out info);
                bool tvEpisode = sender is TVEpisodeViewModel && TVEpisodeInfo.TryGetValue(id, out info);
            }

            if (info != null && info.TryGetValue(e.PropertyName, out object value))
            {
                if (e.PropertyName == nameof(TVShowViewModel.Seasons) && value is IList<TVSeasonViewModel> seasons)
                {
                    foreach (var season in seasons)
                    {
                        //season.InfoRequested += HandleMediaInfoRequests;
                    }
                }

                e.SetValue(value, "Mock");
            }
        }*/

        /*public static MovieViewModel Arrival { get; } = new MovieViewModel
        {
            Title = "Arrival",
            Description = "Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie Also a good movie"
        };*/
        /*public static MovieViewModel Interstellar { get; } = new MovieViewModel
        {
            Title = "Interstellar",
            Tagline = "Between the stars",
            Description = "Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie Really good movie",
            PosterPath = "https://upload.wikimedia.org/wikipedia/en/b/bc/Interstellar_film_poster.jpg",
            Year = new DateTime(2014, 1, 1),
            ContentRating = "PG-13",
            Runtime = TimeSpan.FromMinutes(168),
            Genres = new List<string> { "Adventure", "Sci-Fi" },
            Ratings = new List<Rating>
            {
                new Rating
                {
                    Company = TMDb,
                    Score = 10.0,
                    Reviews = new List<Review>
                    {
                        new Review{ },
                        new Review{ },
                        new Review{ },
                        new Review{ },
                    }
                },
                new Rating
                {
                    Company = new Company
                    {
                        Name = "Rotten Tomatoes",
                        LogoPath = "https://upload.wikimedia.org/wikipedia/commons/6/6e/Tmdb-312x276-logo.png",
                    },
                    Score = 10.0
                }
            },
            Crew = new List<Credit>
            {
                new Credit
                {
                    Role = "Director",
                    Person = new PersonViewModel
                    {
                        Name = "Christopher Nolan"
                    }
                },
                new Credit
                {
                    Role = "Composer",
                    Person = new PersonViewModel
                    {
                        Name = "Hans Zimmer"
                    }
                }
            },
            Cast = new List<Credit>
            {
                new Credit
                {
                    Role = "Cooper",
                    Person = new PersonViewModel
                    {
                        Name = "Matthew M",
                        ProfilePath = "https://upload.wikimedia.org/wikipedia/commons/3/3b/Matthew_McConaughey_2011.jpg",
                        Birthday = new DateTime(1971, 3, 13),
                        Birthplace = "Texas",
                        AlsoKnownAs = new List<string> { "Big M" },
                        Gender = "Male",
                        Bio = "Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy Cool guy",
                        Credits = new List<object>
                        {
                            new MovieViewModel { Title = "Interstellar" },
                            new MovieViewModel { Title = "Interstellar" },
                        },
                    }
                },
                new Credit
                {
                    Role = "Adult Murphy Cooper",
                    Person = new PersonViewModel
                    {
                        Name = "Jessica Chastain"
                    }
                },
                new Credit
                {
                    Role = "Astronaut Brand",
                    Person = new PersonViewModel
                    {
                        Name = "Anne Hathaway"
                    }
                },
                new Credit
                {
                    Role = "Dr. Brand",
                    Person = new PersonViewModel
                    {
                        Name = "Michael Caine"
                    }
                },
                new Credit
                {
                    Role = "Young Murhpy Cooper",
                    Person = new PersonViewModel
                    {
                        Name = "Mackenzie Foy"
                    }
                },
                new Credit
                {
                    Role = "Tom",
                    Person = new PersonViewModel
                    {
                        Name = "Casey Affleck"
                    }
                }
            },
            WatchProviders = new List<WatchProvider>
            {
                new WatchProvider
                {
                    Company = Netflix
                }
            },
            ProductionCompanies = new List<Company>
            {
                Netflix
            },
            Keywords = new List<string> { "Space travel", "Time", "Black holes", "Wormhole", "Astronaut", "Farming", "Interdimensional" },
            Recommended = new Collection
            {
                Name = "Recommended",
                Items = new List<MovieViewModel> { Arrival }
            }
        };*/
        /*public static TVShowViewModel TheOffice { get; } = new TVShowViewModel
        {
            Title = "The Office",
            Description = "Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny Very funny",
            PosterPath = "https://upload.wikimedia.org/wikipedia/en/b/b6/TheOfficeUSSeason1Cover.jpg",
            FirstAirDate = new DateTime(2005, 1, 1),
            LastAirDate = new DateTime(2013, 1, 1),
            ContentRating = "TV-14",
            Runtime = TimeSpan.FromMinutes(21),
            Genres = new List<string> { "Comedy", "Mockumentary" },
            Seasons = new List<SeasonViewModel>
            {
                new SeasonViewModel
                {
                    PosterPath = "https://upload.wikimedia.org/wikipedia/en/b/b6/TheOfficeUSSeason1Cover.jpg",
                    Name = "Season 1",
                    Year = new DateTime(2005, 1, 1),
                    Number = 1,
                    AvgRuntime = TimeSpan.FromMinutes(22),
                    Description = "First season",
                    Cast = new List<Credit>
                    {
                        new Credit
                        {
                            Role = "Dwight Shrute",
                            Person = new PersonViewModel
                            {
                                Name = "Rainn Wilson"
                            }
                        },
                        new Credit
                        {
                            Role = "Michael Scott",
                            Person = new PersonViewModel
                            {
                                Name = "Steve Carrell"
                            }
                        }
                    },
                    Items = new List<EpisodeViewModel>
                    {
                        new EpisodeViewModel
                        {
                            Title = "Diversity Day",
                            Season = 1,
                            Number = 1,
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "Michael does diversity training",
                            AirDate = new DateTime(2005, 1, 1),
                            ContentRating = "TV-14",
                            Ratings = new List<Rating>
                            {
                                new Rating
                                {
                                    Company = TMDb,
                                    Score = 10.0,
                                    Reviews = new List<Review>
                                    {
                                        new Review{ },
                                        new Review{ },
                                        new Review{ },
                                        new Review{ },
                                    }
                                }
                            },
                        },
                        new EpisodeViewModel
                        {
                            Title = "Health Care",
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "Michael lets Dwight pick the health care plan"
                        },
                        new EpisodeViewModel
                        {
                            Title = "Basketball",
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "The upstairs workers play the warehouse in basketball"
                        },
                        new EpisodeViewModel
                        {
                            Title = "The Alliance",
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "Dwight forms and alliance with Jim to protect his job"
                        },
                        new EpisodeViewModel
                        {
                            Title = "Hot Girl",
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "A purse saleswoman visits the office"
                        }
                    }
                },
                new SeasonViewModel
                {
                    Name = "Season 2",
                    Number = 2,
                    AvgRuntime = TimeSpan.FromMinutes(22),
                    Items = new List<EpisodeViewModel>
                    {
                        new EpisodeViewModel
                        {
                            Title = "The Dundies",
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "Michael hosts the annual Dundies awards show"
                        }
                    }
                },
                new SeasonViewModel
                {
                    Name = "Season 3",
                    Number = 3,
                    AvgRuntime = TimeSpan.FromMinutes(22),
                    Items = new List<EpisodeViewModel>
                    {
                        new EpisodeViewModel
                        {
                            Title = "Gay Witch Hunt",
                            Runtime = TimeSpan.FromMinutes(22),
                            Description = "Michael outs Oscar"
                        }
                    }
                }
            }
        };*/
    }
}
