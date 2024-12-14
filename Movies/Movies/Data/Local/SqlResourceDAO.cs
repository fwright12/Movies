using Movies.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Movies.Data.Local
{
    public class LocalMovieCache : IEventAsyncCache<KeyValueRequestArgs<Uri>>
    {
        public IEventAsyncCache<KeyValueRequestArgs<Uri>> Cache { get; }

        public LocalMovieCache(IEventAsyncCache<KeyValueRequestArgs<Uri>> cache)
        {
            Cache = cache;
        }

        public Task<bool> Read(IEnumerable<KeyValueRequestArgs<Uri>> args) => Cache.Read(args);

        public Task<bool> Write(IEnumerable<KeyValueRequestArgs<Uri>> args) => Cache.Write(args.Where(ShouldWrite));

        private bool ShouldWrite(KeyValueRequestArgs<Uri> arg)
        {
            if (arg.Request.Key is UniformItemIdentifier)
            {
                return false;
            }

            return true;
            var url = arg.Request.Key.ToString();

            if (Regex.IsMatch(url, "3/(movie|tv)/\\d+/external_ids.*"))
            {
                return true;
            }

            return false;
        }
    }

    public class SqlResourceDAO : IEventAsyncCache<KeyValueRequestArgs<Uri>>, IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>>
    {
        public TMDbResolver Resolver { get; }

        private const ItemType CACHEABLE_TYPES = ItemType.Movie | ItemType.TVShow | ItemType.Person;

        private Task<SQLiteAsyncConnection> Connection { get; }
        private Task InitTask { get; }

        public const int BATCH_INSERT_DELAY = 1000;

        public SqlResourceDAO(SQLiteAsyncConnection connection, TMDbResolver resolver) : this(Task.FromResult(connection), resolver) { }
        public SqlResourceDAO(Task<SQLiteAsyncConnection> connection, TMDbResolver resolver)
        {
            Connection = Connect(connection);
            Resolver = resolver;
        }

        private async Task<SQLiteAsyncConnection> Connect(Task<SQLiteAsyncConnection> connection1)
        {
            var connection = await connection1;

#if DEBUG
            if (DebugConfig.ClearLocalWebCache)
            {
                await connection.DropTableAsync<Message>();
            }
#endif

            _ = connection.ExecuteAsync($"drop table if exists WebResourceCache");
            _ = connection.ExecuteAsync($"drop table if exists WebResourceCache1");
            await connection.CreateTableAsync<Message>();

#if DEBUG
            Print.Log(await connection.Table<Message>().CountAsync() + " items in cache");
            foreach (var row in await connection.Table<Message>().Take(30).ToListAsync())
            {
                Print.Log(row.Uri, row.Timestamp, row.ETag, row.ExpiresAt);//, Encoding.UTF8.GetString(row.Response.Take(200).ToArray()));
            }
#endif

            return connection;
        }

        public async Task<int> Expire(TimeSpan olderThan)
        {
            var connection = await Connection;
            var cutoffDate = DateTime.Now - olderThan;
            return await connection.Table<Message>().DeleteAsync(message => message.Timestamp < cutoffDate);
        }

        public async Task<Message> GetMessage(string uri)
        {
            var connection = await Connection;
            return await connection.Table<Message>().Where(message => message.Uri == uri).FirstOrDefaultAsync();
        }

        public async Task<int> InsertMessage(Message message)
        {
            var connection = await Connection;
            return await connection.InsertOrReplaceAsync(message);
        }

        public Task<int> UpdateMessage(Message message) => UpdateMessage((object)message);
        private async Task<int> UpdateMessage(object message)
        {
            var connection = await Connection;
            return await connection.UpdateAsync(message);
        }

        Task<bool> IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>>.ProcessAsync(IEnumerable<KeyValueRequestArgs<Uri>> e) => ((IEventAsyncCache<KeyValueRequestArgs<Uri>>)this).Read(e);

        async Task<bool> IEventAsyncCache<KeyValueRequestArgs<Uri>>.Read(IEnumerable<KeyValueRequestArgs<Uri>> args)
        {
            var item = args.Select(arg => arg.Request.Key).OfType<UniformItemIdentifier>().Select(uii => uii.Item).FirstOrDefault(item => item != null);
            return (await Task.WhenAll(args.Select(arg => Read(arg, item)))).All(result => result);
        }

        async Task<bool> IEventAsyncCache<KeyValueRequestArgs<Uri>>.Write(IEnumerable<KeyValueRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Write))).All(result => result);

        private async Task<bool> Read(KeyValueRequestArgs<Uri> arg, Item item)
        {
            var uri = arg.ToString();
            var uii = UniformItemIdentifier.TryParse(arg.Request.Key, out var temp) ? temp : null;
            if (uii != null && Resolver.TryGetRequest(uii, out var request))
            {
                var url = request.GetURL();
                var query = url.Split('?').ElementAtOrDefault(1);

                if (!string.IsNullOrEmpty(query))
                {
                    uri += "?" + query;
                }
            }

            var message = await GetMessage(uri);
            if (message == null)
            {
                return false;
            }

            var byteRepresentation = new ObjectRepresentation<IEnumerable<byte>>(message.Response);
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

            var state = new State(byteRepresentation);
            if (uii != null)
            {
                Func<IEnumerable<byte>, object> converter;

                if (uii.Property == Movie.PARENT_COLLECTION && int.TryParse(Encoding.UTF8.GetString(message.Response.ToArray()), out var id))
                {
                    var collection = await TMDB.GetCollection(id);
                    converter = source => collection;
                }
                else if (!TryGetConverter(uii, item, out converter))
                {
                    converter = null;
                }

                if (converter != null)
                {
                    state.AddRepresentation(uii.Property.FullType, new LazilyConvertedRepresentation<IEnumerable<byte>, object>(byteRepresentation, converter));
                }
            }

            return arg.Handle(RestResponse.Create(arg.Request.Expected, state, headers, null));
        }

        public async Task<bool> Write(KeyValueRequestArgs<Uri> arg)
        {
            if (Resolver.TryGetAnnotations(arg.Request.Key, out var annotations) && annotations.Value.Count > 0)
            {
                var parts = arg.Request.Key.ToString().Split("?").FirstOrDefault().Split("/");
                if (!TryParseItemType(parts[1], out var itemType))
                {
                    return false;
                }

                var collection = (arg.Response as ResourceResponse)?.TryGetRepresentation<IEnumerable<KeyValuePair<Uri, State>>>(out var temp) == true ? temp : null;
                var results = new List<Task<bool>>();

                foreach (var parser in annotations.Value)
                {
                    var uri = new UniformItemIdentifier(itemType, parts[2], parser.Property);
                    IEnumerable<byte> response;

                    if (collection?.TryGetValue(uri, out var state) == true && state.TryGetRepresentation(typeof(IEnumerable<byte>), out var bytesRepresentation) && bytesRepresentation.Value is IEnumerable<byte> bytes)
                    {
                        response = bytes;

                        if (parser.Property == Movie.PARENT_COLLECTION && (TMDB.COLLECTION_PARSER as ParserWrapper)?.Parser.GetPair(response.ToArray()).Value is int parentCollectionId)
                        {
                            response = Encoding.UTF8.GetBytes(parentCollectionId.ToString());
                        }
                    }
                    else
                    {
                        response = null;
                    }

                    results.Add(Write(uri, response, arg));
                }

                return (await Task.WhenAll(results)).All(x => x);
            }
            else if (arg.Value is IEnumerable<byte> bytes || true == (arg.Response as ResourceResponse)?.TryGetRepresentation<IEnumerable<byte>>(out bytes))
            {
                await Write(arg.Request.Key, bytes, arg);
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<bool> Write(Uri uri, IEnumerable<byte> response, KeyValueRequestArgs<Uri> arg)
        {
            var url = uri.ToString().ToLower();
            var path = url.Split('?').ElementAtOrDefault(0);
            var parts = path.Split(':', '/');
            if (!TryParseItemType(parts[1], out var type) || !int.TryParse(parts[2], out var id))
            {
                return false;
            }

            var query = arg.Request.Key.ToString().Split('?').ElementAtOrDefault(1);
            var message = new Message
            {
                Uri = uri.ToString() + (string.IsNullOrEmpty(query) ? "" : ("?" + query)),
                Timestamp = DateTime.Now,
                Id = id,
                Type = type
            };

            //if (arg.Value is IEnumerable<byte> bytes || true == (arg.Response as ResourceResponse)?.TryGetRepresentation<IEnumerable<byte>>(out bytes))
            //{
            //    message.Response = bytes.ToArray();

            //    if ((arg.Request.Key as UniformItemIdentifier)?.Property == Movie.PARENT_COLLECTION && (TMDB.COLLECTION_PARSER as ParserWrapper)?.Parser.GetPair(message.Response).Value is int parentCollectionId)
            //    {
            //        message.Response = Encoding.UTF8.GetBytes(parentCollectionId.ToString());
            //    }
            //}

            if (arg.Response is RestResponse restResponse)
            {
                var headers = new System.Net.Http.HttpResponseMessage().Headers;
                foreach (var kvp in restResponse.ControlData)
                {
                    headers.Add(kvp.Key, kvp.Value);
                }

                message.ETag = headers.ETag?.ToString();
                message.ExpiresAt = headers.Date + headers.CacheControl?.MaxAge - (headers.Age ?? TimeSpan.Zero);
            }

            Task<int> modified;
            if (response == null)
            {
                modified = DebouncedBatchUpdateMessage(new NotModifiedMessage
                {
                    Uri = message.Uri,
                    Timestamp = message.Timestamp,
                    ExpiresAt = message.ExpiresAt
                });
                modified = Task.FromResult(1);
            }
            else
            {
                modified = DebouncedBatchInsertMessage(message, response);
                //return InsertMessageQueue.Add(message);
            }

            return await modified > 0;
        }

        private DebouncedTaskCompletionSource<bool> InsertSource;
        private object InsertLock = new object();
        private DebouncedTaskCompletionSource<bool> UpdateSource;
        private object UpdateLock = new object();

#if DEBUG
        int DebouncedInsertCount = 0;
        int DebouncedUpdateCount = 0;
        const int WRITE_REPORT_RATE = 1000;
#endif

        private async Task<int> DebouncedBatchInsertMessage(Message message, IEnumerable<byte> response)
        {
            await GetDebouncedTask(ref InsertSource, InsertLock);

            message.Response = MinifyJson(response);
            var result = await InsertMessage(message);
#if DEBUG
            if (++DebouncedInsertCount % WRITE_REPORT_RATE == 0)
            {
                Print.Log($"inserted {DebouncedInsertCount} items");
            }
#endif
            return result;
        }

        private async Task<int> DebouncedBatchUpdateMessage(object message)
        {
            await GetDebouncedTask(ref UpdateSource, UpdateLock);
            var result = await UpdateMessage(message);
#if DEBUG
            if (++DebouncedUpdateCount % WRITE_REPORT_RATE == 0)
            {
                Print.Log($"updated {DebouncedUpdateCount} items");
            }
#endif
            return result;
        }

        private static Task GetDebouncedTask(ref DebouncedTaskCompletionSource<bool> source, object lockObj)
        {
            lock (lockObj)
            {
                if (source?.TryReset() != true)
                {
                    source = new DebouncedTaskCompletionSource<bool>(BATCH_INSERT_DELAY);
                }

                return source.Task;
            }
        }

        private bool TryGetConverter(UniformItemIdentifier uii, Item item, out Func<IEnumerable<byte>, object> converter)
        {
            var itemType = uii.ItemType;
            var property = uii.Property;

            if (property == TVShow.SEASONS)
            {
                converter = source => GetTVItems(source as byte[] ?? source.ToArray(), (JsonNode json, out TVSeason season) => TMDB.TryParseTVSeason(json, (TVShow)item, out season));
                return true;
            }
            else if (property == TVSeason.EPISODES)
            {
                if (item is TVSeason season)
                {
                    converter = source => GetTVItems(source as byte[] ?? source.ToArray(), (JsonNode json, out TVEpisode episode) => TMDB.TryParseTVEpisode(json, season, out episode));
                    return true;
                }
                else
                {
                    converter = null;
                    return false;
                }
            }
            else if (TMDbResolver.UseSerializedJson(property, item))
            {
                converter = source => JsonSerializer.Deserialize(source.ToArray(), property.FullType);
                return true;
            }

            if (!Resolver.TryGetParser(itemType, property, out var parser))
            {
                converter = default;
                return false;
            }
            else
            {
                if (Resolver.TryGetRequest(itemType, property, out var request) && request is PagedTMDbRequest pagedRequest)
                {
                    Resolver.RemoveVariables(uii, out var args);
                    parser = TMDbResolver.ReplacePagedParsers(pagedRequest, parser, args);
                }
                else if (parser is ParserWrapper wrapper)
                {
                    parser = wrapper.Parser;
                }
            }

            converter = source => parser.GetPair(source is ArraySegment<byte> segment ? segment : new ArraySegment<byte>(source.ToArray())).Value;
            return true;
        }

        public static IEnumerable<T> GetTVItems<T>(byte[] json, AsyncEnumerable.TryParseFunc<JsonNode, T> parse) => TMDB.ParseCollection(JsonNode.Parse(json).AsArray(), new JsonNodeParser<T>(parse));

        private static bool TryParseItemType(string str, out ItemType itemType)
        {
            if (str == "movie") itemType = ItemType.Movie;
            else if (str == "tv" || str == "tvshow") itemType = ItemType.TVShow;
            //else if (parts[1] == "person") type = ItemType.Person;
            else
            {
                itemType = default;
                return false;
            }

            return true;
        }

        private static byte[] MinifyJson(IEnumerable<byte> bytes)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = true
            });
            
            JsonDocument.Parse(bytes.ToArray()).WriteTo(writer);
            writer.Flush();

            return stream.ToArray();
        }

        private abstract class TMDbPagedJsonConverter<T> : JsonConverter<IAsyncEnumerable<T>>
        {
            public JsonConverter<T> CollectionItemJsonConverter { get; }
            public PagedTMDbRequest Request { get; }

            public TMDbPagedJsonConverter(JsonConverter<T> collectionItemJsonConverter)
            {
                CollectionItemJsonConverter = collectionItemJsonConverter;
            }

            public override IAsyncEnumerable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                int? pages = null;
                var items = new List<T>();

                while (reader.Read())
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        reader.Skip();
                    }

                    string propertyName = reader.GetString();

                    if (propertyName == "results")
                    {

                    }
                    else if (propertyName == "totalPages")
                    {
                        pages = reader.GetInt32();
                    }
                    else if (propertyName == "pageCount")
                    {

                    }
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new Exception("Reader must be positioned at the start of an array");
                }

                options = new JsonSerializerOptions(options);
                options.Converters.Add(CollectionItemJsonConverter);

                JsonSerializer.Deserialize<T>(ref reader, options);
                var item = CollectionItemJsonConverter.Read(ref reader, typeof(T), options);

                return Concat(items, null);
            }

            private static async IAsyncEnumerable<T> Concat(IEnumerable<T> values, IAsyncEnumerable<T> remaining)
            {
                foreach (var value in values)
                {
                    yield return value;
                }

                await foreach (var value in remaining)
                {
                    yield return value;
                }
            }

            public override void Write(Utf8JsonWriter writer, IAsyncEnumerable<T> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        public const string RESOURCE_TABLE_NAME = "Resources";

        [Table(RESOURCE_TABLE_NAME)]
        public class NotModifiedMessage
        {
            [PrimaryKey, NotNull, Unique]
            public string Uri { get; set; }

            public DateTime? Timestamp { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
        }

        [Table(RESOURCE_TABLE_NAME)]
        public class Message : NotModifiedMessage
        {
            public int Id { get; set; }
            public ItemType Type { get; set; }

            public byte[] Response { get; set; }
            public string ETag { get; set; }
        }
    }
}
