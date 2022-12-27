using Movies.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Threading;
using Test = Movies;

namespace Movies.Views
{
    public partial class Database
    {
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

            public Task<SQLiteAsyncConnection> Database;
            public IAssignID<int> IDSystem;
            public override string ID => Id.ToString();

            private int Id;

            public LocalList(Task<SQLiteAsyncConnection> database, int id)
            {
                Database = database;
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

            private async Task Updated()
            {
                var database = await Database;
                await database.ExecuteAsync(string.Format("update {0} set {1} = {2} where {3} = ?", DetailsTable, DetailsCols.UPDATED_AT, GetUpdatedAt(), DetailsCols.ID), ID);
            }

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
                    var database = await Database;
                    var modified = await database.ExecuteAsync(string.Format("insert into {0}({1}) values " + values, ItemsTable, string.Join(',', ItemsTable.Columns.SkipLast(1))), rows.SelectMany(row => row).ToArray());

                    if (modified > 0)
                    {
                        await Updated();
                    }

                    return modified == rows.Count;
                }

                return true;
            }
            protected async override Task<bool?> ContainsAsyncInternal(Item item) => IDSystem.TryGetID(item, out var id) && await Contains(id, item.ItemType);

            private async Task<bool> Contains(int id, Test.ItemType itemType)
            {
                var database = await Database;
                return (await database.ExecuteScalarAsync<int>(string.Format("select count(*) from {0} where {1} = ? and {2} = ? and {3} = ?", ItemsTable, ItemsCols.LIST_ID, ItemsCols.ITEM_ID, ItemsCols.ITEM_TYPE), Id, id, AppItemTypeToDatabaseItemType(itemType))) == 1;
            }

            public override async Task<int> CountAsync()
            {
                var database = await Database;
                return await database.ExecuteScalarAsync<int>(string.Format("select count(*) from {0} where {1} = ?", ItemsTable, ItemsCols.LIST_ID), Id);
            }

            protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Item> items)
            {
                bool success = true;
                int total = 0;

                foreach (var item in items)
                {
                    int rows = 0;

                    if (IDSystem.TryGetID(item, out var id))
                    {
                        var database = await Database;
                        rows = await database.ExecuteAsync(string.Format("delete from {0} where {1} = ? and {2} = ? and {3} = ?", ItemsTable, ItemsCols.LIST_ID, ItemsCols.ITEM_ID, ItemsCols.ITEM_TYPE), Id, id, AppItemTypeToDatabaseItemType(item.ItemType));
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
                var database = await Database;

                if (Id == -1)
                {
                    await database.ExecuteAsync(string.Format("insert into {0} ({1}) values ({2},{3})", DetailsTable, string.Join(",", cols), string.Join(",", Enumerable.Repeat("?", values.Length)), GetUpdatedAt()), values);
                    Id = (await database.QueryScalarsAsync<int>("select last_insert_rowid()")).First();
                }
                else
                {
                    await database.ExecuteAsync(string.Format("update {0} set {1}{4} where {2} = {3}", DetailsTable, string.Join(",", cols.Select(col => col + "=?")).TrimEnd('?'), DetailsCols.ID, Id, GetUpdatedAt()), values);
                }
            }

            public async override Task Delete()
            {
                if (Id <= 2)
                {
                    return;
                }

                var database = await this.Database;
                var details = database.ExecuteAsync(string.Format("delete from {0} where {1} = ?", DetailsTable, DetailsCols.ID), Id);
                var items = database.ExecuteAsync(string.Format("delete from {0} where {1} = ?", ItemsTable, ItemsCols.LIST_ID), Id);

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

                var database = await Database;
                IEnumerable<(int?, int?, string)> items = await database.QueryAsync<(int?, int?, string)>(query, Id);

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

                var database = await Database;
                IEnumerable<(int?, int?, string)> items = await database.QueryAsync<(int?, int?, string)>(string.Format("select {0} from {1} where {2} = ?", SQLiteExtensions.SelectFrom(ItemsCols.ITEM_ID.Name, ItemsCols.ITEM_TYPE.Name, ItemsCols.DATE_ADDED.Name), ItemsTable, ItemsCols.LIST_ID), Id);

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
    }
}
