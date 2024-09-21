using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data
{
    [TestClass]
    public class ResourceIntegrationTests : Resources
    {
        private static readonly string DATABASE_FILENAME = $"{typeof(ResourceIntegrationTests).FullName}.db";

        private static Lazy<dBConnection> LazyDBConnection = new Lazy<dBConnection>(() => new dBConnection(DATABASE_FILENAME));

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
        public async Task RetrieveUncachedMovieDetails()
        {
            var args = CreateAllMovieDetailRequests().ToArray();

            await RequestUncachedItem(args);

            Assert.AreEqual(2, WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_APPENDED, WebHistory[0]);
            Assert.AreEqual("3/collection/1241", WebHistory[1]);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveLocallyCachedMovieDetails()
        {
            var args = CreateAllMovieDetailRequests().ToArray();

            await RequestUncachedItem(args);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual("3/collection/1241", WebHistory[0]);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveMemoryCachedMovieDetails()
        {
            var args = CreateAllMovieDetailRequests().ToArray();

            await RequestUncachedItem(args);

            Assert.AreEqual(0, WebHistory.Count);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveUncachedTVShowDetails()
        {
            var args = CreateAllTVShowDetailRequests().ToArray();

            await RequestUncachedItem(args);

            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual(ResourceUtils.TV_URL_APPENDED, WebHistory[0]);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveLocallyCachedTVShowDetails()
        {
            var args = CreateAllTVShowDetailRequests().ToArray();

            await RequestUncachedItem(args);

            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual(ResourceUtils.TV_URL_APPENDED, WebHistory[0]);
            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
        }

        [TestMethod]
        public async Task RetrieveMemoryCachedTVShowDetails()
        {
            var args = CreateAllTVShowDetailRequests().ToArray();

            await RequestUncachedItem(args);

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

            Assert.AreEqual(ResourceUtils.MOVIE_URL_APPENDED_USENGLISH, handlers.WebHistory.Last());
            Assert.AreEqual(1, handlers.WebHistory.Count);
            Assert.AreEqual(26, handlers.DiskCache.Count, string.Join(", ", handlers.DiskCache.Keys));
            Assert.AreEqual(26, handlers.MemoryCache.Count);

            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Tagline?language=en-US")));
            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Runtime?language=en-US")));
            Assert.IsTrue(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Keywords")));
            Assert.IsFalse(handlers.DiskCache.ContainsKey(new Uri("urn:Movie:0:Keywords?language=en-US")));
        }

        [TestMethod]
        public void RetrieveSameResourceDifferentLocalizations()
        {
            Assert.Inconclusive();
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

        private async Task<HandlerChain> RequestUncachedItem(IEnumerable<KeyValueRequestArgs<Uri>> args)
        {
            var handlers = new HandlerChain();
            await handlers.Chain.Get(args);

            return handlers;
        }

        private async Task<HandlerChain> RequestLocallyCachedItem<T>(T request)
        {
            var handlers = new HandlerChain(diskCache: LazyDBConnection.Value.DAO);

            //await handlers.Chain.Get(new T().DetailRequestArgs);
            await LazyDBConnection.Value.WaitSettledAsync();
            handlers.MemoryCache.Clear();
            TMDB.CollectionCache.Clear();
            WebHistory.Clear();

            //await handlers.Chain.Get(request.DetailRequestArgs);

            return handlers;
        }

        private async Task<HandlerChain> RequestMemoryCachedItem<T>(T request)
        {
            var handlers = new HandlerChain();

            //await handlers.Chain.Get(new T().DetailRequestArgs);
            handlers.DiskCache.Clear();
            WebHistory.Clear();

            //await handlers.Chain.Get(request.DetailRequestArgs);

            return handlers;
        }

        private static IEnumerable<KeyValueRequestArgs<Uri>> CreateAllMovieDetailRequests() => new Property[] { Media.BACKDROP_PATH, Movie.BUDGET, Media.CAST, Movie.CONTENT_RATING, Media.CREW, Media.DESCRIPTION, Movie.GENRES, Media.KEYWORDS, Media.LANGUAGES, Media.ORIGINAL_LANGUAGE, Media.ORIGINAL_TITLE, Movie.PARENT_COLLECTION, Media.POSTER_PATH, Media.PRODUCTION_COMPANIES, Media.PRODUCTION_COUNTRIES, Media.RATING, Media.RECOMMENDED, Movie.RELEASE_DATE, Movie.REVENUE, Media.RUNTIME, Media.TAGLINE, Media.TITLE, Media.TRAILER_PATH, Movie.WATCH_PROVIDERS }.Select(property => new KeyValueRequestArgs<Uri>(new UniformItemIdentifier(Constants.Movie, property), property.FullType));

        private static IEnumerable<KeyValueRequestArgs<Uri>> CreateAllTVShowDetailRequests() => new Property[] { Media.BACKDROP_PATH, Media.CAST, Media.CREW, Media.DESCRIPTION, Media.KEYWORDS, Media.LANGUAGES, Media.ORIGINAL_LANGUAGE, Media.ORIGINAL_TITLE, Media.POSTER_PATH, Media.PRODUCTION_COMPANIES, Media.PRODUCTION_COUNTRIES, Media.RATING, Media.RECOMMENDED, Media.RUNTIME, Media.TAGLINE, Media.TITLE, Media.TRAILER_PATH }.Select(property => new KeyValueRequestArgs<Uri>(new UniformItemIdentifier(Constants.TVShow, property), property.FullType));

        private static Task CachedAsync(DummyDatastore<Uri> diskCache)
        {
            return Task.Delay((Math.Max(diskCache.ReadLatency, diskCache.WriteLatency) + DebugConfig.SimulatedDelay) * 2);
        }
    }
}
