﻿using System.Collections;

namespace MoviesTests.Data.TMDb
{
    [TestClass]
    public class ResourceTests
    {
        private readonly Dictionary<ItemType, List<Property>> ItemProperties = new Dictionary<ItemType, List<Property>>
        {
            [ItemType.Movie] = GetProperties(typeof(Media)).Concat(GetProperties(typeof(Movie))).Append(TMDB.POPULARITY).ToList()
        };

        private List<string> CallHistory => Movies.HttpClient.CallHistory;

        private TMDbResolver Resolver { get; }
        private Controller Controller;
        private ResourceCache ResourceCache;
        private TMDbLocalResources TMDbLocalResources;
        private TMDbClient TMDbClient;
        private DummyJsonCache JsonCache;

        public ResourceTests()
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);
        }

        [TestInitialize]
        public void Reset()
        {
            CallHistory.Clear();

            Controller = new Controller()
                .AddLast(ResourceCache = new ResourceCache())
                .AddLast(TMDbLocalResources = new TMDbLocalResources(JsonCache = new DummyJsonCache(), Resolver)
                {
                    ChangeKeys = TestsConfig.ChangeKeys
                })
                .AddLast(TMDbClient = new TMDbClient(TMDB.WebClient, Resolver));
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromInMemoryCache()
        {
            await ResourceCache.Put(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE), Constants.TAGLINE);
            await ResourceCache.Put(new UniformItemIdentifier(Constants.Movie, Media.ORIGINAL_LANGUAGE), Constants.LANGUAGE);

            // Retrieve an item in the in memory cache
            var response = await Controller.Get<string>(new UniformItemIdentifier(Constants.Movie, Media.TAGLINE));
            Assert.IsTrue(response.Success);
            Assert.AreEqual(Constants.TAGLINE, response.Resource);

            Assert.AreEqual(0, CallHistory.Count);
            Assert.AreEqual(0, JsonCache.Count);
            Assert.AreEqual(2, ResourceCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromDiskCache()
        {
            var uri = new Uri("3/movie/0?language=en-US", UriKind.Relative);
            var json = "{ \"runtime\": 130 }";
            await TMDbLocalResources.PutAsync(new MultiRestEventArgs(new RestRequestArgs(uri, new State(json))), null);

            var request = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME), typeof(TimeSpan?));
            await Controller.Get(request);

            Assert.IsTrue(request.Handled);
            Assert.AreEqual(new TimeSpan(2, 10, 0), request.Response?.TryGetRepresentation<TimeSpan?>(out var value) == true ? value : null);

            Assert.AreEqual(0, CallHistory.Count);
            Assert.AreEqual(1, JsonCache.Count);
            Assert.AreEqual(1, ResourceCache.Count);
        }

        [TestMethod]
        public async Task ResourcesCanBeRetrievedFromApi()
        {
            var response = await Controller.Get<TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));

            Assert.IsTrue(response.Success);
            Assert.AreEqual(new TimeSpan(2, 10, 0), response.Resource);
            Assert.AreEqual($"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,keywords,recommendations,release_dates,videos,watch/providers", CallHistory.Last());

            Assert.AreEqual(1, CallHistory.Count);
            Assert.AreEqual(7, JsonCache.Count);
            //Assert.AreEqual(ItemProperties[ItemType.Movie].Count + 7, ResourceCache.Count);
            Assert.AreEqual(1, ResourceCache.Count);
        }

        // Calls to the api will have a delay
        // If the in memory cache doesn't cache tasks, successive calls will go through because they don't
        // return a result immediately
        [TestMethod]
        public async Task InMemoryCachedAsTasks()
        {
            var arg = new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
            await Task.WhenAll(Controller.Get(arg), Controller.Get(arg));

            Assert.AreEqual(1, CallHistory.Count);
        }

        [TestMethod]
        public async Task RepeatedRequestsAreCached()
        {
            RestRequestArgs[] GetAllRequests() => ItemProperties[ItemType.Movie]
                .Select(property => new RestRequestArgs(new UniformItemIdentifier(Constants.Movie, property)))
                .ToArray();

            await Task.WhenAll(Controller.Get(GetAllRequests()));
            Assert.AreEqual(1, CallHistory.Count);

            // No additional api calls should be made, everything should be cached in memory
            await Task.WhenAll(Controller.Get(GetAllRequests()));
            Assert.AreEqual(1, CallHistory.Count);

            // These properties should result in another api call because they don't cache
            foreach (var property in new Property[]
            {
                Media.ORIGINAL_LANGUAGE,
                Media.RECOMMENDED,
                //Movie.PARENT_COLLECTION,
            })
            {
                CallHistory.Clear();

                await ResourceCache.Delete(new UniformItemIdentifier(Constants.Movie, property));
                await Task.WhenAll(Controller.Get(GetAllRequests()));
                Assert.AreEqual(1, CallHistory.Count);
            }
        }

        private static readonly IReadOnlySet<Property> NO_CHANGE_KEY = new HashSet<Property>
        {
            Media.ORIGINAL_LANGUAGE,
            Media.RECOMMENDED,
            Movie.PARENT_COLLECTION,
            TVShow.SEASONS, // There is a change key for this but I'm not tracking it at this point
            TVShow.NETWORKS,
            Person.GENDER,
            Person.CREDITS,
            TMDB.POPULARITY
        };

        [TestMethod]
        public async Task AllTMDbResourcesAutomated()
        {
            //var request = new RestRequestArgs(new UniformItemIdentifier(Constants.TVShow, TVShow.GENRES), typeof(IEnumerable<Genre>));
            //await Controller.Get(request);
            //request = new RestRequestArgs(new UniformItemIdentifier(Constants.TVShow, TVShow.GENRES), typeof(IEnumerable<Genre>));
            //await Controller.Get(request);

            var backup = DebugConfig.SimulatedDelay;
            DebugConfig.SimulatedDelay = 0;

            await AllResources(Constants.Movie, typeof(Media), new Dictionary<Property, object>
            {
                [Media.TITLE] = "Harry Potter and the Deathly Hallows: Part 2",
                [Media.RUNTIME] = new TimeSpan(2, 10, 0),
                [Media.PRODUCTION_COUNTRIES] = "United Kingdom",
                [Media.KEYWORDS] = new Keyword { Id = 83, Name = "saving the world" }
            });
            await AllResources(Constants.Movie, new Dictionary<Property, object>
            {
                [Movie.BUDGET] = 125000000L,
            });
            await AllResources(Constants.TVShow, new Dictionary<Property, object>
            {
                [TVShow.GENRES] = new Genre { Id = 35, Name = "Comedy" }
            });
            await AllResources(Constants.TVSeason, new Dictionary<Property, object>
            {
                [TVSeason.YEAR] = new DateTime(2006, 9, 21)
            });
            await AllResources(Constants.TVEpisode, new Dictionary<Property, object>
            {
                [TVEpisode.AIR_DATE] = new DateTime(2007, 4, 26)
            });
            await AllResources(Constants.Person, new Dictionary<Property, object>
            {
                [Person.ALSO_KNOWN_AS] = "Jessica Howard",
            });

            DebugConfig.SimulatedDelay = backup;
        }

        private static IEnumerable<Property?> GetProperties(Type type) => type
            .GetFields()
            .Where(field => typeof(Property).IsAssignableFrom(field.FieldType))
            .Select(field => (Property)field.GetValue(null));

        private Task AllResources(Item item, IDictionary<Property, object> expectedValues = null) => AllResources(item, item.GetType(), expectedValues);
        private async Task AllResources(Item item, Type type, IDictionary<Property, object> expectedValues = null)
        {
            var properties = GetProperties(type);

            foreach (var test in new (Controller Controller, IEnumerable<Property?> Properties)[]
            {
                (Controller, properties),
                //new Controller().AddLast(ResourceCache),
                (new Controller().AddLast(TMDbLocalResources), type == typeof(TVSeason) || type == typeof(TVEpisode) ? Enumerable.Empty<Property>() : properties.Except(NO_CHANGE_KEY))
            })
            {
                foreach (var property in test.Properties)
                {
                    if (property == Movie.PARENT_COLLECTION)
                        continue;

                    var uii = new UniformItemIdentifier(item, property);
                    var arg = new RestRequestArgs(uii, property.FullType);

                    try
                    {
                        await test.Controller.Get(arg);
                    }
                    catch (Exception e)
                    {
                        Print.Log(e);
                    }

                    Assert.IsTrue(arg.Handled, $"Could not get value for property {property} of type {type}");
                    Assert.IsTrue(arg.Response.TryGetRepresentation(property.FullType, out var value), $"Property {property}. Expected type: {property.FullType}. Available types: {string.Join(", ", arg.Response.OfType<object>().Select(rep => rep.GetType()))}");

                    if (expectedValues?.TryGetValue(property, out var expected) == true)
                    {
                        if (property.AllowsMultiple && !expected.GetType().IsAssignableFrom(property.FullType))
                        {
                            var values = value as IEnumerable;
                            Assert.IsNotNull(values);
                            Assert.AreEqual(expected, values.OfType<object>().First());
                        }
                        else
                        {
                            Assert.AreEqual(expected, value);
                        }
                    }
                }
            }
        }
    }
}
