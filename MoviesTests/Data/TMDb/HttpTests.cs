namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class TMDbHttpTests
    {
        private List<string> CallHistory => MockHandler.CallHistory;
        private HttpMessageInvoker Invoker { get; }

        public TMDbHttpTests()
        {
            Invoker = new HttpMessageInvoker(new BufferedHandler(new MockHandler()));
        }

        [TestInitialize]
        public void ClearCallHistory()
        {
            CallHistory.Clear();
        }

        [TestMethod]
        public async Task SingleDetailsRequest()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "3/movie/0?language=en-US");
            var response = await Invoker.SendAsync(message, default);

            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.AreEqual(1, CallHistory.Count);
            Assert.AreEqual("3/movie/0?language=en-US", CallHistory.Last());
        }

        [TestMethod]
        public async Task BufferedGetRequests()
        {
            var backup = DebugConfig.SimulatedDelay;
            DebugConfig.SimulatedDelay = 100;

            var first = new HttpRequestMessage(HttpMethod.Get, "3/movie/0?language=en-US");
            var second = new HttpRequestMessage(HttpMethod.Get, "3/movie/0?language=en-US");

            var tasks = new List<Task>
            {
                Invoker.SendAsync(first, default),
                Invoker.SendAsync(second, default)
            };
            await Task.WhenAll(tasks);

            Assert.AreEqual(1, CallHistory.Count);
            DebugConfig.SimulatedDelay = backup;
        }
    }
}
