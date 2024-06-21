using Movies.Data.Local;

namespace MoviesTests
{
    [TestClass]
    public static class TestsConfig
    {
        public static TMDB TMDb { get; private set; }
        public static Movies.IAsyncCollection<string> ChangeKeys { get; private set; }

        public static TMDbResolver Resolver { get; } = new TMDbResolver(TMDB.ITEM_PROPERTIES);

        [AssemblyInitialize]
        public static void ConfigureHttp(TestContext context)
        {
            TMDb = new TMDB(string.Empty, string.Empty, new DummyCache());
            ChangeKeys = new HashSetAsyncWrapper<string>(Task.FromResult(Enumerable.Empty<string>()));

            DataService.Register(new InMemoryService(DataService.Instance.ResourceCache));
            DataService.Register(new PersistenceService(new TMDbLocalCache(new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50,
            }, Resolver)));
            DataService.Register(new TMDbService(new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(new MockHandler()))), Resolver));

#if DEBUG
            DebugConfig.LogWebRequests = true;

            DebugConfig.UseLiveRequestsFor.Clear();
            DebugConfig.AllowLiveRequests = false;
            DebugConfig.AllowTMDbRequests = false;
            DebugConfig.AllowTMDbImages = false;

            DebugConfig.SimulatedDelay = 1000;
#endif
        }
    }

    public class Resources
    {
        protected List<string> WebHistory => MockHandler.CallHistory;
    }
}
