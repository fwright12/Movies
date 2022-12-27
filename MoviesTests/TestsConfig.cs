namespace MoviesTests
{
    [TestClass]
    public class TestsConfig
    {
        public static TMDB TMDb { get; private set; }

        [AssemblyInitialize]
        public static void ConfigureHttp(TestContext context)
        {
            TMDb = new TMDB(string.Empty, string.Empty, new DummyJsonCache());

#if DEBUG
            DebugConfig.AllowLiveRequests = false;
            DebugConfig.BreakOnRequest = false;
            DebugConfig.SimulatedDelay = 1000;
#endif
        }
    }
}
