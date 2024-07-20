using Movies.Data.Local;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data
{
    [TestClass]
    public class ResourceTests : Resources
    {
        private const string MOVIE_APPENDED_URL = $"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";
        private const string TV_APPENDED_URL = $"3/tv/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=aggregate_credits,content_ratings,external_ids,keywords,recommendations,videos,watch/providers";

        [TestInitialize]
        public void Reset()
        {
            DebugConfig.SimulatedDelay = 100;
            //DiskCache.SimulatedDelay = 50;

            WebHistory.Clear();
            //InMemoryCache.Clear();
            //DiskCache.Clear();
            TMDB.CollectionCache.Clear();
        }

        //[TestMethod]
        public async Task RunInBulk()
        {
            int count = 100;

            for (int i = 0; i < count; i++)
            {
                await ResourcesCanBeRetrievedFromDiskCache();
            }
        }

        [TestMethod]
        public async Task RetrieveRuntime()
        {
            await AssertMovieCachedAtAllLayers(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), new TimeSpan(2, 10, 0));
        }

        [TestMethod]
        public async Task RetrieveLastAirDate()
        {
            await AssertTVCachedAtAllLayers<DateTime?>(new UniformItemIdentifier(Constants.TVShow, TVShow.LAST_AIR_DATE), new DateTime(2013, 5, 16));
        }

        private Task AssertMovieCachedAtAllLayers<T>(Uri uri, T expected) => AssertCachedAtAllLayers(uri, expected, MOVIE_APPENDED_URL, ResourceAssert.ExpectedMovieResourceCount);
        private Task AssertTVCachedAtAllLayers<T>(Uri uri, T expected) => AssertCachedAtAllLayers(uri, expected, TV_APPENDED_URL, ResourceAssert.ExpectedTVResourceCount);
        private async Task AssertCachedAtAllLayers<T>(Uri uri, T expected, string apiEndpoint, int expectedResourceCount)
        {
            var handlers = new HandlerChain();

            var arg = new KeyValueRequestArgs<Uri, T>(uri);
            await handlers.Chain.Get(arg);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(arg.IsHandled);
            Assert.AreEqual(expected, arg.Value);
            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(apiEndpoint, handlers.WebHistory[0]);

            arg = new KeyValueRequestArgs<Uri, T>(uri);
            await handlers.PersistenceService.Processor.ProcessAsync(arg.AsEnumerable(), null);

            Assert.IsTrue(arg.IsHandled);
            Assert.AreEqual(expected, arg.Value);

            arg = new KeyValueRequestArgs<Uri, T>(uri);
            await handlers.InMemoryService.Cache.ProcessAsync(arg.AsEnumerable(), null);

            Assert.IsTrue(arg.IsHandled);
            Assert.AreEqual(expected, arg.Value);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromInMemoryCache()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            await handlers.InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), State.Create(Constants.TAGLINE));
            await handlers.InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), State.Create(Constants.LANGUAGE));

            // Retrieve an item in the in memory cache
            var uii = new UniformItemIdentifier(Constants.Movie, Media.TAGLINE);
            var response = await chain.TryGet<string>(uii);
            Assert.IsTrue(response.IsHandled);
            Assert.AreEqual(Constants.TAGLINE, response.Value);

            Assert.AreEqual(0, handlers.MockHttpHandler.LocalCallHistory.Count);
            Assert.AreEqual(0, handlers.DiskCache.Count);
            Assert.AreEqual(2, handlers.InMemoryCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromDiskCache()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = new Uri("urn:Movie:0:Runtime", UriKind.RelativeOrAbsolute);
            handlers.DiskCache.TryAdd(uri, new ResourceResponse<IEnumerable<byte>>(Encoding.UTF8.GetBytes("130")));
            Assert.AreEqual(1, handlers.DiskCache.Count);

            var request = new KeyValueRequestArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Value);

            Assert.AreEqual(0, handlers.WebHistory.Count);
            Assert.AreEqual(1, handlers.DiskCache.Count);
            Assert.AreEqual(1, handlers.InMemoryCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromApi()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var args = new KeyValueRequestArgs<Uri, TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(args);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(args.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), args.Value);

            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory.Last());
            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(26, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
            Assert.AreEqual(26, handlers.InMemoryCache.Count);
        }

        //[TestMethod]
        public async Task GetExternalIds()
        {
            //DataService.Instance.ResourceCache.Clear();
            var handlers = new HandlerChain();
            DataService.Instance.Controller
                .SetNext(handlers.LocalTMDbProcessor)
                .SetNext(handlers.RemoteTMDbProcessor);

            var movie = await TMDB.WithExternalIdsAsync(Constants.Movie, 0);
            await CachedAsync(handlers.DiskCache);

            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(8, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
            Assert.AreEqual(1, DataService.Instance.ResourceCache.Count);

            Assert.IsTrue(movie.TryGetID(IMDb.ID, out var imdbId));
            Assert.AreEqual("tt1201607", imdbId);

            Assert.IsTrue(movie.TryGetID(Wikidata.ID, out var wikiId));
            Assert.AreEqual("Q232009", wikiId);

            Assert.IsTrue(movie.TryGetID(Facebook.ID, out var fbId));
            Assert.AreEqual("harrypottermovie", fbId);

            Assert.IsTrue(movie.TryGetID(Instagram.ID, out var igId));
            Assert.AreEqual("harrypotterfilm", igId);

            Assert.IsTrue(movie.TryGetID(Twitter.ID, out var twitterId));
            Assert.AreEqual("HarryPotterFilm", twitterId);

            handlers.DiskCache.Clear();
            await TMDB.WithExternalIdsAsync(Constants.Movie, 0);
            await CachedAsync(handlers.DiskCache);

            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task RetrieveMovieParentCollection()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var request = new KeyValueRequestArgs<Uri, Collection>(new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION));
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new Collection().WithID(TMDB.IDKey, 1241), request.Value);

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory[0]);
            Assert.AreEqual("3/collection/1241?language=en-US", WebHistory[1]);
        }

        [TestMethod]
        public async Task RetrieveMovieParentCollectionPartial()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var request = new KeyValueRequestArgs<Uri, Collection>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory[0]);

            await CachedAsync(handlers.DiskCache);

            request = new KeyValueRequestArgs<Uri, Collection>(new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION));
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new Collection().WithID(TMDB.IDKey, 1241), request.Value);

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual("3/collection/1241?language=en-US", WebHistory[1]);
        }

        [TestMethod]
        public async Task RetrieveRecommendedMovies()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var request = new KeyValueRequestArgs<Uri, IAsyncEnumerable<Item>>(new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED));
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);

            int count = 0;
            try { await foreach (var item in request.Value) { count++; } } catch { }

            Assert.AreEqual(20, count);
            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=recommendations,credits,external_ids,keywords,release_dates,videos,watch/providers", handlers.WebHistory[0]);
            Assert.AreEqual($"3/movie/0/recommendations?language=en-US&page=2", WebHistory[1]);
        }

        [TestMethod]
        public async Task GetAllResources()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            await handlers.InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), State.Create(new TimeSpan(2, 10, 0)));

            var requests = GetAllRequests();
            await Task.WhenAll(chain.Get(requests));
            await CachedAsync(handlers.DiskCache);

            Assert.AreEqual(2, WebHistory.Count);
            ResourceAssert.DiskCacheCount(handlers.DiskCache, Constants.Movie);
            ResourceAssert.InMemoryCacheCount(handlers.InMemoryCache, Constants.Movie);
        }

        [TestMethod]
        public async Task AllTMDbResourcesAutomated()
        {
            //var request = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), typeof(string));
            //await Controller.Get(request);
            //request = new RestRequestArgs(new UniformItemIdentifier(Constants.TVShow, TVShow.GENRES), typeof(IEnumerable<Genre>));
            //await Controller.Get(request);

            var handlers = new HandlerChain();

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.ReadLatency = handlers.DiskCache.WriteLatency = 0;

            await AllResources(handlers, Constants.Movie, typeof(Media), new Dictionary<Property, object>
            {
                [Media.TITLE] = "Harry Potter and the Deathly Hallows: Part 2",
                [Media.RUNTIME] = new TimeSpan(2, 10, 0),
                [Media.PRODUCTION_COUNTRIES] = "United Kingdom",
                [Media.KEYWORDS] = new Keyword { Id = 616, Name = "witch" },
            });
            await AllResources(handlers, Constants.Movie, new Dictionary<Property, object>
            {
                [Movie.BUDGET] = 125000000L,
                [Movie.CONTENT_RATING] = "PG-13",
                [Movie.PARENT_COLLECTION] = new Collection().WithID(TMDB.IDKey, 1241)
            });
            await AllResources(handlers, Constants.TVShow, new Dictionary<Property, object>
            {
                [TVShow.GENRES] = new Genre { Id = 35, Name = "Comedy" },
                [TVShow.CONTENT_RATING] = "TV-14",
            });
            await AllResources(handlers, Constants.TVSeason, new Dictionary<Property, object>
            {
                [TVSeason.YEAR] = new DateTime(2006, 9, 21)
            });
            await AllResources(handlers, Constants.TVEpisode, new Dictionary<Property, object>
            {
                [TVEpisode.AIR_DATE] = new DateTime(2007, 4, 26)
            });
            await AllResources(handlers, Constants.Person, new Dictionary<Property, object>
            {
                [Person.ALSO_KNOWN_AS] = "Jessica Howard",
            });
        }

        private Task AllResources(HandlerChain handlers, Item item, IDictionary<Property, object> expectedValues = null) => AllResources(handlers, item, item.GetType(), expectedValues);
        private async Task AllResources(HandlerChain handlers, Item item, Type type, IDictionary<Property, object> expectedValues = null)
        {
            var properties = GetProperties(type);
            var chains = new (ChainLink<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Controller, IEnumerable<Property> Properties)[]
            {
                (handlers.Chain, properties),
                (AsyncCoRExtensions.Create(new EventCacheReadProcessor<KeyValueRequestArgs<Uri>>(handlers.LocalTMDbCache)), type == typeof(TVSeason) || type == typeof(TVEpisode) ? Enumerable.Empty<Property>() : properties)
            };

            foreach (var chain in chains)
            {
                foreach (var property in chain.Properties)
                {
                    if (property == Movie.PARENT_COLLECTION && chain.Controller != handlers.Chain)
                    {
                        continue;
                    }

                    var request = CreateRequest(item, property);
                    await chain.Controller.Get(request);
                    var value = request.Value;

                    Assert.IsTrue(request.IsHandled, $"Could not get value for property {property} of type {type} using chain {Array.IndexOf(chains, chain)}");
                    //Assert.IsTrue(arg.Response.TryGetRepresentation(property.FullType, out var value), $"Property {property}. Expected type: {property.FullType}. Available types: {string.Join(", ", arg.Response.OfType<object>().Select(rep => rep.GetType()))}");

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

        //[TestMethod]
        public async Task CheckPerformance()
        {
            //DataService.Instance.Controller.SetNext(chain.Next);
            DebugConfig.SimulatedDelay = 0;
            //DiskCache.SimulatedDelay = 0;
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

        private static Task CachedAsync(DummyDatastore<Uri> diskCache)
        {
            return Task.Delay((Math.Max(diskCache.ReadLatency, diskCache.WriteLatency) + DebugConfig.SimulatedDelay) * 2);
        }

        private Property[] AllMovieProperties() => GetProperties(typeof(Media)).Concat(GetProperties(typeof(Movie))).Append(TMDB.POPULARITY).ToArray();

        private KeyValueRequestArgs<Uri>[] GetAllRequests() => GetAllRequests(AllMovieProperties());
        private KeyValueRequestArgs<Uri>[] GetAllRequests(IEnumerable<Property> properties) => properties
            .Select(property => CreateRequest(Constants.Movie, property))
            .ToArray();
        private static KeyValueRequestArgs<Uri> CreateRequest(Item item, Property property) => (KeyValueRequestArgs<Uri>)Activator.CreateInstance(typeof(KeyValueRequestArgs<,>).MakeGenericType(typeof(Uri), property.FullType), new UniformItemIdentifier(item, property));

        private static IEnumerable<Property> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));
    }
}
