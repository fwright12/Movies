namespace MoviesTests.Data.TMDb
{
    //[TestClass]
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
}
