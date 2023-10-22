using Movies;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Metadata = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>;
using ControlData = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace REpresentationalStateTransfer
{
    public interface Connector
    {
        RestResponse Send(Uri identifer, Task<ControlData> controlData, Task<Representation<object>> representation = null);
    }

    public interface Representation<out T>
    {
        T Value { get; }
        Metadata Metadata { get; }
    }

    public delegate IEnumerable<Entity> Resource();

    public class Entity
    {
        public object Value { get; }

        public Entity(Uri identifier)
        {
            Value = identifier;
        }

        public Entity(Representation<object> representation)
        {
            Value = representation;
        }

        public static implicit operator Entity(Uri uri) => new Entity(uri);

        public static Entity Create<T>(Representation<T> representation) where T : class => new Entity(representation);
        public static Entity Create<T>(T representation) where T : class => new Entity(new ObjectRepresentation<T>(representation));
    }

    public class RestRequest
    {
        public Uri Uri { get; set; }
        public Task<Representation<object>> Representation { get; set; }
        public Task<ControlData> ControlData { get; }

        public RestRequest(Uri uri, Task<ControlData> controlData, Task<Representation<object>> representation = null)
        {
            Uri = uri;
            Representation = representation;
            ControlData = controlData;
        }
    }

    public class RestResponse
    {
        public Task<Representation<object>> Representation { get; }
        public Task<ControlData> ControlData { get; }
        public Task<Metadata> Metadata { get; }

        public RestResponse(Task<ControlData> controlData, Task<Representation<object>> representation = null, Task<Metadata> metadata = null)
        {
            Representation = representation;
            ControlData = controlData;
            Metadata = metadata;
        }
    }

    public static class Rest
    {
        public const string CONTENT_TYPE = "content type";
    }

    public static class RestExtensions
    {
        public static RestResponse Send(this Connector connector, RestRequest request) => connector.Send(request.Uri, request.ControlData, request.Representation);

        public static bool HasRepresentation(this IEnumerable<Entity> entities, object type)
        {
            if (entities is State state && type is Type type1)
            {
                return state.HasRepresentation(type1);
            }

            foreach (var entity in entities)
            {
                if (entity is Representation<object> representation && representation.Metadata.TryGetValue("", out var repType) && repType == type.ToString())
                {
                    return true;
                }
            }

            return false;
        }
    }
}

namespace Movies
{
    public abstract class DatastoreWriteArgs
    {
        public object RawValue { get; }

        protected DatastoreWriteArgs(object value)
        {
            RawValue = value;
        }
    }

