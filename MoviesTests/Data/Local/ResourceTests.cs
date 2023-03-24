namespace MoviesTests.Data.Local
{
    [TestClass]
    public class ResourceTests
    {
        private IControllerLink Controller => ResourceCache;
        private ResourceCache ResourceCache { get; }
        private Task SeedTask { get; }

        public ResourceTests()
        {
            ResourceCache = new ResourceCache();
            SeedTask = Seed();
        }

        [TestInitialize]
        public async Task WaitSeed()
        {
            await SeedTask;
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

        private async Task Seed()
        {
            await ResourceCache.PutAsync(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Constants.TAGLINE);
            await ResourceCache.PutAsync(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), Constants.LANGUAGE);
        }
    }
}
