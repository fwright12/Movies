using System.Text;

namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class ResourceTests : Resources
    {
        private const string MOVIE_APPENDED_URL = $"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";

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

            Assert.AreEqual(MOVIE_APPENDED_URL, handlers.WebHistory.Last());
            Assert.AreEqual(1, handlers.WebHistory.Count);
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

        public class HandlerChain
        {
            public readonly TMDbResolver Resolver;

            public List<string> WebHistory => MockHttpHandler.LocalCallHistory;

            public TMDbHttpProcessor RemoteTMDbProcessor { get; }
            public MockHandler MockHttpHandler { get; }

            public TMDbService TMDbService { get; }

            public HandlerChain()
            {
                Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

                var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
                RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);
            }
        }
    }
}
