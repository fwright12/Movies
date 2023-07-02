using Microsoft.VisualStudio.TestTools.UnitTesting;
using Movies;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class ResourceTests : Resources
    {
        private readonly TMDbResolver Resolver;

        private UiiDictionaryDatastore InMemoryCache { get; }
        private DummyDatastore<IEnumerable<byte>> DiskCache { get; }

        private TMDbLocalHandlers LocalTMDbHandlers { get; }
        private TMDbReadHandler RemoteTMDbHandlers { get; }

        private ChainLink<MultiRestEventArgs> Chain;

        public ResourceTests()
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            DiskCache = new DummyDatastore<IEnumerable<byte>>();
            InMemoryCache = new UiiDictionaryDatastore();

            LocalTMDbHandlers = new TMDbLocalHandlers(new LocalTMDbDatastore(DiskCache, Resolver)
            {
                ChangeKeys = TestsConfig.ChangeKeys
            }, Resolver);
            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(new MockHandler())));
            RemoteTMDbHandlers = new TMDbReadHandler(invoker, Resolver, TMDbApi.AutoAppend);
        }

        [TestInitialize]
        public void Reset()
        {
            DebugConfig.SimulatedDelay = 100;
            DiskCache.SimulatedDelay = 50;

            WebHistory.Clear();
            InMemoryCache.Clear();
            DiskCache.Clear();
            TMDB.CollectionCache.Clear();

            Chain = new CacheAsideLink(InMemoryCache);
            Chain.SetNext(new CacheAsideLink(LocalTMDbHandlers))
                .SetNext(new ChainLinkAsync<MultiRestEventArgs>(RemoteTMDbHandlers.HandleGet));
        }

        //[TestMethod]
        public async Task CheckPerformance()
        {
            DataService.Instance.Controller.SetNext(Chain.Next);
            DebugConfig.SimulatedDelay = 0;
            DiskCache.SimulatedDelay = 0;
            var list = Enumerable.Range(0, 1000)
                .Select(i => new Movie(i.ToString()).WithID(TMDB.IDKey, i))
                .Select(movie => new MovieViewModel(movie))
                .ToArray();

            var source = new TaskCompletionSource();
            int count = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            System.ComponentModel.PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (++count >= list.Length)
                {
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed;

                    source.SetResult();
                }
            };

            foreach (var movie in list)
            {
                var providers = movie.WatchProviders;

                if (providers == null)
                {
                    movie.PropertyChanged += handler;
                }
                else
                {
                    handler(null, null);
                }
                //var providers = await Chain.Get<IEnumerable<WatchProvider>>(new UniformItemIdentifier(movie, Movie.WATCH_PROVIDERS));
            }

            await source.Task;
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromInMemoryCache()
        {
            await InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), State.Create(Constants.TAGLINE));
            await InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), State.Create(Constants.LANGUAGE));

            // Retrieve an item in the in memory cache
            var uii = new UniformItemIdentifier(Constants.Movie, Media.TAGLINE);
            var response = await Chain.TryGet<string>(uii);
            Assert.IsTrue(response.Success);
            Assert.AreEqual(Constants.TAGLINE, response.Resource);

            Assert.AreEqual(0, WebHistory.Count);
            Assert.AreEqual(0, DiskCache.Count);
            Assert.AreEqual(2, InMemoryCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromDiskCache()
        {
            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_PARTIAL_RESPONSE;// "{ \"runtime\": 130 }";
            await DiskCache.CreateAsync(uri, State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json)));

            var request = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), typeof(TimeSpan?));
            await Chain.Get(request);

            Assert.IsTrue(request.Handled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Response?.TryGetRepresentation<TimeSpan?>(out var value) == true ? value : null);

            Assert.AreEqual(0, WebHistory.Count);
            Assert.AreEqual(1, DiskCache.Count);
            Assert.AreEqual(1, InMemoryCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromApi()
        {
            var uii = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var response = await Chain.TryGet<TimeSpan>(uii);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response.Resource);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,keywords,recommendations,release_dates,videos,watch/providers", WebHistory.Last());

            await CachedAsync();

            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual(6, DiskCache.Count);
            //Assert.AreEqual(ItemProperties[ItemType.Movie].Count + 7, ResourceCache.Count);
            // There are a total of 25 properties, but a movie's parent collection will not be parsed
            // (because it requires another API call) and therefore will not cached
            Assert.AreEqual(1, InMemoryCache.Count);
        }

        [TestMethod]
        public async Task RetrieveMovieParentCollection()
        {
            var request = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION), Movie.PARENT_COLLECTION.FullType);
            await Chain.Get(request);

            Assert.IsTrue(request.Handled);
            Assert.AreEqual(new Collection().WithID(TMDB.IDKey, 1241), request.Response.TryGetRepresentation<Collection>(out var value) ? value : null);

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,keywords,recommendations,release_dates,videos,watch/providers", WebHistory[0]);
            Assert.AreEqual("3/collection/1241?language=en-US", WebHistory[1]);
        }

        [TestMethod]
        public async Task RetrieveRecommendedMovies()
        {
            var uri = new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED);
            var e = await Chain.Get<IAsyncEnumerable<Item>>(uri);

            Assert.IsTrue(e.Handled);
            Assert.IsTrue(e.Response.TryGetRepresentation<IAsyncEnumerable<Item>>(out var value));

            int count = 0;
            try { await foreach (var item in value) { count++; } } catch { }

            Assert.AreEqual(21, count);
            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=recommendations,credits,keywords,release_dates,videos,watch/providers", WebHistory[0]);
            Assert.AreEqual($"3/movie/0/recommendations?language=en-US&page=2", WebHistory[1]);
        }

        [TestMethod]
        public async Task InMemoryCachedAsTasks()
        {
            DebugConfig.SimulatedDelay = 0;
            DiskCache.SimulatedDelay = 50;

            var arg1 = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            var arg2 = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            // Should take 50 ms to read (and fail) from disk, then another 50 ms to write
            // Send the second request while (hopefully) still writing to disk, but after response
            // cleared from the http handler cache
            var first = Chain.Get(arg1);
            await Task.Delay(40);
            var second = Chain.Get(arg2);
            await Task.WhenAll(first, second);

            Assert.AreEqual(1, WebHistory.Count);
        }

        // Calls to the api will have a delay
        // If the in memory cache doesn't cache tasks, successive calls will go through because they don't
        // return a result immediately
        [TestMethod]
        public async Task HttpRequestsAreCached()
        {
            DebugConfig.SimulatedDelay = 100;
            DiskCache.SimulatedDelay = 50;

            var arg1 = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            var arg2 = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            await Task.WhenAll(Chain.Get(arg1), Chain.Get(arg2));

            Assert.AreEqual(1, WebHistory.Count);
        }

        // Calls to the api will have a delay
        // If the in memory cache doesn't cache tasks, successive calls will go through because they don't
        // return a result immediately
        [TestMethod]
        public async Task HttpRequestsAreCacheddsf()
        {
            DebugConfig.SimulatedDelay = 0;
            DiskCache.SimulatedDelay = 0;

            var arg1 = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, TVShow.SEASONS));
            var arg2 = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            await Task.WhenAll(Chain.Get(arg1), Chain.Get(arg2));

            Assert.AreEqual(1, WebHistory.Count);
        }

        [TestMethod]
        public async Task RequestsAreBatched()
        {
            RestRequestArgs[] getAllRequests() => GetAllRequests(AllMovieProperties().Except(new Property[] { Movie.PARENT_COLLECTION }));

            var requests = getAllRequests();
            await Task.WhenAll(Chain.Get(requests));
            await CachedAsync();
            Assert.AreEqual(1, WebHistory.Count);

            // No additional api calls should be made, everything should be cached in memory
            await Task.WhenAll(Chain.Get(getAllRequests()));
            await CachedAsync();
            Assert.AreEqual(1, WebHistory.Count);

            // These properties should result in another api call because they don't cache
            foreach (var property in new Property[]
            {
                Media.ORIGINAL_LANGUAGE,
                Media.RECOMMENDED,
                //Movie.PARENT_COLLECTION,
            })
            {
                WebHistory.Clear();

                await InMemoryCache.DeleteAsync(new UniformItemIdentifier(Constants.Movie, property));
                await Chain.Get(requests = getAllRequests());
                await CachedAsync();
                Assert.AreEqual(1, WebHistory.Count);
            }
        }

        [TestMethod]
        public async Task GetAllResources()
        {
            await InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), State.Create(new TimeSpan(2, 10, 0)));

            var requests = GetAllRequests();
            await Task.WhenAll(Chain.Get(requests));

            var runtime = requests.Where(request => (request.Uri as UniformItemIdentifier)?.Property == Media.RUNTIME).FirstOrDefault();
            var watchProviders = requests.Where(request => (request.Uri as UniformItemIdentifier)?.Property == Movie.WATCH_PROVIDERS).FirstOrDefault();

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual(new TimeSpan(2, 10, 0), runtime.Response?.TryGetRepresentation<TimeSpan?>(out var value) == true ? value : null);
            Assert.AreEqual("Apple iTunes", watchProviders.Response?.TryGetRepresentation<IEnumerable<WatchProvider>>(out var value1) == true ? value1.FirstOrDefault()?.Company.Name : null);
        }

        private Task CachedAsync()
        {
            return Task.Delay((DiskCache.SimulatedDelay + DebugConfig.SimulatedDelay) * 2);
        }

        private Property[] AllMovieProperties() => GetProperties(typeof(Media)).Concat(GetProperties(typeof(Movie))).Append(TMDB.POPULARITY).ToArray();

        private RestRequestArgs[] GetAllRequests() => GetAllRequests(AllMovieProperties());
        private RestRequestArgs[] GetAllRequests(IEnumerable<Property> properties) => properties
                        .Select(property => new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, property), property.FullType))
                        .ToArray();

        private static readonly IReadOnlySet<Property> NO_CHANGE_KEY = new HashSet<Property>
        {
            Media.ORIGINAL_LANGUAGE,
            Media.RECOMMENDED,
            Movie.PARENT_COLLECTION,
            TVShow.SEASONS, // There is a change key for this but I'm not tracking it at this point
            TVShow.NETWORKS,
            Person.GENDER,
            Person.CREDITS,
            TMDB.POPULARITY
        };

        [TestMethod]
        public async Task AllTMDbResourcesAutomated()
        {
            //var request = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), typeof(string));
            //await Controller.Get(request);
            //request = new RestRequestArgs(new UniformItemIdentifier(Constants.TVShow, TVShow.GENRES), typeof(IEnumerable<Genre>));
            //await Controller.Get(request);

            DebugConfig.SimulatedDelay = 0;
            DiskCache.SimulatedDelay = 0;

            await AllResources(Constants.Movie, typeof(Media), new Dictionary<Property, object>
            {
                [Media.TITLE] = "Harry Potter and the Deathly Hallows: Part 2",
                [Media.RUNTIME] = new TimeSpan(2, 10, 0),
                [Media.PRODUCTION_COUNTRIES] = "United Kingdom",
                [Media.KEYWORDS] = new Keyword { Id = 83, Name = "saving the world" },
            });
            await AllResources(Constants.Movie, new Dictionary<Property, object>
            {
                [Movie.BUDGET] = 125000000L,
                [Movie.CONTENT_RATING] = "PG-13",
                [Movie.PARENT_COLLECTION] = new Collection().WithID(TMDB.IDKey, 1241)
            });
            await AllResources(Constants.TVShow, new Dictionary<Property, object>
            {
                [TVShow.GENRES] = new Genre { Id = 35, Name = "Comedy" },
                [TVShow.CONTENT_RATING] = "TV-14",
            });
            await AllResources(Constants.TVSeason, new Dictionary<Property, object>
            {
                [TVSeason.YEAR] = new DateTime(2006, 9, 21)
            });
            await AllResources(Constants.TVEpisode, new Dictionary<Property, object>
            {
                [TVEpisode.AIR_DATE] = new DateTime(2007, 4, 26)
            });
            await AllResources(Constants.Person, new Dictionary<Property, object>
            {
                [Person.ALSO_KNOWN_AS] = "Jessica Howard",
            });
        }

        private static IEnumerable<Property?> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));

        private Task AllResources(Item item, IDictionary<Property, object> expectedValues = null) => AllResources(item, item.GetType(), expectedValues);
        private async Task AllResources(Item item, Type type, IDictionary<Property, object> expectedValues = null)
        {
            var properties = GetProperties(type);

            foreach (var test in new (ChainLink<MultiRestEventArgs> Controller, IEnumerable<Property?> Properties)[]
            {
                (Chain, properties),
                //new Controller().AddLast(ResourceCache),
                (new ChainLinkAsync<MultiRestEventArgs>(LocalTMDbHandlers.HandleGet), type == typeof(TVSeason) || type == typeof(TVEpisode) ? Enumerable.Empty<Property>() : properties.Except(NO_CHANGE_KEY))
            })
            {
                foreach (var property in test.Properties)
                {
                    var uii = new UniformItemIdentifier(item, property);
                    var arg = new RestRequestArgs(uii, property.FullType);

                    await test.Controller.Get(arg);

                    Assert.IsTrue(arg.Handled, $"Could not get value for property {property} of type {type}");
                    Assert.IsTrue(arg.Response.TryGetRepresentation(property.FullType, out var value), $"Property {property}. Expected type: {property.FullType}. Available types: {string.Join(", ", arg.Response.OfType<object>().Select(rep => rep.GetType()))}");

                    if (expectedValues?.TryGetValue(property, out var expected) == true)
                    {
                        if (property.AllowsMultiple && !expected.GetType().IsAssignableFrom(property.FullType))
                        {
                            var values = value as IEnumerable;
                            Assert.IsNotNull(values);
                            Assert.AreEqual(expected, values.OfType<object>().First());
                        }
                        else
                        {
                            Assert.AreEqual(expected, value);
                        }
                    }
                }
            }
        }
    }
}
