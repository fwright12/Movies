using Movies.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Test = Movies;

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

        public Database(IAssignID<int> tmdb, ID<int>.Key idKey)
        {
            IDSystem = tmdb;
            IDKey = idKey;

            string path = GetFilePath(UserInfoDBFilename);
            //File.Delete(path);
            UserInfo = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            path = GetFilePath(ItemInfoDBFilename);
            //File.Delete(path);
            ItemInfo = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

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

            var itemTables = new Dictionary<ItemType, Table>
            {
                [ItemType.Movie] = Movies,
                [ItemType.TVShow] = TVShows,
                [ItemType.Person] = People,
                [ItemType.Collection] = Collections
            };
            await Task.WhenAll(itemTables.Values.Select(table => ItemInfo.CreateTable(table)));

            //await Task.WhenAll(SQL.CreateTable(SyncLists), SQL.CreateTable(LocalList.DetailsTable), SQL.CreateTable(LocalList.ItemsTable));//, SQL.CreateTable(Movies));
            if ((await UserInfo.QueryScalarsAsync<int>(string.Format("select count(*) from {0}", LocalList.DetailsTable))).First() == 0)
            {
                await UserInfo.ExecuteAsync(string.Format("insert into {0} ({1}) values (?), (?), (?)", LocalList.DetailsTable, LocalList.DetailsCols.ID), 0, 1, 2);
                //await SQL.ExecuteAsync(string.Format("insert into {0} values (?), (?), (?)", SyncLists, 0, 1, 2);
            }

            await ItemInfo.ExecuteAsync($"attach database '{GetFilePath(UserInfoDBFilename)}' as items");
            foreach (var kvp in itemTables)
            {
                //Print.Log(string.Join(',', await ItemInfo.QueryAsync<ValueTuple<string>>($"select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?", kvp.Key)));
                //Print.Log($"delete from {kvp.Value} where {kvp.Value.Columns[0]} not in (select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?)");
                _ = ItemInfo.ExecuteAsync($"delete from {kvp.Value} where {kvp.Value.Columns[0]} not in (select {LocalList.ItemsCols.ITEM_ID} from items.{LocalList.ItemsTable} where {LocalList.ItemsCols.ITEM_TYPE} = ?)", kvp.Key);
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
#endif
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

                Items = GetItems();
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

            private async IAsyncEnumerable<Item> GetItems()
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
    }
}