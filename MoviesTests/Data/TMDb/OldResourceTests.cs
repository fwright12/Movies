using Microsoft.VisualStudio.TestTools.UnitTesting;
using Movies.Models;
using Movies;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data.TMDb
{
    //[TestClass]
    public class OldResourceTests : Resources
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

            var request = new KeyValueRequestArgs<Uri, TimeSpan?>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await chain.Get(request);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Value);

            Assert.AreEqual(0, handlers.WebHistory.Count);
            Assert.AreEqual(1, handlers.DiskCache.Count);
            Assert.AreEqual(18, handlers.InMemoryCache.Count);
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
            Assert.AreEqual(8, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
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
        public async Task EtagDoesNotMatch()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_PARTIAL_RESPONSE;// "{ \"runtime\": 130 }";
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
            Assert.AreEqual(8, handlers.DiskCache.Count);
            Assert.AreEqual(26, handlers.InMemoryCache.Count);
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
            Assert.AreEqual(0, handlers.InMemoryCache.Count);

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

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_PARTIAL_RESPONSE;// "{ \"runtime\": 130 }";
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
            Assert.AreEqual(0, handlers.WebHistory.Count);
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

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.INTERSTELLAR_RESPONSE;
            Assert.IsTrue(await handlers.DiskCache.CreateAsync(uri, State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json))));

            uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var request = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await chain.Get(request);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(Constants.INTERSTELLAR_RUNTIME, request.Value);

            await handlers.InMemoryCache.DeleteAsync(GetUiis(Media.RUNTIME).First());
            await CachedAsync(handlers.DiskCache);

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

            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = DUMMY_TMDB_DATA.INTERSTELLAR_RESPONSE;
            handlers.DiskCache.TryAdd(uri, new RestResponse((IEnumerable<REpresentationalStateTransfer.Entity>)State.Create<ArraySegment<byte>>(Encoding.UTF8.GetBytes(json)), new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.ETAG] = new List<string> { MockHandler.DEFAULT_ETAG }
            }, new Dictionary<string, string> { }));

            Assert.AreEqual(1, handlers.DiskCache.Count);

            uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var request = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await handlers.LocalTMDbCache.Read(request.AsEnumerable());

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(Constants.INTERSTELLAR_RUNTIME, request.Value);

            request = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
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

            var runtime = (KeyValueRequestArgs<Uri, TimeSpan>)requests.Where(request => (request.Request.Key as UniformItemIdentifier)?.Property == Media.RUNTIME).FirstOrDefault();
            var watchProviders = (KeyValueRequestArgs<Uri, IEnumerable<WatchProvider>>)requests.Where(request => (request.Request.Key as UniformItemIdentifier)?.Property == Movie.WATCH_PROVIDERS).FirstOrDefault();

            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual(new TimeSpan(2, 10, 0), runtime.Value);
            Assert.AreEqual("fuboTV", watchProviders.Value.FirstOrDefault()?.Company.Name);

            WebHistory.RemoveAt(1);
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

        private static IEnumerable<Property> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));

        private Task AllResources(HandlerChain handlers, Item item, IDictionary<Property, object> expectedValues = null) => AllResources(handlers, item, item.GetType(), expectedValues);
        private async Task AllResources(HandlerChain handlers, Item item, Type type, IDictionary<Property, object> expectedValues = null)
        {
            var properties = GetProperties(type);

            foreach (var test in new (ChainLink<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Controller, IEnumerable<Property> Properties)[]
            {
                (handlers.Chain, properties),
                (AsyncCoRExtensions.Create<IEnumerable<KeyValueRequestArgs<Uri>>>(handlers.LocalTMDbCache.Processor), type == typeof(TVSeason) || type == typeof(TVEpisode) ? Enumerable.Empty<Property>() : properties)
            })
            {
                foreach (var property in test.Properties)
                {
                    if (property == Movie.PARENT_COLLECTION && test.Controller != handlers.Chain)
                    {
                        continue;
                    }

                    var request = CreateRequest(item, property);
                    await test.Controller.Get(request);
                    var value = request.Value;

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
            public readonly ChainLink<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Chain;
            public readonly TMDbResolver Resolver;

            public List<string> WebHistory => MockHttpHandler.LocalCallHistory;

            public UiiDictionaryDataStore InMemoryCache { get; }
            public DummyDatastore<Uri> DiskCache { get; }

            public TMDbLocalCache LocalTMDbCache { get; }
            public IAsyncCoRProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> LocalTMDbProcessor { get; }
            public TMDbHttpProcessor RemoteTMDbProcessor { get; }
            public MockHandler MockHttpHandler { get; }

            public HandlerChain()
            {
                Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

                DiskCache = new DummyDatastore<Uri>
                {
                    ReadLatency = 50,
                    WriteLatency = 50,
                };
                InMemoryCache = new UiiDictionaryDataStore();

                LocalTMDbCache = new TMDbLocalCache(DiskCache, Resolver)
                {
                    //ChangeKeys = TestsConfig.ChangeKeys
                };
                var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
                RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);

                //var context = new DataSyncContext<IEnumerable<KeyValueRequestArgs<Uri>>>();
                //Chain = context.Synchronize((dynamic)InMemoryCache);
                //Chain.SetNext(context.Synchronize(LocalTMDbCache)).SetNext(context.Synchronize(RemoteTMDbProcessor));

                Chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(new ResourceBufferedCache<Uri>(InMemoryCache)).ToChainLink();
                Chain.SetNext(LocalTMDbProcessor = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(new UriBufferedCache(LocalTMDbCache)))
                    .SetNext(RemoteTMDbProcessor);
                //.SetNext(new EventCacheReadProcessor<KeyValueRequestArgs<Uri>>(new ResourceBufferedCache<Uri>(new ReadOnlyEventCache<KeyValueRequestArgs<Uri>>(RemoteTMDbProcessor))));
            }
        }
    }
}
