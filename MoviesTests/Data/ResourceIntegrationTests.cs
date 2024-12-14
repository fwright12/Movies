using Movies.Data.Local;
using System.Diagnostics;
using System.Text;

namespace MoviesTests.Data
{
    [TestClass]
    public class ResourceIntegrationTests : Resources
    {
        public static readonly Uri EXTERNAL_IDS_MOVIE_URI = new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), "0"), UriKind.Relative);
        //public static readonly Uri EXTERNAL_IDS_MOVIE_URI = new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL($"{TMDbRequest.LANGUAGE_PARAMETER_KEY}=en-US"), "0"), UriKind.Relative);

        private const string MOVIE_PARENT_COLLECTION_ENDPOINT = "3/collection/1241?language=en-US";
        private const string MOVIE_RECOMMENDED_PAGE_2_ENDPOINT = "3/movie/0/recommendations?language=en-US&page=2";
        private const string TV_SHOW_RECOMMENDED_PAGE_2_ENDPOINT = "3/tv/0/recommendations?language=en-US&page=2";
        private static readonly string DATABASE_FILENAME = $"{typeof(ResourceIntegrationTests).FullName}.db";
        private static readonly Uri EXTERNAL_IDS_URI = new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), 0), UriKind.Relative);

        private static dBConnection Connection = new dBConnection(DATABASE_FILENAME);
        private Language? BackupLanguage;

        [TestInitialize]
        public async Task Reset()
        {
            DebugConfig.SimulatedDelay = 100;
            //DiskCache.SimulatedDelay = 50;
            BackupLanguage = TMDB.LANGUAGE;

            ClearCaches();

            await EmptyDb();
        }

        [TestCleanup]
        public void Cleanup()
        {
            TMDB.LANGUAGE = BackupLanguage;
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
            var args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).Append(new KeyValueRequestArgs<Uri>(EXTERNAL_IDS_URI)).ToArray();

            await handlers.Chain.Get(args);
            var idsArg = await handlers.Chain.TryGet<IEnumerable<byte>>(EXTERNAL_IDS_URI);
            var recommended = await TryReadAll(recommendedArg.Value);

            Assert.AreEqual(3, WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, WebHistory[0]);
            Assert.AreEqual(MOVIE_PARENT_COLLECTION_ENDPOINT, WebHistory[1]);
            Assert.AreEqual(MOVIE_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[2]);

            foreach (var arg in args.Append(idsArg)) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedMovieRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveFreshLocallyCachedMovieDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            await SeedCaches(handlers, CreateRequests(Constants.Movie, out _, ALL_MOVIE_PROPERTIES), local: true);
            var args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).Append(new KeyValueRequestArgs<Uri>(EXTERNAL_IDS_URI)).ToArray();

            await handlers.Chain.Get(args);
            var idsArg = await handlers.Chain.TryGet<IEnumerable<byte>>(EXTERNAL_IDS_URI);
            var recommended = await TryReadAll(recommendedArg.Value);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual(MOVIE_PARENT_COLLECTION_ENDPOINT, WebHistory[0]);
            Assert.AreEqual(MOVIE_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[1]);

            foreach (var arg in args.Append(idsArg)) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedMovieRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveStaleLocallyCachedMovieDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            await SeedCaches(handlers, CreateRequests(Constants.Movie, out _, ALL_MOVIE_PROPERTIES), local: true);
            await ExpireResources();
            var args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).Append(new KeyValueRequestArgs<Uri>(EXTERNAL_IDS_URI)).ToArray();

            await handlers.Chain.Get(args);
            var idsArg = await handlers.Chain.TryGet<IEnumerable<byte>>(EXTERNAL_IDS_URI);
            var recommended = await TryReadAll(recommendedArg.Value);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(3, WebHistory.Count);
            Assert.AreEqual(MOVIE_PARENT_COLLECTION_ENDPOINT, WebHistory[0]);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, WebHistory[1]);
            Assert.AreEqual(MOVIE_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[2]);

            foreach (var arg in args.Append(idsArg)) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedMovieRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveMemoryCachedMovieDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            await SeedCaches(handlers, CreateRequests(Constants.Movie, out _, ALL_MOVIE_PROPERTIES), memory: true);
            var args = CreateRequests(Constants.Movie, out var recommendedArg, ALL_MOVIE_PROPERTIES).Append(new KeyValueRequestArgs<Uri>(EXTERNAL_IDS_URI)).ToArray();

            await handlers.Chain.Get(args);
            var idsArg = await handlers.Chain.TryGet<IEnumerable<byte>>(EXTERNAL_IDS_URI);
            var recommended = await TryReadAll(recommendedArg.Value);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual(MOVIE_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[0]);

            foreach (var arg in args.Append(idsArg)) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedMovieRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveUncachedTVShowDetails()
        {
            var handlers = new HandlerChain();
            var args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();

            await handlers.Chain.Get(args);
            var recommended = await TryReadAll(recommendedArg.Value);

            Assert.AreEqual(2, WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.TV_URL_USENGLISH_APPENDED, WebHistory[0]);
            Assert.AreEqual(TV_SHOW_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[1]);

            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedTVShowRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveFreshLocallyCachedTVShowDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            await SeedCaches(handlers, CreateRequests(Constants.TVShow, ALL_TVSHOW_PROPERTIES), local: true);
            var args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();

            await handlers.Chain.Get(args);
            var recommended = await TryReadAll(recommendedArg.Value);

            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual(TV_SHOW_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[0]);

            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedTVShowRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveStaleLocallyCachedTVShowDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            await SeedCaches(handlers, CreateRequests(Constants.TVShow, ALL_TVSHOW_PROPERTIES), local: true);
            await ExpireResources();
            var args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();

            await handlers.Chain.Get(args);
            var recommended = await TryReadAll(recommendedArg.Value);

            Assert.AreEqual(2, WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.TV_URL_USENGLISH_APPENDED, WebHistory[0]);
            Assert.AreEqual(TV_SHOW_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[1]);

            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedTVShowRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveMemoryCachedTVShowDetails()
        {
            var handlers = new HandlerChain(diskCache: new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver));
            await SeedCaches(handlers, CreateRequests(Constants.TVShow, ALL_TVSHOW_PROPERTIES), memory: true);
            var args = CreateRequests(Constants.TVShow, out var recommendedArg, ALL_TVSHOW_PROPERTIES).ToArray();

            await handlers.Chain.Get(args);
            var recommended = await TryReadAll(recommendedArg.Value);

            // Parent collection details do not cache, and they are needed to construct the complete object, so they will have to be requested again
            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual(TV_SHOW_RECOMMENDED_PAGE_2_ENDPOINT, WebHistory[0]);

            foreach (var arg in args) ResourceAssert.Success(arg, $"URI: {arg.Request.Key}. ");
            ResourceAssert.IsExpectedTVShowRecommended(recommended);
        }

        [TestMethod]
        public async Task RetrieveLocalizedResource()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var chain = handlers.Chain;
            TMDB.LANGUAGE = new Language("en-US");

            var args = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE));
            await chain.Get(args);
            await Connection.WaitSettledAsync();
            var messages = await Task.WhenAll(
                dao.GetMessage("urn:Movie:0:Tagline?language=en-US"),
                dao.GetMessage("urn:Movie:0:Runtime?language=en-US"),
                dao.GetMessage("urn:Movie:0:Keywords"),
                dao.GetMessage("urn:Movie:0:Keywords?language=en-US")
                );

            ResourceAssert.Success(args);

            Assert.AreEqual(1, handlers.WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, handlers.WebHistory[0]);

            Assert.IsNotNull(messages[0]);
            Assert.IsNotNull(messages[1]);
            Assert.IsNotNull(messages[2]);
            Assert.IsNull(messages[3]);
        }

        [TestMethod]
        public async Task RetrieveSameResourceDifferentLocalizations()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var chain = handlers.Chain;
            var brazilianPortugueseLanguage = new Language("pt-BR");

            var args1 = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE));
            TMDB.LANGUAGE = new Language("en-US");
            await chain.Get(args1);
            await Connection.WaitSettledAsync();
            handlers.MemoryCache.Clear();

            var args2 = new KeyValueRequestArgs<Uri, string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE));
            TMDB.LANGUAGE = brazilianPortugueseLanguage;
            await chain.Get(args2);
            await Connection.WaitSettledAsync();

            var messages = await Task.WhenAll(
                dao.GetMessage("urn:Movie:0:Tagline?language=en-US"),
                dao.GetMessage("urn:Movie:0:Tagline?language=pt-BR"),
                dao.GetMessage("urn:Movie:0:Keywords"),
                dao.GetMessage("urn:Movie:0:Keywords?language=en-US"),
                dao.GetMessage("urn:Movie:0:Keywords?language=pt-BR")
                );

            Assert.IsTrue(args1.IsHandled);
            Assert.IsTrue(args2.IsHandled);

            Assert.AreEqual(2, handlers.WebHistory.Count);
            ResourceAssert.AreEquivalentTMDbUrl(ResourceUtils.MOVIE_URL_USENGLISH_APPENDED, handlers.WebHistory[0]);
            ResourceAssert.AreEquivalentTMDbUrl($"3/movie/0?{TMDbRequest.LANGUAGE_PARAMETER_KEY}={brazilianPortugueseLanguage.Iso_639}&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers", handlers.WebHistory[1]);

            Assert.IsNotNull(messages[0]);
            Assert.IsNotNull(messages[1]);
            Assert.IsNotNull(messages[2]);
            Assert.IsNull(messages[3]);
            Assert.IsNull(messages[4]);
        }

        [TestMethod]
        public async Task RetrieveStaleResourceEtagDoesMatch()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var chain = handlers.Chain;

            var uii = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var uri = uii.ToString() + $"?{TMDbRequest.LANGUAGE_PARAMETER_KEY}={TMDB.LANGUAGE.Iso_639}";
            var message = new SqlResourceDAO.Message
            {
                Uri = uri,
                Response = Encoding.UTF8.GetBytes("1"), // This value is different than what is expected from the mock service
                ETag = "W/" + MockHandler.DEFAULT_ETAG, // Etag should match
                ExpiresAt = DateTimeOffset.Now.Subtract(new TimeSpan(1, 0, 0)) // Message is stale
            };
            await dao.InsertMessage(message);



            var request = await chain.TryGet<TimeSpan>(uii);
            await Connection.WaitSettledAsync();
            var newMessage = await dao.GetMessage(uri);



            Assert.AreEqual(1, WebHistory.Count);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(TimeSpan.FromMinutes(1), request.Value);

            // Etags should match, expiresAt should be updated to indicate resource was refreshed
            Assert.AreNotEqual(message.ExpiresAt, newMessage.ExpiresAt);
            Assert.AreEqual(message.ETag, newMessage.ETag);
            // Response should not be changed from value we inserted
            Assert.AreEqual(Encoding.UTF8.GetString(message.Response.ToArray()), Encoding.UTF8.GetString(newMessage.Response.ToArray()));
        }

        [TestMethod]
        public async Task RetrieveStaleResourceEtagDoesNotMatch()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var chain = handlers.Chain;

            var uii = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var uri = uii.ToString() + $"?{TMDbRequest.LANGUAGE_PARAMETER_KEY}={TMDB.LANGUAGE.Iso_639}";
            var message = new SqlResourceDAO.Message
            {
                Uri = uri,
                Response = Encoding.UTF8.GetBytes("1"), // This value is different than what is expected from TMDb
                ETag = "\"non matching etag\"", // Etag should not match
                ExpiresAt = DateTimeOffset.Now.Subtract(new TimeSpan(1, 0, 0)) // Message is stale
            };
            await dao.InsertMessage(message);



            var request = await chain.TryGet<TimeSpan>(uii);
            await Connection.WaitSettledAsync();
            var newMessage = await dao.GetMessage(uri);



            Assert.AreEqual(1, WebHistory.Count);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(TimeSpan.FromMinutes(130), request.Value);

            // Etags should not match, expiresAt should be updated to indicate resource was refreshed
            Assert.AreNotEqual(message.ExpiresAt, newMessage.ExpiresAt);
            Assert.AreEqual("W/" + MockHandler.DEFAULT_ETAG, newMessage.ETag);
            // Response should be updated with value from API
            Assert.AreEqual("130", Encoding.UTF8.GetString(newMessage.Response.ToArray()));
        }

        [TestMethod]
        public async Task ResourcesCanBeExpired()
        {
            var dao = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);
            var handlers = new HandlerChain(diskCache: dao);
            var movie1 = new Movie("other movie").WithID(TMDB.IDKey, 1);

            await handlers.Chain.Get(CreateRequests(Constants.Movie, ALL_MOVIE_PROPERTIES).ToArray());
            var between = DateTime.Now;
            await handlers.Chain.Get(CreateRequests(movie1, ALL_MOVIE_PROPERTIES).ToArray());
            await Connection.WaitSettledAsync();

            var modified = await dao.Expire(DateTime.Now - between);
            var message0 = await dao.GetMessage($"{new UniformItemIdentifier(Constants.Movie, Media.TITLE).ToString()}?{TMDbRequest.LANGUAGE_PARAMETER_KEY}={TMDB.LANGUAGE.Iso_639}");
            var message1 = await dao.GetMessage($"{new UniformItemIdentifier(movie1, Media.TITLE).ToString()}?{TMDbRequest.LANGUAGE_PARAMETER_KEY}={TMDB.LANGUAGE.Iso_639}");

            Assert.IsTrue(modified > 0);
            Assert.IsNull(message0);
            Assert.IsNotNull(message1);
        }

        [TestMethod]
        public async Task GetExternalIds()
        {
            var handlers = new HandlerChain();

            var movie = await TMDB.WithExternalIdsAsync(Constants.Movie, 0);
            await CachedAsync(handlers.DiskCache);

            Assert.AreEqual(1, WebHistory.Count);

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

        private Task SeedCaches(HandlerChain handlers, bool memory, bool local, params KeyValueRequestArgs<Uri>[] args) => SeedCaches(handlers, args, memory, local);
        private async Task SeedCaches(HandlerChain handlers, IEnumerable<KeyValueRequestArgs<Uri>> args, bool memory = false, bool local = false)
        {
            // Request data from API so it caches at each layer, then clear not requested layers
            await handlers.Chain.Get(args);
            await Connection.WaitSettledAsync();
            ClearCaches();

            if (!memory)
            {
                handlers.MemoryCache.Clear();
            }

            if (!local)
            {
                await EmptyDb();
            }
        }

        // Make resources stale
        private async Task ExpireResources()
        {
            await Connection.SqlConnection.ExecuteAsync($"update {SqlResourceDAO.RESOURCE_TABLE_NAME} set {nameof(SqlResourceDAO.Message.ExpiresAt)} = 0");
        }

        private static readonly Property[] ALL_MOVIE_PROPERTIES = new Property[] { Media.BACKDROP_PATH, Movie.BUDGET, Media.CAST, Movie.CONTENT_RATING, Media.CREW, Media.DESCRIPTION, Movie.GENRES, Media.KEYWORDS, Media.LANGUAGES, Media.ORIGINAL_LANGUAGE, Media.ORIGINAL_TITLE, Movie.PARENT_COLLECTION, Media.POSTER_PATH, Media.PRODUCTION_COMPANIES, Media.PRODUCTION_COUNTRIES, Media.RATING, Media.RECOMMENDED, Movie.RELEASE_DATE, Movie.REVENUE, Media.RUNTIME, Media.TAGLINE, Media.TITLE, Media.TRAILER_PATH, Movie.WATCH_PROVIDERS };

        private static readonly Property[] ALL_TVSHOW_PROPERTIES = new Property[] { Media.BACKDROP_PATH, Media.CAST, TVShow.CONTENT_RATING, Media.CREW, Media.DESCRIPTION, TVShow.FIRST_AIR_DATE, TVShow.GENRES, Media.KEYWORDS, Media.LANGUAGES, TVShow.LAST_AIR_DATE, TVShow.NETWORKS, Media.ORIGINAL_LANGUAGE, Media.ORIGINAL_TITLE, Media.POSTER_PATH, Media.PRODUCTION_COMPANIES, Media.PRODUCTION_COUNTRIES, Media.RATING, Media.RECOMMENDED, Media.RUNTIME, Media.TAGLINE, Media.TITLE, Media.TRAILER_PATH, TVShow.WATCH_PROVIDERS };

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

        private static async Task<List<T>> TryReadAll<T>(IAsyncEnumerable<T> source)
        {
            var result = new List<T>();

            try
            {
                await foreach (var item in source)
                {
                    result.Add(item);
                }
            }
            catch { }

            return result;
        }
    }
}
