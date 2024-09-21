using System.Collections;
using System.Text;
using System.Web;

namespace MoviesTests.Data
{
    internal class ResourceAssert
    {
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
            [new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION)] = new Collection().WithID(TMDB.IDKey, 1241),
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
                Assert.IsTrue(itr.MoveNextAsync().Result);
                Assert.AreEqual("Harry Potter and the Deathly Hallows: Part 1", itr.Current.Name);
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
        };

        public static void Success(KeyValueRequestArgs<Uri> request) => Success(request, string.Empty, null);
        public static void Success(KeyValueRequestArgs<Uri> request, string? message, params object?[]? parameters)
        {
            Assert.IsTrue(request.IsHandled, message, parameters);

            if (EXPECTED_VALUES.TryGetValue(request.Request.Key, out var expected))
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
