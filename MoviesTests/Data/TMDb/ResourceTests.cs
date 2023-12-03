using System.Collections;
using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class ResourceTests : Resources
    {
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

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_PARTIAL_RESPONSE;// "{ \"runtime\": 130 }";
            handlers.DiskCache.TryAdd(uri, new ResourceResponse<IEnumerable<byte>>(Encoding.UTF8.GetBytes(json)));
            Assert.AreEqual(1, handlers.DiskCache.Count);

            var request = new ResourceReadArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
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

            var args = new ResourceReadArgs<Uri, TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(args);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(args.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), args.Value);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory.Last());

            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(7, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
            Assert.AreEqual(1, handlers.InMemoryCache.Count);
        }

        [TestMethod]
        public async Task ResourcesWithEtagDontHandleImmediately()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_PARTIAL_RESPONSE;// "{ \"runtime\": 130 }";
            handlers.DiskCache.TryAdd(uri, new RestResponse(State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json)), new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.ETAG] = new List<string> { "\"non matching etag\"" }
            }, new Dictionary<string, string>()));
            Assert.AreEqual(1, handlers.DiskCache.Count);

            var request = new ResourceReadArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Value);

            // Normally this would just handle from the cache, but since it has an etag we need to make the api call
            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(7, handlers.DiskCache.Count);
            Assert.AreEqual(1, handlers.InMemoryCache.Count);
        }

        [TestMethod]
        public async Task FailedRequestsAreNotCached()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            handlers.MockHttpHandler.Disconnect();

            var uii = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var response = await chain.TryGet<TimeSpan>(uii);

            Assert.IsFalse(response.IsHandled);

            await CachedAsync(handlers.DiskCache);

            Assert.AreEqual(0, handlers.WebHistory.Count);
            Assert.AreEqual(0, handlers.DiskCache.Count);
            Assert.AreEqual(0, handlers.InMemoryCache.Count);

            handlers.MockHttpHandler.Reconnect();

            response = await chain.TryGet<TimeSpan>(uii);

            Assert.IsTrue(response.IsHandled);
            Assert.AreEqual(response.Value, new TimeSpan(2, 10, 0));
        }

        [TestMethod]
        public async Task RetrieveMovieParentCollection()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var request = new ResourceReadArgs<Uri, Collection>(new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION));
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new Collection().WithID(TMDB.IDKey, 1241), request.Value);

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory[0]);
            Assert.AreEqual("3/collection/1241?language=en-US", WebHistory[1]);
        }

        [TestMethod]
        public async Task GetCredits()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var r1 = new ResourceReadArgs<Uri, DateTime>(new UniformItemIdentifier(Constants.Person, Person.BIRTHDAY));
            var r2 = new ResourceReadArgs<Uri, IEnumerable<Item>>(new UniformItemIdentifier(Constants.Person, Person.CREDITS));
            var temp = chain.Get(r1, r2);

            await Task.Delay(10);

            var request1 = new ResourceReadArgs<Uri, IEnumerable<Item>>(new UniformItemIdentifier(Constants.Person, Person.CREDITS));

            await Task.WhenAll(chain.Get(request1), temp);

            Assert.IsTrue(r1.IsHandled);
            Assert.IsTrue(r2.IsHandled);
            Assert.IsTrue(request1.IsHandled);
        }

        [TestMethod]
        public async Task OverwriteOldData()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.INTERSTELLAR_RESPONSE;
            Assert.IsTrue(await handlers.DiskCache.CreateAsync(uri, State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json))));

            uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var request = new ResourceReadArgs<Uri, TimeSpan>(uri);
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(Constants.INTERSTELLAR_RUNTIME, request.Value);

            await handlers.InMemoryCache.DeleteAsync(GetUiis(Media.RUNTIME).First());
            await CachedAsync(handlers.DiskCache);

            // This will force an api call (since watch providers are at a different endpoint)
            var requests = new ResourceReadArgs<Uri>[]
            {
                request = new ResourceReadArgs<Uri, TimeSpan>(uri),
                CreateArgs(Movie.WATCH_PROVIDERS)
            };
            await chain.Get(requests);

            Assert.IsTrue(requests.All(request => request.IsHandled), string.Join(", ", requests.Where(req => !req.IsHandled).Select(req => req.Key)));
            // We want this to have changed, since it should have been overwritten
            // with the newer data that was just fetched
            Assert.AreEqual(Constants.HARRY_POTTER_RUNTIME, request.Value);
            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task DontUpdateResourcesOnEtagMatch()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.INTERSTELLAR_RESPONSE;
            handlers.DiskCache.TryAdd(uri, new RestResponse(State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json)), new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.ETAG] = new List<string> { MockHandler.DEFAULT_ETAG }
            }, new Dictionary<string, string> { }));

            Assert.AreEqual(1, handlers.DiskCache.Count);

            uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var request = new ResourceReadArgs<Uri, TimeSpan>(uri);
            await handlers.LocalTMDbCache.Read(request.AsEnumerable());

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(Constants.INTERSTELLAR_RUNTIME, request.Value);
            
            request = new ResourceReadArgs<Uri, TimeSpan>(uri);
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            // By default data for Harry Potter 7 is returned. But the etags should match, so data will not be updated and will still be Interstellar data we put in
            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(Constants.INTERSTELLAR_RUNTIME, request.Value);
            Assert.AreEqual(1, handlers.DiskCache.Count);
            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        private static IEnumerable<UniformItemIdentifier> GetUiis(params Property[] properties) => properties.Select(property => new UniformItemIdentifier(Constants.Movie, property));

        private static ResourceReadArgs<Uri, T> CreateArgs<T>(Property<T> property) => new ResourceReadArgs<Uri, T>(new UniformItemIdentifier(Constants.Movie, property));
        private static ResourceReadArgs<Uri, IEnumerable<T>> CreateArgs<T>(MultiProperty<T> property) => new ResourceReadArgs<Uri, IEnumerable<T>>(new UniformItemIdentifier(Constants.Movie, property));

        [TestMethod]
        public async Task RetrieveRecommendedMovies()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var request = new ResourceReadArgs<Uri, IAsyncEnumerable<Item>>(new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED));
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);

            int count = 0;
            try { await foreach (var item in request.Value) { count++; } } catch { }

            Assert.AreEqual(21, count);
            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=recommendations,credits,keywords,release_dates,videos,watch/providers", handlers.WebHistory[0]);
            Assert.AreEqual($"3/movie/0/recommendations?language=en-US&page=2", WebHistory[1]);
        }

        [TestMethod]
        public async Task InMemoryCachedAsTasks()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.SimulatedDelay = 50;

            var uri = new UniformItemIdentifier(Constants.Movie, Media.TITLE);
            var arg1 = new ResourceReadArgs<Uri, string>(uri);
            var arg2 = new ResourceReadArgs<Uri, string>(uri);
            // Should take 50 ms to read (and fail) from disk, then another 50 ms to write
            // Send the second request while (hopefully) still writing to disk, but after response
            // cleared from the http handler cache
            var first = chain.Get(arg1);
            await Task.Delay(40);
            var second = chain.Get(arg2);
            await Task.WhenAll(first, second);

            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task CacheRequestsInTransit()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.SimulatedDelay = 100;

            var arg1 = CreateArgs(Media.TITLE);
            var arg2 = CreateArgs(Media.RECOMMENDED);
            Task t1 = chain.Get(arg1, arg2);

            // Requests arg1 and arg2 get delayed reading from the disk cache, but we allow arg3 to go through
            // synchronously. However, we already know we want recommended movies, so the web request should only be made once
            handlers.DiskCache.SimulatedDelay = 0;
            var arg3 = CreateArgs(Movie.WATCH_PROVIDERS);
            Task t2 = chain.Get(arg3);
            handlers.DiskCache.SimulatedDelay = 100;

            await Task.WhenAll(t1, t2);

            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        // Not ready for this yet
        //[TestMethod]
        public async Task ExtraRequestsAreCached()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.SimulatedDelay = 100;

            var arg1 = new ResourceReadArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            var arg2 = new ResourceReadArgs<Uri, IEnumerable<WatchProvider>>(new UniformItemIdentifier(Constants.Movie, Movie.WATCH_PROVIDERS));
            var arg3 = new ResourceReadArgs<Uri, IAsyncEnumerable<Item>>(new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED));

            // The request for arg1 and arg2 gets delayed reading from the disk cache, while arg3 goes through
            // immediately because the disk cache knows that property is not cacheable. However, we already know
            // we want recommended movies, so the web request should only be made once
            Task t1 = chain.Get(arg1, arg2);
            Task t2 = chain.Get(arg3);
            await Task.WhenAll(t1, t2);

            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task HttpRequestsAreCached()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 100;
            handlers.DiskCache.SimulatedDelay = 0;

            var uri = new UniformItemIdentifier(Constants.Movie, Media.TITLE);
            var arg1 = new ResourceReadArgs<Uri, string>(uri);
            var arg2 = new ResourceReadArgs<Uri, string>(uri);
            await Task.WhenAll(chain.Get(arg1), chain.Get(arg2));

            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        // Calls to the api will have a delay
        // If the in memory cache doesn't cache tasks, successive calls will go through because they don't
        // return a result immediately
        [TestMethod]
        public async Task RestRequestsAreCached()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.SimulatedDelay = 0;

            var arg1 = new ResourceReadArgs<Uri, IEnumerable<TVSeason>>(new UniformItemIdentifier(Constants.TVShow, TVShow.SEASONS));
            var arg2 = new ResourceReadArgs<Uri, string>(new UniformItemIdentifier(Constants.TVShow, Media.TITLE));
            await Task.WhenAll(chain.Get(arg1), chain.Get(arg2));

            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task RequestsAreBatched()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            ResourceReadArgs<Uri>[] getAllRequests() => GetAllRequests(AllMovieProperties().Except(new Property[] { Movie.PARENT_COLLECTION }));

            var requests = getAllRequests();
            await Task.WhenAll(chain.Get(requests));
            await CachedAsync(handlers.DiskCache);
            Assert.IsTrue(requests.All(request => request.IsHandled), "The following requests were not handled:\n\t" + string.Join("\n\t", requests.Where(request => !request.IsHandled).Select(request => request.Key)));
            Assert.AreEqual(1, handlers.WebHistory.Count);

            // No additional api calls should be made, everything should be cached in memory
            await Task.WhenAll(chain.Get(getAllRequests()));
            await CachedAsync(handlers.DiskCache);
            Assert.AreEqual(1, handlers.WebHistory.Count);
            return;
            await CachedAsync(handlers.DiskCache);

            // These properties should result in another api call because they don't cache
            foreach (var property in new Property[]
            {
                Media.ORIGINAL_LANGUAGE,
                Media.RECOMMENDED,
                //Movie.PARENT_COLLECTION,
            })
            {
                handlers.WebHistory.Clear();

                await handlers.InMemoryCache.DeleteAsync(new UniformItemIdentifier(Constants.Movie, property));
                await chain.Get(requests = getAllRequests());
                await CachedAsync(handlers.DiskCache);
                Assert.AreEqual(1, handlers.WebHistory.Count, property.ToString());
            }
        }

        [TestMethod]
        public async Task GetAllResources()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            await handlers.InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), State.Create(new TimeSpan(2, 10, 0)));

            var requests = GetAllRequests();
            await Task.WhenAll(chain.Get(requests));

            var runtime = (ResourceReadArgs<Uri, TimeSpan>)requests.Where(request => (request.Key as UniformItemIdentifier)?.Property == Media.RUNTIME).FirstOrDefault();
            var watchProviders = (ResourceReadArgs<Uri, IEnumerable<WatchProvider>>)requests.Where(request => (request.Key as UniformItemIdentifier)?.Property == Movie.WATCH_PROVIDERS).FirstOrDefault();

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual(new TimeSpan(2, 10, 0), runtime.Value);
            Assert.AreEqual("Apple iTunes", watchProviders.Value.FirstOrDefault()?.Company.Name);
        }

        private static Task CachedAsync(DummyDatastore<Uri> diskCache)
        {
            return Task.Delay((diskCache.SimulatedDelay + DebugConfig.SimulatedDelay) * 2);
        }

        private Property[] AllMovieProperties() => GetProperties(typeof(Media)).Concat(GetProperties(typeof(Movie))).Append(TMDB.POPULARITY).ToArray();

        private ResourceReadArgs<Uri>[] GetAllRequests() => GetAllRequests(AllMovieProperties());
        private ResourceReadArgs<Uri>[] GetAllRequests(IEnumerable<Property> properties) => properties
            .Select(property => CreateRequest(Constants.Movie, property))
            .ToArray();
        private static ResourceReadArgs<Uri> CreateRequest(Item item, Property property) => (ResourceReadArgs<Uri>)Activator.CreateInstance(typeof(ResourceReadArgs<,>).MakeGenericType(typeof(Uri), property.FullType), new UniformItemIdentifier(item, property));

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

            var handlers = new HandlerChain();

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.SimulatedDelay = 0;

            await AllResources(handlers, Constants.Movie, typeof(Media), new Dictionary<Property, object>
            {
                [Media.TITLE] = "Harry Potter and the Deathly Hallows: Part 2",
                [Media.RUNTIME] = new TimeSpan(2, 10, 0),
                [Media.PRODUCTION_COUNTRIES] = "United Kingdom",
                [Media.KEYWORDS] = new Keyword { Id = 83, Name = "saving the world" },
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

        private static IEnumerable<Property> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));

        private Task AllResources(HandlerChain handlers, Item item, IDictionary<Property, object> expectedValues = null) => AllResources(handlers, item, item.GetType(), expectedValues);
        private async Task AllResources(HandlerChain handlers, Item item, Type type, IDictionary<Property, object> expectedValues = null)
        {
            var properties = GetProperties(type);

            foreach (var test in new (ChainLink<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> Controller, IEnumerable<Property> Properties)[]
            {
                (handlers.Chain, properties),
                (AsyncCoRExtensions.Create<IEnumerable<ResourceReadArgs<Uri>>>(handlers.LocalTMDbCache.Processor), type == typeof(TVSeason) || type == typeof(TVEpisode) ? Enumerable.Empty<Property>() : properties)
            })
            {
                foreach (var property in test.Properties)
                {
                    var request = CreateRequest(item, property);
                    await test.Controller.Get(request);
                    var value = request.Response?.RawValue;

                    Assert.IsTrue(request.IsHandled, $"Could not get value for property {property} of type {type}");
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

        public class HandlerChain
        {
            //public readonly ChainLink<MultiRestEventArgs> Chain;
            public readonly ChainLink<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> Chain;
            public readonly TMDbResolver Resolver;

            public List<string> WebHistory => MockHttpHandler.LocalCallHistory;

            public UiiDictionaryDatastore InMemoryCache { get; }
            public DummyDatastore<Uri> DiskCache { get; }

            public TMDbLocalCache LocalTMDbCache { get; }
            public TMDbHttpProcessor RemoteTMDbProcessor { get; }
            public MockHandler MockHttpHandler { get; }

            public HandlerChain()
            {
                Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

                DiskCache = new DummyDatastore<Uri>
                {
                    SimulatedDelay = 50
                };
                InMemoryCache = new UiiDictionaryDatastore();

                LocalTMDbCache = new TMDbLocalCache(DiskCache, Resolver)
                {
                    //ChangeKeys = TestsConfig.ChangeKeys
                };
                var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
                RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);

                Chain = new AsyncCacheAsideProcessor<ResourceReadArgs<Uri>>(new ResourceBufferedCache<Uri>(InMemoryCache)).ToChainLink();
                Chain.SetNext(new AsyncCacheAsideProcessor<ResourceReadArgs<Uri>>(new ResourceBufferedCache<Uri>(LocalTMDbCache)))
                    .SetNext(RemoteTMDbProcessor);
            }
        }
    }
}
