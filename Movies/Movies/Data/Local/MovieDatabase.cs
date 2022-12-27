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

namespace Movies.Views
{
    public abstract class SQLiteDatabase
    {
        public Task<SQLiteAsyncConnection> Connection { get; }

        public SQLiteDatabase(SQLiteAsyncConnection connection)
        {
            Connection = Init(connection);
        }

        protected abstract Task<SQLiteAsyncConnection> Init(SQLiteAsyncConnection connection);
    }

    public class UserInfoDatabase : SQLiteDatabase
    {
        public UserInfoDatabase(SQLiteAsyncConnection connection) : base(connection) { }

        protected override Task<SQLiteAsyncConnection> Init(SQLiteAsyncConnection connection)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Database : IListProvider, IAssignID<int>
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

        private Task<SQLiteAsyncConnection> UserInfo;
        private Task<SQLiteAsyncConnection> ItemInfo;

        public bool NeedsCleaning => DateTime.Now - LastCleaned <= App.DB_CLEANING_SCHEDULE == false;

        public ItemInfoCache ItemCache { get; }
        public string Name { get; } = "Local";

        private static readonly Table SyncLists = new Table(SyncListsCols.ID, SyncListsCols.SOURCE, SyncListsCols.SYNC_ID, SyncListsCols.SYNC_SOURCE)
        {
            Name = "SyncLists",
            ConstraintsDecl = string.Format("primary key({0}, {1}, {2}, {3})", SyncListsCols.ID, SyncListsCols.SOURCE, SyncListsCols.SYNC_ID, SyncListsCols.SYNC_SOURCE)
        };

        private static readonly Table Movies = new Table(MovieCols.ID, MovieCols.TITLE, MovieCols.YEAR) { Name = "Movies" };
        private static readonly Table TVShows = new Table(TVCols.ID, TVCols.TITLE) { Name = "TVShows" };
        private static readonly Table People = new Table(PersonCols.ID, PersonCols.NAME, PersonCols.BIRTH_YEAR) { Name = "People" };
        public static readonly Table Collections = new Table(CollectionCols.ID, CollectionCols.NAME, CollectionCols.COUNT, CollectionCols.DESCRIPTION, CollectionCols.POSTER_PATH) { Name = "Collections" };

        private static readonly Dictionary<ItemType, Table> ItemTables = new Dictionary<ItemType, Table>
        {
            [ItemType.Movie] = Movies,
            [ItemType.TVShow] = TVShows,
            [ItemType.Person] = People,
            [ItemType.Collection] = Collections
        };

        private IAssignID<int> IDSystem;
        private ID<int>.Key IDKey;
        private DateTime? LastCleaned;

