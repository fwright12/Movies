using Movies;
using System.Data;
using System.Reflection;
using HttpClient = Movies.HttpClient;

namespace MoviesTests.TMDb
{
    [TestClass]
    public class ResourceTests
    {
        private const string TAGLINE = "lakjflkasjdflkajsdklj";
        private static readonly Language LANGUAGE = new Language("en");

        private Controller Resources { get; }

        private ResourceCache ResourceCache { get; }
        private TMDbCompressor TMDbCompressor { get; }
        private TMDbLocalResources TMDbLocalResources { get; }
        private TMDbRemoteResources TMDbRemoteResources { get; }

        public ResourceTests()
        {
            var resources = ResourceCache = new ResourceCache();
            TMDbLocalResources = new TMDbLocalResources(null);
            TMDbRemoteResources = new TMDbRemoteResources();
            TMDbCompressor = new TMDbCompressor(TMDbLocalResources);

            Resources = new Controller(ResourceCache).SetNext(TMDbCompressor).SetNext(TMDbRemoteResources);
            //var mainChain = new Controller(new ControllerWrapper(resources), tmdbChain);
            //Resources = new Controller(mainChain);

            foreach (var cache in new ResourceCache[] { ResourceCache, resources })
            {
                cache.Post(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Task.FromResult(TAGLINE));
                cache.Post(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), Task.FromResult(LANGUAGE));
            }
        }

        [TestMethod]
        public async Task SingleLocalResource()
        {
            var response = await Get<Language>(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), ResourceCache);
            Assert.IsTrue(response.Success);
            Assert.AreEqual(LANGUAGE, response.Resource);

