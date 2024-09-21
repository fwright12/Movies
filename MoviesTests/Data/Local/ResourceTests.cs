using Movies.Data.Local;
using SQLite;

namespace MoviesTests.Data.Local
{
    [TestClass]
    public class ResourceTests
    {
        [TestMethod]
        public void UpdateTimestampOnEtagMatch()
        {
            Assert.Inconclusive();
        }
    }

    [TestClass]
    public class MovieResourceTests : Data.MovieResourceTests
    {
        private static readonly string DATABASE_FILENAME = $"{typeof(ResourceTests).FullName}.db";

        private static dBConnection Connection = new dBConnection(DATABASE_FILENAME);

        public MovieResourceTests() : base(new LocalProcessorFactory(Connection.SqlConnection)) { }

        [ClassInitialize]
        public static async Task Init(TestContext context)
        {
            var chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(Connection.PersistenceService.Processor).ToChainLink();
            chain.SetNext(new HandlerChain().RemoteTMDbProcessor);

            // Seed database with cached data from dummy web service
            var requests = new UniformItemIdentifier[]
            {
                new UniformItemIdentifier(Constants.Movie, Media.RUNTIME),
                new UniformItemIdentifier(Constants.TVShow, Media.RUNTIME),
            }.Select(uii => chain.Get(new KeyValueRequestArgs<Uri>(uii).AsEnumerable()));
            await Task.WhenAll(requests);
            //await chain.Get(requests);

            await Connection.WaitSettledAsync();
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            await Connection.CloseAsync();
        }
    }

    internal class LocalProcessorFactory : IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>>
    {
        public SQLiteAsyncConnection Connnection { get; }

        public LocalProcessorFactory(SQLiteAsyncConnection connnection)
        {
            Connnection = connnection;
        }

        public IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> Create()
        {
            return new AsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>>(new PersistenceService(new SqlResourceDAO(Connnection, TestsConfig.Resolver)).Processor);
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
