namespace MoviesTests
{
    [TestClass]
    public static class TestsConfig
    {
        public static TMDB TMDb { get; private set; }
        public static Movies.IAsyncCollection<string> ChangeKeys { get; private set; }

        [AssemblyInitialize]
        public static void ConfigureHttp(TestContext context)
        {
            TMDb = new TMDB(string.Empty, string.Empty, new DummyJsonCache());
            ChangeKeys = new HashSetAsyncWrapper<string>(GetChangeKeys());

#if DEBUG
            DebugConfig.AllowLiveRequests = false;
            DebugConfig.BreakOnRequest = false;
            DebugConfig.SimulatedDelay = 1000;
#endif
        }

        private static async Task<IEnumerable<string>> GetChangeKeys()
        {
            await TMDb.LoadChangeKeys;
            return TMDb.ChangeKeys;
        }
    }
}