        public Database(IAssignID<int> tmdb, ID<int>.Key idKey, DateTime? lastCleaned = null)
        {
            IDSystem = tmdb;
            IDKey = idKey;
            LastCleaned = lastCleaned;

            var userInfoPath = GetFilePath(UserInfoDBFilename);
            //Print.Log(new System.IO.FileInfo(path).Length);
            //File.Delete(path);
            var userInfo = new SQLiteAsyncConnection(userInfoPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            var itemInfoPath = GetFilePath(ItemInfoDBFilename);
            //Print.Log(new System.IO.FileInfo(path).Length);
            //File.Delete(path);
            var itemInfo = new SQLiteAsyncConnection(itemInfoPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            UserInfo = CreateUserInfo(userInfo, NeedsCleaning);
            ItemInfo = CreateItemInfo(itemInfo, UserInfo, NeedsCleaning);

            ItemCache = new ItemInfoCache(ItemInfo);

#if DEBUG
            async Task Dummy()
            {
                await Task.WhenAll(UserInfo, ItemInfo);

                Print.Log("stored movies");
                foreach (var row in await itemInfo.QueryAsync<(int, string, int)>("select * from " + Movies.Name + " limit 10"))
                {
                    Print.Log(row);
                }

                Print.Log("stored collections");
                foreach (var row in await itemInfo.QueryAsync<(int, string, int?, string, string)>("select * from " + Collections.Name + " limit 10"))
                {
                    Print.Log(row);
                }

                var history = await GetHistory();
                //await UserInfo.ExecuteAsync($"delete from {LocalList.ItemsTable} where {LocalList.ItemsCols.LIST_ID} = ?", history.ID);
                //return;
                if (history.Count > 0)
                {
                    Print.Log("not adding");
                }
                else
                {
                    var temp = await userInfo.ExecuteAsync($"delete from {LocalList.ItemsTable} where {LocalList.ItemsCols.LIST_ID} = ?", history.ID);

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
                }
            }

            _ = Dummy();
#endif
        }

        private static async Task<SQLiteAsyncConnection> CreateUserInfo(SQLiteAsyncConnection userInfo, bool clean = false)
        {
            //await test.ExecuteAsync("drop table if exists " + MyLists.Name);
            //await test.ExecuteAsync("drop table if exists " + LocalLists.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.ItemsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.DetailsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + SyncLists.Name);

            await userInfo.ExecuteAsync("drop table if exists Movies");
            //await SQL.ExecuteAsync("drop table if exists MovieInfo");
            //await SQL.ExecuteAsync("drop table if exists TVShows");
            //await SQL.ExecuteAsync("drop table if exists People");
            //await SQL.ExecuteAsync("drop table if exists Collections");

            var itemsCols = await userInfo.GetTableInfoAsync(LocalList.ItemsTable.Name);
            if (itemsCols.Count > 0 && !itemsCols.Select(col => col.Name).Contains(LocalList.ItemsCols.DATE_ADDED.Name))
            {
                await userInfo.CreateTable(new Table(LocalList.ItemsTable.Columns) { Name = "temp" });
                await userInfo.ExecuteAsync(string.Format("insert into temp select {0}, null as {1} from {2}", string.Join(',', LocalList.ItemsTable.Columns.Take(3)), LocalList.ItemsCols.DATE_ADDED, LocalList.ItemsTable));

                await userInfo.ExecuteAsync("drop table " + LocalList.ItemsTable.Name);
                await userInfo.ExecuteAsync("alter table temp rename to " + LocalList.ItemsTable.Name);
            }

            await Task.WhenAll(new Table[]
            {
                SyncLists,
                LocalList.DetailsTable,
                LocalList.ItemsTable,
            }.Select(table => userInfo.CreateTable(table)));

            //await Task.WhenAll(SQL.CreateTable(SyncLists), SQL.CreateTable(LocalList.DetailsTable), SQL.CreateTable(LocalList.ItemsTable));//, SQL.CreateTable(Movies));
            if ((await userInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).First() == 0)
            {
                await userInfo.ExecuteAsync(string.Format("insert into {0} ({1}) values (?), (?), (?)", LocalList.DetailsTable, LocalList.DetailsCols.ID), 0, 1, 2);
                //await SQL.ExecuteAsync(string.Format("insert into {0} values (?), (?), (?)", SyncLists, 0, 1, 2);
            }

#if DEBUG
            Print.Log("synced lists");
            foreach (var row in await userInfo.QueryAsync<(string, string, string, string)>("select * from " + SyncLists.Name))
            {
                Print.Log(row);
            }

            try
            {
                Print.Log("local lists", (await userInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).FirstOrDefault());
                foreach (var row in await userInfo.QueryAsync<(string, string, string, string, string, string, string)>("select * from " + LocalList.DetailsTable.Name))
                {
                    Print.Log(DateTime.TryParse(row.Item7, out var date) ? date.ToLocalTime().ToString() : "", row);
                }
            }
            catch (Exception e)
            {
                Print.Log(e);
                ;
            }

            //Print.Log(DateTime.Parse((await UserInfo.QueryScalarsAsync<string>("SELECT CURRENT_TIMESTAMP")).FirstOrDefault()));
            Print.Log("items in lists", (await userInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.ItemsTable))).FirstOrDefault());
            foreach (var row in await userInfo.QueryAsync<(int?, int?, int?, string)>("select * from " + LocalList.ItemsTable.Name + " limit 10"))
            {
                Print.Log(row);
            }
#endif

            if (clean)
            {
                await userInfo.ExecuteAsync("vacuum");
            }

            return userInfo;
        }

        private static async Task<SQLiteAsyncConnection> CreateItemInfo(SQLiteAsyncConnection itemInfo, Task<SQLiteAsyncConnection> userInfoConn, bool clean = false)
        {
            //await test.ExecuteAsync("drop table if exists " + MyLists.Name);
            //await test.ExecuteAsync("drop table if exists " + LocalLists.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.ItemsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.DetailsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + SyncLists.Name);

            await itemInfo.ExecuteAsync("drop table if exists JsonCache");
            //await SQL.ExecuteAsync("drop table if exists MovieInfo");
            //await SQL.ExecuteAsync("drop table if exists TVShows");
            //await SQL.ExecuteAsync("drop table if exists People");
            //await SQL.ExecuteAsync("drop table if exists Collections");

            await Task.WhenAll(ItemTables.Values.Select(table => itemInfo.CreateTable(table)));

            if (clean)
            {
                var userInfo = await userInfoConn;
                await itemInfo.ExecuteAsync($"attach database '{GetFilePath(UserInfoDBFilename)}' as items");

                var cleaning = new List<Task<int>>();

                foreach (var kvp in ItemTables)
                {
                    //Print.Log(string.Join(',', await ItemInfo.QueryAsync<ValueTuple<string>>($"select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?", kvp.Key)));
                    //Print.Log($"delete from {kvp.Value} where {kvp.Value.Columns[0]} not in (select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?)");
                    var id = kvp.Value.Columns[0];
                    var userLists = $"(select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?)";
                    cleaning.Add(itemInfo.ExecuteAsync($"delete from {kvp.Value} where {id} not in {userLists}", kvp.Key));
                    cleaning.Add(itemInfo.ExecuteAsync($"delete from {SQLJsonCache.TABLE_NAME} where {ItemInfoCache.TYPE} = ? and {ItemInfoCache.ID} not in {userLists}", kvp.Key, kvp.Key));
                }

                var rows = await Task.WhenAll(cleaning);
#if DEBUG
                Print.Log($"cleaned {rows.Sum()} rows from item info");
#endif

                await itemInfo.ExecuteAsync("vacuum");
            }

            return itemInfo;
        }

