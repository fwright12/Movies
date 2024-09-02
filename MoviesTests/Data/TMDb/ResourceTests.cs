using System.Text;

namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class MovieResourceTests : ResourceTests
    {
        public MovieResourceTests() : base(ResourceUtils.MOVIE_URL_APPENDED) { }

        [TestMethod]
        public async Task RetreiveRuntime()
        {
            var response = await AssertRestResponse(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME, language: TMDB.LANGUAGE));
            AssertRepresentation(response, new TimeSpan(2, 10, 0));
            AssertByteRepresentation(response, "130");
        }

        [TestMethod]
        public async Task RetreiveTagline()
        {
            var response = await AssertRestResponse(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE, language: TMDB.LANGUAGE));
            AssertRepresentation(response, "It all ends.");
            AssertByteRepresentation(response, "\"It all ends.\"");
        }
    }

    [TestClass]
    public class TVShowResourceTests : ResourceTests
    {
        public TVShowResourceTests() : base(ResourceUtils.TV_URL_APPENDED) { }

        [TestMethod]
        public async Task RetreiveLastAirDate()
        {
            var response = await AssertRestResponse(new UniformItemIdentifier(Constants.TVShow, TVShow.LAST_AIR_DATE));
            AssertRepresentation<DateTime?>(response, new DateTime(2013, 5, 16));
            AssertByteRepresentation(response, "130");
        }
    }

    public class ResourceTests : Resources
    {
        private readonly TMDbResolver Resolver;

        protected TMDbHttpProcessor RemoteTMDbProcessor { get; }
        protected MockHandler MockHttpHandler { get; }

        protected string ExpectedEndpoint { get; }

        public ResourceTests(string expectedEndpoint)
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
            RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);
            ExpectedEndpoint = expectedEndpoint;
        }

        [TestInitialize]
        public void Reset()
        {
            DebugConfig.SimulatedDelay = 100;
            //DiskCache.SimulatedDelay = 50;

            WebHistory.Clear();
            //InMemoryCache.Clear();
            //DiskCache.Clear();
            TMDB.CollectionCache.Clear();
        }

        [TestMethod]
        public async Task GetMovieResource()
        {
            var handlers = new HandlerChain();

            var arg = new KeyValueRequestArgs<Uri, TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            await handlers.RemoteTMDbProcessor.ProcessAsync(arg.AsEnumerable());

            Assert.IsTrue(arg.IsHandled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), arg.Value);

            Assert.AreEqual(ExpectedEndpoint, handlers.WebHistory.Last());
            Assert.AreEqual(1, handlers.WebHistory.Count);
        }

        protected async Task<RestResponse> AssertRestResponse(Uri uri)
        {
            var arg = new KeyValueRequestArgs<Uri, TimeSpan>(uri);
            await RemoteTMDbProcessor.ProcessAsync(arg.AsEnumerable());

            Assert.IsTrue(arg.IsHandled);

            var restResponse = arg.Response as RestResponse;

            Assert.IsNotNull(restResponse, $"Response {arg.Response?.GetType()} is not a rest response");
            Assert.AreEqual(ExpectedEndpoint, WebHistory.Last());
            Assert.AreEqual(1, WebHistory.Count);

            return restResponse;
        }

        protected void AssertByteRepresentation(RestResponse response, string str)
        {
            Assert.IsTrue(response.TryGetRepresentation<IEnumerable<byte>>(out var bytes));
            Assert.AreEqual(str, Encoding.UTF8.GetString(bytes.ToArray()));
        }

        protected void AssertRepresentation<T>(RestResponse response, T expected)
        {
            Assert.IsTrue(response.TryGetRepresentation<T>(out var value), $"Could not get representation of type {typeof(T)}");
            Assert.AreEqual(expected, value);
        }

        [TestMethod]
        public async Task AllResources()
        {
            var handlers = new HandlerChain();

            var arg = new KeyValueRequestArgs<Uri, TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));
            var args = new BulkEventArgs<KeyValueRequestArgs<Uri>>(arg);
            await handlers.RemoteTMDbProcessor.ProcessAsync(args);

            var urls = args.Where(arg => arg.Request.Key is UniformItemIdentifier == false).ToArray();
            var urns = args.Where(arg => arg.Request.Key is UniformItemIdentifier).ToArray();

            Assert.AreEqual(8, urls.Length);
            Assert.AreEqual(25, urns.Length);

            var values = urns.ToDictionary(arg => arg.Request.Key.ToString(), arg => (arg.Response as ResourceResponse)?.TryGetRepresentation<IEnumerable<byte>>(out var bytes) == true ? Encoding.UTF8.GetString(bytes.ToArray()) : "");
            foreach (var kvp in values)
            {
                Print.Log($"\t{kvp.Key}: {kvp.Value}");
            }
        }
    }
}
