using System.Collections;
using System.Text;
using System.Web;

namespace MoviesTests.Data
{
    internal class ResourceAssert
    {
        public static readonly Uri EXTERNAL_IDS_MOVIE_URI = new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), "0"), UriKind.Relative);

        private static readonly Uri BASE_TMDB_URI = new Uri("https://tmdb.com");

        public static void AreEquivalentTMDbUrl(string expected, string actual)
        {
            var expectedUri = new Uri(BASE_TMDB_URI, expected);
            var actualUri = new Uri(BASE_TMDB_URI, actual);

            Assert.AreEqual(expectedUri.AbsolutePath, actualUri.AbsolutePath);
            CollectionAssert.AreEquivalent(HttpUtility.ParseQueryString(expectedUri.Query), HttpUtility.ParseQueryString(actualUri.Query), $"Expected: <{expectedUri.Query}>. Actual: <{actualUri.Query}>");
        }

        private static readonly IReadOnlyDictionary<Uri, object> EXPECTED_VALUES = new Dictionary<Uri, object>
        {
            #region Movie
            [new UniformItemIdentifier(Constants.Movie, Media.BACKDROP_PATH)] = "/original/n5A7brJCjejceZmHyujwUTVgQNC.jpg",
            [new UniformItemIdentifier(Constants.Movie, Movie.BUDGET)] = 125000000L,
            [new UniformItemIdentifier(Constants.Movie, Media.CAST)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(102, actual.Count());
                Assert.AreEqual("Daniel Radcliffe", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.Movie, Movie.CONTENT_RATING)] = "PG-13",
            [new UniformItemIdentifier(Constants.Movie, Media.CREW)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(143, actual.Count());
                Assert.AreEqual("Fiona Weir", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.Movie, Media.DESCRIPTION)] = "Harry, Ron and Hermione continue their quest to vanquish the evil Voldemort once and for all. Just as things begin to look hopeless for the young wizards, Harry discovers a trio of magical objects that endow him with powers to rival Voldemort's formidable skills.",
            [EXTERNAL_IDS_MOVIE_URI] = Encoding.UTF8.GetBytes(" {\r\n    \"imdb_id\": \"tt1201607\",\r\n    \"wikidata_id\": \"Q232009\",\r\n    \"facebook_id\": \"harrypottermovie\",\r\n    \"instagram_id\": \"harrypotterfilm\",\r\n    \"twitter_id\": \"HarryPotterFilm\"\r\n  }"),
            [new UniformItemIdentifier(Constants.Movie, Movie.GENRES)] = (IEnumerable<Genre> actual) =>
            {
                Assert.AreEqual(2, actual.Count());
                Assert.AreEqual("Fantasy", actual.First().Name);
            },
            [new UniformItemIdentifier(Constants.Movie, Media.KEYWORDS)] = (IEnumerable<Keyword> actual) =>
            {
                Assert.AreEqual(15, actual.Count());
                Assert.AreEqual(new Keyword { Id = 616, Name = "witch" }, actual.First());
            },
            [new UniformItemIdentifier(Constants.Movie, Media.LANGUAGES)] = new Language[] { new Language("en") },
            [new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE)] = new Language("en"),
            [new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_TITLE)] = "Harry Potter and the Deathly Hallows: Part 2",
            [new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION)] = (Collection actual) =>
            {
                Assert.AreEqual(new Collection().WithID(TMDB.IDKey, 1241), actual);
                var itr = actual.GetAsyncEnumerator();
                Assert.IsTrue(itr.MoveNextAsync().Result);
                Assert.AreEqual("Harry Potter and the Philosopher's Stone", itr.Current.Name);
            },
            [new UniformItemIdentifier(Constants.Movie, Media.POSTER_PATH)] = "/w342/c54HpQmuwXjHq2C9wmoACjxoom3.jpg",
            [new UniformItemIdentifier(Constants.Movie, Media.PRODUCTION_COMPANIES)] = (IEnumerable<Company> actual) =>
            {
                Assert.AreEqual(2, actual.Count());
                Assert.AreEqual("Warner Bros. Pictures", actual.First().Name);
            },
            [new UniformItemIdentifier(Constants.Movie, Media.PRODUCTION_COUNTRIES)] = new List<string> { "United Kingdom", "United States of America" },
            [new UniformItemIdentifier(Constants.Movie, Media.RATING)] = (Rating actual) =>
            {
                Assert.AreEqual("TMDb", actual.Company.Name);
                Assert.AreEqual("81%", actual.Score);
            },
            [new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED)] = (IAsyncEnumerable<Item> actual) =>
            {
                var itr = actual.GetAsyncEnumerator();
                var items = Enumerable.Range(0, 20).Select(_ => { Assert.IsTrue(itr.MoveNextAsync().Result); return itr.Current; }).ToArray();
                IsExpectedMovieRecommended(items);
            },
            [new UniformItemIdentifier(Constants.Movie, Movie.RELEASE_DATE)] = new DateTime(2011, 7, 12),
            [new UniformItemIdentifier(Constants.Movie, Movie.REVENUE)] = 1341511219L,
            [new UniformItemIdentifier(Constants.Movie, Media.RUNTIME)] = new TimeSpan(2, 10, 0),
            [new UniformItemIdentifier(Constants.Movie, Media.TAGLINE)] = "It all ends.",
            [new UniformItemIdentifier(Constants.Movie, Media.TITLE)] = "Harry Potter and the Deathly Hallows: Part 2",
            [new UniformItemIdentifier(Constants.Movie, Media.TRAILER_PATH)] = "https://www.youtube.com/embed/5NYt1qirBWg",
            [new UniformItemIdentifier(Constants.Movie, Movie.WATCH_PROVIDERS)] = (IEnumerable<WatchProvider> actual) =>
            {
                Assert.AreEqual(19, actual.Count());
                Assert.AreEqual("fuboTV", actual.First().Company.Name);
            },
            #endregion

            #region TVShow
            [new UniformItemIdentifier(Constants.TVShow, Media.BACKDROP_PATH)] = "/original/vNpuAxGTl9HsUbHqam3E9CzqCvX.jpg",
            [new UniformItemIdentifier(Constants.TVShow, Media.CAST)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(355, actual.Count());
                Assert.AreEqual("Rainn Wilson", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.TVShow, TVShow.CONTENT_RATING)] = "TV-14",
            [new UniformItemIdentifier(Constants.TVShow, Media.CREW)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(135, actual.Count());
                Assert.AreEqual("Steve Rostine", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.TVShow, Media.DESCRIPTION)] = "The everyday lives of office employees in the Scranton, Pennsylvania branch of the fictional Dunder Mifflin Paper Company.",
            [new UniformItemIdentifier(Constants.TVShow, TVShow.FIRST_AIR_DATE)] = new DateTime(2005, 3, 24),
            [new UniformItemIdentifier(Constants.TVShow, TVShow.GENRES)] = new[] { new Genre { Id = 35, Name = "Comedy" } },
            [new UniformItemIdentifier(Constants.TVShow, Media.KEYWORDS)] = (IEnumerable<Keyword> actual) =>
            {
                Assert.AreEqual(9, actual.Count());
                Assert.AreEqual(new Keyword { Id = 3640, Name = "work" }, actual.First());
            },
            [new UniformItemIdentifier(Constants.TVShow, Media.LANGUAGES)] = new[] { new Language("en") },
            [new UniformItemIdentifier(Constants.TVShow, TVShow.LAST_AIR_DATE)] = new DateTime(2013, 5, 16),
            [new UniformItemIdentifier(Constants.TVShow, TVShow.NETWORKS)] = (IEnumerable<Company> actual) =>
            {
                Assert.AreEqual(1, actual.Count());
                Assert.AreEqual("NBC", actual.First().Name);
            },
            [new UniformItemIdentifier(Constants.TVShow, Media.ORIGINAL_LANGUAGE)] = new Language("en"),
            [new UniformItemIdentifier(Constants.TVShow, Media.ORIGINAL_TITLE)] = "The Office",
            [new UniformItemIdentifier(Constants.TVShow, Media.POSTER_PATH)] = "/w342/qWnJzyZhyy74gjpSjIXWmuk0ifX.jpg",
            [new UniformItemIdentifier(Constants.TVShow, Media.PRODUCTION_COMPANIES)] = new Company[] { },
            [new UniformItemIdentifier(Constants.TVShow, Media.PRODUCTION_COUNTRIES)] = new List<string> { "United States of America" },
            [new UniformItemIdentifier(Constants.TVShow, Media.RATING)] = (Rating actual) =>
            {
                Assert.AreEqual("TMDb", actual.Company.Name);
                Assert.AreEqual("86%", actual.Score);
            },
            [new UniformItemIdentifier(Constants.TVShow, Media.RECOMMENDED)] = (IAsyncEnumerable<Item> actual) =>
            {
                var itr = actual.GetAsyncEnumerator();
                var items = Enumerable.Range(0, 21).Select(_ => { Assert.IsTrue(itr.MoveNextAsync().Result); return itr.Current; }).ToArray();
                IsExpectedTVShowRecommended(items);
            },
            [new UniformItemIdentifier(Constants.TVShow, Media.RUNTIME)] = new TimeSpan(0, 22, 0),
            [new UniformItemIdentifier(Constants.TVShow, TVShow.SEASONS)] = (IEnumerable<TVSeason> actual) =>
            {
                Assert.AreEqual(10, actual.Count());
                Assert.AreEqual("Season 1", actual.ElementAt(1).Name);
            },
            [new UniformItemIdentifier(Constants.TVShow, Media.TAGLINE)] = "",
            [new UniformItemIdentifier(Constants.TVShow, Media.TITLE)] = "The Office",
            [new UniformItemIdentifier(Constants.TVShow, Media.TRAILER_PATH)] = "https://www.youtube.com/embed/LHOtME2DL4g",
            [new UniformItemIdentifier(Constants.TVShow, TVShow.WATCH_PROVIDERS)] = (IEnumerable<WatchProvider> actual) =>
            {
                Assert.AreEqual(11, actual.Count());
                Assert.AreEqual("Peacock", actual.First().Company.Name);
            },
            #endregion

            #region TVSeason
            [new UniformItemIdentifier(Constants.TVSeason, TVSeason.CAST)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(53, actual.Count());
                Assert.AreEqual("Steve Carell", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.TVSeason, TVSeason.CREW)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(51, actual.Count());
                Assert.AreEqual("Randall Einhorn", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.TVSeason, TVSeason.EPISODES)] = (IEnumerable<TVEpisode> actual) =>
            {
                Assert.AreEqual(23, actual.Count());
                Assert.AreEqual("Gay Witch Hunt", actual.First().Name);
            },
            [new UniformItemIdentifier(Constants.TVSeason, TVSeason.YEAR)] = new DateTime(2006, 9, 21),
            #endregion

            #region TVEpisode
            [new UniformItemIdentifier(Constants.TVEpisode, TVEpisode.AIR_DATE)] = new DateTime(2007, 4, 26),
            [new UniformItemIdentifier(Constants.TVEpisode, Media.CAST)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(19, actual.Count());
                Assert.AreEqual("Steve Carell", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.TVEpisode, Media.CREW)] = (IEnumerable<Credit> actual) =>
            {
                Assert.AreEqual(3, actual.Count());
                Assert.AreEqual("Randall Einhorn", actual.First().Person.Name);
            },
            [new UniformItemIdentifier(Constants.TVEpisode, Media.DESCRIPTION)] = "Michael was in crisis mode at Dunder-Mifflin when a disgruntled employee at the paper mill put an obscene watermark on one of their most popular orders of paper. Michael called the staff into a meeting and pointed the blame on Creed, who was responsible for quality assurance at the paper mill and blew it off.",
            [new UniformItemIdentifier(Constants.TVEpisode, Media.POSTER_PATH)] = "/original/iHvrE0L2w033SpwC56I7SLhoQVm.jpg",
            [new UniformItemIdentifier(Constants.TVEpisode, Media.RATING)] = (Rating actual) =>
            {
                Assert.AreEqual("TMDb", actual.Company.Name);
                Assert.AreEqual("83%", actual.Score);
            },
            [new UniformItemIdentifier(Constants.TVEpisode, Media.RUNTIME)] = new TimeSpan(0, 22, 0),
            [new UniformItemIdentifier(Constants.TVEpisode, Media.TITLE)] = "Product Recall",
            #endregion

            #region Person
            [new UniformItemIdentifier(Constants.Person, Person.ALSO_KNOWN_AS)] = (IEnumerable<string> actual) =>
            {
                Assert.AreEqual(15, actual.Count());
                Assert.AreEqual("Jessica Howard", actual.First());
            },
            [new UniformItemIdentifier(Constants.Person, Person.BIO)] = "Jessica Michelle Chastain (born March 24, 1977) is an American actress and film producer. Known for her roles in films with feminist themes, she has received various accolades, including an Academy Award, a Golden Globe Award, in addition to nominations for two British Academy Film Awards. Time magazine named her one of the 100 most influential people in the world in 2012.\n\nBorn and raised in Sacramento, California, Chastain developed an interest in acting from an early age. In 1998, she made her professional stage debut as Shakespeare's Juliet. After studying acting at the Juilliard School, she was signed to a talent holding deal with the television producer John Wells. She was a recurring guest star in several television series, including Law & Order: Trial by Jury. She also took on roles in the stage productions of Anton Chekhov's play The Cherry Orchard in 2004 and Oscar Wilde's tragedy Salome in 2006.\n\nChastain made her film debut in the drama Jolene (2008), and gained wide recognition for her starring roles in the dramas Take Shelter (2011) and The Tree of Life (2011). Her performance as an aspiring socialite in The Help (2011) earned her a nomination for the Academy Award for Best Supporting Actress. In 2012, she won a Golden Globe Award and received a nomination for the Academy Award for Best Actress for playing a CIA analyst in the thriller Zero Dark Thirty. Chastain made her Broadway debut in a revival of The Heiress in the same year. Her highest-grossing releases came with the science fiction films Interstellar (2014) and The Martian (2015), and the horror film It Chapter Two (2019), and she continued to receive critical acclaim for her performances in the dramas A Most Violent Year (2014), Miss Sloane (2016), Molly's Game (2017). For her portrayal of Tammy Faye in the biopic The Eyes of Tammy Faye (2021), which she also produced, Chastain won the Academy Award for Best Actress.\n\nChastain is the founder of the production company Freckle Films, which was created to promote diversity in film. She is vocal about mental health issues, as well as gender and racial equality. She is married to fashion executive Gian Luca Passi de Preposulo, with whom she has two children.\n\nDescription above from the Wikipedia article Jessica Chastain, licensed under CC-BY-SA, full list of contributors on Wikipedia.",
            [new UniformItemIdentifier(Constants.Person, Person.BIRTHDAY)] = new DateTime(1977, 3, 24),
            [new UniformItemIdentifier(Constants.Person, Person.BIRTHPLACE)] = "Southern California, California, USA",
            [new UniformItemIdentifier(Constants.Person, Person.CREDITS)] = (IEnumerable<Item> actual) =>
            {
                Assert.AreEqual(106, actual.Count());
                Assert.AreEqual("Jolene", actual.First().Name);
            },
            [new UniformItemIdentifier(Constants.Person, Person.DEATHDAY)] = null!,
            [new UniformItemIdentifier(Constants.Person, Person.GENDER)] = "",
            [new UniformItemIdentifier(Constants.Person, Person.NAME)] = "Jessica Chastain",
            [new UniformItemIdentifier(Constants.Person, Person.PROFILE_PATH)] = "/original/eQKnihReJeB9vQEa5gySzAlKfZt.jpg",
            #endregion
        };

        public static void Success(KeyValueRequestArgs<Uri> request) => Success(request, string.Empty, null);
        public static void Success(KeyValueRequestArgs<Uri> request, string? message, params object?[]? parameters)
        {
            Assert.IsTrue(request.IsHandled, message, parameters);

            if (EXPECTED_VALUES.TryGetValue(request.Request.Key, out var expected) || EXPECTED_VALUES.TryGetValue(new Uri(request.Request.Key.ToString().Replace("?language=en-US", "").Replace("&region=US", ""), UriKind.RelativeOrAbsolute), out expected))
            {
                if (expected is Delegate del && expected.GetType().GenericTypeArguments.Length == 1)
                {
                    if (request.Value != null)
                    {
                        var expectedType = expected.GetType().GenericTypeArguments[0];
                        Assert.IsInstanceOfType(request.Value, expectedType, message, parameters);
                    }

                    del.DynamicInvoke(request.Value);
                }
                else if (expected is IEnumerable expectedEnumerable && false == expected is string)
                {
                    Assert.IsInstanceOfType<IEnumerable>(request.Value, message, parameters);
                    CollectionAssert.AreEqual(expectedEnumerable.OfType<object>().ToList(), ((IEnumerable)request.Value).OfType<object>().ToList(), message, parameters);
                }
                else
                {
                    Assert.AreEqual(expected, request.Value, message, parameters);
                }
            }
            else
            {
                Assert.Inconclusive($"Expected value not known. " + message, parameters);
            }
        }

        public static void IsExpectedMovieRecommended(IEnumerable<Item> actual)
        {
            Assert.AreEqual(20, actual.Count());
            Assert.AreEqual("Harry Potter and the Deathly Hallows: Part 1", actual.First().Name);
        }

        public static void IsExpectedTVShowRecommended(IEnumerable<Item> actual)
        {
            Assert.AreEqual(21, actual.Count());
            Assert.AreEqual("Parks and Recreation", actual.First().Name);
        }

        protected void AssertByteRepresentation(RestResponse response, string str)
        {
            Assert.IsTrue(response.TryGetRepresentation<IEnumerable<byte>>(out var bytes));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(str), bytes.ToArray(), $"\n{str}\n{Encoding.UTF8.GetString(bytes.ToArray())}");
        }

        protected void AssertRepresentation<T>(RestResponse response, T expected)
        {
            Assert.IsTrue(response.TryGetRepresentation<T>(out var value), $"Could not get representation of type {typeof(T)}");
            if (expected is ICollection collection)
            {
                ICollection? actual = value is IEnumerable enumerable ? new List<object>(enumerable.OfType<object>()) : new List<object>();
                CollectionAssert.AreEquivalent(collection, actual, $"\n{string.Join(", ", collection.OfType<object>())}\n{string.Join(", ", actual.OfType<object>())}");
            }
            else
            {
                Assert.AreEqual(expected, value);
            }
        }

        public static void InMemoryCacheCount(UiiDictionaryDataStore cache, int expected) => CacheCount(cache.Keys, expected, null);
        public static void InMemoryCacheCount(UiiDictionaryDataStore cache, Item item)
        {
            var expectedResources = ExpectedResources(item);
            if (item.TryGetID(TMDB.ID, out var id))
            {
                if (item is Movie)
                {
                    expectedResources.Add(new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
                else if (item is TVShow)
                {
                    expectedResources.Add(new Uri(string.Format(API.TV.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
            }

            CacheCount(cache.Keys, expectedResources.Count, expectedResources);
        }

        public static void DiskCacheCount(DummyDatastore<Uri> cache, int expected) => CacheCount(cache.Keys, expected, null);
        public static void DiskCacheCount(DummyDatastore<Uri> cache, Item item)
        {
            var expectedResources = ExpectedResources(item);
            if (item.TryGetID(TMDB.ID, out var id))
            {
                if (item is Movie)
                {
                    expectedResources.Add(new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
                else if (item is TVShow)
                {
                    //uris.Add(new Uri(string.Format(API.TV.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
            }

            CacheCount(cache.Keys, expectedResources.Count, expectedResources);
        }
        private static void CacheCount(ICollection<Uri> cache, int expectedCount, ICollection<Uri> expectedUris = null)
        {
            Assert.AreEqual(expectedCount, cache.Count, expectedUris == null ? "" : $"\n{ReportAsymmetricSetDiff(cache, expectedUris)}");
        }

        private static string ReportAsymmetricSetDiff<T>(ICollection<T> collection1, ICollection<T> collection2)
        {
            var set1 = new HashSet<T>(collection1);
            set1.ExceptWith(collection2);

            var set2 = new HashSet<T>(collection2);
            set2.ExceptWith(collection1);

            var report = new List<string>();
            if (set2.Count > 0)
            {
                report.Add(string.Join("\n\t\t", set2.Select(uri => uri.ToString()).Prepend("\tMissing:")));
            }
            if (set1.Count > 0)
            {
                report.Add(string.Join("\n\t\t", set1.Select(uri => uri.ToString()).Prepend("\tExtra:")));
            }

            return string.Join("\n", report);
        }

        public static int ExpectedMovieResourceCount => GetExpectedResourceCount(ItemType.Movie);
        public static int ExpectedTVResourceCount => GetExpectedResourceCount(ItemType.TVShow);

        private static IDictionary<ItemType, int> ExpectedResourceCounts = new Dictionary<ItemType, int>();
        private static int GetExpectedResourceCount(ItemType type) => ComputeIfAbsent(ExpectedResourceCounts, type, key => ComputeExpectedResourceCount(key));
        private static TValue ComputeIfAbsent<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> compute)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return dictionary[key] = compute(key);
        }

        public static ISet<Uri> ExpectedResources(Item item)
        {
            if (!TMDB.ITEM_PROPERTIES.TryGetValue(item.ItemType, out var properties) || !item.TryGetID(TMDB.ID, out var id))
            {
                return new HashSet<Uri>();
            }

            return properties.PropertyLookup.Keys.Select(property => new UniformItemIdentifier(item, property)).ToHashSet<Uri>();
        }

        private static int ComputeExpectedResourceCount(ItemType type)
        {
            if (!TMDB.ITEM_PROPERTIES.TryGetValue(type, out var properties))
            {
                return 0;
            }

            int count = properties.PropertyLookup.Count;
            if (type == ItemType.Movie)
            {
                // external ids endpoint
                count++;
            }
            else if (type == ItemType.TVShow)
            {

            }

            return count;
        }
    }
}
