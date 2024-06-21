﻿using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Movies.Data.Local
{
    internal class ResourceDAO : IEventAsyncCache<ResourceRequestArgs<Uri>>, IAsyncEventProcessor<IEnumerable<ResourceRequestArgs<Uri>>>
    {
        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;
        public TMDbResolver Resolver { get; }

        private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        private const string DatabaseFilename = "TodoSQLite.db3";
        private const ItemType CACHEABLE_TYPES = ItemType.Movie | ItemType.TVShow | ItemType.Person;

        private Task<SQLiteAsyncConnection> Connection { get; }
        private Task InitTask { get; }

        public ResourceDAO()
        {
            Connection = Connect();
            //Cache = new UriBufferedCache(this);
        }

        private async Task<SQLiteAsyncConnection> Connect()
        {
            var connection = new SQLiteAsyncConnection(DatabasePath, Flags);

#if DEBUG
            if (DebugConfig.ClearLocalWebCache)
            {
                await connection.DropTableAsync<Message>();
            }

            Print.Log(await connection.Table<Message>().CountAsync() + " items in cache");
            foreach (var row in await connection.Table<Message>().Take(10).ToListAsync())
            {
                Print.Log(row.Uri, row.Timestamp, row.ETag, row.ExpiresAt);
            }
#endif

            await connection.CreateTableAsync<Message>();
            return connection;
        }

        private async Task<Message> GetMessage(Uri uri)
        {
            var connection = await Connection;
            return await connection.Table<Message>().Where(message => message.Uri == uri).FirstOrDefaultAsync();
        }

        private async Task<int> UpdateMessage(Message message)
        {
            var connection = await Connection;
            return await connection.InsertOrReplaceAsync(message);
        }

        Task<bool> IAsyncEventProcessor<IEnumerable<ResourceRequestArgs<Uri>>>.ProcessAsync(IEnumerable<ResourceRequestArgs<Uri>> e) => ((IEventAsyncCache<ResourceRequestArgs<Uri>>)this).Read(e);

        async Task<bool> IEventAsyncCache<ResourceRequestArgs<Uri>>.Read(IEnumerable<ResourceRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Read))).All(result => result);

        async Task<bool> IEventAsyncCache<ResourceRequestArgs<Uri>>.Write(IEnumerable<ResourceRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Write))).All(result => result);

        private async Task<bool> Read(ResourceRequestArgs<Uri> arg)
        {
            var message = await GetMessage(arg.Request.Key);
            if (message == null)
            {
                return false;
            }

            var representation = new ObjectRepresentation<byte[]>(message.Response);
            var headers = new System.Net.Http.HttpResponseMessage().Headers;

            if (message.ETag != null && System.Net.Http.Headers.EntityTagHeaderValue.TryParse(message.ETag, out var etag))
            {
                headers.ETag = etag;
            }
            if (message.ExpiresAt.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                headers.Date = now;
                headers.CacheControl = System.Net.Http.Headers.CacheControlHeaderValue.Parse("public, max-age=" + (int)Math.Max(0, (message.ExpiresAt.Value - now).TotalSeconds));
#if DEBUG
                if (DebugConfig.SimulateStaleCache)
                {
                    headers.CacheControl = null;
                }
#endif
            }

            return arg.Handle(new RestResponse(representation, headers, null, arg.Request.Expected));
        }

        private async Task<bool> Write(ResourceRequestArgs<Uri> arg)
        {
            if (arg.Response == null)
            {
                return false;
            }
            if (!arg.Response.TryGetRepresentation<byte[]>(out var bytes))
            {
                return false;
            }

            var message = new Message
            {
                Uri = arg.Request.Key,
                Response = bytes,
            };

            if (arg.Response is RestResponse restResponse)
            {
                var state = restResponse.Resource.Get() as State;
                var headers = new System.Net.Http.HttpResponseMessage().Headers;
                foreach (var kvp in restResponse.ControlData)
                {
                    headers.Add(kvp.Key, kvp.Value);
                }

                message.ETag = headers.ETag?.ToString();
                message.ExpiresAt = headers.Date + headers.CacheControl?.MaxAge - (headers.Age ?? TimeSpan.Zero);
            }

            return await UpdateMessage(message) > 0;
        }

        private bool ShouldWrite(ResourceRequestArgs<Uri> arg)
        {
            if (arg.Request.Key is UniformItemIdentifier)
            {
                return false;
            }

            return true;
        }

        private class Message
        {
            [PrimaryKey, NotNull, Unique]
            public Uri Uri { get; set; }
            public int Id { get; set; }
            public ItemType Type { get; set; }

            public byte[] Response { get; set; }
            public DateTime? Timestamp { get; set; }
            public string ETag { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
        }
    }
}