namespace MoviesTests.Data.Local
{
    [TestClass]
    public class ResourceTests : Resources
    {
        private static readonly string DATABASE_FILENAME = "MovieResourceTests.db";

        private static dBConnection Connection = new dBConnection(DATABASE_FILENAME);

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            var chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(Connection.PersistenceService.Processor).ToChainLink();
            chain.SetNext(new HandlerChain().RemoteTMDbProcessor);

            // Seed database with cached data from dummy web service
            var requests = new UniformItemIdentifier[]
            {
                new UniformItemIdentifier(Constants.Movie, Media.RUNTIME),
                new UniformItemIdentifier(Constants.TVShow, Media.RUNTIME),
            }.Select(uii => chain.Get(new KeyValueRequestArgs<Uri>(uii).AsEnumerable()));
            await Task.WhenAll(requests);
            //await chain.Get(requests);

            await Connection.WaitSettledAsync();
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            await Connection.CloseAsync();
        }

        [TestMethod]
        public async Task GetMovieProperty()
        {
            var value = await GetResource<TimeSpan?>(Constants.Movie, Media.RUNTIME);
            Assert.AreEqual(new TimeSpan(2, 10, 0), value);
        }

        [TestMethod]
        public async Task GetTVProperty()
        {
            var value = await GetResource<DateTime?>(Constants.TVShow, TVShow.LAST_AIR_DATE);
            Assert.AreEqual(new DateTime(2013, 5, 16), value);
        }

        private Task<T> GetResource<T>(Item item, Property property) => GetResource<T>(new UniformItemIdentifier(item, property));
        private async Task<T> GetResource<T>(UniformItemIdentifier uii)
        {
            var handlers = new HandlerChain();

            var request = new KeyValueRequestArgs<Uri, T>(uii);
            await Connection.PersistenceService.Processor.ProcessAsync(request.AsEnumerable(), null);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            return request.Value;
        }

        private static Task CachedAsync(DummyDatastore<Uri> diskCache)
        {
            return Task.Delay(Math.Max(diskCache.ReadLatency, diskCache.WriteLatency) * 2);
        }
    }
}
