using Movies.Models;
using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Movies.API;
using Test = Movies;

namespace Movies
{
    public class SQLJsonCache : IJsonCache
    {
        public const string TABLE_NAME = "WebResourceCache";

        public SQLiteAsyncConnection DB { get; }
        public Table Table { get; } = new Table(URL, RESPONSE, TIMESTAMP)
        {
            Name = TABLE_NAME
        };

        public static readonly Table.Column URL = new Table.Column("url", "text", "primary key not null unique");
        public static readonly Table.Column RESPONSE = new Table.Column("json", "blob");
        public static readonly Table.Column TIMESTAMP = new Table.Column("timestamp", "text");

        public Task Setup { get; }

        public SQLJsonCache(SQLiteAsyncConnection db)
        {
            DB = db;

            Setup = DB.CreateTable(Table);
        }

        public async Task AddAsync(string url, JsonResponse response) => await ExecuteAsync($"insert into {Table} ({URL}, {RESPONSE}, {TIMESTAMP}) values (?,?,?)", url, await response.Content.ReadAsByteArrayAsync(), response.Timestamp);

        public Task Clear() => ExecuteAsync($"delete from {Table}");

        public async Task<bool> IsCached(string url)
        {
            await Setup;
            return (await DB.QueryScalarsAsync<int>($"select count(*) from {Table} where {URL} = ?", url)).FirstOrDefault() == 1;
        }

        public async Task<bool> Expire(string url) => await ExecuteAsync($"delete from {Table} where {URL} = ?", url) == 1;

        public async Task<JsonResponse> TryGetValueAsync(string url)
        {
            await Setup;

            var rows = await DB.QueryAsync<(byte[], DateTime)>($"select {RESPONSE}, {TIMESTAMP} from {Table} where {URL} = ?", url);

            if (rows.Count == 1)
            {
                return new JsonResponse(rows[0].Item1, rows[0].Item2);
            }
            else
            {
                return null;
            }
        }

        public async IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Setup;

            foreach (var row in await DB.QueryAsync<(string, byte[], DateTime)>($"select * from {Table}"))
            {
                yield return new KeyValuePair<string, JsonResponse>(row.Item1, new JsonResponse(row.Item2, row.Item3));
            }
        }

        private async Task<int> ExecuteAsync(string query, params object[] args)
        {
            await Setup;
            return await DB.ExecuteAsync(query, args);
        }
    }



    public class ItemInfoCache2 : IAsyncEnumerable<KeyValuePair<string, JsonResponse>>
    {
        public static readonly Table.Column ID = new Table.Column("id", "integer");
        public static readonly Table.Column TYPE = new Table.Column("type", "integer");

        public Task Setup { get; }

        private SQLJsonCache Cache;
        private Dictionary<string, JsonResponse> Buffer = new Dictionary<string, JsonResponse>();

        private SemaphoreSlim BufferSemaphore { get; } = new SemaphoreSlim(1, 1);

        public ItemInfoCache2(SQLiteAsyncConnection db)
        {
            Setup = Init(db);
            _ = Load();
        }

        private async Task Load()
        {
            await Setup;

            var cols = new List<Table.Column> { ID, SQLJsonCache.URL, SQLJsonCache.RESPONSE, SQLJsonCache.TIMESTAMP, TYPE };
            var select = string.Join(", ", cols);
            var query = await Cache.DB.QueryAsync<(int Id, string URL, string Json, DateTime Timestamp, int Type)>($"select {select} from {Cache.Table}");

            await BufferSemaphore.WaitAsync();

            await Task.Run(() =>
            {
                foreach (var row in query)
                {
                    Buffer.TryAdd(row.URL, new JsonResponse(row.Json, row.Timestamp));
                }
            });

            BufferSemaphore.Release();
            Print.Log("done loading", Buffer.Count);
            ;
        }

        private async Task Init(SQLiteAsyncConnection db)
        {
            var cache = new SQLJsonCache(db);
            await cache.Setup;
            await cache.DB.AddColumns(cache.Table, ID, TYPE);
            //await cache.Clear();

#if DEBUG
            var rows = await cache.DB.QueryAsync<(string, string, string, string, string)>($"select * from {cache.Table} limit 10");
            Print.Log((await cache.DB.QueryScalarsAsync<int>($"select count(*) from {cache.Table}")).FirstOrDefault() + " items in cache");
            foreach (var row in rows)
            {
                Print.Log(row.Item1, row.Item3, row.Item4, row.Item5);//, row.Item2);
            }
#endif

            Cache = cache;
        }

        public async Task AddAsync(ItemType type, int id, string url, JsonResponse response)
        {
            return;
            await BufferSemaphore.WaitAsync();
            Buffer.TryAdd(url, response);
            BufferSemaphore.Release();

            await Setup;
            await Cache.AddAsync(url, response);
            await Cache.DB.ExecuteAsync($"update {Cache.Table} set {TYPE} = ?, {ID} = ? where {SQLJsonCache.URL} = ?", (int)type, id, url);
        }

        public async Task Clear()
        {
            await BufferSemaphore.WaitAsync();
            Buffer.Clear();
            BufferSemaphore.Release();

            await Setup;
            await Cache.Clear();
        }

        public async Task<bool> Expire(string url)
        {
            await BufferSemaphore.WaitAsync();
            Buffer.Remove(url);
            BufferSemaphore.Release();

            await Setup;
            return await Cache.Expire(url);
        }

        public async Task<bool> Expire(ItemType type, int id)
        {
            await Setup;
            var rows = await Cache.DB.QueryScalarsAsync<string>($"select {SQLJsonCache.URL} from {Cache.Table} where {TYPE} = ? and {ID} = ?", (int)type, id);

            await BufferSemaphore.WaitAsync();
            foreach (var url in rows)
            {
                Buffer.Remove(url);
            }
            BufferSemaphore.Release();

            return await Cache.DB.ExecuteAsync($"delete from {Cache.Table} where {TYPE} = ? and {ID} = ?", (int)type, id) >= 1;
        }

        public async Task<bool> IsCached(string url)
        {
            await BufferSemaphore.WaitAsync();
            var contains = Buffer.ContainsKey(url);
            BufferSemaphore.Release();

            if (contains)
            {
                return true;
            }

            await Setup;
            return await Cache.IsCached(url);
        }

        public async Task<JsonResponse> TryGetValueAsync(string url)
        {
            await BufferSemaphore.WaitAsync();
            var success = Buffer.TryGetValue(url, out var response);
            BufferSemaphore.Release();

            return response;

            if (success)
            {
                return response;
            }

            await Setup;
            return await Cache.TryGetValueAsync(url);
        }

        public async IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Setup;

            await foreach (var value in Cache)
            {
                yield return value;
            }
        }
    }

    public class ItemInfoCache : IJsonCache, IAsyncEnumerable<KeyValuePair<string, JsonResponse>>
    {
        public static readonly Table.Column ID = new Table.Column("id", "integer");
        public static readonly Table.Column TYPE = new Table.Column("type", "integer");

        public Task Setup { get; }
        public async Task<Table> GetTable() => (await Cache).Table;

        private Task<SQLJsonCache> Cache { get; }

        public ItemInfoCache(SQLiteAsyncConnection db)
        {
            Setup = Cache = Init(db);
            //_ = Expire(ItemType.Movie, 453395);
        }

        private async Task<SQLJsonCache> Init(SQLiteAsyncConnection db)
        {
            var cache = new SQLJsonCache(db);
            await cache.Setup;
            await cache.DB.AddColumns(cache.Table, ID, TYPE);

#if DEBUG
            //await cache.Clear();

            var rows = await cache.DB.QueryAsync<(string, byte[], string, string, string)>($"select * from {cache.Table} limit 10");
            Print.Log((await cache.DB.QueryScalarsAsync<int>($"select count(*) from {cache.Table}")).FirstOrDefault() + " items in cache");
            foreach (var row in rows)
            {
                Print.Log(row.Item1, row.Item2.Length, row.Item3, row.Item4, row.Item5);//, row.Item2);
            }
#endif

            return cache;
        }

        private static string InsertCols(params Table.Column[] names)
        {
            var cols = string.Join(", ", (IEnumerable<object>)names);
            return $"({cols}) values ({string.Join(", ", Enumerable.Repeat("?", names.Length))})";
        }

        private Task Batch;
        private CancellationTokenSource CancelBatch;
        private string BatchQuery;
        private IEnumerable<object> BatchArgs;

        private void ExecuteBatched(string query, params object[] args)
        {
            CancelBatch?.Cancel();
            CancelBatch = new CancellationTokenSource();

            if (query[query.Length - 1] != ';')
            {
                query += ";\n";
            }

            //BatchQuery ??= "begin;";
            BatchQuery += query;
            BatchArgs = BatchArgs?.Concat(args) ?? args;

            Batch = ExecuteBatched(CancelBatch.Token);
        }

        private List<Task<IEnumerable<object>>> BatchInsert = new List<Task<IEnumerable<object>>>();

        private void InsertBatched(params object[] args) => InsertBatched(Task.FromResult<IEnumerable<object>>(args));

        private void InsertBatched(Task<IEnumerable<object>> args)
        {
            CancelBatch?.Cancel();
            CancelBatch = new CancellationTokenSource();

            BatchInsert.Add(args);

            Batch = ExecuteBatched(CancelBatch.Token);
        }

        private async Task ExecuteBatched(CancellationToken cancellationToken = default)
        {
            await Task.Delay(BATCH_INSERT_DELAY, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                await FlushBatch();
            }
        }

        private const int BATCH_INSERT_DELAY = 1000;
        private const int MAX_BATCH_SIZE = 1000;

        private async Task FlushBatch()
        {
            CancelBatch?.Cancel();

            var cols = string.Join(", ", SQLJsonCache.URL, SQLJsonCache.RESPONSE, SQLJsonCache.TIMESTAMP, TYPE, ID);
            var rows = await Task.WhenAll(BatchInsert);
            var values = string.Join(", ", rows.Select(row => "(" + string.Join(", ", Enumerable.Repeat("?", row.Count())) + ")"));
            var query = $"insert or replace into {(await Cache).Table} ({cols}) values {values}";

            BatchQuery = null;
            BatchInsert.Clear();

            Print.Log("flushing");
            await (await Cache).DB.ExecuteAsync(query, rows.SelectMany().ToArray());
            Print.Log($"flushed {rows.Length} items");
        }

        async Task IJsonCache.AddAsync(string url, JsonResponse response) => InsertBatched(url, await response.Content.ReadAsByteArrayAsync(), response.Timestamp, 0, 0);

        public Task AddAsync(ItemType type, int id, string url, JsonResponse response) => AddAsync(type, id, url, Task.FromResult(response));
        public async Task AddAsync(ItemType type, int id, string url, Task<JsonResponse> response)
        {
            var cache = await Cache;
            //var cols = InsertCols(SQLJsonCache.URL, SQLJsonCache.RESPONSE, SQLJsonCache.TIMESTAMP, TYPE, ID);
            //ExecuteBatched($"insert into {cache.Table} {cols}", url, response.Json, response.Timestamp, (int)type, id);
            //InsertBatched(url, response.Json, response.Timestamp, (int)type, id);
            InsertBatched(Unwrap(url, response, type, id));

            if (BatchInsert.Count >= MAX_BATCH_SIZE)
            {
                //Print.Log("flushing");
                //await FlushBatch();
            }
            //await cache.AddAsync(url, response);
            //await cache.DB.ExecuteAsync($"update {cache.Table} set {TYPE} = ?, {ID} = ? where {SQLJsonCache.URL} = ?", (int)type, id, url);
        }

        private static async Task<IEnumerable<object>> Unwrap(string url, Task<JsonResponse> response, ItemType type, int id)
        {
            var json = await response;
            return new object[] { url, await json.Content.ReadAsByteArrayAsync(), json.Timestamp, (int)type, id };
        }

        public async Task Clear() => await (await Cache).Clear();

        public async Task<bool> Expire(string url) => await (await Cache).Expire(url);

        public async Task<int> ExpireAll(string pattern, DateTime? olderThan = null)
        {
            var cache = await Cache;
            var query = $"delete from {cache.Table} where {SQLJsonCache.URL} like ?";

            if (olderThan.HasValue)
            {
                query += $" and {SQLJsonCache.TIMESTAMP} < ?";
            }

            return await cache.DB.ExecuteAsync(query, pattern, olderThan);
        }

        public const int MAX_SQL_VARIABLES = 32766;

        private static IEnumerable<T> Take<T>(IEnumerator<T> itr, int size)
        {
            for (int i = 0; i < size && itr.MoveNext(); i++)
            {
                yield return itr.Current;
            }
        }

        private static IEnumerable<T> ToEnumerable<T>(IEnumerator<T> itr)
        {
            while (itr.MoveNext())
            {
                yield return itr.Current;
            }
        }

        public Task<int> Expire(ItemType type, params int[] ids) => Expire(type, (IEnumerable<int>)ids);
        public async Task<int> Expire(ItemType type, IEnumerable<int> ids)
        {
            if (!ids.Any())
            {
                return 0;
            }

            var cache = await Cache;
            var itr = ids.OfType<object>().GetEnumerator();
            var args = Take(itr, MAX_SQL_VARIABLES - 1).Prepend((int)type).ToArray();
            var values = string.Join(",", Enumerable.Repeat("?", args.Length - 1));
            var query = $"delete from {cache.Table} where {TYPE} = ? and {ID} in ({values})";

            var counts = await Task.WhenAll(
                cache.DB.ExecuteAsync(query, args),
                Expire(type, ToEnumerable(itr).OfType<int>()));
            return counts.Sum();
        }

        public async Task<bool> IsCached(string url) => await (await Cache).IsCached(url);

        public async Task<JsonResponse> TryGetValueAsync(string url) => await (await Cache).TryGetValueAsync(url);

        public async IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var cache = await Cache;

            await foreach (var value in cache)
            {
                yield return value;
            }
        }
    }
}

