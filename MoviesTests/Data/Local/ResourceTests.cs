using Movies.Data.Local;
using System;
using System.Text;
using System.Text.Json;

namespace MoviesTests.Data.Local
{
    [TestClass]
    public class ResourceTests : Resources
    {
        [TestMethod]
        public async Task GetMovieProperty()
        {
            var value = await GetResource<TimeSpan?>(Constants.Movie, Media.RUNTIME);
            Assert.AreEqual(new TimeSpan(2, 10, 0), value);
        }

        [TestMethod]
        public async Task GetTVProperty()
        {
            var value = await GetResource<DateTime?>(Constants.TVShow, TVShow.LAST_AIR_DATE);
            Assert.AreEqual(new DateTime(2013, 5, 16), value);
        }

        private Task<T> GetResource<T>(Item item, Property property) => GetResource<T>(new UniformItemIdentifier(item, property));
        private async Task<T> GetResource<T>(UniformItemIdentifier uii)
        {
            var handlers = new HandlerChain();

            var request = new KeyValueRequestArgs<Uri, T>(uii);
            await handlers.LocalTMDbProcessor.ProcessAsync(request.AsEnumerable(), null);
            await CachedAsync(handlers.DiskCache);

            Assert.IsTrue(request.IsHandled);
            return request.Value;
        }

        private static Task CachedAsync(DummyDatastore<Uri> diskCache)
        {
            return Task.Delay(Math.Max(diskCache.ReadLatency, diskCache.WriteLatency) * 2);
        }

        public class HandlerChain
        {
            public readonly TMDbResolver Resolver;

            public DummyDatastore<Uri> DiskCache { get; }

            public IEventAsyncCache<KeyValueRequestArgs<Uri>> LocalTMDbCache { get; }
            public IAsyncCoRProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> LocalTMDbProcessor { get; }
            
            public PersistenceService PersistenceService { get; }

            public HandlerChain()
            {
                Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

                DiskCache = new DummyDatastore<Uri>
                {
                    ReadLatency = 50,
                    WriteLatency = 50,
                };

                LocalTMDbCache = new PersistentCache(DiskCache, Resolver);
                PersistenceService = new PersistenceService(LocalTMDbCache);
                LocalTMDbProcessor = PersistenceService.Processor;

                InsertData(TestData);
            }

            private static IDictionary<string, object> TestData = new Dictionary<string, object>
            {
                ["urn:Movie:0:Runtime"] = 130,
                ["urn:TVShow:0:Last Air Date"] = new DateTime(2013, 5, 16)
            };

            private void InsertData(IDictionary<string, object> data)
            {
                foreach (var kvp in data)
                {
                    int count = DiskCache.Count;
                    var uri = new Uri(kvp.Key, UriKind.RelativeOrAbsolute);

                    DiskCache.TryAdd(uri, new ResourceResponse<IEnumerable<byte>>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(kvp.Value))));
                    Assert.AreEqual(count + 1, DiskCache.Count);
                }
            }
        }
    }
}
