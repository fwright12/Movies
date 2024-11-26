using Movies.Data.Local;
using SQLite;

namespace MoviesTests.Data
{
    internal class ResourceUtils
    {
        public const string MOVIE_URL_APPENDED = $"3/movie/0?{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";
        public const string MOVIE_URL_USENGLISH_APPENDED = $"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";
        public const string MOVIE_URL_USENGLISH_USREGION_APPENDED = $"3/movie/0?{TMDbRequest.LANGUAGE_PARAMETER_KEY}=en-US&{TMDbRequest.REGION_PARAMETER_KEY}=US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";
        public const string TV_URL_APPENDED = $"3/tv/0?{Constants.APPEND_TO_RESPONSE}=aggregate_credits,content_ratings,external_ids,keywords,recommendations,videos,watch/providers";
        public const string TV_URL_USENGLISH_APPENDED = $"3/tv/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=aggregate_credits,content_ratings,external_ids,keywords,recommendations,videos,watch/providers";
        public const string TV_URL_USENGLISH_USREGION_APPENDED = $"3/tv/0?{TMDbRequest.LANGUAGE_PARAMETER_KEY}=en-US&{TMDbRequest.REGION_PARAMETER_KEY}=US&{Constants.APPEND_TO_RESPONSE}=aggregate_credits,content_ratings,external_ids,keywords,recommendations,videos,watch/providers";

        public static Uri RUNTIME_URI = new Uri("urn:Movie:0:Runtime", UriKind.RelativeOrAbsolute);
    }

    public class dBConnection
    {
        public PersistenceService PersistenceService { get; }
        public SqlResourceDAO DAO { get; }
        public SQLiteAsyncConnection SqlConnection { get; }
        public string DatabasePath { get; }

        public dBConnection(string filename)
        {
            DatabasePath = Path.Combine(Environment.CurrentDirectory, filename);
            //File.Delete(DatabasePath);

            SqlConnection = new SQLiteAsyncConnection(DatabasePath);
            DAO = new SqlResourceDAO(SqlConnection, TestsConfig.Resolver);
            PersistenceService = new PersistenceService(DAO);
        }

        public async Task WaitSettledAsync()
        {
            await Task.Delay(SqlResourceDAO.BATCH_INSERT_DELAY);

            // Wait until nothing has been written to the database in a while
            var lastCount = -1;
            for (int i = 0; ; i++)
            {
                try
                {
                    var count = (await SqlConnection.QueryScalarsAsync<int>($"select count(*) from {SqlResourceDAO.RESOURCE_TABLE_NAME}"))[0];
                    if (count == lastCount)
                    {
                        break;
                    }

                    lastCount = count;
                }
                catch { }

                if (i >= 10)
                {
                    throw new Exception("Timed out initializing database");
                }
                await Task.Delay(100);
            }
        }

        public async Task CloseAsync()
        {
            await SqlConnection.CloseAsync();
            try
            {
                //File.Delete(DatabasePath);
            }
            catch { }
        }
    }

    public static class ServiceFactory
    {
        public static TMDbResolver Resolver => TestsConfig.Resolver;

        public static InMemoryService CreateInMemory(out UiiDictionaryDataStore memoryCache)
        {
            memoryCache = new UiiDictionaryDataStore();
            return new InMemoryService(memoryCache);
        }

        public static PersistenceService CreatePersistence(string filename, out dBConnection connection)
        {
            connection = new dBConnection(filename);
            return connection.PersistenceService;
        }

        public static PersistenceService CreateMockPersistence(out DummyDatastore<Uri> diskCache)
        {
            diskCache = new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50,
            };
            return new PersistenceService(diskCache);
        }

