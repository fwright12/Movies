using Movies.Data.Local;
using System.Text;

namespace MoviesTests.Data
{
    [TestClass]
    public class CachingTests : Resources
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
        public async Task ResourceCachesAtAllLayers()
        {
            var uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var handlers = new HandlerChain();

            var arg = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await handlers.Chain.Get(arg);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(arg.IsHandled);
            ResourceAssert.Success(arg);

            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(ResourceUtils.MOVIE_URL_APPENDED, handlers.WebHistory[0]);
            Assert.AreEqual(26, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
            Assert.AreEqual(26, handlers.MemoryCache.Count);

            arg = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await handlers.PersistenceService.Processor.ProcessAsync(arg.AsEnumerable(), null);

            Assert.IsTrue(arg.IsHandled);
            ResourceAssert.Success(arg);

            arg = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await handlers.InMemoryService.Cache.ProcessAsync(arg.AsEnumerable(), null);

            Assert.IsTrue(arg.IsHandled);
            ResourceAssert.Success(arg);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromMemoryCache()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            await handlers.MemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), State.Create(Constants.TAGLINE));
            await handlers.MemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), State.Create(Constants.LANGUAGE));

            // Retrieve an item in the in memory cache
            var uii = new UniformItemIdentifier(Constants.Movie, Media.TAGLINE);
            var response = await chain.TryGet<string>(uii);
            Assert.IsTrue(response.IsHandled);
            Assert.AreEqual(Constants.TAGLINE, response.Value);

            Assert.AreEqual(0, handlers.MockHttpHandler.LocalCallHistory.Count);
            Assert.AreEqual(0, handlers.DiskCache.Count);
            Assert.AreEqual(2, handlers.MemoryCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromDiskCache()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = ResourceUtils.RUNTIME_URI;
            handlers.DiskCache.TryAdd(uri, new ResourceResponse<TimeSpan>(new TimeSpan(2, 10, 0)));
            Assert.AreEqual(1, handlers.DiskCache.Count);

            var request = new KeyValueRequestArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Value);

            Assert.AreEqual(0, handlers.WebHistory.Count);
            Assert.AreEqual(1, handlers.DiskCache.Count);
            Assert.AreEqual(1, handlers.MemoryCache.Count);
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

            Assert.AreEqual(ResourceUtils.MOVIE_URL_APPENDED, handlers.WebHistory.Last());
            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(26, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
            Assert.AreEqual(26, handlers.MemoryCache.Count);
        }

        [TestMethod]
        public async Task EtagDoesNotMatch()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = ResourceUtils.RUNTIME_URI;
            var json = "169";
            handlers.DiskCache.TryAdd(uri, new RestResponse((IEnumerable<REpresentationalStateTransfer.Entity>)State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json)), new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.ETAG] = new List<string> { "\"non matching etag\"" }
            }, new Dictionary<string, string>()));
            Assert.AreEqual(1, handlers.DiskCache.Count);

            var request = new KeyValueRequestArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Value);

            // Normally this would just handle from the cache, but since it has an etag we need to make the api call
            Assert.AreEqual(1, handlers.WebHistory.Count);
            ResourceAssert.DiskCacheCount(handlers.DiskCache, Constants.Movie);
            ResourceAssert.InMemoryCacheCount(handlers.MemoryCache, Constants.Movie);
        }

        [TestMethod]
        public async Task FailedRequestsAreNotCached()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            handlers.MockHttpHandler.Disconnect();

            var uii = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            try
            {
                await chain.TryGet<TimeSpan>(uii);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Print.Log(e);
            }

            await CachedAsync(handlers.DiskCache);

            Assert.AreEqual(0, handlers.WebHistory.Count);
            Assert.AreEqual(0, handlers.DiskCache.Count);
            Assert.AreEqual(0, handlers.MemoryCache.Count);

            handlers.MockHttpHandler.Reconnect();

            var response = await chain.TryGet<TimeSpan>(uii);

            Assert.IsTrue(response.IsHandled);
            Assert.AreEqual(response.Value, new TimeSpan(2, 10, 0));
        }

        [TestMethod]
        public async Task UseStaleDataIfEtaggedRequestFails()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            handlers.MockHttpHandler.Disconnect();
            var uri = ResourceUtils.RUNTIME_URI;
            handlers.DiskCache.TryAdd(uri, new RestResponse((IEnumerable<REpresentationalStateTransfer.Entity>)State.Create(new TimeSpan(2, 10, 0)), new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.ETAG] = new List<string> { "\"non matching etag\"" }
            }, new Dictionary<string, string>()));
            Assert.AreEqual(1, handlers.DiskCache.Count);

            var request = new KeyValueRequestArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Value);
            Assert.AreEqual(0, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task GetCredits()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var r1 = new KeyValueRequestArgs<Uri, DateTime>(new UniformItemIdentifier(Constants.Person, Person.BIRTHDAY));
            var r2 = new KeyValueRequestArgs<Uri, IEnumerable<Item>>(new UniformItemIdentifier(Constants.Person, Person.CREDITS));
            var temp = chain.Get(r1, r2);

            await Task.Delay(20);

            var request1 = new KeyValueRequestArgs<Uri, IEnumerable<Item>>(new UniformItemIdentifier(Constants.Person, Person.CREDITS));

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

            var uri = ResourceUtils.RUNTIME_URI;
            Assert.IsTrue(await handlers.DiskCache.CreateAsync(uri, State.Create(Constants.INTERSTELLAR_RUNTIME)));

            uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var request = new KeyValueRequestArgs<Uri, TimeSpan>(uri);

            // This will force an api call (since watch providers are at a different endpoint)
            var requests = new KeyValueRequestArgs<Uri>[]
            {
                request = new KeyValueRequestArgs<Uri, TimeSpan>(uri),
                CreateArgs(Movie.WATCH_PROVIDERS)
            };
            await chain.Get(requests);

            Assert.IsTrue(requests.All(request => request.IsHandled), string.Join(", ", requests.Where(req => !req.IsHandled).Select(req => req.Request.Key)));
            // We want this to have changed, since it should have been overwritten
            // with the newer data that was just fetched
            Assert.AreEqual(Constants.HARRY_POTTER_RUNTIME, request.Value);
            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        [TestMethod]
        public async Task EtagDoesMatch()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = ResourceUtils.RUNTIME_URI;
            handlers.DiskCache.TryAdd(uri, new RestResponse((IEnumerable<REpresentationalStateTransfer.Entity>)State.Create(Constants.INTERSTELLAR_RUNTIME), new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.ETAG] = new List<string> { MockHandler.DEFAULT_ETAG }
            }, new Dictionary<string, string> { }));

            Assert.AreEqual(1, handlers.DiskCache.Count);

            uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var request = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            // By default data for Harry Potter 7 is returned. But the etags should match, so data will not be updated and will still be Interstellar data we put in
            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(Constants.INTERSTELLAR_RUNTIME, request.Value);
            Assert.AreEqual(1, handlers.DiskCache.Count);
            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        private static IEnumerable<UniformItemIdentifier> GetUiis(params Property[] properties) => properties.Select(property => new UniformItemIdentifier(Constants.Movie, property));

        private static KeyValueRequestArgs<Uri, T> CreateArgs<T>(Property<T> property) => new KeyValueRequestArgs<Uri, T>(new UniformItemIdentifier(Constants.Movie, property));
        private static KeyValueRequestArgs<Uri, IEnumerable<T>> CreateArgs<T>(MultiProperty<T> property) => new KeyValueRequestArgs<Uri, IEnumerable<T>>(new UniformItemIdentifier(Constants.Movie, property));

        [TestMethod]
        public async Task InMemoryCachedAsTasks()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.ReadLatency = handlers.DiskCache.WriteLatency = 50;

            var uri = new UniformItemIdentifier(Constants.Movie, Media.TITLE);
            var arg1 = new KeyValueRequestArgs<Uri, string>(uri);
            var arg2 = new KeyValueRequestArgs<Uri, string>(uri);
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
        public async Task RequestsAreBatched()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            KeyValueRequestArgs<Uri>[] getAllRequests() => GetAllRequests(AllMovieProperties().Except(new Property[] { Movie.PARENT_COLLECTION }));

            var requests = getAllRequests();
            await Task.WhenAll(chain.Get(requests));
            await CachedAsync(handlers.DiskCache);
            Assert.IsTrue(requests.All(request => request.IsHandled), "The following requests were not handled:\n\t" + string.Join("\n\t", requests.Where(request => !request.IsHandled).Select(request => request.Request.Key)));
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

                await handlers.MemoryCache.DeleteAsync(new UniformItemIdentifier(Constants.Movie, property));
                await chain.Get(requests = getAllRequests());
                await CachedAsync(handlers.DiskCache);
                Assert.AreEqual(1, handlers.WebHistory.Count, property.ToString());
            }
        }

        [TestMethod]
        public async Task CacheRequestsInTransit()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.ReadLatency = 100;
            handlers.DiskCache.WriteLatency = 1000;

            var arg1 = CreateArgs(Media.TITLE);
            var arg2 = CreateArgs(Media.RECOMMENDED);
            Task t1 = chain.Get(arg1, arg2);

            // Requests arg1 and arg2 get delayed reading from the disk cache, but we allow arg3 to go through
            // synchronously. However, we already know we want recommended movies, so the web request should only be made once
            handlers.DiskCache.ReadLatency = 0;
            var arg3 = CreateArgs(Movie.WATCH_PROVIDERS);
            Task t2 = chain.Get(arg3);
            handlers.DiskCache.ReadLatency = 100;

            await Task.WhenAll(t1, t2);

            Assert.IsTrue(arg1.IsHandled);
            Assert.IsTrue(arg2.IsHandled);
            Assert.IsTrue(arg3.IsHandled);
            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        //[TestMethod]
        public async Task ExtraRequestsAreCached()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            DebugConfig.SimulatedDelay = 0;
            handlers.DiskCache.ReadLatency = handlers.DiskCache.WriteLatency = 100;

            var arg1 = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            var arg2 = new KeyValueRequestArgs<Uri, IEnumerable<WatchProvider>>(new UniformItemIdentifier(Constants.Movie, Movie.WATCH_PROVIDERS));
            var arg3 = new KeyValueRequestArgs<Uri, IAsyncEnumerable<Item>>(new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED));

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
            handlers.DiskCache.ReadLatency = handlers.DiskCache.WriteLatency = 0;

            var uri = new UniformItemIdentifier(Constants.Movie, Media.TITLE);
            var arg1 = new KeyValueRequestArgs<Uri, string>(uri);
            var arg2 = new KeyValueRequestArgs<Uri, string>(uri);
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
            handlers.DiskCache.ReadLatency = handlers.DiskCache.WriteLatency = 0;

            var arg1 = new KeyValueRequestArgs<Uri, IEnumerable<TVSeason>>(new UniformItemIdentifier(Constants.TVShow, TVShow.SEASONS));
            var arg2 = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.TVShow, Media.TITLE));
            await Task.WhenAll(chain.Get(arg1), chain.Get(arg2));

            Assert.AreEqual(1, handlers.WebHistory.Count);
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