    public class DatastoreKeyValueWriteArgs<TKey, TValue> : DatastoreWriteArgs
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public DatastoreKeyValueWriteArgs(TKey key, TValue value) : base(value)
        {
            Key = key;
            Value = value;
        }
    }

    public abstract class DatastoreResponse
    {
        public abstract object RawValue { get; }
    }

    public class DatastoreResponse<T> : DatastoreResponse
    {
        public T Value { get; }
        public override object RawValue => Value;

        public DatastoreResponse(T value)
        {
            Value = value;
        }
    }

    public abstract class DatastoreReadArgs : EventArgsRequest
    {
        public DatastoreResponse Response { get; private set; }

        public bool Handle(DatastoreResponse response)
        {
            if (Accept(response))
            {
                Response = response;
                Handle();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual bool Accept(DatastoreResponse response) => true;
    }

    public class DatastoreKeyValueReadArgs<TKey> : DatastoreReadArgs
    {
        public TKey Key { get; }
        public Type Expected { get; private set; }

        public DatastoreKeyValueReadArgs(TKey key, Type expected = null)
        {
            Key = key;
            Expected = expected;
        }

        protected override bool Accept(DatastoreResponse response)
        {
            if (Expected == null)
            {
                return base.Accept(response);
            }
            else if (response.RawValue == null)
            {
                return !Expected.IsValueType;
            }
            else
            {
                return Expected.IsAssignableFrom(response.RawValue.GetType());
            }
        }

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is DatastoreKeyValueReadArgs<TKey> other && Equals(Key, other.Key) && Equals(Expected, other.Expected);
    }

    public class DatastoreKeyValueReadArgs<TKey, TValue> : DatastoreKeyValueReadArgs<TKey>
    {
        public virtual TValue Value => Response?.RawValue is TValue value ? value : default;

        public DatastoreKeyValueReadArgs(TKey key) : base(key, typeof(TValue)) { }

        protected DatastoreKeyValueReadArgs(TKey key, Type expected) : base(key, expected) { }

        public bool Handle(TValue value) => Handle(new DatastoreResponse<TValue>(value));

        protected override bool Accept(DatastoreResponse response) => response.RawValue is TValue || (response.RawValue == null && default(TValue) == null);
    }

    public interface IAsyncResponse
    {
        Task Response { get; }
    }

    public sealed class EventArgsAsyncWrapper<T> : EventArgs, IAsyncResponse
    {
        public T Args { get; }
        public Task Task { get; private set; }

        public Task Response => Task;

        public EventArgsAsyncWrapper(T args)
        {
            Args = args;
        }

        public void ExecuteInvoke(object sender, AsyncEventHandler<T> handler)
        {
            Task = handler.Invoke(sender, this);
        }
    }

    public class RestRequestArgs : DatastoreKeyValueReadArgs<Uri, object>
    {
        new public State Response => throw new NotImplementedException();// (base.Response as DatastoreResponse<Resource>)?.Value() as State ?? null;
        public string MimeType { get; }

        public Representation<string> RequestRepresentation { get; set; }
        public ControlData RequestControlData { get; }

        public RestRequestArgs(Uri uri, Type expected = null) : base(uri, expected) { }

        //public RestRequestArgs(RestRequest request) : this(request.Uri, request.Representation, request.ControlData) { }

        public RestRequestArgs(Uri uri, Representation<string> representation, ControlData controlData) : base(uri, controlData.TryGetValue(Rest.CONTENT_TYPE, out var expected) ? typeof(string) : null)
        {
            RequestRepresentation = representation;
        }
    }

    public class HttpRequestArgs : RestRequestArgs
    {
        public HttpRequestArgs(Uri uri, Type expected = null) : base(uri, expected)
        {
        }
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }

    public class RestResponse : DatastoreResponse
    {
        public override object RawValue => Expected != null && TryGetRepresentation(Entities, Expected, out var value) ? value : Entities.OfType<Representation<object>>().FirstOrDefault()?.Value;
        public IEnumerable<Entity> Entities { get; }
        public object Expected { get; set; }

        public Representation<object> Representation { get; private set; }
        public ControlData ControlData { get; private set; }
        public Metadata Metadata { get; private set; }

        public RestResponse(params Entity[] entities) : this((IEnumerable<Entity>)entities) { }
        public RestResponse(IEnumerable<Entity> entities)
        {
            Entities = entities;
        }

        public RestResponse(Representation<object> representation, ControlData controlData, Metadata metadata)
        {
            Representation = representation;
            ControlData = controlData;
            Metadata = metadata;

            Entities = State.Create(representation.Value);
        }

        public bool Add<T>(Func<object, T> converter)
        {
            return false;
        }

        protected bool Add(params Entity[] entities)
        {
            if (Entities is State state)
            {
                foreach (var entity in entities)
                {
                    state.Add(entity);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetRepresentation(object expected, out object value) => TryGetRepresentation(Entities, expected, out value);
        public bool TryGetRepresentation<T>(out T value)
        {
            if (TryGetRepresentation(Entities, typeof(T), out var valueObj) && valueObj is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        private static bool TryGetRepresentation(IEnumerable<Entity> entities, object expected, out object value)
        {
            if (entities is State state && expected is Type type)
            {
                return state.TryGetRepresentation(type, out value);
            }

            foreach (var entity in entities.OfType<Representation<object>>())
            {

            }

            throw new NotImplementedException();
        }
    }

    public class HttpResponse : RestResponse
    {
        public Task BindingDelay { get; }

        private HttpResponseMessage Message { get; }

        public HttpResponse(HttpResponseMessage message) : base(new State())// base(null, message.Headers, null)
        {
            Message = message;
            BindingDelay = BindLate(message.Content);
        }

        public async Task Add(IHttpConverter<object> converter)
        {
            var converted = await converter.Convert(Message.Content);
            (Entities as State)?.Add(converted.GetType(), converted);
        }

        private async Task BindLate(HttpContent content)
        {
            var bytes = await content.ReadAsByteArrayAsync();

            Add(Entity.Create(bytes));
            //Add(Entity.Create(Encoding.UTF8.GetString(bytes)));
        }
    }

    public class ResourceNamingAuthority
    {
        public Connector Connector { get; }
        public RestRequest Request { get; }

        private IEnumerable<Entity> Entities;

        public ResourceNamingAuthority(Connector connector, RestRequest request)
        {
            Connector = connector;
            Request = request;
            Entities = Enumerable.Empty<Entity>();
        }

        public async Task<REpresentationalStateTransfer.RestResponse> UpdateAsync()
        {
            var response = Connector.Send(Request);
            var representation = await response.Representation;

            Entities = new Entity[] { new Entity(representation) };
            return response;
        }

        public IEnumerable<Entity> Resource()
        {
            foreach (var entity in Entities)
            {
                yield return entity;
            }
        }
    }
}