        private async Task Setup()
        {
            //await test.ExecuteAsync("drop table if exists " + MyLists.Name);
            //await test.ExecuteAsync("drop table if exists " + LocalLists.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.ItemsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + LocalList.DetailsTable.Name);
            //await UserInfo.ExecuteAsync("drop table if exists " + SyncLists.Name);

            var userInfo = await UserInfo;
            var itemInfo = await ItemInfo;

            await userInfo.ExecuteAsync("drop table if exists Movies");
            await itemInfo.ExecuteAsync("drop table if exists JsonCache");
            //await SQL.ExecuteAsync("drop table if exists MovieInfo");
            //await SQL.ExecuteAsync("drop table if exists TVShows");
            //await SQL.ExecuteAsync("drop table if exists People");
            //await SQL.ExecuteAsync("drop table if exists Collections");

            var itemsCols = await userInfo.GetTableInfoAsync(LocalList.ItemsTable.Name);
            if (itemsCols.Count > 0 && !itemsCols.Select(col => col.Name).Contains(LocalList.ItemsCols.DATE_ADDED.Name))
            {
                await userInfo.CreateTable(new Table(LocalList.ItemsTable.Columns) { Name = "temp" });
                await userInfo.ExecuteAsync(string.Format("insert into temp select {0}, null as {1} from {2}", string.Join(',', LocalList.ItemsTable.Columns.Take(3)), LocalList.ItemsCols.DATE_ADDED, LocalList.ItemsTable));

                await userInfo.ExecuteAsync("drop table " + LocalList.ItemsTable.Name);
                await userInfo.ExecuteAsync("alter table temp rename to " + LocalList.ItemsTable.Name);
            }

            await Task.WhenAll(new Table[]
            {
                SyncLists,
                LocalList.DetailsTable,
                LocalList.ItemsTable,
            }.Select(table => userInfo.CreateTable(table)));

            await Task.WhenAll(ItemTables.Values.Select(table => itemInfo.CreateTable(table)));

            //await Task.WhenAll(SQL.CreateTable(SyncLists), SQL.CreateTable(LocalList.DetailsTable), SQL.CreateTable(LocalList.ItemsTable));//, SQL.CreateTable(Movies));
            if ((await userInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).First() == 0)
            {
                await userInfo.ExecuteAsync(string.Format("insert into {0} ({1}) values (?), (?), (?)", LocalList.DetailsTable, LocalList.DetailsCols.ID), 0, 1, 2);
                //await SQL.ExecuteAsync(string.Format("insert into {0} values (?), (?), (?)", SyncLists, 0, 1, 2);
            }

#if DEBUG
            Print.Log("stored movies");
            foreach (var row in await itemInfo.QueryAsync<(int, string, int)>("select * from " + Movies.Name + " limit 10"))
            {
                Print.Log(row);
            }

            Print.Log("stored collections");
            foreach (var row in await itemInfo.QueryAsync<(int, string, int?, string, string)>("select * from " + Collections.Name + " limit 10"))
            {
                Print.Log(row);
            }

            Print.Log("synced lists");
            foreach (var row in await userInfo.QueryAsync<(string, string, string, string)>("select * from " + SyncLists.Name))
            {
                Print.Log(row);
            }

            Print.Log("local lists", (await userInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).FirstOrDefault());
            foreach (var row in await userInfo.QueryAsync<(string, string, string, string, string, string, string)>("select * from " + LocalList.DetailsTable.Name))
            {
                Print.Log(DateTime.TryParse(row.Item7, out var date) ? date.ToLocalTime().ToString() : "", row);
            }

            //Print.Log(DateTime.Parse((await UserInfo.QueryScalarsAsync<string>("SELECT CURRENT_TIMESTAMP")).FirstOrDefault()));
            Print.Log("items in lists", (await userInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.ItemsTable))).FirstOrDefault());
            foreach (var row in await userInfo.QueryAsync<(int?, int?, int?, string)>("select * from " + LocalList.ItemsTable.Name + " limit 10"))
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
            var temp = await userInfo.ExecuteAsync($"delete from {LocalList.ItemsTable} where {LocalList.ItemsCols.LIST_ID} = ?", history.ID);

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