namespace Movies.Views
{
    public class Database : IListProvider, IAssignID<int>
    {
        private static class SyncListsCols
        {
            public static readonly Table.Column ID = new Table.Column("ID", "text");
            public static readonly Table.Column SOURCE = new Table.Column("Source", "text");
            public static readonly Table.Column SYNC_ID = new Table.Column("SyncID", "text");
            public static readonly Table.Column SYNC_SOURCE = new Table.Column("SyncSource", "text");
        }

        private static class MovieCols
        {
            public static readonly Table.Column ID = new Table.Column("ID", "integer", "primary key not null unique");
            public static readonly Table.Column TITLE = new Table.Column("Title", "text");
            public static readonly Table.Column YEAR = new Table.Column("Year", "integer");
            public static readonly Table.Column POSTER_PATH = new Table.Column("PosterPath", "text");
        }

        private static class TVCols
        {
            public static readonly Table.Column ID = new Table.Column("ID", "integer", "primary key not null unique");
            public static readonly Table.Column TITLE = new Table.Column("Title", "text");
            public static readonly Table.Column POSTER_PATH = new Table.Column("PosterPath", "text");
            //public static readonly Table.Column YEAR = new Table.Column("Year", "integer");
        }

        private static class PersonCols
        {
            public static readonly Table.Column ID = new Table.Column("ID", "integer", "primary key not null unique");
            public static readonly Table.Column NAME = new Table.Column("Name", "text");
            public static readonly Table.Column BIRTH_YEAR = new Table.Column("BirthYear", "integer");
            public static readonly Table.Column PROFILE_PATH = new Table.Column("ProfilePath", "text");
        }

        public static class CollectionCols
        {
            public static readonly Table.Column ID = new Table.Column("ID", "integer", "primary key not null unique");
            public static readonly Table.Column NAME = new Table.Column("Name", "text");
            public static readonly Table.Column DESCRIPTION = new Table.Column("Description", "text");
            public static readonly Table.Column POSTER_PATH = new Table.Column("PosterPath", "text");
            public static readonly Table.Column COUNT = new Table.Column("Count", "integer");
        }

        //public static Database Instance = new Database(MockData.Instance);

        public const string UserInfoDBFilename = "Movies.db3";
        public const string ItemInfoDBFilename = "Items.db3";

        private SQLiteAsyncConnection UserInfo;
        private SQLiteAsyncConnection ItemInfo;

        public Task Init { get; }
        public ItemInfoCache ItemCache { get; }
        public string Name { get; } = "Local";

        private Table SyncLists = new Table(SyncListsCols.ID, SyncListsCols.SOURCE, SyncListsCols.SYNC_ID, SyncListsCols.SYNC_SOURCE)
        {
            Name = "SyncLists",
            ConstraintsDecl = string.Format("primary key({0}, {1}, {2}, {3})", SyncListsCols.ID, SyncListsCols.SOURCE, SyncListsCols.SYNC_ID, SyncListsCols.SYNC_SOURCE)
        };

        private Table Movies = new Table(MovieCols.ID, MovieCols.TITLE, MovieCols.YEAR) { Name = "Movies" };
        private Table TVShows = new Table(TVCols.ID, TVCols.TITLE) { Name = "TVShows" };
        private Table People = new Table(PersonCols.ID, PersonCols.NAME, PersonCols.BIRTH_YEAR) { Name = "People" };
        public Table Collections = new Table(CollectionCols.ID, CollectionCols.NAME, CollectionCols.COUNT, CollectionCols.DESCRIPTION, CollectionCols.POSTER_PATH) { Name = "Collections" };

        private IAssignID<int> IDSystem;
        private ID<int>.Key IDKey;
        private readonly Dictionary<ItemType, Table> ItemTables;

        public Database(IAssignID<int> tmdb, ID<int>.Key idKey)
        {
            IDSystem = tmdb;
            IDKey = idKey;

            string path = GetFilePath(UserInfoDBFilename);
            //Print.Log(new System.IO.FileInfo(path).Length);
            //File.Delete(path);
            UserInfo = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            path = GetFilePath(ItemInfoDBFilename);
            //Print.Log(new System.IO.FileInfo(path).Length);
            //File.Delete(path);
            ItemInfo = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            ItemCache = new ItemInfoCache(ItemInfo);

            ItemTables = new Dictionary<ItemType, Table>
            {
                [ItemType.Movie] = Movies,
                [ItemType.TVShow] = TVShows,
                [ItemType.Person] = People,
                [ItemType.Collection] = Collections
            };

            Init = Setup();
        }

        private string GetFilePath(string dbName) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dbName);

