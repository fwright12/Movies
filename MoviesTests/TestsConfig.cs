﻿namespace MoviesTests
{
    [TestClass]
    public static class TestsConfig
    {
        public static TMDB TMDb { get; private set; }
        public static Movies.IAsyncCollection<string> ChangeKeys { get; private set; }

        [AssemblyInitialize]
        public static void ConfigureHttp(TestContext context)
        {
            TMDb = new TMDB(string.Empty, string.Empty, new DummyCache());
            ChangeKeys = new HashSetAsyncWrapper<string>(Task.FromResult(Enumerable.Empty<string>()));

#if DEBUG
            DebugConfig.AllowLiveRequests = false;
            DebugConfig.SimulatedDelay = 1000;
            DebugConfig.LogWebRequests = true;
#endif
        }
    }

    public class Resources
    {
        protected List<string> WebHistory => MockHandler.CallHistory;
    }
}
