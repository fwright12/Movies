namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class TMDbClientTests : Resources
    {
        private readonly TMDbHttpProcessor TMDbReadHandler;
        private ChainLink<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> Chain => AsyncCoRExtensions.Create<IEnumerable<ResourceRequestArgs<Uri>>>(TMDbReadHandler);
        
        public TMDbClientTests()
        {
            var resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);
            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(new MockHandler())));

            TMDbReadHandler = new TMDbHttpProcessor(invoker, resolver);
        }

        [TestInitialize]
        public void ClearCallHistory()
        {
            DebugConfig.SimulatedDelay = 0;
            WebHistory.Clear();
        }

        [TestMethod]
        public async Task SingleDetailsRequest()
        {
            var url = "3/movie/0?language=en-US";
            var resource = await Chain.Get(url);

            Assert.IsTrue(resource.IsHandled);
            Assert.AreEqual(1, WebHistory.Count);
            Assert.AreEqual("3/movie/0?language=en-US", WebHistory.Last());
        }

        [TestMethod]
        public async Task SingleAppendedRequestDetailsFirst()
        {
            var urls = new string[]
            {
                "3/movie/0?language=en-US",
                "3/movie/0/keywords",
                "3/movie/0/recommendations?language=en-US",
            };
            var args = await Chain.Get(urls);

            AssertHandled(args);
            Assert.AreEqual(1, WebHistory.Count);

            var parts = WebHistory.Last().Split('?');
            var path = parts[0];
            var query = parts[1];
            Assert.AreEqual("3/movie/0", path);
            Assert.IsTrue(query.Contains("language=en-US"));
            Assert.IsTrue(query.Contains($"{Constants.APPEND_TO_RESPONSE}=keywords,recommendations"));
        }

        [TestMethod]
        public async Task SingleAppendedRequestAnyOrder()
        {
            var urls = new List<string>
            {
                "3/movie/0?language=en-US",
                "3/movie/0/keywords",
                "3/movie/0/recommendations?language=en-US",
            };

            for (int i = 0; i < 3; i++)
            {
                WebHistory.Clear();
                var args = await Chain.Get(urls.ToArray());

                AssertHandled(args);
                Assert.AreEqual(1, WebHistory.Count, "\n" + string.Join('\n', urls));

                urls.Add(urls.First());
                urls.RemoveAt(0);
            }
        }

        [TestMethod]
        public async Task MultipleRequests()
        {
            var urls = new string[]
            {
                "3/movie/0?language=en-US",
                "3/tv/0/keywords"
            };
            var args = await Chain.Get(urls);

            AssertHandled(args);
            Assert.AreEqual(2, WebHistory.Count);
            Assert.AreEqual("3/movie/0?language=en-US", WebHistory.SkipLast(1).Last());
            Assert.AreEqual("3/tv/0/keywords", WebHistory.Last());
        }

        [TestMethod]
        public async Task MultipleAppendedRequests()
        {
            var urls = new string[]
            {
                "3/movie/0?language=en-US",
                "3/person/0?language=en-US",
                "3/tv/0/watch/providers",
                "3/movie/0/keywords",
                "3/movie/1/keywords",
                "3/tv/0?language=en-US",
                "3/movie/0/recommendations?language=en-US",
            };
            var args = await Chain.Get(urls);

            AssertHandled(args);
            Assert.AreEqual(4, WebHistory.Count);
        }

        [TestMethod]
        public async Task ApiRequestsBuffered()
        {
            DebugConfig.SimulatedDelay = 100;

            var first = Chain.Get("3/tv/0?language=en-US&append_to_response=aggregate_credits,content_ratings,keywords,recommendations,videos,watch/providers");
            var second = Chain.Get("3/tv/0?language=en-US&append_to_response=keywords,watch/providers");
            await Task.WhenAll(first, second);

            Assert.AreEqual(1, WebHistory.Count);
        }

        private void AssertHandled(params ResourceRequestArgs<Uri>[] e) => Assert.IsTrue(e.All(arg => arg.IsHandled), "The following uris where not handled:\n" + string.Join('\n', e.Where(arg => !arg.IsHandled).Select(arg => arg.Request.Key.ToString())));
    }
}
