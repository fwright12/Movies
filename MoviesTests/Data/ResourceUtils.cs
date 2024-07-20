using Movies.Data.Local;

namespace MoviesTests.Data
{
    internal class ResourceUtils
    {
    }

    public class HandlerChain
    {
        //public readonly ChainLink<MultiRestEventArgs> Chain;
        public readonly ChainLink<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Chain;
        public readonly TMDbResolver Resolver;

        public List<string> WebHistory => MockHttpHandler.LocalCallHistory;

        public UiiDictionaryDataStore InMemoryCache { get; }
        public DummyDatastore<Uri> DiskCache { get; }

        public IEventAsyncCache<KeyValueRequestArgs<Uri>> LocalTMDbCache { get; }
        public IAsyncCoRProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> LocalTMDbProcessor { get; }
        public TMDbHttpProcessor RemoteTMDbProcessor { get; }
        public MockHandler MockHttpHandler { get; }

        public InMemoryService InMemoryService { get; }
        public PersistenceService PersistenceService { get; }
        public TMDbService TMDbService { get; }

        public HandlerChain()
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            DiskCache = new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50,
            };
            InMemoryCache = new UiiDictionaryDataStore();
            InMemoryService = new InMemoryService(InMemoryCache);

            LocalTMDbCache = new PersistentCache(DiskCache, Resolver);
            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
            RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);

            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyData.db");
            //PersistenceService = new PersistenceService(new SqlResourceDAO(new SQLite.SQLiteAsyncConnection("")));
            PersistenceService = new PersistenceService(LocalTMDbCache);
            LocalTMDbProcessor = PersistenceService.Processor;

            Chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(InMemoryService.Cache).ToChainLink();
            Chain.SetNext(new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(PersistenceService.Processor))
                .SetNext(RemoteTMDbProcessor);
        }
    }
}
