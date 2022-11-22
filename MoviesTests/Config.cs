namespace MoviesTests
{
    [TestClass]
    public class Config
    {
        public static TMDB TMDb { get; private set; }

        [AssemblyInitialize]
        public static void ConfigureHttp(TestContext context)
        {
#if DEBUG
            Movies.HttpClient.AllowLiveRequests = false;
            Movies.HttpClient.BreakOnRequest = false;
            Movies.HttpClient.SimulatedDelay = 1000;
#endif

            TMDb = new TMDB(string.Empty, string.Empty, new DummyJsonCache());
        }
    }
}
