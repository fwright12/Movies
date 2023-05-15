using Movies;

namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class TMDbControllerTests
    {
        public TMDbController Controller { get; }
        public TMDbResolver Resolver { get; }

        public TMDbControllerTests()
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            var TMDbRemoteHandler = new BufferedHandler(new InvokerHandlerWrapper(TMDB.WebClient));

            Controller = new TMDbController(new HttpMessageInvoker(TMDbRemoteHandler), Resolver, TMDbApi.AutoAppend);
        }

        [TestMethod]
        public async Task RetrieveProperty()
        {
            var uii = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var e = new RestArgs(new RestRequest(HttpMethod.Get, uii));
            
            await Controller.Send(e);
            Assert.IsTrue(e.Handled);

            var response = await e.Response;
            Assert.IsTrue(response.Body.TryGetRepresentation<TimeSpan>(out var runtime));
            Assert.AreEqual(new TimeSpan(2, 10, 0), runtime);
        }

        [TestMethod]
        public async Task RetrieveAppendedProperty()
        {
            var uii = new UniformItemIdentifier(Constants.Movie, Movie.WATCH_PROVIDERS);
            var e = new RestArgs(new RestRequest(HttpMethod.Get, uii));

            await Controller.Send(e);
            Assert.IsTrue(e.Handled);

            var response = await e.Response;
            Assert.IsTrue(response.Body.TryGetRepresentation<IEnumerable<WatchProvider>>(out var watchProviders));
            Assert.AreEqual("Apple iTunes", watchProviders.First().Company.Name);
        }

        [TestMethod]
        public async Task RetrieveAllProperties()
        {
            var properties = GetProperties(typeof(Media))
                .Concat(GetProperties(typeof(Movie)))
                .ToArray();
            var requests = properties
                .Select(property => new UniformItemIdentifier(Constants.Movie, property))
                .Select(uii => new RestArgs(new RestRequest(HttpMethod.Get, uii)))
                .ToArray();

            await Controller.Send(requests);
            var failed = requests
                .Where(request => !request.Handled)
                .Select(request => request.Request.Uri)
                .OfType<UniformItemIdentifier>()
                .Select(uii => uii.Property);
            Assert.IsTrue(requests.All(request => request.Handled), string.Join(", ", failed));

            var responses = await Task.WhenAll(requests.Select(request => request.Response));
            var values = responses
                .Select(response => response.Body)
                .Zip(properties, (body, property) => (property, body.HasRepresentation(property.FullType)));
            Assert.IsTrue(values.All(value => value.Item2));
        }

        private static IEnumerable<Property?> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));
    }

    [TestClass]
    public class TMDbClientTests
    {
        private List<string> CallHistory => MockHandler.CallHistory;
        private Controller Controller { get; }
        private TMDbClient Client { get; }

        public TMDbClientTests()
        {
            Client = new TMDbClient(TMDB.WebClient, new TMDbResolver(TMDB.ITEM_PROPERTIES));
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
                Assert.AreEqual(1, CallHistory.Count, string.Join('\n', urls));

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
        public async Task BufferedGetRequests()
        {
            var url = "3/movie/0?language=en-US";
            await Task.WhenAll(Controller.Get(url), Controller.Get(url));

            Assert.AreEqual(1, CallHistory.Count);
        }

        private void AssertHandled(params RestRequestArgs[] e) => Assert.IsTrue(e.All(arg => arg.Handled), "The following uris where not handled:\n" + string.Join('\n', e.Where(arg => !arg.Handled).Select(arg => arg.Uri.ToString())));
    }
}