            var response1 = await Get<IEnumerable<Keyword>>(new UniformItemIdentifier(Constants.Movie, Media.KEYWORDS), ResourceCache);
            Assert.IsFalse(response1.Success);
        }

        [TestMethod]
        public async Task SingleTMDbResource()
        {
            var response = await Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), TMDbRemoteResources);
            Assert.IsTrue(response.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response.Resource);

            var response1 = await Get<IEnumerable<Language>>(new UniformItemIdentifier(Constants.Movie, Media.LANGUAGES), TMDbRemoteResources);
            Assert.IsTrue(response1.Success);
            Assert.AreEqual(1, response1.Resource.Count());
            Assert.AreEqual("en", response1.Resource.First().Iso_639);

            var response2 = await Get<IEnumerable<Keyword>>(new UniformItemIdentifier(Constants.Movie, Media.KEYWORDS), TMDbRemoteResources);
            Assert.IsTrue(response2.Success);
            Assert.AreEqual(13, response2.Resource.Count());
            Assert.AreEqual("saving the world", response2.Resource.First().Name);
        }

        [TestMethod]
        public async Task SingleResource()
        {
            HttpClient.CallHistory.Clear();

            // Retrieve an item in the in memory cache
            var response = await Get<string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Resources);
            Assert.IsTrue(response.Success);
            Assert.AreEqual(TAGLINE, response.Resource);
            Assert.AreEqual(0, HttpClient.CallHistory.Count);

            // Retrieve an item from the web
            var response1 = await Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), Resources);
            Assert.IsTrue(response1.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response1.Resource);
            Assert.AreEqual(1, HttpClient.CallHistory.Count);

            // Make sure the just retrieved item is cached in memory
            HttpClient.CallHistory.Clear();
            response1 = await Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), Resources);
            Assert.IsTrue(response1.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response1.Resource);
            Assert.AreEqual(0, HttpClient.CallHistory.Count);
        }

        [TestMethod]
        public async Task AllTMDbResourcesAutomated()
        {
            var backup = DebugConfig.SimulatedDelay;
            DebugConfig.SimulatedDelay = 0;

            await AllResources(Constants.Movie, typeof(Media));
            await AllResources(Constants.Movie);
            //await AllResources(Constants.TVShow,
            //await AllResources(Constants.TVSeason,
            //await AllResources(Constants.TVEpisode,
            await AllResources(Constants.Person);

            DebugConfig.SimulatedDelay = backup;
        }

        private IEnumerable<Task> AllResources(params Item[] items) => items.Select(item => AllResources(item));

        private async Task AllResources(Item item, Type type = null)
        {
            var getMethod = typeof(Controller)
                .GetMethods()
                .FirstOrDefault(method => method.IsGenericMethod && method.Name == nameof(Controller.GetChain));
            getMethod = GetType().GetMethod(nameof(Get), BindingFlags.NonPublic | BindingFlags.Instance);

            type ??= item.GetType();
            var properties = type
                .GetFields()
                .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
                .Select(field => (Property)field.GetValue(null));
            var uiis = properties.Select(property => new UniformItemIdentifier(Constants.Movie, property));

            foreach (var controller in new Controller[] { new Controller(TMDbCompressor), Resources })
            {
                foreach (var property in properties)
                {
                    var uii = new UniformItemIdentifier(item, property);

                    var propertyType = property.GetType().ToString().ToLower().Contains("multi") ? typeof(IEnumerable<>).MakeGenericType(property.Type) : property.Type;
                    var task = (Task)getMethod?.MakeGenericMethod(propertyType).Invoke(this, new object?[] { uii, controller });
                    await task;

                    var tupleType = typeof(ValueTuple<,>).MakeGenericType(typeof(bool), propertyType);
                    var responseType = typeof(Task<>).MakeGenericType(tupleType);
                    var response = responseType.GetProperty("Result").GetValue(task);

                    Assert.IsTrue((bool)tupleType.GetField("Item1").GetValue(response), $"Could not get value for property {property}");
                    var resource = tupleType.GetField("Item2").GetValue(response);

                    if (resource != null)
                    {
                        var resourceType = resource.GetType();
                        Assert.IsTrue(resourceType?.IsAssignableTo(propertyType), $"Property: {property}. Resource of type {resourceType} cannot be assigned to ${propertyType}");
                    }
                }
            }
        }

        private static Property[] ForcedProperties = new Property[]
        {
            Media.RUNTIME,
            Media.CREW,
            Media.RECOMMENDED,
            Media.KEYWORDS,
            Media.TRAILER_PATH,
            Movie.RELEASE_DATE,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS,
            Person.NAME,
            Person.CREDITS
        };

        //private async Task<(bool Success, T Resource)> Get<T>(UniformItemIdentifier uii, Controller controller = null) => (await (controller ?? TMDbResources).TryHandle<T>(ForcedProperties.Select(property => new UniformItemIdentifier(uii.Item, property)).Append(uii))).Last();
        //private Task<(bool Success, T Resource)> Get<T>(UniformItemIdentifier uii, IRestService service) => Get<T>(uii, new ControllerWrapper(service));
        private Task<(bool Success, T Resource)> Get<T>(UniformItemIdentifier uii, ControllerLink link)
        {
            var uiis = new List<UniformItemIdentifier> { uii };

            if (link == TMDbRemoteResources)
            {
                uiis.InsertRange(0, ForcedProperties.Select(property => new UniformItemIdentifier(uii.Item, property)));
            }

            return Get<T>(uiis, new Controller(link));
        }

        private Task<(bool Success, T Resource)> Get<T>(UniformItemIdentifier uii, Controller controller = null) => Get<T>(new List<UniformItemIdentifier> { uii }, controller);

        private async Task<(bool Success, T Resource)> Get<T>(IEnumerable<UniformItemIdentifier> uiis, Controller controller = null)
        {
            var args = uiis.Select(uii => new GetEventArgs<Task<T>>(uii)).ToArray();
            await controller.Get(args);

            var arg = args.Last();
            return (arg.Handled, arg.Resource == null ? default : await arg.Resource);
        }
    }
}
