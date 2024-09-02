using FFImageLoading.Cache;
using Movies.Data.Local;
using SQLite;

namespace MoviesTests.Data
{
    internal class ResourceUtils
    {
        public const string MOVIE_URL_APPENDED = $"3/movie/0?{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";
        public const string MOVIE_URL_APPENDED_USENGLISH = $"3/movie/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=credits,external_ids,keywords,recommendations,release_dates,videos,watch/providers";
        public const string TV_URL_APPENDED = $"3/tv/0?{Constants.APPEND_TO_RESPONSE}=aggregate_credits,content_ratings,external_ids,keywords,recommendations,videos,watch/providers";
        public const string TV_URL_APPENDED_USENGLISH = $"3/tv/0?language=en-US&{Constants.APPEND_TO_RESPONSE}=aggregate_credits,content_ratings,external_ids,keywords,recommendations,videos,watch/providers";

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
            File.Delete(DatabasePath);

            SqlConnection = new SQLiteAsyncConnection(DatabasePath);
            DAO = new SqlResourceDAO(SqlConnection, TestsConfig.Resolver);
            PersistenceService = new PersistenceService(DAO);
        }

        public async Task WaitSettledAsync()
        {
            // Wait until nothing has been written to the database in a while
            var lastCount = -1;
            for (int i = 0; ; i++)
            {
                try
                {
                    var count = (await SqlConnection.QueryScalarsAsync<int>("select count(*) from Message"))[0];
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
            File.Delete(DatabasePath);
        }
    }

    public static class ServiceFactory
    {
        public static TMDbResolver Resolver => TestsConfig.Resolver;

        public static InMemoryService CreateInMemory()
        {
            var inMemoryCache = new UiiDictionaryDataStore();
            return new InMemoryService(inMemoryCache);
        }

        public static PersistenceService CreatePersistence(string filename)
        {
            return new dBConnection(filename).PersistenceService;
        }

        public static PersistenceService CreateMockPersistence()
        {
            var diskCache = new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50,
            };
            return new PersistenceService(diskCache);
        }

        public static TMDbService CreateTMDb()
        {
            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(new MockHandler())));
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

        public UiiDictionaryDataStore InMemoryCache { get; }
        public DummyDatastore<Uri> DiskCache { get; }

        public TMDbHttpProcessor RemoteTMDbProcessor { get; }
        public MockHandler MockHttpHandler { get; }

        public InMemoryService InMemoryService { get; }
        public PersistenceService PersistenceService { get; }
        public TMDbService TMDbService { get; }

        public HandlerChain()
        {
            Resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);

            InMemoryCache = new UiiDictionaryDataStore();
            InMemoryService = new InMemoryService(InMemoryCache);

            DiskCache = new DummyDatastore<Uri>
            {
                ReadLatency = 50,
                WriteLatency = 50,
            };
            PersistenceService = new PersistenceService(DiskCache);

            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(MockHttpHandler = new MockHandler())));
            RemoteTMDbProcessor = new TMDbHttpProcessor(invoker, Resolver, TMDbApi.AutoAppend);

            Chain = new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(InMemoryService.Cache).ToChainLink();
            Chain.SetNext(new AsyncCacheAsideProcessor<KeyValueRequestArgs<Uri>>(PersistenceService.Processor))
                .SetNext(RemoteTMDbProcessor);
        }
    }
}
