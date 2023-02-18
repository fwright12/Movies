namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class ResourceTests
    {
        private List<string> CallHistory => Movies.HttpClient.CallHistory;

        private Controller Controller { get; }
        private TMDbResources TMDbResources { get; }

        public ResourceTests()
        {
            var resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);
            var local = new TMDbLocalResources(new DummyJsonCache(), resolver)
            {
                ChangeKeys = TestsConfig.ChangeKeys
            };
            var remote = new TMDbClient(TMDB.WebClient, resolver);

            TMDbResources = new TMDbResources(local, remote, resolver, TMDbResources.AutoAppend);
            Controller = new Controller().SetNext(TMDbResources);

            //TMDbResources.Post(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Task.FromResult(Constants.TAGLINE));
            //TMDbResources.Post(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), Task.FromResult(Constants.LANGUAGE));
        }

        [TestInitialize]
        public void ClearCallHistory()
        {
            CallHistory.Clear();
        }

        [TestMethod]
        public async Task SingleDetailsResource()
        {
            var response = await Controller.Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));

            Assert.IsTrue(response.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response.Resource);

            Assert.AreEqual(1, CallHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,keywords,recommendations,release_dates,videos,watch/providers", CallHistory.Last());

            await AssertCached(MOVIE_PROPERTIES);
        }

        [TestMethod]
        public async Task SingleSubDetailsResource()
        {
            var response = await Controller.Get<IEnumerable<WatchProvider>>(new UniformItemIdentifier(Constants.Movie, Movie.WATCH_PROVIDERS));

            Assert.IsTrue(response.Success);
            Assert.AreEqual(21, response.Resource.Count());
            Assert.IsTrue(response.Resource.FirstOrDefault(provider => provider.Company.Name == "HBO Max") != null);

            Assert.AreEqual(1, CallHistory.Count);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=watch/providers,credits,keywords,recommendations,release_dates,videos", CallHistory.Last());

            await AssertCached(MOVIE_PROPERTIES);
        }

        private static readonly Property[] MOVIE_PROPERTIES = new Property[]
        {
            Media.RUNTIME,
            Media.CREW,
            Media.RECOMMENDED,
            Media.KEYWORDS,
            Media.TRAILER_PATH,
            Movie.RELEASE_DATE,
            Movie.WATCH_PROVIDERS,
        };

        private async Task AssertCached(params Property[] properties)
        {
            int calls = CallHistory.Count;
            await Task.WhenAll(properties.Select(property => Controller.Get(new UniformItemIdentifier(Constants.Movie, property))));

            Assert.AreEqual(calls, CallHistory.Count);
        }

        [TestMethod]
        public async Task SingleEnumerableResource()
        {
            var response1 = await Controller.Get<IEnumerable<Language>>(new UniformItemIdentifier(Constants.Movie, Media.LANGUAGES));
            Assert.IsTrue(response1.Success);
            Assert.AreEqual(1, response1.Resource.Count());
            Assert.AreEqual("en", response1.Resource.First().Iso_639);

            var response2 = await Controller.Get<IEnumerable<Keyword>>(new UniformItemIdentifier(Constants.Movie, Media.KEYWORDS));
            Assert.IsTrue(response2.Success);
            Assert.AreEqual(13, response2.Resource.Count());
            Assert.AreEqual("saving the world", response2.Resource.First().Name);
        }

        [TestMethod]
        public async Task AllTMDbResourcesAutomated()
        {
            var backup = DebugConfig.SimulatedDelay;
            DebugConfig.SimulatedDelay = 0;
            return;
            await AllResources(Constants.Movie, typeof(Media));
            await AllResources(Constants.Movie);
            //await AllResources(Constants.TVShow,
            //await AllResources(Constants.TVSeason,
            //await AllResources(Constants.TVEpisode,
            await AllResources(Constants.Person);

            DebugConfig.SimulatedDelay = backup;
        }

        private IEnumerable<Task> AllResources(params Item[] items) => items.Select(item => AllResources(item));

        private static IEnumerable<Property> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));

        private async Task AllResources(Item item, Type type = null)
        {
            type ??= item.GetType();
            var properties = GetProperties(type);
            var uiis = properties.Select(property => new UniformItemIdentifier(Constants.Movie, property));

            var controller = new Controller().SetNext(TMDbResources);

            foreach (var property in properties)
            {
                var uii = new UniformItemIdentifier(item, property);

                var propertyType = property.GetType().ToString().ToLower().Contains("multi") ? typeof(IEnumerable<>).MakeGenericType(property.Type) : property.Type;
                RestRequestArgs e = null;
                //var task = (Task)getMethod?.MakeGenericMethod(propertyType).Invoke(this, new object?[] { uii, controller });
                await controller.Get(e);

                var tupleType = typeof(ValueTuple<,>).MakeGenericType(typeof(bool), propertyType);
                var responseType = typeof(Task<>).MakeGenericType(tupleType);
                var response = responseType.GetProperty("Result").GetValue(e.Response);

                Assert.IsTrue((bool)tupleType.GetField("Item1").GetValue(response), $"Could not get value for property {property}");
                var resource = tupleType.GetField("Item2").GetValue(response);

                if (resource != null)
                {
                    var resourceType = resource.GetType();
                    Assert.IsTrue(resourceType?.IsAssignableTo(propertyType), $"Property: {property}. Resource of type {resourceType} cannot be assigned to ${propertyType}");
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
    }
}