        private static string GetFilePath(string dbName) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dbName);

        private class Query<T> { public T Value { get; set; } }

        public async Task<List<(string, string)>> GetMyLists()
        {
            var userInfo = await UserInfo;
            return await userInfo.QueryAsync<(string, string)>(string.Format("select {0} from {1}", SQLiteExtensions.SelectFrom(SyncListsCols.ID.Name, SyncListsCols.SOURCE.Name), SyncLists.Name));
        }

        public async Task<List<(string Name, string ID)>> GetSyncsWithAsync(string source, string id)
        {
            var result = new List<(string, string)>();
            var userInfo = await UserInfo;

            foreach (var row in await userInfo.QueryAsync<(string, string, string, string)>(string.Format("select * from {0} where ({1} = ? and {2} = ?) or ({3} = ? and {4} = ?)", SyncLists, SyncListsCols.SOURCE, SyncListsCols.ID, SyncListsCols.SYNC_SOURCE, SyncListsCols.SYNC_ID), source, id, source, id))
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

        public async Task SetSyncsWithAsync(string source, string id, string syncSource, string syncID)
        {
            var userInfo = await UserInfo;
            await (id == null || syncID == null ? Task.CompletedTask : userInfo.ExecuteAsync(string.Format("insert or ignore into {0} values (?,?,?,?)", SyncLists), id, source, syncID, syncSource));
        }
        public async Task RemoveSyncsWithAsync(string source, string id, string syncSource, string syncID)
        {
            var userInfo = await UserInfo;
            await userInfo.ExecuteAsync(string.Format("delete from {0} where {1} = ? and {2} = ? and {3} = ? and {4} = ?", SyncLists, SyncListsCols.ID, SyncListsCols.SOURCE, SyncListsCols.SYNC_ID, SyncListsCols.SYNC_SOURCE), id, source, syncID, syncSource);
        }

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
                var itemInfo = await ItemInfo;
                await itemInfo.ExecuteAsync(string.Format("insert or replace into {0} values (" + test + ")", table), values);
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
            var itemInfo = await ItemInfo;

            if (type == Test.ItemType.Movie)
            {
                var info = await itemInfo.QueryAsync<(int, string, int)>(string.Format("select * from {0} where {1} = ?", Movies, MovieCols.ID), id);

                if (info.Count > 0)
                {
                    item = new Movie(info[0].Item2, info[0].Item3);
                }
            }
            else if (type == Test.ItemType.TVShow)
            {
                var info = await itemInfo.QueryAsync<(int, string)>(string.Format("select * from {0} where {1} = ?", TVShows, TVCols.ID), id);

                if (info.Count > 0)
                {
                    item = new TVShow(info[0].Item2);
                }
            }
            else if (type == Test.ItemType.Person)
            {
                var info = await itemInfo.QueryAsync<(int, string, int)>(string.Format("select * from {0} where {1} = ?", People, PersonCols.ID), id);

                if (info.Count > 0)
                {
                    item = new Person(info[0].Item2, info[0].Item3);
                }
            }
            else if (type == Test.ItemType.Collection)
            {
                var info = await itemInfo.QueryAsync<(int, string, int?, string, string)>(string.Format("select * from {0} where {1} = ?", Collections, CollectionCols.ID), id);

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
            var userInfo = await UserInfo;

            await userInfo.ExecuteAsync(string.Format("delete from {0} where {1} = ?", LocalList.ItemsTable.Name, LocalList.ItemsCols.LIST_ID.Name), listID);
            await userInfo.ExecuteAsync(string.Format("delete from {0} where {1} = ?", LocalList.DetailsTable.Name, LocalList.DetailsCols.ID.Name), listID);
        }

        public async Task<List> GetListAsync(string id) => !int.TryParse(id, out var intID) || intID < 0 ? null : (await QueryListsAsync(string.Format("where {0} = ?", LocalList.DetailsCols.ID), id)).FirstOrDefault();

        private async Task<List<List>> QueryListsAsync(string where = null, params object[] args)
        {
            var userInfo = await UserInfo;
            var query = await userInfo.QueryAsync<(int, int?, string, string, string, bool, string)>(string.Format("select {0} from {1} " + where, string.Join(',', (object[])LocalList.DetailsTable.Columns), LocalList.DetailsTable), args);

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
    }
}