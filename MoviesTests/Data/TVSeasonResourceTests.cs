namespace MoviesTests.Data
{
    public class TVSeasonResourceTests : ItemResourceTests
    {
        public TVSeasonResourceTests(IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> processorFactory) : base(processorFactory) { }

        [TestMethod]
        public Task RetrieveCast() => RetrieveResource(TVSeason.CAST);

        [TestMethod]
        public Task RetrieveCrew() => RetrieveResource(TVSeason.CREW);

        [TestMethod]
        public Task RetrieveEpisodes() => RetrieveResource(TVSeason.EPISODES);

        [TestMethod]
        public Task RetrieveYear() => RetrieveResource(TVSeason.YEAR);

        private Task RetrieveResource<T>(Property<T> property) => RetrieveResource(Constants.TVSeason, property);
        private Task RetrieveResource<T>(MultiProperty<T> property) => RetrieveResource(Constants.TVSeason, property);
    }
}
