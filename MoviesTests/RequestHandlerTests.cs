namespace MoviesTests
{
    [TestClass]
    public class RequestHandlerTests
    {
        public RequestHandlerTests()
        {
#if DEBUG
            Movies.HttpClient.AllowLiveRequests = false;
            Movies.HttpClient.BreakOnRequest = false;
            Movies.HttpClient.SimulatedDelay = 1000;
#endif
            var tmdb = new TMDB(string.Empty, string.Empty, new DummyJsonCache());
        }

        [TestMethod]
        public Task MovieChangeKeysTests() => ChangeKeysTest(ItemType.Movie);

        [TestMethod]
        public Task TVShowChangeKeysTests() => ChangeKeysTest(ItemType.TVShow);

        [TestMethod]
        public Task PersonChangeKeysTests() => ChangeKeysTest(ItemType.Person);

        private async Task ChangeKeysTest(ItemType type)
        {
            if (!TMDB.ITEM_PROPERTIES.TryGetValue(type, out var properties))
            {
                Assert.Fail("Couldn't find associated ItemProperties for " + type );
            }
            var handler = new ItemProperties.RequestHandler(properties, new Movie("movie"));

            await properties.ChangeKeysLoaded;

            foreach (var kvp in properties.Info)
            {
                foreach (var parser in kvp.Value)
                {
                    Assert.IsTrue(ItemProperties.NO_CHANGE_KEY.Contains(parser.Property) ^ handler.IsCacheValid(parser.Property), parser.Property.ToString());
                }
            }
        }
    }
}