        public static TMDbService CreateMockTMDb(out MockHandler mockHttpMessageHandler)
        {
            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(mockHttpMessageHandler = new MockHandler())));
            var processor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);
            return new TMDbService(invoker, Resolver);
        }
    }

    public class HandlerChain
    {
        //public readonly ChainLink<MultiRestEventArgs> Chain;
        public readonly ChainLink<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Chain;
        public readonly TMDbResolver Resolver;

        public List<string> WebHistory => MockHttpHandler.LocalCallHistory;

        public UiiDictionaryDataStore MemoryCache { get; }
        public DummyDatastore<Uri> DiskCache { get; }

        public TMDbHttpProcessor RemoteTMDbProcessor { get; }
        public MockHandler MockHttpHandler { get; }

        public InMemoryService InMemoryService { get; }
        public PersistenceService PersistenceService { get; }
        public TMDbService TMDbService { get; }

        public static IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Create(out InMemoryService inMemoryService, out PersistenceService persistenceService, out TMDbService tmdbService, UiiDictionaryDataStore memoryCache = null!, IEventAsyncCache<KeyValueRequestArgs<Uri>> diskCache = null!, HttpMessageHandler tmdbHandler = null!)
        {
            var resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            inMemoryService = new InMemoryService(memoryCache ?? new UiiDictionaryDataStore());
            persistenceService = new PersistenceService(diskCache ?? new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50
            });
            tmdbService = new TMDbService(new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(new MockHandler()))), resolver);

            var chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(inMemoryService.Cache).ToChainLink();
            chain.SetNext(new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(persistenceService.Processor))
                .SetNext(tmdbService.Processor);

            return chain;
        }

        public HandlerChain(UiiDictionaryDataStore memoryCache = null!, IEventAsyncCache<KeyValueRequestArgs<Uri>> diskCache = null!, HttpMessageHandler tmdbHandler = null!)
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            InMemoryService = new InMemoryService(MemoryCache = memoryCache ?? new UiiDictionaryDataStore());
            PersistenceService = new PersistenceService(diskCache ?? new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50
            });
            TMDbService = new TMDbService(new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler()))), Resolver);

            DiskCache = null!;
            RemoteTMDbProcessor = (TMDbService.Processor as TMDbHttpProcessor)!;

            Chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(InMemoryService.Cache).ToChainLink();
            Chain.SetNext(new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(PersistenceService.Processor))
                .SetNext(TMDbService.Processor);
        }

        public HandlerChain() //: this(ServiceFactory.CreateInMemory(out var memoryCache), memoryCache, ServiceFactory.CreateMockPersistence(out _), ServiceFactory.CreateMockTMDb(out _))
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            MemoryCache = new UiiDictionaryDataStore();
            InMemoryService = new InMemoryService(MemoryCache);

            DiskCache = new DummyDatastore<Uri>();
            DiskCache.ReadLatency = 50;
            DiskCache.WriteLatency = 50;
            PersistenceService = new PersistenceService(DiskCache);

            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
            RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);

            Chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(InMemoryService.Cache).ToChainLink();
            Chain.SetNext(new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(PersistenceService.Processor))
                .SetNext(RemoteTMDbProcessor);
        }

        public HandlerChain(InMemoryService memoryService, PersistenceService persistenceService, TMDbService tmdbService) : this(null!, null!, null!, null!, memoryService, persistenceService, tmdbService) { }

        private HandlerChain(UiiDictionaryDataStore memoryCache, DummyDatastore<Uri> diskCache, TMDbHttpProcessor remoteTMDbProcessor, MockHandler mockHttpHandler, InMemoryService inMemoryService, PersistenceService persistenceService, TMDbService tmdbService)
        {
            MemoryCache = memoryCache;
            DiskCache = diskCache;
            RemoteTMDbProcessor = remoteTMDbProcessor;
            MockHttpHandler = mockHttpHandler;
            InMemoryService = inMemoryService;
            PersistenceService = persistenceService;
            TMDbService = tmdbService;

            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            Chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(InMemoryService.Cache).ToChainLink();
            Chain.SetNext(new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(PersistenceService.Processor))
                .SetNext(RemoteTMDbProcessor);
        }
    }
}
