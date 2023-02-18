using HttpClient = Movies.HttpClient;

namespace MoviesTests.Data
{
    [TestClass]
    public class ResourceTests
    {
        private Controller Resources { get; }

        public ResourceTests()
        {
            //var tMDbResources = new TMDbResources(null);
            var resourceCache = new ResourceCache();
            //Resources = new Controller().SetNext(resourceCache).SetNext(tMDbResources);

            resourceCache.Post(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Task.FromResult(Constants.TAGLINE));
            resourceCache.Post(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), Task.FromResult(Constants.LANGUAGE));
        }

        [TestMethod]
        public async Task SingleResource()
        {
            HttpClient.CallHistory.Clear();

            // Retrieve an item in the in memory cache
            var response = await Resources.Get<string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE));
            Assert.IsTrue(response.Success);
            Assert.AreEqual(Constants.TAGLINE, response.Resource);
            Assert.AreEqual(0, HttpClient.CallHistory.Count);

            // Retrieve an item from the web
            var response1 = await Resources.Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            Assert.IsTrue(response1.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response1.Resource);
            Assert.AreEqual(1, HttpClient.CallHistory.Count);

            // Make sure the just retrieved item is cached in memory
            HttpClient.CallHistory.Clear();
            response1 = await Resources.Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            Assert.IsTrue(response1.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response1.Resource);
            Assert.AreEqual(0, HttpClient.CallHistory.Count);
        }
    }

    public static class DataHelpers
    {
        public static Task<(bool Success, HttpContent Resource)> Get(this ControllerLink link, string url) => Get(link, new Uri(url, UriKind.Relative));
        public static Task<(bool Success, HttpContent Resource)> Get(this ControllerLink link, Uri uri)
        {
            var controller = new Controller().SetNext(link);
            return controller.Get(uri);
        }

        public static Task<(bool Success, T Resource)> Get<T>(this ControllerLink link, string url) => Get<T>(link, new Uri(url, UriKind.Relative));
        public static Task<(bool Success, T Resource)> Get<T>(this ControllerLink link, Uri uri)
        {
            var controller = new Controller().SetNext(link);
            return controller.Get<T>(uri);
        }
    }
}
