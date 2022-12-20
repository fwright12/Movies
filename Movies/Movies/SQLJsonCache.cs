using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
}
