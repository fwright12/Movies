namespace MoviesTests.Data
{
    public class TVShowResourceTests : ItemResourceTests
    {
        public TVShowResourceTests(IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> processorFactory) : base(processorFactory) { }

        [TestMethod]
        public Task RetrieveBackdropPath() => RetrieveResource(Media.BACKDROP_PATH);

        [TestMethod]
        public Task RetrieveCast() => RetrieveResource(Media.CAST);

        [TestMethod]
        public Task RetrieveContentRating() => RetrieveResource(TVShow.CONTENT_RATING);

        [TestMethod]
        public Task RetrieveCrew() => RetrieveResource(Media.CREW);

        [TestMethod]
        public Task RetrieveDescription() => RetrieveResource(Media.DESCRIPTION);

        [TestMethod]
        public Task RetrieveFirstAirDate() => RetrieveResource(TVShow.FIRST_AIR_DATE);

        [TestMethod]
        public Task RetrieveGenres() => RetrieveResource(TVShow.GENRES);

        [TestMethod]
        public Task RetrieveKeywords() => RetrieveResource(Media.KEYWORDS);

        [TestMethod]
        public Task RetrieveLanguages() => RetrieveResource(Media.LANGUAGES);

        [TestMethod]
        public Task RetrieveLastAirDate() => RetrieveResource(TVShow.LAST_AIR_DATE);

        [TestMethod]
        public Task RetrieveNetworks() => RetrieveResource(TVShow.NETWORKS);

        [TestMethod]
        public Task RetrieveOriginalLanguage() => RetrieveResource(Media.ORIGINAL_LANGUAGE);

        [TestMethod]
        public Task RetrieveOriginalTitle() => RetrieveResource(Media.ORIGINAL_TITLE);

        [TestMethod]
        public Task RetrievePosterPath() => RetrieveResource(Media.POSTER_PATH);

        [TestMethod]
        public Task RetrieveProductionCompanies() => RetrieveResource(Media.PRODUCTION_COMPANIES);

        [TestMethod]
        public Task RetrieveProductionCountries() => RetrieveResource(Media.PRODUCTION_COUNTRIES);

        [TestMethod]
        public Task RetrieveRating() => RetrieveResource(Media.RATING);

        [TestMethod]
        public Task RetrieveRecommended() => RetrieveResource(Media.RECOMMENDED);

        [TestMethod]
        public Task RetrieveRuntime() => RetrieveResource(Media.RUNTIME);

        [TestMethod]
        public Task RetrieveSeasons() => RetrieveResource(TVShow.SEASONS);

        [TestMethod]
        public Task RetrieveTagline() => RetrieveResource(Media.TAGLINE);

        [TestMethod]
        public Task RetrieveTitle() => RetrieveResource(Media.TITLE);

        [TestMethod]
        public Task RetrieveTrailerPath() => RetrieveResource(Media.TRAILER_PATH);

        [TestMethod]
        public Task RetrieveWatchProviders() => RetrieveResource(TVShow.WATCH_PROVIDERS);

        private Task RetrieveResource<T>(Property<T> property) => RetrieveResource(Constants.TVShow, property);
        private Task RetrieveResource<T>(MultiProperty<T> property) => RetrieveResource(Constants.TVShow, property);
    }
}
