namespace MoviesTests.Data
{
    public class TVEpisodeResourceTests : ItemResourceTests
    {
        public TVEpisodeResourceTests(IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> processorFactory) : base(processorFactory) { }

        [TestMethod]
        public Task RetrieveAirdate() => RetrieveResource(TVEpisode.AIR_DATE);

        [TestMethod]
        public Task RetrieveCast() => RetrieveResource(Media.CAST);

        [TestMethod]
        public Task RetrieveCrew() => RetrieveResource(Media.CREW);

        [TestMethod]
        public Task RetrieveDescription() => RetrieveResource(Media.DESCRIPTION);

        [TestMethod]
        public Task RetrievePosterPath() => RetrieveResource(Media.POSTER_PATH);

        [TestMethod]
        public Task RetrieveRating() => RetrieveResource(Media.RATING);

        [TestMethod]
        public Task RetrieveRuntime() => RetrieveResource(Media.RUNTIME);

        [TestMethod]
        public Task RetrieveTitle() => RetrieveResource(Media.TITLE);

        private Task RetrieveResource<T>(Property<T> property) => RetrieveResource(Constants.TVEpisode, property);
        private Task RetrieveResource<T>(MultiProperty<T> property) => RetrieveResource(Constants.TVEpisode, property);
    }
}
