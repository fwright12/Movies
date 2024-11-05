using Movies.Data.Local;
using SQLite;
using System.Text;

namespace MoviesTests.Data.Local
{
    [TestClass]
    public class MovieResourceTests : Data.MovieResourceTests
    {
        public MovieResourceTests() : base(new LocalProcessorFactory(ResourceTests.Connection.SqlConnection)) { }

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            await ResourceTests.SeedDBAsync(new UniformItemIdentifier(Constants.Movie, Media.TITLE));
        }
    }

    [TestClass]
    public class TVShowResourceTests : Data.TVShowResourceTests
    {
        private static readonly TVShow STILL_RUNNING_SHOW = new TVShow("still running show").WithID(TMDB.IDKey, 1);

        public TVShowResourceTests() : base(new LocalProcessorFactory(ResourceTests.Connection.SqlConnection)) { }

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            await ResourceTests.SeedDBAsync(new UniformItemIdentifier(Constants.TVShow, Media.TITLE));
            await ResourceTests.Connection.DAO.InsertMessage(new SqlResourceDAO.Message
            {
                Uri = new UniformItemIdentifier(STILL_RUNNING_SHOW, TVShow.LAST_AIR_DATE).ToString() + $"?{TMDbRequest.LANGUAGE_PARAMETER_KEY}={TMDB.LANGUAGE.Iso_639}",
                Response = Encoding.UTF8.GetBytes("null")
            });
        }

        [TestMethod]
        public async Task RetrieveStillRunningShowLastAirDate()
        {
            var processor = ProcessorFactory.Create();
            var request = new KeyValueRequestArgs<Uri, DateTime?>(new UniformItemIdentifier(STILL_RUNNING_SHOW, TVShow.LAST_AIR_DATE));
            var requests = new[] { request };

            await processor.ProcessAsync(requests);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(null, request.Value);
        }
    }

    //[TestClass]
    public class TVSeasonResourceTests : Data.TVSeasonResourceTests
    {
        public TVSeasonResourceTests() : base(new LocalProcessorFactory(ResourceTests.Connection.SqlConnection)) { }

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            await ResourceTests.SeedDBAsync(new UniformItemIdentifier(Constants.TVSeason, TVSeason.YEAR));
        }
    }

    //[TestClass]
    public class TVEpisodeResourceTests : Data.TVEpisodeResourceTests
    {
        public TVEpisodeResourceTests() : base(new LocalProcessorFactory(ResourceTests.Connection.SqlConnection)) { }

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            await ResourceTests.SeedDBAsync(new UniformItemIdentifier(Constants.TVEpisode, Media.TITLE));
        }
    }

    //[TestClass]
    public class PersonResourceTests : Data.PersonResourceTests
    {
        private static readonly Person LIVING_PERSON = new Person("living person").WithID(TMDB.IDKey, 1);

        public PersonResourceTests() : base(new LocalProcessorFactory(ResourceTests.Connection.SqlConnection)) { }

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            await ResourceTests.SeedDBAsync(new UniformItemIdentifier(Constants.Person, Person.NAME));
            await ResourceTests.SeedDBAsync(new UniformItemIdentifier(LIVING_PERSON, Person.DEATHDAY), Encoding.UTF8.GetBytes("null"));
        }

        [TestMethod]
        public async Task RetrieveLivingPersonDeathday()
        {
            var processor = ProcessorFactory.Create();
            var request = new KeyValueRequestArgs<Uri, DateTime?>(new UniformItemIdentifier(LIVING_PERSON, Person.DEATHDAY));
            var requests = new[] { request };

            await processor.ProcessAsync(requests);

            Assert.IsTrue(request.IsHandled);
            Assert.AreEqual(null, request.Value);
        }
    }

    [TestClass]
    // Dummy test class to manage database connection for all item resource tests
    public class ResourceTests
    {
        public static dBConnection Connection => _Connection ??= new dBConnection(DATABASE_FILENAME);
        private static dBConnection _Connection = null!;

        private static readonly string DATABASE_FILENAME = $"{typeof(ResourceTests).FullName}.db";

        public static async Task SeedDBAsync(params Uri[] uris)
        {
            var chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(new PersistenceService(new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver)).Processor).ToChainLink();
            chain.SetNext(new HandlerChain().RemoteTMDbProcessor);

            // Seed database with cached data from dummy web service
            var requests = uris.Select(uii => chain.Get(new KeyValueRequestArgs<Uri>(uii).AsEnumerable()));
            await Task.WhenAll(requests);

            await Connection.WaitSettledAsync();
        }

        public static async Task SeedDBAsync(Uri uri, object value)
        {
            IEventAsyncCache<KeyValueRequestArgs<Uri>> db = new SqlResourceDAO(Connection.SqlConnection, TestsConfig.Resolver);

            var request = new KeyValueRequestArgs<Uri>(uri);
            request.Handle(new KeyValueResponse(value));
            await db.Write(request.AsEnumerable());

            await Connection.WaitSettledAsync();
        }

        [AssemblyCleanup]
        public static async Task Cleanup()
        {
            if (Connection != null)
            {
                await Connection.CloseAsync();
            }
        }
    }

    internal class LocalProcessorFactory : IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>>
    {
        public SQLiteAsyncConnection Connection { get; }

        public LocalProcessorFactory(SQLiteAsyncConnection connnection)
        {
            Connection = connnection;
        }

        public IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> Create()
        {
            return new AsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>>(new PersistenceService(new SqlResourceDAO(Connection, TestsConfig.Resolver)).Processor);
        }

        private class AsyncEventProcessor<T> : IAsyncEventProcessor<T>
        {
            public IAsyncCoRProcessor<T> Processor { get; }

            public AsyncEventProcessor(IAsyncCoRProcessor<T> processor)
            {
                Processor = processor;
            }

            public Task<bool> ProcessAsync(T e) => Processor.ProcessAsync(e, null);
        }
    }
}
