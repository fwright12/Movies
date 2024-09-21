using Movies.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Xamarin.Essentials.AppleSignInAuthenticator;

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
                return true;
            }

            var url = arg.Request.Key.ToString();

            if (Regex.IsMatch(url, "3/movie/\\d/external_ids.*"))
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

        public SqlResourceDAO(SQLiteAsyncConnection connection, TMDbResolver resolver)
        {
            Connection = Connect(connection);
            Resolver = resolver;
        }

        private async Task<SQLiteAsyncConnection> Connect(SQLiteAsyncConnection connection)
        {
#if DEBUG
            if (DebugConfig.ClearLocalWebCache)
            {
                await connection.DropTableAsync<Message>();
            }
#endif
            await connection.CreateTableAsync<Message>();

#if DEBUG
            Print.Log(await connection.Table<Message>().CountAsync() + " items in cache");
            foreach (var row in await connection.Table<Message>().Take(10).ToListAsync())
            {
                Print.Log(row.Uri, row.Timestamp, row.ETag, row.ExpiresAt);
            }
#endif

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

        Task<bool> IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>>.ProcessAsync(IEnumerable<KeyValueRequestArgs<Uri>> e) => ((IEventAsyncCache<KeyValueRequestArgs<Uri>>)this).Read(e);

        async Task<bool> IEventAsyncCache<KeyValueRequestArgs<Uri>>.Read(IEnumerable<KeyValueRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Read))).All(result => result);

        async Task<bool> IEventAsyncCache<KeyValueRequestArgs<Uri>>.Write(IEnumerable<KeyValueRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Write))).All(result => result);

        private async Task<bool> Read(KeyValueRequestArgs<Uri> arg)
        {
            var message = await GetMessage(arg.Request.Key);
            var count = await (await Connection).Table<Message>().CountAsync();
            if (message == null)
            {
                return false;
            }

            var byteRepresentation = new ObjectRepresentation<byte[]>(message.Response);
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
            if (UniformItemIdentifier.TryParse(arg.Request.Key, out var uii))
            {
                Func<IEnumerable<byte>, object> converter;

                if (uii.Property == Movie.PARENT_COLLECTION && int.TryParse(Encoding.UTF8.GetString(message.Response), out var id))
                {
                    var collection = await TMDB.GetCollection(id);
                    converter = source => collection;
                }
                else if (!TryGetConverter(uii.ItemType, uii.Property, out converter))
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

        private async Task<bool> Write(KeyValueRequestArgs<Uri> arg)
        {
            if (arg.Response == null)
            {
                return false;
            }
            if (false == arg.Value is IEnumerable<byte> bytes && true != (arg.Response as ResourceResponse)?.TryGetRepresentation<IEnumerable<byte>>(out bytes))
            {
                return false;
            }

            var message = new Message
            {
                Uri = arg.Request.Key,
                Response = bytes.ToArray(),
            };

            if ((arg.Request.Key as UniformItemIdentifier)?.Property == Movie.PARENT_COLLECTION && (TMDB.COLLECTION_PARSER as ParserWrapper)?.Parser.GetPair(message.Response).Value is int id)
            {
                message.Response = Encoding.UTF8.GetBytes(id.ToString());
            }

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

            return await UpdateMessage(message) > 0;
        }

        private bool TryGetConverter(ItemType itemType, Property property, out Func<IEnumerable<byte>, object> converter)
        {
            if (property == TVShow.LAST_AIR_DATE)
            {
                converter = source => JsonSerializer.Deserialize(source.ToArray(), property.FullType);
                return true;
            }

            if (!Resolver.TryGetParser(itemType, property, out var parser))
            {
                converter = default;
                return false;
            }
            else if (parser is ParserWrapper wrapper)
            {
                parser = wrapper.Parser;
            }

            converter = source => parser.GetPair(source is ArraySegment<byte> segment ? segment : new ArraySegment<byte>(source.ToArray())).Value;
            return true;
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

                return null;
            }

            public override void Write(Utf8JsonWriter writer, IAsyncEnumerable<T> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
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