        private async Task Setup()
        {
            //await test.ExecuteAsync("drop table if exists " + MyLists.Name);
            //await test.ExecuteAsync("drop table if exists " + LocalLists.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.ItemsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.DetailsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + SyncLists.Name);

            await UserInfo.ExecuteAsync("drop table if exists Movies");
            await ItemInfo.ExecuteAsync("drop table if exists JsonCache");
            //await SQL.ExecuteAsync("drop table if exists MovieInfo");
            //await SQL.ExecuteAsync("drop table if exists TVShows");
            //await SQL.ExecuteAsync("drop table if exists People");
            //await SQL.ExecuteAsync("drop table if exists Collections");

            var itemsCols = await UserInfo.GetTableInfoAsync(LocalList.ItemsTable.Name);
            if (itemsCols.Count > 0 && !itemsCols.Select(col => col.Name).Contains(LocalList.ItemsCols.DATE_ADDED.Name))
            {
                await UserInfo.CreateTable(new Table(LocalList.ItemsTable.Columns) { Name = "temp" });
                await UserInfo.ExecuteAsync(string.Format("insert into temp select {0}, null as {1} from {2}", string.Join(',', LocalList.ItemsTable.Columns.Take(3)), LocalList.ItemsCols.DATE_ADDED, LocalList.ItemsTable));

                await UserInfo.ExecuteAsync("drop table " + LocalList.ItemsTable.Name);
                await UserInfo.ExecuteAsync("alter table temp rename to " + LocalList.ItemsTable.Name);
            }

            await Task.WhenAll(new Table[]
            {
                SyncLists,
                LocalList.DetailsTable,
                LocalList.ItemsTable,
            }.Select(table => UserInfo.CreateTable(table)));

            await Task.WhenAll(ItemTables.Values.Select(table => ItemInfo.CreateTable(table)));

            //await Task.WhenAll(SQL.CreateTable(SyncLists), SQL.CreateTable(LocalList.DetailsTable), SQL.CreateTable(LocalList.ItemsTable));//, SQL.CreateTable(Movies));
            if ((await UserInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).First() == 0)
            {
                await UserInfo.ExecuteAsync(string.Format("insert into {0} ({1}) values (?), (?), (?)", LocalList.DetailsTable, LocalList.DetailsCols.ID), 0, 1, 2);
                //await SQL.ExecuteAsync(string.Format("insert into {0} values (?), (?), (?)", SyncLists, 0, 1, 2);
            }

#if DEBUG
            Print.Log("stored movies");
            foreach (var row in await ItemInfo.QueryAsync<(int, string, int)>("select * from " + Movies.Name + " limit 10"))
            {
                Print.Log(row);
            }

            Print.Log("stored collections");
            foreach (var row in await ItemInfo.QueryAsync<(int, string, int?, string, string)>("select * from " + Collections.Name + " limit 10"))
            {
                Print.Log(row);
            }

            Print.Log("synced lists");
            foreach (var row in await UserInfo.QueryAsync<(string, string, string, string)>("select * from " + SyncLists.Name))
            {
                Print.Log(row);
            }

            Print.Log("local lists", (await UserInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).FirstOrDefault());
            foreach (var row in await UserInfo.QueryAsync<(string, string, string, string, string, string, string)>("select * from " + LocalList.DetailsTable.Name))
            {
                Print.Log(DateTime.TryParse(row.Item7, out var date) ? date.ToLocalTime().ToString() : "", row);
            }

            //Print.Log(DateTime.Parse((await UserInfo.QueryScalarsAsync<string>("SELECT CURRENT_TIMESTAMP")).FirstOrDefault()));
            Print.Log("items in lists", (await UserInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.ItemsTable))).FirstOrDefault());
            foreach (var row in await UserInfo.QueryAsync<(int?, int?, int?, string)>("select * from " + LocalList.ItemsTable.Name + " limit 10"))
            {
                Print.Log(row);
            }

            var history = await GetHistory();
            //await UserInfo.ExecuteAsync($"delete from {LocalList.ItemsTable} where {LocalList.ItemsCols.LIST_ID} = ?", history.ID);
            //return;
            if (history.Count > 0)
            {
                Print.Log("not adding");
                return;
            }
            var temp = await UserInfo.ExecuteAsync($"delete from {LocalList.ItemsTable} where {LocalList.ItemsCols.LIST_ID} = ?", history.ID);

            var data = MyMovies.Split("\r\n").ToList<string>();
            var movies = new List<Movie>();
            foreach (var line in data)
            {
                var info = line.Split(',');

                if (info.Length != 3 || !int.TryParse(info[1], out var year) || !int.TryParse(info[2], out var id))
                {
                    continue;
                }

                movies.Add(new Movie(info[0], year).WithID(TMDB.IDKey, id));
            }

            await history.AddAsync(movies);
            Print.Log("done");
