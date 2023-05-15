﻿using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class ItemInfoCache : IJsonCache, IAsyncEnumerable<KeyValuePair<string, JsonResponse>>
    {
        public static readonly Table.Column ID = new Table.Column("id", "integer");
        public static readonly Table.Column TYPE = new Table.Column("type", "integer");

        public Task Setup { get; }
        public async Task<Table> GetTable() => (await Cache).Table;

        private Task<SQLJsonCache> Cache { get; }

        public ItemInfoCache(Task<SQLiteAsyncConnection> db)
        {
            Setup = Cache = Init(db);
            //_ = Expire(ItemType.Movie, 453395);
        }

        private async Task<SQLJsonCache> Init(Task<SQLiteAsyncConnection> db)
        {
            var cache = new SQLJsonCache(await db);
            await cache.Setup;
            await cache.DB.AddColumns(cache.Table, ID, TYPE);

#if DEBUG
            await cache.Clear();

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
}