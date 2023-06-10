namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class TMDbClientTests
    {
        private List<string> CallHistory => MockHandler.CallHistory;
        private Controller Controller { get; }
        private TMDbClient Client { get; }

        public TMDbClientTests()
        {
            Client = new TMDbClient(new BufferedHandler(new TMDbBufferedHandler(new MockHandler())), new TMDbResolver(TMDB.ITEM_PROPERTIES));
            Controller = new Controller().AddLast(Client);
        }

        [TestInitialize]
        public void ClearCallHistory()
        {
            CallHistory.Clear();
        }

        [TestMethod]
        public async Task SingleDetailsRequest()
        {
            var url = "3/movie/0?language=en-US";
            var resource = await Controller.Get(url);

            Assert.IsTrue(resource.Handled);
            Assert.AreEqual(1, CallHistory.Count);
            Assert.AreEqual("3/movie/0?language=en-US", CallHistory.Last());
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
            var args = await Controller.Get(urls);

            AssertHandled(args);
            Assert.AreEqual(1, CallHistory.Count);

            var parts = CallHistory.Last().Split('?');
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
                CallHistory.Clear();
                var args = await Controller.Get(urls.ToArray());

                AssertHandled(args);
                Assert.AreEqual(1, CallHistory.Count, "\n" + string.Join('\n', urls));

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
            var args = await Controller.Get(urls);

            AssertHandled(args);
            Assert.AreEqual(2, CallHistory.Count);
            Assert.AreEqual("3/movie/0?language=en-US", CallHistory.SkipLast(1).Last());
            Assert.AreEqual("3/tv/0/keywords", CallHistory.Last());
        }

        [TestMethod]
        public async Task MultipleAppendedRequests()
        {
            var urls = new string[]
            {
                "3/movie/0?language=en-US",
                "3/person/0?language=en-US",
                "3/tv/0/watch/providers?region=US",
                "3/movie/0/keywords",
                "3/movie/1/keywords",
                "3/tv/0?language=en-US",
                "3/movie/0/recommendations?language=en-US",
            };
            var args = await Controller.Get(urls);

            AssertHandled(args);
            Assert.AreEqual(4, CallHistory.Count);
        }

        [TestMethod]
        public async Task ApiRequestsBuffered()
        {
            await Task.WhenAll(new List<Task>
            {
                Controller.Get("3/tv/0?language=en-US&append_to_response=aggregate_credits,content_ratings,keywords,recommendations,videos,watch/providers"),
                Controller.Get("3/tv/0?language=en-US&append_to_response=keywords,watch/providers")
            });

            Assert.AreEqual(1, CallHistory.Count);
        }

        private void AssertHandled(params RestRequestArgs[] e) => Assert.IsTrue(e.All(arg => arg.Handled), "The following uris where not handled:\n" + string.Join('\n', e.Where(arg => !arg.Handled).Select(arg => arg.Uri.ToString())));
    }
}