#endif
        }

        public async Task Clean()
        {
            await Init;
            await ItemInfo.ExecuteAsync($"attach database '{GetFilePath(UserInfoDBFilename)}' as items");

            var cleaning = new List<Task<int>>();

            foreach (var kvp in ItemTables)
            {
                //Print.Log(string.Join(',', await ItemInfo.QueryAsync<ValueTuple<string>>($"select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?", kvp.Key)));
                //Print.Log($"delete from {kvp.Value} where {kvp.Value.Columns[0]} not in (select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?)");
                var id = kvp.Value.Columns[0];
                var userLists = $"(select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?)";
                cleaning.Add(ItemInfo.ExecuteAsync($"delete from {kvp.Value} where {id} not in {userLists}", kvp.Key));
                cleaning.Add(ItemInfo.ExecuteAsync($"delete from {SQLJsonCache.TABLE_NAME} where {ItemInfoCache.TYPE} = ? and {ItemInfoCache.ID} not in {userLists}", kvp.Key, kvp.Key));
            }

            var rows = await Task.WhenAll(cleaning);
#if DEBUG
            Print.Log($"cleaned {rows.Sum()} rows from item info");
#endif

            await Task.WhenAll(new SQLiteAsyncConnection[] { ItemInfo, UserInfo }.Select(db => db.ExecuteAsync("vacuum")));
        }

        private class Query<T> { public T Value { get; set; } }

        public Task<List<(string, string)>> GetMyLists() => UserInfo.QueryAsync<(string, string)>(string.Format("select {0} from {1}", SQLiteExtensions.SelectFrom(SyncListsCols.ID.Name, SyncListsCols.SOURCE.Name), SyncLists.Name));

        public async Task<List<(string Name, string ID)>> GetSyncsWithAsync(string source, string id)
        {
            var result = new List<(string, string)>();

            foreach (var row in await UserInfo.QueryAsync<(string, string, string, string)>(string.Format("select * from {0} where ({1} = ? and {2} = ?) or ({3} = ? and {4} = ?)", SyncLists, SyncListsCols.SOURCE, SyncListsCols.ID, SyncListsCols.SYNC_SOURCE, SyncListsCols.SYNC_ID), source, id, source, id))
            {
                (string, string) sync;
                if (row.Item2.Equals(source) && row.Item1.Equals(id))
                {
                    sync = (row.Item4, row.Item3);
                }
                else
                {
                    sync = (row.Item2, row.Item1);
                }

                if (!result.Contains(sync))
                {
                    result.Add(sync);
                }
            }

            return result;
        }

        public Task SetSyncsWithAsync(string source, string id, string syncSource, string syncID) => id == null || syncID == null ? Task.CompletedTask : UserInfo.ExecuteAsync(string.Format("insert or ignore into {0} values (?,?,?,?)", SyncLists), id, source, syncID, syncSource);
        public Task RemoveSyncsWithAsync(string source, string id, string syncSource, string syncID) => UserInfo.ExecuteAsync(string.Format("delete from {0} where {1} = ? and {2} = ? and {3} = ? and {4} = ?", SyncLists, SyncListsCols.ID, SyncListsCols.SOURCE, SyncListsCols.SYNC_ID, SyncListsCols.SYNC_SOURCE), id, source, syncID, syncSource);

        [Flags]
        public enum ItemType
        {
            Movie = 1,
            TVShow = 2,
            TVSeason = 4,
            TVEpisode = 8,
            Person = 16,
            Collection = 32,
            List = 64,
            Company = 128,
            Network = 256,
            WatchProvider = 512,
            All = ~0,
            AllMedia = Movie | TVShow | TVSeason | TVEpisode,
            AllCollections = Collection | List,
            AllCompanies = Company | Network | WatchProvider
        }

        private async Task StoreInfo(Item item, int id)
        {
            Table table = null;
            object[] values = null;

            if (item is Movie movie)
            {
                table = Movies;
                values = new object[] { id, movie.Title, movie.Year };
            }
            else if (item is TVShow show)
            {
                table = TVShows;
                values = new object[] { id, show.Title };
            }
            else if (item is Person person)
            {
                table = People;
                values = new object[] { id, person.Name, person.BirthYear };
            }
            else if (item.ItemType == Test.ItemType.Collection && item is Collection collection)
            {
                table = Collections;
                values = new object[] { id, collection.Name, collection.Count, collection.Description, collection.PosterPath };
            }

            if (table != null)
            {
                string test = string.Join(',', Enumerable.Repeat("?", values.Length));
                await ItemInfo.ExecuteAsync(string.Format("insert or replace into {0} values (" + test + ")", table), values);
            }
        }

        public bool TryGetID(Item item, out int id)
        {
            if (IDSystem.TryGetID(item, out id))
            {
                _ = StoreInfo(item, id);
                return true;
            }

            return false;
        }

        public async Task<Item> GetItem(Movies.ItemType type, int id)
        {
            Item item = null;

            if (type == Test.ItemType.Movie)
            {
                var info = await ItemInfo.QueryAsync<(int, string, int)>(string.Format("select * from {0} where {1} = ?", Movies, MovieCols.ID), id);

                if (info.Count > 0)
                {
                    item = new Movie(info[0].Item2, info[0].Item3);
                }
            }
            else if (type == Test.ItemType.TVShow)
            {
                var info = await ItemInfo.QueryAsync<(int, string)>(string.Format("select * from {0} where {1} = ?", TVShows, TVCols.ID), id);

                if (info.Count > 0)
                {
                    item = new TVShow(info[0].Item2);
                }
            }
            else if (type == Test.ItemType.Person)
            {
                var info = await ItemInfo.QueryAsync<(int, string, int)>(string.Format("select * from {0} where {1} = ?", People, PersonCols.ID), id);

                if (info.Count > 0)
                {
                    item = new Person(info[0].Item2, info[0].Item3);
                }
            }
            else if (type == Test.ItemType.Collection)
            {
                var info = await ItemInfo.QueryAsync<(int, string, int?, string, string)>(string.Format("select * from {0} where {1} = ?", Collections, CollectionCols.ID), id);

                if (info.Count > 0)
                {
                    item = new Collection
                    {
                        Name = info[0].Item2,
                        Count = info[0].Item3,
                        Description = info[0].Item4,
                        PosterPath = info[0].Item5
                    };
                }
            }

            if (item == null)
            {
                try
                {
                    item = await IDSystem.GetItem(type, id);
                    await StoreInfo(item, id);
                }
                catch (Exception e)
                {
#if DEBUG
                    Print.Log(e);
#endif
                }
            }
            else
            {
                if (item is Collection collection && IDSystem is TMDB tmdb)
                {
                    collection.Items = tmdb.GetCollectionItems(item, id);
                }

                item.SetID(IDKey, id);
            }

            return item;
        }

        public class LocalList : List
        {
            public static class DetailsCols
            {
                public static readonly Table.Column ID = new Table.Column("ID", "integer", "primary key not null unique");
                public static readonly Table.Column ALLOWED_TYPES = new Table.Column("AllowedTypes", "integer");
                public static readonly Table.Column NAME = new Table.Column("Name", SQLTypeFromProperty<List>(nameof(Name)));
                public static readonly Table.Column DESCRIPTION = new Table.Column("Description", SQLTypeFromProperty<List>(nameof(Description)));
                public static readonly Table.Column AUTHOR = new Table.Column("Author", SQLTypeFromProperty<List>(nameof(Author)));
                public static readonly Table.Column PRIVATE = new Table.Column("Private", SQLTypeFromProperty<List>(nameof(Public)));
                //public static readonly Table.Column EDITABLE = new Table.Column("Editable", SQLTypeFromProperty<List>(nameof(Editable)));
                public static readonly Table.Column UPDATED_AT = new Table.Column("UpdatedAt", "datetime");
            }

            public static class ItemsCols
            {
                public static readonly Table.Column LIST_ID = new Table.Column("ListID", "integer");
                public static readonly Table.Column ITEM_ID = new Table.Column("ItemID", "integer");
                public static readonly Table.Column ITEM_TYPE = new Table.Column("ItemType", "integer");
                public static readonly Table.Column DATE_ADDED = new Table.Column("DateAdded", "datetime default current_timestamp");
            }

            public static readonly Table DetailsTable = new Table(DetailsCols.ID, DetailsCols.ALLOWED_TYPES, DetailsCols.NAME, DetailsCols.DESCRIPTION, DetailsCols.AUTHOR, DetailsCols.PRIVATE, DetailsCols.UPDATED_AT) { Name = "LocalLists" };
            public static readonly Table ItemsTable = new Table(ItemsCols.LIST_ID, ItemsCols.ITEM_ID, ItemsCols.ITEM_TYPE, ItemsCols.DATE_ADDED) { Name = "LocalListItems" };

            public SQLiteAsyncConnection SQLDB;
            public IAssignID<int> IDSystem;
            public override string ID => Id.ToString();

            private int Id;

            public LocalList(SQLiteAsyncConnection database, int id)
            {
                SQLDB = database;
                Id = id;

                Items = TryFilter(FilterPredicate.TAUTOLOGY, out _);// GetItems();
#if DEBUG
                if (id == 1)
                {
                    //Items = MockData.Instance.GetTrending();
                }
#endif
            }

            private object GetUpdatedAt() => "current_timestamp";

            private async Task Updated() => await SQLDB.ExecuteAsync(string.Format("update {0} set {1} = {2} where {3} = ?", DetailsTable, DetailsCols.UPDATED_AT, GetUpdatedAt(), DetailsCols.ID), ID);

            protected override async Task<bool> AddAsyncInternal(IEnumerable<Item> items)
            {
                var rows = new List<List<object>>();
                foreach (var item in items)
                {
                    if (IDSystem.TryGetID(item, out var id) && !await Contains(id, item.ItemType))
                    {
                        rows.Add(new List<object> { Id, id, AppItemTypeToDatabaseItemType(item.ItemType) });
                    }
                }

                if (Id >= 0 && Id <= 2)
                {
                    //rows.Reverse();
                }

                //var rows = newItems.Select(item => new List<object> { Id, IDSystem.TryGetID(item, out var id) ? (int?)id : null, AppItemTypeToDatabaseItemType(item.ItemType) }).Where(row => row[1] != null).SelectMany(row => row).ToArray();//.Aggregate<IEnumerable<object>>((a, b) => a.Concat(b)).ToArray();

                if (rows.Count > 0)
                {
                    string values = string.Join(',', Enumerable.Repeat("(?,?,?)", rows.Count));
                    var modified = await SQLDB.ExecuteAsync(string.Format("insert into {0}({1}) values " + values, ItemsTable, string.Join(',', ItemsTable.Columns.SkipLast(1))), rows.SelectMany(row => row).ToArray());

                    if (modified > 0)
                    {
                        await Updated();
                    }

                    return modified == rows.Count;
                }

                return true;
            }

            protected async override Task<bool?> ContainsAsyncInternal(Item item) => IDSystem.TryGetID(item, out var id) && await Contains(id, item.ItemType);

            private async Task<bool> Contains(int id, Test.ItemType itemType) => (await SQLDB.ExecuteScalarAsync<int>(string.Format("select count(*) from {0} where {1} = ? and {2} = ? and {3} = ?", ItemsTable, ItemsCols.LIST_ID, ItemsCols.ITEM_ID, ItemsCols.ITEM_TYPE), Id, id, AppItemTypeToDatabaseItemType(itemType))) == 1;

            public override Task<int> CountAsync() => SQLDB.ExecuteScalarAsync<int>(string.Format("select count(*) from {0} where {1} = ?", ItemsTable, ItemsCols.LIST_ID), Id);

            protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Item> items)
            {
                bool success = true;
                int total = 0;

                foreach (var item in items)
                {
                    int rows = 0;

                    if (IDSystem.TryGetID(item, out var id))
                    {
                        rows = await SQLDB.ExecuteAsync(string.Format("delete from {0} where {1} = ? and {2} = ? and {3} = ?", ItemsTable, ItemsCols.LIST_ID, ItemsCols.ITEM_ID, ItemsCols.ITEM_TYPE), Id, id, AppItemTypeToDatabaseItemType(item.ItemType));
                        total += rows;
                    }

                    if (rows == 0)
                    {
                        success = false;
                    }
                }

                if (total > 0)
                {
                    await Updated();
                }

                return success;
            }

            public override async Task Update()
            {
                //while (await test.ExecuteScalarAsync<int>(string.Format("select count(*) from {0} where {1} = ?", LocalLists.Name, LocalListsCols.ID.Name), list.ID) > 0)
                if (Id >= 0 && Id <= 2)
                {
                    await Updated();
                    return;
                }

                object[] values = new object[]
                {
                    AppItemTypeToDatabaseItemType(AllowedTypes),
                    Name,
                    Description,
                    Author,
                    Public
                };

                var cols = DetailsTable.Columns.Where(col => col != DetailsCols.ID).Select(col => col.Name);

                if (Id == -1)
                {
                    await SQLDB.ExecuteAsync(string.Format("insert into {0} ({1}) values ({2},{3})", DetailsTable, string.Join(",", cols), string.Join(",", Enumerable.Repeat("?", values.Length)), GetUpdatedAt()), values);
                    Id = (await SQLDB.QueryScalarsAsync<int>("select last_insert_rowid()")).First();
                }
                else
                {
                    await SQLDB.ExecuteAsync(string.Format("update {0} set {1}{4} where {2} = {3}", DetailsTable, string.Join(",", cols.Select(col => col + "=?")).TrimEnd('?'), DetailsCols.ID, Id, GetUpdatedAt()), values);
                }
            }

            public async override Task Delete()
            {
                if (Id <= 2)
                {
                    return;
                }

                var details = SQLDB.ExecuteAsync(string.Format("delete from {0} where {1} = ?", DetailsTable, DetailsCols.ID), Id);
                var items = SQLDB.ExecuteAsync(string.Format("delete from {0} where {1} = ?", ItemsTable, ItemsCols.LIST_ID), Id);

                await Task.WhenAll(details, items);
            }

            public override IAsyncEnumerable<Item> TryFilter(FilterPredicate filter, out FilterPredicate partial, CancellationToken cancellationToken = default)
            {
                var types = Models.ItemHelpers.RemoveTypes(filter, out partial);
                return GetItemsAsync(types, cancellationToken);
                //return ItemHelpers.Filter(GetItemsAsync(types, cancellationToken), filter, cancellationToken);
            }

            private async IAsyncEnumerable<Item> GetItemsAsync(List<Type> types, CancellationToken cancellationToken = default)
            {
                var cols = SQLiteExtensions.SelectFrom(ItemsCols.ITEM_ID.Name, ItemsCols.ITEM_TYPE.Name, ItemsCols.DATE_ADDED.Name);
                var query = $"select {cols} from {ItemsTable} where {ItemsCols.LIST_ID} = ?";

                if (types.Count > 0)
                {
                    var typeFilter = types.TrySelect<Type, Test.ItemType>(App.TypeMap.TryGetValue).Select(type => (int)AppItemTypeToDatabaseItemType(type));
                    query += $" and {ItemsCols.ITEM_TYPE} in ({string.Join(',', typeFilter)})";
                }

                IEnumerable<(int?, int?, string)> items = await SQLDB.QueryAsync<(int?, int?, string)>(query, Id);

                if (Reverse)
                {
                    // items must be reversed to properly order items that were added at the same time
                    items = items.Reverse().OrderByDescending(item => item.Item3);
                }
                else
                {
                    items = items.OrderBy(item => item.Item3);
                }

                //for (int i = items.Count - 1; i >= 0; i--)
                //for (int i = 0; i < items.Count; i++)
                foreach (var row in items)
                {
                    //var row = items[Reverse ? items.Count - 1 - i : i];
                    //var row = items[i];
                    if (!row.Item1.HasValue || !row.Item2.HasValue)
                    {
                        continue;
                    }

                    Item item = await IDSystem.GetItem(DatabaseItemTypeToAppItemType((ItemType)row.Item2), row.Item1.Value);

                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }

            private async IAsyncEnumerable<Item> GetItems1()
            {
                /*var knownAddDate = await SQLDB.QueryAsync<(int?, int?)>(string.Format("select {0} from {1} where {3} is not null and {2} = ? order by {3} {4}", SQLiteExtensions.SelectFrom(ItemsCols.ITEM_ID.Name, ItemsCols.ITEM_TYPE.Name), ItemsTable, ItemsCols.LIST_ID, ItemsCols.DATE_ADDED, Reverse ? "desc" : "asc"), Id);
                var unknownAddDate = await SQLDB.QueryAsync<(int?, int?)>(string.Format("select {0} from {1} where {3} is null and {2} = ?", SQLiteExtensions.SelectFrom(ItemsCols.ITEM_ID.Name, ItemsCols.ITEM_TYPE.Name), ItemsTable, ItemsCols.LIST_ID, ItemsCols.DATE_ADDED), Id);
                Count = knownAddDate.Count + unknownAddDate.Count;

                IEnumerable<(int?, int?)> items = knownAddDate;
                if (Reverse)
                {
                    items = items.Concat(Enumerable.Reverse(unknownAddDate));
                }
                else
                {
                    items = unknownAddDate.Concat(items);
                }*/

                IEnumerable<(int?, int?, string)> items = await SQLDB.QueryAsync<(int?, int?, string)>(string.Format("select {0} from {1} where {2} = ?", SQLiteExtensions.SelectFrom(ItemsCols.ITEM_ID.Name, ItemsCols.ITEM_TYPE.Name, ItemsCols.DATE_ADDED.Name), ItemsTable, ItemsCols.LIST_ID), Id);

                if (Reverse)
                {
                    items = items.Reverse().OrderByDescending(item => item.Item3);
                }
                else
                {
                    items = items.OrderBy(item => item.Item3);
                }

                //for (int i = items.Count - 1; i >= 0; i--)
                //for (int i = 0; i < items.Count; i++)
                foreach (var row in items)
                {
                    //var row = items[Reverse ? items.Count - 1 - i : i];
                    //var row = items[i];
                    if (!row.Item1.HasValue || !row.Item2.HasValue)
                    {
                        continue;
                    }

                    Item item = await IDSystem.GetItem(DatabaseItemTypeToAppItemType((ItemType)row.Item2), row.Item1.Value);

                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }

            public static ItemType AppItemTypeToDatabaseItemType(Movies.ItemType type)
            {
                return (ItemType)type;
            }

            public static Movies.ItemType DatabaseItemTypeToAppItemType(ItemType type)
            {
                return (Movies.ItemType)type;
            }
        }

        #region IListSource Implementation

        public async Task<List> GetHistory()
        {
            var list = await GetListAsync("0");
            list.AllowedTypes = Test.ItemType.Movie | Test.ItemType.TVShow;
            list.LastUpdated = null;
            return list;
        }
        public async Task<List> GetWatchlist()
        {
            var list = await GetListAsync("1");
            list.AllowedTypes = Test.ItemType.Movie | Test.ItemType.TVShow;
            return list;
        }

        public Task<List> GetFavorites() => GetListAsync("2");

        public async IAsyncEnumerable<List> GetAllListsAsync()
        {
            //foreach (int id in await test.QueryAsync<int>(string.Format("select {0} from {1}", LocalListsCols.ID, LocalList.DetailsTable)))
            var lists = await QueryListsAsync(string.Format(" where {0} > 2", LocalList.DetailsCols.ID));
            lists.Reverse();
            foreach (var list in lists)
            {
                yield return list;
            }
        }

        public List CreateList() => CreateList(-1);
        private List CreateList(int id, Test.ItemType allowedTypes = Test.ItemType.All) => new LocalList(UserInfo, id)
        {
            IDSystem = this,
            AllowedTypes = allowedTypes
        };

        public async Task DeleteListAsync(int listID)
        {
            await UserInfo.ExecuteAsync(string.Format("delete from {0} where {1} = ?", LocalList.ItemsTable.Name, LocalList.ItemsCols.LIST_ID.Name), listID);
            await UserInfo.ExecuteAsync(string.Format("delete from {0} where {1} = ?", LocalList.DetailsTable.Name, LocalList.DetailsCols.ID.Name), listID);
        }

        public async Task<List> GetListAsync(string id) => !int.TryParse(id, out var intID) || intID < 0 ? null : (await QueryListsAsync(string.Format("where {0} = ?", LocalList.DetailsCols.ID), id)).FirstOrDefault();

        private async Task<List<List>> QueryListsAsync(string where = null, params object[] args)
        {
            var query = await UserInfo.QueryAsync<(int, int?, string, string, string, bool, string)>(string.Format("select {0} from {1} " + where, string.Join(',', (object[])LocalList.DetailsTable.Columns), LocalList.DetailsTable), args);

            var lists = new List<List>();

            foreach (var row in query)
            {
                var list = new LocalList(UserInfo, row.Item1)
                {
                    IDSystem = this,
                    Name = row.Item3,
                    Description = row.Item4,
                    Author = row.Item5,
                    Public = row.Item6,
                };

                if (row.Item2.HasValue)
                {
                    list.AllowedTypes = LocalList.DatabaseItemTypeToAppItemType((ItemType)row.Item2.Value);
                }
                if (DateTime.TryParse(row.Item7, out var date))
                {
                    list.LastUpdated = date;
                }
                list.Count = await list.CountAsync();

                lists.Add(list);
            }

            return lists;
        }

        /*private int? ObjectToID(object item)
        {
            byte itemCode = (byte)Math.Log((int)AppItemTypeToDatabaseItemType(App.GetItemType(item)), 2);
            
            int itemID;
            if (item is MediaViewModel media) itemID = media.ID;
            else if (item is PersonViewModel person) itemID = person.ID;
            else if (item is Collection collection) itemID = collection.ID;
            else return null;
            
            return (itemID << (sizeof(byte) * 8)) | itemCode;
        }*/

        /*private Task<object> IDToObject(int id)
        {
            int itemCode = id & byte.MaxValue;
            int itemID = id >> (sizeof(byte) * 8);
            ItemType itemType = (ItemType)(1 << itemCode);
#if DEBUG
            Print.Log("id " + id + " to object", itemCode, itemID, itemType);
#endif
            return App.GetItem(itemID, DatabaseItemTypeToAppItemType(itemType));
        }*/
        #endregion

        /*public async Task<IEnumerable<object>> GetItemsInList(int listID, IDataSource dataSource, int limit = -1, int offset = 0)
        {
            List<object> items = new List<object>();

            string moviesInList = "MoviesInList";
            string listIDCol = "ListID";
            string movieIDCol = "MovieID";
            string typeCol = "Type";
            string titleCol = "Title";

            await test.ExecuteAsync("drop table " + moviesInList);
            await test.ExecuteAsync(string.Format("create table if not exists {0}({1} int, {2} int, {3} varchar, {4} varchar, primary key({1}, {2}))", moviesInList, listIDCol, movieIDCol, typeCol, titleCol));

            //Dictionary<int, List<int>> listIDs = new Dictionary<int, List<int>>();
            string range = limit > 0 ? string.Format(" limit {0} offset {1}", limit, offset) : string.Empty;
            foreach (var row in await test.QueryAsync<(int, int, string)>(string.Format("select {0} from {1} where {2} = {3}" + range, TupleMap(listIDCol, movieIDCol, typeCol), moviesInList, listIDCol, listID)))
            {
                Print.Log(row.Item1, row.Item2);

                if (row.Item3 == "Movie")
                {
                    items.Add(dataSource.GetMovieAsync(row.Item2));
                }

                /*List<int> list;
                if (!listIDs.TryGetValue(row.Item1, out list))
                {
                    list = new List<int>();
                }

                list.Add(row.Item2);
            }

            return items;
        }*/

        private static string SQLTypeFromProperty<T>(string propertyName, bool storeDateTimeAsTicks = true, bool storeTimeSpanAsTicks = true) => Orm.SqlType(new TableMapping.Column(typeof(T).GetProperty(propertyName)), storeDateTimeAsTicks, storeTimeSpanAsTicks);

#if DEBUG
        private static readonly string MyMovies = @"
Ice Age,2002,425
E.T. the Extra-Terrestrial,1982,601
Percy Jackson & the Olympians: The Lightning Thief,2010,32657
Race to Witch Mountain,2009,13836
Harry Potter and the Philosopher's Stone,2001,671
Shrek,2001,808
Despicable Me,2010,20352
Shark Tale,2004,10555
WALL·E,2008,10681
Frozen,2013,109445
Finding Nemo,2003,12
Madagascar,2005,953
National Treasure,2004,2059
The Water Horse,2007,54318
The Lorax,2012,73723
Throw Momma from the Train,1987,11896
The Pink Panther,2006,12096
Turner & Hooch,1989,6951
Night at the Museum,2006,1593
Rocky,1976,1366
Ender's Game,2013,80274
The Martian,2015,286217
Get Smart,2008,11665
Iron Man,2008,1726
The Avengers,2012,24428
Arrival,2016,329865
John Carter,2012,49529
Clue,1985,15196
Fantastic Beasts and Where to Find Them,2016,259316
Lemony Snicket's A Series of Unfortunate Events,2004,11774
Three Days of the Condor,1975,11963
The Matrix,1999,603
A Beautiful Mind,2001,453
Transformers,2007,1858
Planet of the Apes,1968,871
Bad Boys,1995,9737
Predator,1987,106
The Italian Job,2003,9654
Sherlock Holmes,2009,10528
Cast Away,2000,8358
The Mummy,1999,564
Interstellar,2014,157336
I Am Number Four,2011,46529
Elf,2003,10719
Star Wars,1981,11
Jurassic Park,1993,329
Happy Gilmore,1996,9614
Happy Feet,2006,9836
Underworld,2003,277
The Fast and the Furious,2001,9799
The Bourne Identity,2002,2501
Se7en,1995,807
The Abyss,1989,2756
Trading Places,1983,1621
Tremors,1990,9362
Under Siege,1992,8845
The Terminator,1984,218
Black Hawk Down,2001,855
Lethal Weapon,1987,941
Brubaker,1980,1623
Sharky's Machine,1981,14664
U.S. Marshals,1998,11808
Minority Report,2002,180
The Usual Suspects,1995,629
Total Recall,1990,861
Searching for Bobby Fischer,1993,14291
The Last Stand,2013,76640
The Last Starfighter,1984,11884
Léon: The Professional,1994,101
Resident Evil,2002,1576
A.I. Artificial Intelligence,2001,644
Executive Decision,1996,2320
Alien,1979,348
Mad Max: Fury Road,2015,76341
Inside Out,2015,150540
King Kong,2005,254
Gravity,2013,49047
Twelve Monkeys,1995,63
Now You See Me,2013,75656
Toy Story,1995,862
Argo,2012,68734
Zootopia,2016,269149
The Jungle Book,1967,9325
The Jungle Book,2016,278927
Star Trek: The Motion Picture,1979,152
Jaws,1975,578
Hidden Figures,2016,381284
John Wick,2014,245891
Guardians of the Galaxy,2014,118340
Rise of the Guardians,2012,81188
Gifted,2017,400928
Kubo and the Two Strings,2016,313297
Captain America: The First Avenger,2011,1771
The Nice Guys,2016,290250
Eye in the Sky,2016,333352
Doctor Strange,2016,284052
10 Cloverfield Lane,2016,333371
Cloverfield,2008,7191
Rogue One: A Star Wars Story,2016,330459
Deadpool,2016,293660
Pete's Dragon,2016,294272
Lion,2016,334543
The Lion King,1994,8587
Kung Fu Panda,2008,9502
Creed,2015,312221
Shaun the Sheep Movie,2015,263109
Mission: Impossible,1996,954
Sicario,2015,273481
Paddington,2015,116149
Spy Kids,2001,10054
Bridge of Spies,2015,296098
Trainwreck,2015,271718
How to Train Your Dragon,2010,10191
The Peanuts Movie,2015,227973
The Lego Movie,2014,137106
Edge of Tomorrow,2014,137113
X-Men,2000,36657
The Imitation Game,2014,205596
Big Hero 6,2014,177572
Big,1988,2280
Tim's Vermeer,2013,212063
""McFarland, USA"",2015,228203
The Hunger Games,2012,70160
Saving Mr. Banks,2013,140823
Looper,2012,59967
Kingsman: The Secret Service,2015,207703
The Secret World of Arrietty,2012,51739
Life of Pi,2012,87827
Frankenweenie,2012,62214
ParaNorman,2012,77174
Wreck-It Ralph,2012,82690
Moneyball,2011,60308
Rango,2011,44896
Super 8,2011,37686
Puss in Boots,2011,417859
Thor,2011,10195
True Lies,1994,36955
True Grit,2010,44264
Inception,2010,27205
The Other Guys,2010,27581
Fantastic Mr. Fox,2009,10315
How the Grinch Stole Christmas,2000,8871
Robin Hood: Men in Tights,1993,8005
Nacho Libre,2006,9353
Batman v Superman: Dawn of Justice,2016,209112
Twilight Zone: The Movie,1983,15301
Manhunter,1986,11454
Hancock,2008,8960
Total Recall,2012,64635
The Prestige,2006,1124
The Good Dinosaur,2015,105864
Monsters University,2013,62211
Ratatouille,2007,2062
Cars,2006,920
The Incredibles,2004,9806
""Monsters, Inc."",2001,585
A Bug's Life,1998,9487
Coraline,2009,14836
Ponyo,2009,12429
Tomorrowland,2015,158852
Avatar,2010,19995
Cloudy with a Chance of Meatballs,2009,22794
Where the Wild Things Are,2009,16523
Bolt,2008,13053
The Spiderwick Chronicles,2008,8204
Horton Hears a Who!,2008,12222
Ella Enchanted,2004,14442
Bridge to Terabithia,2007,1265
Die Hard,1988,562
Akeelah and the Bee,2006,13751
Suicide Squad,2016,297761
Charlotte's Web,2006,9986
Over the Hedge,2006,7518
A Grand Day Out,1990,530
Howl's Moving Castle,2005,4935
Charlie and the Chocolate Factory,2005,118
Serenity,2005,16320
""The Chronicles of Narnia: The Lion, the Witch and the Wardrobe"",2005,411
Pitch Perfect,2012,114150
Pitch Black,2000,2787
Spider-Man,2002,557
Million Dollar Arm,2014,198185
Hotel Transylvania,2012,76492
Hotel Rwanda,2004,205
School of Rock,2003,1584
Kill Bill: Vol. 1,2003,24
Pirates of the Caribbean: The Curse of the Black Pearl,2003,22
Holes,2003,8326
Spirited Away,2002,129
Catch Me If You Can,2002,640
Ocean's Eleven,2001,161
Chicken Run,2000,7443
Shanghai Noon,2000,8584
""O Brother, Where Art Thou?"",2000,134
Remember the Titans,2000,10637
Unbreakable,2000,9741
Dinosaur,2000,10567
The Iron Giant,1999,10386
October Sky,1999,13466
The Hunt for Red October,1990,1669
The Sixth Sense,1999,745
Austin Powers: International Man of Mystery,1997,816
Mulan,1998,10674
The Legend of Zorro,2005,1656
Babe,1995,9598
Blade,1998,36647
Blade Runner,1982,78
Mighty Joe Young,1998,9822
The Mighty Ducks,1992,10414
Deep Impact,1998,8656
Good Will Hunting,1997,489
Men in Black,1997,607
Hercules,2014,184315
Air Force One,1997,9772
The Fifth Element,1997,18
Pacific Rim,2013,68726
Taken,2009,8681
Lockout,2012,81796
Cloud Atlas,2012,83542
Fargo,1996,275
James and the Giant Peach,1996,10539
The Hunchback of Notre Dame,1996,10545
Independence Day,1996,602
Pulp Fiction,1994,680
Forrest Gump,1994,13
Stargate,1994,2164
Beverly Hills Cop,1984,90
Groundhog Day,1993,137
The Nightmare Before Christmas,1993,9479
The Secret Garden,1993,11236
The Sandlot,1993,11528
Aladdin,1992,812
A League of Their Own,1992,11287
Beauty and the Beast,1991,10020
Teenage Mutant Ninja Turtles,2014,98566
Back to the Future,1985,105
Home Alone,1990,771
The Little Mermaid,1989,10144
Field of Dreams,1989,2323
Dead Poets Society,1989,207
Who Framed Roger Rabbit,1988,856
My Neighbor Totoro,1989,8392
The Princess Bride,1987,2493
""Planes, Trains and Automobiles"",1987,2609
Paper Planes,2015,286875
RoboCop,2014,97020
RoboCop,1987,5548
Hoosiers,1986,5693
Ferris Bueller's Day Off,1986,9377
Top Gun,1986,744
Ghostbusters,2016,43074
Ghostbusters,1984,620
The Karate Kid,1984,1885
Romancing the Stone,1984,9326
Poltergeist,1982,609
The Thing,1982,1091
Tron,1982,97
Raiders of the Lost Ark,1981,85
For Your Eyes Only,1981,699
The Shining,1980,694
The Fog,2005,791
Close Encounters of the Third Kind,1977,840
Young Frankenstein,1974,3034
The Sting,1973,9277
Dirty Harry,1971,984
Butch Cassidy and the Sundance Kid,1969,642
The Sound of Music,1965,15121
The Birds,1963,571
The Great Escape,1963,5925
To Kill a Mockingbird,1962,595
3 Days to Kill,2014,192102
The Magnificent Seven,1960,966
Singin' in the Rain,1952,872
The Day the Earth Stood Still,1951,828
Green Lantern,2011,44912
The Hurt Locker,2009,12162
Castle in the Sky,1989,10515
Kiki's Delivery Service,1998,16859
Whisper of the Heart,1995,37797
From Up on Poppy Hill,2012,83389
Ant-Man,2015,102899
The Incredible Hulk,2008,1724
Alvin and the Chipmunks,2007,6477
Jack Ryan: Shadow Recruit,2014,137094
The Da Vinci Code,2006,591
Divergent,2014,157350
The Expendables,2010,27578
The Smurfs,2011,41513
Smokey and the Bandit,1977,11006
The Transporter,2002,4108
Free Willy,1993,1634
Free Birds,2013,175574
Escape to Witch Mountain,1975,14821
The Bad News Bears,1976,23479
Highlander,1986,8009
Snow White and the Seven Dwarfs,1983,408
One Hundred and One Dalmatians,1961,12230
101 Dalmatians,1996,11674
Bambi,1942,3170
The Fox and the Hound,1981,10948
Tangled,2010,38757
The Wizard of Oz,1939,630
Elysium,2013,68724
It's a Wonderful Life,1946,1585
Miracle on 34th Street,1947,11881
Miracle on Ice,1981,6945
The Man with One Red Shoe,1985,10905
The Terminal,2004,594
Nightcrawler,2014,242582
Gremlins,1984,927
Pinocchio,1940,10895
Cinderella,1950,11224
Mary Poppins,1964,433
Lassie,1994,29918
A Christmas Story,1983,850
Freaky Friday,2003,10330
Solaris,2002,2103
The Hateful Eight,2015,273248
Jurassic World,2015,135397
Miss Peregrine's Home for Peculiar Children,2016,283366
Terminator Genisys,2015,87101
Jupiter Ascending,2015,76757
Alice in Wonderland,2010,12155
Prometheus,2012,70981
Penguins of Madagascar,2014,270946
Undisputed,2002,15070
Beetlejuice,1988,4011
The Polar Express,2004,5255
The Sorcerer's Apprentice,2010,27022
Chappie,2015,198184
Lucy,2014,240832
Pixels,2015,257344
World War Z,2013,72190
Home,2015,228161
Armageddon,1998,95
TRON: Legacy,2010,20526
Journey to the Center of the Earth,2008,88751
Riddick,2013,87421
Oblivion,2013,75612
Atlantis: The Lost Empire,2001,10865
Event Horizon,1997,8413
Æon Flux,2005,8202
The Giver,2014,227156
Short Circuit,1986,2605
Enemy Mine,1985,11864
Next,2007,1738
The Secret Life of Walter Mitty,2013,116745
Finding Neverland,2004,866
Brother Bear,2003,10009
Mr. Peabody & Sherman,2014,82703
xXx,2002,7451
Jack Reacher,2012,75780
The Equalizer,2014,156022
The Theory of Everything,2014,266856
American Ultra,2015,261392
Crossworlds,1997,17821
42,2013,109410
San Andreas,2015,254128
No Escape,2015,192141
Central Intelligence,2016,302699
Pain & Gain,2013,134374
Lone Survivor,2013,193756
Victor Frankenstein,2015,228066
Survivor,2015,334074
Alex Cross,2012,94348
Tower Heist,2011,59108
48 Hrs.,1982,150
""Alexander and the Terrible, Horrible, No Good, Very Bad Day"",2014,218778
Anchorman: The Legend of Ron Burgundy,2004,8699
The Nut Job,2014,227783
Monsters vs Aliens,2009,15512
Concussion,2015,321741
Hitch,2005,8488
The Legend of Bagger Vance,2000,4958
The Intern,2015,257211
Grudge Match,2013,64807
Christmas with the Kranks,2004,13673
Finding Dory,2016,127380
A Very Murray Christmas,2015,364067
Bee Movie,2007,5559
Rage,2014,242310
The Croods,2013,49519
Stolen,2012,127493
Lord of War,2005,1830
Gone in Sixty Seconds,2000,9679
RED,2010,39514
Matilda,1996,10830
The Adventures of Tintin,2011,17578
Parental Guidance,2012,88042
Roxanne,1987,11584
Flushed Away,2006,11619
Spectral,2016,324670
Sully,2016,363676
The 'Burbs,1989,11974
Splash,1984,2619
Flubber,1997,9574
Jumanji,1995,8844
Chitty Chitty Bang Bang,1968,11708
Escape Plan,2013,107846
Grumpy Old Men,1993,11520
Witness,1985,9281
Blackhat,2015,201088
Stone Cold,2005,31046
Alien: Covenant,2017,126889
Turbo,2013,77950
Rio,2011,46195
The Punisher,2004,7220
St. Vincent,2014,239563
R.I.P.D.,2013,49524
Surf's Up,2007,9408
Inkheart,2008,2309
Reasonable Doubt,2014,240916
S.W.A.T.,2003,9257
The Bourne Legacy,2012,49040
Jack Reacher: Never Go Back,2016,343611
Delivery Man,2013,146239
Max,2015,272878
Daddy's Home,2015,274167
Baby Geniuses,1999,22345
The Recruit,2003,1647
Into the Woods,2014,224141
Collateral Beauty,2016,345920
Black Sea,2015,246080
Anger Management,2003,9506
Star Wars: The Force Awakens,2016,140607
Hardball,2001,20857
Osmosis Jones,2001,12610
Legend of the Guardians: The Owls of Ga'Hoole,2010,41216
National Security,2003,11078
Tammy,2014,226486
Paul Blart: Mall Cop,2009,14560
The Benchwarmers,2006,9957
The Waterboy,1998,10663
The Golden Compass,2007,2268
""I, Frankenstein"",2014,100241
Babylon A.D.,2008,9381
Sinbad: Legend of the Seven Seas,2003,14411
Lucky Number Slevin,2006,186
Run All Night,2015,241554
Radio,2003,13920
The Finest Hours,2016,300673
Footloose,1984,1788
Maximum Conviction,2012,118683
Stakeout,1987,10859
The BFG,2016,267935
Tears of the Sun,2003,9567
16 Blocks,2006,2207
The Gunman,2015,266396
Non-Stop,2014,225574
American Sniper,2015,190859
The Miracle Worker,1962,1162
Unbroken,2014,227306
The Spy Next Door,2010,23172
The Accidental Spy,2002,11847
The 33,2015,293646
White House Down,2013,117251
Assassin's Bullet,2012,117923
Racing Stripes,2005,6439
The Man from U.N.C.L.E.,2015,203801
The Hobbit: An Unexpected Journey,2012,49051
Labyrinth,1986,13597
Igor,2008,14248
Slap Shot,1977,11590
Stuart Little,1999,10137
The Secret Life of Pets,2016,328111
Minions,2015,211672
The Curse of the Were-Rabbit,2005,533
Little Boy,2015,256962
Out of Time,2003,2116
Secondhand Lions,2003,13156
Poseidon,2006,503
Race,2016,323677
I Am Legend,2007,6479
Here Comes Mr. Jordan,1941,38914
Guardians of the Galaxy Vol. 2,2017,283995
John Wick: Chapter 2,2017,324552
Wild Card,2015,265208
Parker,2013,119283
Crank,2006,1948
Jesse Stone: Sea Change,2007,26114
Jesse Stone: Death in Paradise,2006,30347
Jesse Stone: Night Passage,2006,31047
Zoolander,2001,9398
Southpaw,2015,307081
Inferno,2016,207932
Joy,2015,274479
Jason Bourne,2016,324668
The Bourne Ultimatum,2007,2503
The Bourne Supremacy,2004,2502
Ocean's Thirteen,2007,298
Ocean's Twelve,2004,163
Captain America: Civil War,2016,271110
Avengers: Age of Ultron,2015,99861
Captain America: The Winter Soldier,2014,100402
Thor: The Dark World,2013,76338
Resident Evil: Retribution,2012,71679
Resident Evil: Afterlife,2010,35791
Resident Evil: Extinction,2007,7737
Resident Evil: Apocalypse,2004,1577
Underworld: Rise of the Lycans,2009,12437
Underworld: Evolution,2006,834
Transformers: Age of Extinction,2014,91314
Transformers: Dark of the Moon,2011,38356
Transformers: Revenge of the Fallen,2009,8373
Percy Jackson: Sea of Monsters,2013,76285
The Chronicles of Riddick,2004,2789
X-Men: Days of Future Past,2014,127585
Night at the Museum: Secret of the Tomb,2014,181533
The Wolverine,2013,76170
X-Men: First Class,2011,49538
X-Men Origins: Wolverine,2009,2080
X2,2003,36658
Sing,2016,335797
Now You See Me 2,2016,291805
Harry Potter and the Deathly Hallows: Part 2,2011,12445
Harry Potter and the Deathly Hallows: Part 1,2010,12444
Harry Potter and the Half-Blood Prince,2009,767
Harry Potter and the Order of the Phoenix,2007,675
Harry Potter and the Goblet of Fire,2005,674
Harry Potter and the Prisoner of Azkaban,2004,673
Harry Potter and the Chamber of Secrets,2002,672
Kung Fu Panda 3,2016,140300
How to Train Your Dragon 2,2014,82702
Blade II,2002,36586
Kung Fu Panda 2,2011,49444
Moana,2016,277834
RED 2,2013,146216
The Edge of Seventeen,2016,376660
The Pursuit of Happyness,2006,1402
The Longest Yard,2005,9291
Rio 2,2014,172385
Spider-Man: Homecoming,2017,315635
The Breakfast Club,1985,2108
D3: The Mighty Ducks,1996,10680
D2: The Mighty Ducks,1994,11164
Home Alone 3,1997,9714
Home Alone 2: Lost in New York,1992,772
Men in Black 3,2012,41154
Men in Black II,2002,608
Bad Boys II,2003,8961
The Smurfs 2,2013,77931
Cloudy with a Chance of Meatballs 2,2013,109451
Resident Evil: The Final Chapter,2017,173897
Trouble with the Curve,2012,87825
The Angry Birds Movie,2016,153518
District 9,2009,17654
Sullivan's Travels,1941,16305
X-Men: Apocalypse,2016,246655
Keeping Up with the Joneses,2016,331313
Psych: The Movie,2017,457840
Vice,2015,307663
Popstar: Never Stop Never Stopping,2016,341012
Life,2017,395992
The Accountant,2016,302946
A Christmas Tree Miracle,2013,233481
""I Want a Dog for Christmas, Charlie Brown"",2003,24251
Wonder Woman,2017,297762
Star Wars: The Last Jedi,2017,181808
2 Fast 2 Furious,2003,584
Star Wars: Episode III - Revenge of the Sith,2005,1895
Star Wars: Episode II - Attack of the Clones,2002,1894
Star Wars: Episode I - The Phantom Menace,1999,1893
Return of the Jedi,1983,1892
The Empire Strikes Back,1980,1891
A Dog's Purpose,2017,381289
A River Runs Through It,1992,293
Hell or High Water,2016,338766
The Magnificent Seven,2016,333484
Hacksaw Ridge,2016,324786
The Matrix Reloaded,2003,604
The Matrix Revolutions,2003,605
Pirates of the Caribbean: On Stranger Tides,2011,1865
Pirates of the Caribbean: At World's End,2007,285
Pirates of the Caribbean: Dead Man's Chest,2006,58
Bright,2017,400106
The Lego Batman Movie,2017,324849
Kong: Skull Island,2017,293167
Angels & Demons,2009,13448
Pirates of the Caribbean: Dead Men Tell No Tales,2017,166426
Storks,2016,332210
Promised Land,2012,133694
Shrek the Third,2007,810
Shrek 2,2004,809
Toy Story 3,2010,10193
Toy Story 2,1999,863
Madagascar 3: Europe's Most Wanted,2012,80321
Madagascar: Escape 2 Africa,2008,10527
Ice Age: Collision Course,2016,278154
Ice Age: Continental Drift,2012,57800
Ice Age: Dawn of the Dinosaurs,2009,8355
Ice Age: The Meltdown,2006,950
Despicable Me 2,2013,93456
Bad Moms,2016,376659
Must Love Dogs,2005,11648
Logan,2017,263115
About a Boy,2002,245
The Mummy Returns,2001,1734
The Boss Baby,2017,295693
Queen of Katwe,2016,317557
Patriots Day,2016,388399
Planet of the Apes,2001,869
Dawn of the Planet of the Apes,2014,119450
Rise of the Planet of the Apes,2011,61791
Conquest of the Planet of the Apes,1972,1688
Beneath the Planet of the Apes,1970,1685
Escape from the Planet of the Apes,1971,1687
Battle for the Planet of the Apes,1973,1705
Terminator 3: Rise of the Machines,2003,296
Terminator 2: Judgment Day,1991,280
Going in Style,2017,353070
Arsenic and Old Lace,1944,212
Constantine,2005,561
Up in the Air,2009,22947
Bleed for This,2016,332979
Pitch Perfect 2,2015,254470
Sausage Party,2016,223702
Ghostbusters II,1989,2978
The Cloverfield Paradox,2018,384521
Passengers,2008,13944
Avengers: Infinity War,2018,299536
Iron Man 3,2013,68721
Iron Man 2,2010,10138
Ghost in the Shell,2017,315837
Beverly Hills Cop III,1994,306
Beverly Hills Cop II,1987,96
The Hunger Games: Catching Fire,2013,101299
The Hunger Games: Mockingjay - Part 1,2014,131631
The Hunger Games: Mockingjay - Part 2,2015,131634
Rocky II,1979,1367
Rocky IV,1985,1374
Rocky III,1982,1371
Rocky V,1990,1375
Die Hard 2,1990,1573
Live Free or Die Hard,2007,1571
Die Hard: With a Vengeance,1995,1572
Lethal Weapon 4,1998,944
Lethal Weapon 2,1989,942
Lethal Weapon 3,1992,943
Blood Work,2002,9573
Valerian and the City of a Thousand Planets,2017,339964
The Village,2004,6947
Despicable Me 3,2017,324852
Blue Streak,1999,11001
Ocean's Eight,2018,402900
Night at the Museum: Battle of the Smithsonian,2009,18360
Paul Blart: Mall Cop 2,2015,256961
Mission: Impossible - Rogue Nation,2015,177677
Mission: Impossible - Ghost Protocol,2011,56292
Mission: Impossible II,2000,955
Mission: Impossible III,2006,956
Carol,2015,258480
Thor: Ragnarok,2017,284053
Logan Lucky,2017,399170
Journey 2: The Mysterious Island,2012,72545
Deepwater Horizon,2016,296524
The Hitman's Bodyguard,2017,390043
Spectre,2015,206647
Casino Royale,2006,36557
Megan Leavey,2017,424488
Atomic Blonde,2017,341013
War for the Planet of the Apes,2017,281338
Under Siege 2: Dark Territory,1995,3512
Jumanji: Welcome to the Jungle,2017,353486
""Murder, She Wrote: South by Southwest"",1997,238098
The Hunted,2003,10632
Baby Driver,2017,339403
Insurgent,2015,262500
The Lost World: Jurassic Park,1997,330
Transformers: The Last Knight,2017,335988
Jurassic Park III,2001,331
Dunkirk,2017,374720
The Expendables 3,2014,138103
Game of Death,2011,46541
The Expendables 2,2012,76163
Swiss Army Man,2016,347031
Incredibles 2,2018,260513
The Big Sick,2017,416477
Blade Runner 2049,2017,335984
Baywatch,2017,339846
The Lobster,2015,254320
Unthinkable,2010,38199
Extinction,2018,429415
No Reservations,2007,3638
Star Trek,2009,13475
The Blind Side,2009,22881
Looney Tunes: Back in Action,2003,10715
Black Panther,2018,284054
White Men Can't Jump,1992,10158
Fist Fight,2017,345922
The Karate Kid,2010,38575
Ex Machina,2015,264660
Her,2013,152601
Up,2009,14160
Sleight,2016,347882
Breakfast at Tiffany's,1961,164
Lara Croft: Tomb Raider,2001,1995
The Birdcage,1996,11000
CHiPS,2017,417644
Wind River,2017,395834
White Christmas,1954,13368
Eternal Sunshine of the Spotless Mind,2004,38
Grown Ups,2010,38365
Escape Plan 2: Hades,2018,440471
The Christmas Chronicles,2018,527435
Tomb Raider,2018,338970
The First Great Train Robbery,1979,11583
Isle of Dogs,2018,399174
Rampage,2018,427641
The Hitchhiker's Guide to the Galaxy,2005,7453
Bumblebee,2018,424783
Escape from Alcatraz,1979,10734
Monty Python and the Holy Grail,1975,762
Annihilation,2018,300668
Edward Scissorhands,1990,162
""Three Billboards Outside Ebbing, Missouri"",2017,359940
Blockers,2018,437557
Children of Men,2006,9693
Tag,2018,455980
Ant-Man and the Wasp,2018,363088
Them!,1954,11071
Uncle Drew,2018,474335
Captain Fantastic,2016,334533
Surrogates,2009,19959
Sicario: Day of the Soldado,2018,400535
Gone Baby Gone,2007,4771
Early Man,2018,387592
""Definitely, Maybe"",2008,8390
Okja,2017,387426
Death Wish,2018,395990
Back to the Future Part III,1990,196
Back to the Future Part II,1989,165
DodgeBall: A True Underdog Story,2004,9472
Deadpool 2,2018,383498
In the Valley of Elah,2007,6973
Salt,2010,27576
The Spectacular Now,2013,157386
Venom,2018,335983
Pacific Rim: Uprising,2018,268896
The Shawshank Redemption,1994,278
Ghost Town,2008,12797
Wanted,2008,8909
Jurassic World: Fallen Kingdom,2018,351286
Blade: Trinity,2004,36648
Captain Marvel,2019,299537
The Meg,2018,345940
Crazy Rich Asians,2018,455207
Moonlight,2016,376867
The Kindergarten Teacher,2018,489927
Night School,2018,454293
Always Be My Maybe,2019,513576
Limitless,2011,51876
Zombieland,2009,19908
Ralph Breaks the Internet,2018,404368
Hotel Transylvania 2,2015,159824
Skyscraper,2018,447200
Gattaca,1997,782
The Hangover,2009,18785
National Treasure: Book of Secrets,2007,6637
50/50,2011,40807
Observe and Report,2009,16991
Avengers: Endgame,2019,299534
Spider-Man: Into the Spider-Verse,2018,324857
8 Mile,2002,65
Lost in Translation,2003,153
Caddyshack,1980,11977
Peppermint,2018,458594
A Quiet Place,2018,447332
Fantastic Beasts: The Crimes of Grindelwald,2018,338952
Eighth Grade,2018,489925
Signs,2002,2675
Ferdinand,2017,364689
Paddington 2,2018,346648
The Lego Movie 2: The Second Part,2019,280217
A Star Is Born,2018,332562
Tracers,2015,290764
Aquaman,2018,297802
Fast Times at Ridgemont High,1982,13342
The Big Lebowski,1998,115
Transporter 2,2005,9335
Bad Teacher,2011,52449
Insomnia,2002,320
Hulk,2003,1927
Shazam!,2019,287947
Long Shot,2019,459992
Young Adult,2011,57157
Brittany Runs a Marathon,2019,529862
Mad Max 2,1982,8810
The Grinch,2018,360920
Bad Santa,2003,10147
Star Wars: The Rise of Skywalker,2019,181812
Unforgiven,1992,33
Cold Pursuit,2019,438650
Mrs. Doubtfire,1993,788
What Men Want,2019,487297
Men in Black: International,2019,479455
Shaft,2000,479
Beginners,2011,55347
John Wick: Chapter 3 - Parabellum,2019,458156
About Time,2013,122906
Real Steel,2011,39254
Alita: Battle Angel,2019,399579
Spider-Man: Far from Home,2019,429617
Pokémon Detective Pikachu,2019,447404
Five Feet Apart,2019,527641
Hot Rod,2007,10074
Being John Malkovich,1999,492
1917,2019,530915
Midnight in Paris,2011,59436
End of Watch,2012,77016
Knives Out,2019,546554
Rain Man,1988,380
Lady Bird,2017,391713
The Aeronauts,2019,514921
Yesterday,2019,515195
Jumanji: The Next Level,2019,512200
Tinker Tailor Soldier Spy,2011,49517
The Rainmaker,1997,11975
21 Jump Street,2012,64688
The Angry Birds Movie 2,2019,454640
Split,2016,381288
Rocketman,2019,504608
Glass,2019,450465
BlacKkKlansman,2018,487558
Blinded by the Light,2019,534259
The Happening,2008,8645
Stuber,2019,513045
Steve Jobs,2015,321697
Spenser Confidential,2020,581600
Slumdog Millionaire,2008,12405
Dazed and Confused,1993,9571
Boy,2010,39356
My Spy,2020,592834
Ford v Ferrari,2019,359724
Zombieland: Double Tap,2019,338967
Manchester by the Sea,2016,334541
Awakenings,1990,11005
Psych 2: Lassie Come Home,2020,582218
Hanna,2011,50456
Psycho,1960,539
Ad Astra,2019,419704
Dark Phoenix,2019,320288
Bad Boys for Life,2020,38700
A Beautiful Day in the Neighborhood,2019,501907
Jexi,2019,620725
Bloodshot,2020,338762
Jojo Rabbit,2019,515001
The Man from Earth,2007,13363
Million Dollar Baby,2004,70
Birds of Prey (and the Fantabulous Emancipation of One Harley Quinn),2020,495764
Project Power,2020,605116
Spies in Disguise,2019,431693
Uncut Gems,2019,473033
On the Basis of Sex,2019,339380
The Spy Who Dumped Me,2018,454992
The Old Guard,2020,547016
Hubie Halloween,2020,617505
Heist,2001,11088
Bill & Ted's Excellent Adventure,1989,1648
Bill & Ted's Bogus Journey,1991,1649
Driving Miss Daisy,1989,403
Sunshine Cleaning,2008,13090
The Peanut Butter Falcon,2019,463257
Hunt for the Wilderpeople,2016,371645
Tom and Jerry: The Movie,1992,22582
Gemini Man,2019,453405
The King of Staten Island,2020,579583
Snowden,2016,302401
Batman Begins,2005,272
The Dark Knight,2008,155
The Dark Knight Rises,2012,49026
Enemy of the State,1998,9798
Earwig and the Witch,2021,683127
Grave of the Fireflies,1989,12477
Irresistible,2020,595148
Princess Mononoke,1999,128
Nausicaä of the Valley of the Wind,1985,81
The Way Back,2020,529485
Palm Springs,2020,587792
Only Yesterday,2019,15080
Porco Rosso,1992,11621
Tom & Jerry,2021,587807
Prisoners,2013,146233
The Wind Rises,2014,149870
Richard Jewell,2019,292011
Pom Poko,2005,15283
A Monster Calls,2016,258230
Dark Waters,2019,552178
When Marnie Was There,2015,242828
Godzilla,2014,124905
Godzilla: King of the Monsters,2019,373571
Godzilla vs. Kong,2021,399566
Source Code,2011,45612
The Adjustment Bureau,2011,38050
Tales from Earthsea,2010,37933
Ocean Waves,2016,21057
Weathering with You,2020,568160
Tom Clancy's Without Remorse,2021,567189
Trailer Park Boys: The Movie,2008,9958
Celeste & Jesse Forever,2012,84184
Spider-Man 2,2004,558
Spider-Man 3,2007,559
Man of Steel,2013,49521
The Mitchells vs. The Machines,2021,501929
Shooter,2007,7485
The Cat Returns,2002,15370
Due Date,2010,41733
Army of the Dead,2021,503736
The Age of Adaline,2015,293863
Tenet,2020,577922
The Croods: A New Age,2020,529203
Luca,2021,508943
Bad Words,2014,209403
The Tomorrow War,2021,588228
Monster Hunter,2020,458576
Soul,2020,508442
Onward,2020,508439
Toy Story 4,2019,301528
Bill & Ted Face the Music,2020,501979
Aladdin,2019,420817
Brave,2012,62177
Raya and the Last Dragon,2021,527774
The Suicide Squad,2021,436969
Chaos Walking,2021,412656
Wonder Woman 1984,2020,464052
Terminator: Dark Fate,2019,290859
Lupin the Third: The Castle of Cagliostro,1999,15371
Mary and The Witch's Flower,2017,430447
The Man from Nowhere,2010,51608
Zodiac,2007,1949
Grown Ups 2,2013,109418
Minari,2020,615643
Dune,2021,438631
Captain Phillips,2013,109424
2001: A Space Odyssey,1968,62
Psych 3: This Is Gus,2021,829400
Love and Monsters,2020,590223
King Richard,2021,614917
The Matrix Resurrections,2021,624860
Hitman's Wife's Bodyguard,2021,522931
Memento,2001,77
Nobody,2021,615457
Donnie Darko,2001,141
Free Guy,2021,550988
Dredd,2012,49049
A Silent Voice: The Movie,2017,378064
";
#endif
    }
}