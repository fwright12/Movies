namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class MovieResourceTests : Data.MovieResourceTests
    {
        public MovieResourceTests() : base(new TMDbProcessorFactory()) { }
    }

    [TestClass]
    public class TVShowResourceTests : Data.TVShowResourceTests
    {
        private static readonly TVShow STILL_RUNNING_SHOW = new TVShow("still running show").WithID(TMDB.IDKey, 1);

        public TVShowResourceTests() : base(new TMDbProcessorFactory()) { }

        [TestMethod]
        public void RetrieveStillRunningShowLastAirDate()
        {
            Assert.Inconclusive("Code needs to be refactored to be testable to check this. Until then, this should be verified manually");
        }
    }

    [TestClass]
    public class TVSeasonResourceTests : Data.TVSeasonResourceTests
    {
        public TVSeasonResourceTests() : base(new TMDbProcessorFactory()) { }
    }

    [TestClass]
    public class TVEpisodeResourceTests : Data.TVEpisodeResourceTests
    {
        public TVEpisodeResourceTests() : base(new TMDbProcessorFactory()) { }
    }

    [TestClass]
    public class PersonResourceTests : Data.PersonResourceTests
    {
        public PersonResourceTests() : base(new TMDbProcessorFactory()) { }
    }

    internal class TMDbProcessorFactory : IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>>
    {
        public IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> Create()
        {
            var processor = ServiceFactory.CreateMockTMDb(out var handler).Processor;
            handler.SimulatedDelay = 0;

            return processor;
        }
    }
}
