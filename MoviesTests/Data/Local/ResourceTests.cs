namespace MoviesTests.Data.Local
{
    [TestClass]
    public class ResourceTests
    {
        private ControllerLink Controller => ResourceCache;
        private ResourceCache ResourceCache { get; }

        public ResourceTests()
        {
            ResourceCache = new ResourceCache();

            ResourceCache.Post(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Task.FromResult(Constants.TAGLINE));
            ResourceCache.Post(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), Task.FromResult(Constants.LANGUAGE));
        }

        [TestMethod]
        public async Task SingleCachedResource()
        {
            var uii = new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE);
            var response = await Controller.Get<Language>(uii);
            Assert.IsTrue(response.Success);
            Assert.AreEqual(Constants.LANGUAGE, response.Resource);
        }

        [TestMethod]
        public async Task SingleUncachedResource()
        {
            var response1 = await Controller.Get<IEnumerable<Keyword>>(new UniformItemIdentifier(Constants.Movie, Media.KEYWORDS));
            Assert.IsFalse(response1.Success);
        }
    }
}
