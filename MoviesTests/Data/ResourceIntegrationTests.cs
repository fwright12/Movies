using Movies.Data.Local;
using Movies.Models;
using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data
{
    [TestClass]
    public class ResourceIntegrationTests : Resources
    {
        private static readonly string DATABASE_FILENAME = $"{typeof(ResourceIntegrationTests).FullName}.db";

        private static dBConnection Connection = new dBConnection(DATABASE_FILENAME);

        [TestInitialize]
        public async Task Reset()
        {
            DebugConfig.SimulatedDelay = 100;
            //DiskCache.SimulatedDelay = 50;

            ClearCaches();

            await EmptyDb();
        }

        private void ClearCaches()
        {
            WebHistory.Clear();
            //InMemoryCache.Clear();
            //DiskCache.Clear();
            TMDB.CollectionCache.Clear();
        }

        private async Task EmptyDb()
        {
            await Connection.SqlConnection.ExecuteAsync($"drop table if exists {SqlResourceDAO.RESOURCE_TABLE_NAME}");
        }

        [TestMethod]
        public async Task RetrieveUncachedMovieDetails()
        {
            var handlers = new HandlerChain();
            var args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).ToArray();
            
            await handlers.Chain.Get(args);
            //var recommended = await recommendedArg.Value.ReadAll();

            Assert.AreEqual(2, WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, WebHistory[0]);
            Assert.AreEqual("3/collection/1241", WebHistory[1]);
            //Assert.AreEqual("api.themoviedb.org/3/movie/0/recommendations?page=2", WebHistory[2]);

            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            //Assert.AreEqual(40, recommended.Count);
            //Assert.AreEqual("Harry Potter", recommended[21].Name);
        }

        [TestMethod]
        public async Task RetrieveLocallyCachedMovieDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            // Request data from API so it caches in db and memory, then clear memory to simulate app close
            var args = CreateRequests(Constants.Movie, ALL_MOVIE_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);
            await Connection.WaitSettledAsync();
            ClearCaches();
            handlers.MemoryCache.Clear();

            args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual("3/collection/1241", WebHistory[0]);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveMemoryCachedMovieDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            // Request data from API so it caches in db and memory, then clear db so requests are forced to be handled from memory
            var args = CreateRequests(Constants.Movie, ALL_MOVIE_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);
            await Connection.WaitSettledAsync();
            ClearCaches();
            await EmptyDb();

            args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(0, WebHistory.Count);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveUncachedTVShowDetails()
        {
            var handlers = new HandlerChain();
            var args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();

            await handlers.Chain.Get(args);

            Assert.AreEqual(1, WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.TV_URL_USENGLISH_APPENDED, WebHistory[0]);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveLocallyCachedTVShowDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            // Request data from API so it caches in db and memory, then clear memory to simulate app close
            var args = CreateRequests(Constants.TVShow, ALL_TVSHOW_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);
            await Connection.WaitSettledAsync();
            ClearCaches();
            handlers.MemoryCache.Clear();

            args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);

            Assert.AreEqual(0, WebHistory.Count);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveMemoryCachedTVShowDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            // Request data from API so it caches in db and memory, then clear db so requests are forced to be handled from memory
            var args = CreateRequests(Constants.TVShow, ALL_TVSHOW_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);
            await Connection.WaitSettledAsync();
            ClearCaches();
            await EmptyDb();

            args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();
            await handlers.Chain.Get(args);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(0, WebHistory.Count);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
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
            Assert.AreEqual($"3/movie/0?{Constants.APPEND_TO_RESPONSE}=recommendations,credits,external_ids,keywords,release_dates,videos,watch/providers", handlers.WebHistory[0]);
            Assert.AreEqual($"3/movie/0/recommendations?page=2", WebHistory[1]);
        }

        [TestMethod]
        public async Task RetrieveLocalizedResource()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;

            var args = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE, new Language("en-US")));
            await chain.Get(args);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(args.IsHandled);
            Assert.AreEqual("It all ends.", args.Value);

            Assert.AreEqual(1, handlers.WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, handlers.WebHistory[0]);

            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Tagline?language=en-US")));
            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Runtime?language=en-US")));
            Assert.IsFalse(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Keywords")));
            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Keywords?language=en-US")));
        }

        [TestMethod]
        public async Task RetrieveSameResourceDifferentLocalizations()
        {
            var handlers = new HandlerChain();
            var chain = handlers.Chain;
            var argsEn = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE, new Language("en-US")));
            var argsPt = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE, new Language("pt-BR")));

            await chain.Get(argsEn);
            await chain.Get(argsPt);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(argsEn.IsHandled);
            Assert.IsTrue(argsPt.IsHandled);

            Assert.AreEqual(2, handlers.WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, handlers.WebHistory.Last());
            ResourceAssert.AreEquivalentTMDbUrl($"3/movie/0?language=&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory[1]);

            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Tagline?language=en-US")));
            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Tagline?language=pt-BR")));
        }

        [TestMethod]
        public async Task RetrieveStaleResourceEtagDoesMatch()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var chain = handlers.Chain;

            var uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var message = new SqlResourceDAO.Message
            {
                Uri = uri,
                Response = Encoding.UTF8.GetBytes("1"), // This value is different than what is expected from TMDb
                ETag = MockHandler.DEFAULT_ETAG, // Etag should match
                ExpiresAt = DateTimeOffset.Now.Subtract(new TimeSpan(1, 0, 0)) // Message is stale
            };
            await dao.InsertMessage(message);

            var request = await chain.TryGet<TimeSpan>(uri);
            await Connection.WaitSettledAsync();
            var newMessage = await dao.GetMessage(uri);

            Assert.AreEqual(1, WebHistory.Count);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(TimeSpan.FromMinutes(1), request.Value);

            // Etags should match, expiresAt should be updated to indicate resource was refreshed
            Assert.AreNotEqual(message.ExpiresAt, newMessage.ExpiresAt);
            Assert.AreEqual(MockHandler.DEFAULT_ETAG, newMessage.ETag);
            // Response should not be updated from message we inserted
            Assert.AreEqual(Encoding.UTF8.GetString(message.Response), Encoding.UTF8.GetString(newMessage.Response));
        }

        [TestMethod]
        public async Task RetrieveStaleResourceEtagDoesNotMatch()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var chain = handlers.Chain;

            var uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var message = new SqlResourceDAO.Message
            {
                Uri = uri,
                Response = Encoding.UTF8.GetBytes("1"), // This value is different than what is expected from TMDb
                ETag = "\"non matching etag\"", // Etag should not match
                ExpiresAt = DateTimeOffset.Now.Subtract(new TimeSpan(1, 0, 0)) // Message is stale
            };
            await dao.InsertMessage(message);

            var request = await chain.TryGet<TimeSpan>(uri);
            await Connection.WaitSettledAsync();
            var newMessage = await dao.GetMessage(uri);

            Assert.AreEqual(1, WebHistory.Count);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(TimeSpan.FromMinutes(130), request.Value);

            // Etags should not match, expiresAt should be updated to indicate resource was refreshed
            Assert.AreNotEqual(message.ExpiresAt, newMessage.ExpiresAt);
            Assert.AreEqual("W/" + MockHandler.DEFAULT_ETAG, newMessage.ETag);
            // Response should be updated with value from API
            Assert.AreEqual("130", Encoding.UTF8.GetString(newMessage.Response));
        }

        //[TestMethod]
        public async Task RetrieveMovieParentCollection()
        {
            var handlers = new HandlerChain();

            var request = new KeyValueRequestArgs<Uri, Collection>(new UniformItemIdentifier(Constants.Movie, Movie.PARENT_COLLECTION));
            //await Connection.PersistenceService.Processor.ProcessAsync(request.AsEnumerable(), null);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(new Collection().WithID(TMDB.IDKey, 1241), request.Value);

            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual("3/collection/1241?language=en-US", WebHistory[0]);
        }

        //[TestMethod]
        public async Task AllResources()
        {
            var handlers = new HandlerChain();

            var arg = new KeyValueRequestArgs<Uri, TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            var args = new BulkEventArgs<KeyValueRequestArgs<Uri>>(arg);
            await handlers.RemoteTMDbProcessor.ProcessAsync(args);

            var urls = args.Where(arg => arg.Request.Key is UniformItemIdentifier == false).ToArray();
            var urns = args.Where(arg => arg.Request.Key is UniformItemIdentifier).ToArray();

            Assert.AreEqual(8, urls.Length);
            Assert.AreEqual(25, urns.Length);

            var values = urns.ToDictionary(arg => arg.Request.Key.ToString(), arg => (arg.Response as ResourceResponse)?.TryGetRepresentation<IEnumerable<byte>>(out var bytes) == true ? Encoding.UTF8.GetString(bytes.ToArray()) : "");
            foreach (var kvp in values)
            {
                Print.Log($"\t{kvp.Key}: {kvp.Value}");
            }
        }

        //[TestMethod]
        public async Task GetExternalIds()
        {
            //DataService.Instance.ResourceCache.Clear();
            var handlers = new HandlerChain();
            DataService.Instance.Controller
                .SetNext(handlers.PersistenceService.Processor)
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

        private static readonly Property[] ALL_MOVIE_PROPERTIES = new Property[] { Media.BACKDROP_PATH, Movie.BUDGET, Media.CAST, Movie.CONTENT_RATING, Media.CREW, Media.DESCRIPTION, Movie.GENRES, Media.KEYWORDS, Media.LANGUAGES, Media.ORIGINAL_LANGUAGE, Media.ORIGINAL_TITLE, Movie.PARENT_COLLECTION, Media.POSTER_PATH, Media.PRODUCTION_COMPANIES, Media.PRODUCTION_COUNTRIES, Media.RATING, Media.RECOMMENDED, Movie.RELEASE_DATE, Movie.REVENUE, Media.RUNTIME, Media.TAGLINE, Media.TITLE, Media.TRAILER_PATH, Movie.WATCH_PROVIDERS };

        private static readonly Property[] ALL_TVSHOW_PROPERTIES = new Property[] { Media.BACKDROP_PATH, Media.CAST, Media.CREW, Media.DESCRIPTION, Media.KEYWORDS, Media.LANGUAGES, Media.ORIGINAL_LANGUAGE, Media.ORIGINAL_TITLE, Media.POSTER_PATH, Media.PRODUCTION_COMPANIES, Media.PRODUCTION_COUNTRIES, Media.RATING, Media.RECOMMENDED, Media.RUNTIME, Media.TAGLINE, Media.TITLE, Media.TRAILER_PATH };

        private static IEnumerable<KeyValueRequestArgs<Uri>> CreateRequests(Item item, out KeyValueRequestArgs<Uri, IAsyncEnumerable<Item>> recommendedArg, params Property[] properties)
        {
            recommendedArg = new KeyValueRequestArgs<Uri, IAsyncEnumerable<Item>>(GetUii(item, Media.RECOMMENDED));
            return CreateRequests(item, properties).Where(arg => (arg.Request.Key as UniformItemIdentifier)?.Property != Media.RECOMMENDED).Append(recommendedArg);
        }

        private static IEnumerable<KeyValueRequestArgs<Uri>> CreateRequests(Item item, params Property[] properties) => properties.Select(property => new KeyValueRequestArgs<Uri>(GetUii(item, property), property.FullType));

        private static UniformItemIdentifier GetUii(Item item, Property property) => new UniformItemIdentifier(item, property, language: Constants.US_ENGLISH_LANGUAGE);

        private static Task CachedAsync(DummyDatastore<Uri> diskCache)
        {
            return Task.Delay((Math.Max(diskCache.ReadLatency, diskCache.WriteLatency) + DebugConfig.SimulatedDelay) * 2);
        }
    }
}
