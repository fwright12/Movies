using Movies;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Metadata = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>;
using ControlData = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>;
using System.Linq;

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
    public abstract class DatastoreRequest
    {

    }

    public abstract class DatastoreResponse
    {
        public abstract object Response { get; }
    }

    public class DatastoreKeyRequest<T> : DatastoreRequest
    {
        public T Key { get; }

        public DatastoreKeyRequest(T key)
        {
            Key = key;
        }
    }

    public class DatastoreResponse<T> : DatastoreResponse
    {
        public T Value { get; }
        public override object Response => Value;

        public DatastoreResponse(T value)
        {
            Value = value;
        }
    }

    public abstract class DatastoreArgs : EventArgsRequest
    {
        public DatastoreRequest Request { get; }
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

    public class DatastoreKeyArgs<TKey> : DatastoreArgs
    {
        public TKey Key { get; }

        public DatastoreKeyArgs(TKey key)
        {
            Key = key;
        }

        //public bool Handle<TValue>(TValue value) => Handle(new DatastoreResponse<TValue>(value));
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

    public class ResourceArgs : DatastoreKeyArgs<Uri>
    {
        public object Expected { get; protected set; }
        public Metadata Metadata { get; private set; }

        private Task LateBindingDelay;

        public ResourceArgs(Uri uri, object expected) : base(uri)
        {
            Expected = expected;
            Metadata = new Dictionary<string, string>();
        }

        public bool Handle(Resource resource) => Handle(new DatastoreResponse<Resource>(resource));

        protected override bool Accept(DatastoreResponse response)
        {
            if (Expected == null || (response is DatastoreResponse<Resource> resource && resource.Value().HasRepresentation(Expected)))
            {
                return base.Accept(response);
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> HandleLateBound(Resource resource, Task lateBindingDelay)
        {
            await (LateBindingDelay = lateBindingDelay.ContinueWith(_ => Handle(resource)));
            return IsHandled;
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

        public async Task<RestResponse> UpdateAsync()
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

    public class RestRequestArgs : ResourceArgs
    {
        public Uri Uri => Key;
        public State Body { get; }
        new public State Response => (base.Response as DatastoreResponse<Resource>)?.Value() as State ?? null;

        public Task<Representation<object>> RequestRepresentation { get; set; }
        public Task<ControlData> RequestControlData { get; }

        public RestRequestArgs(Uri uri, Type expected = null) : base(uri, expected) { }

        public RestRequestArgs(Uri uri, State body) : base(uri, null)
        {
            Body = body;
        }

        public RestRequestArgs(RestRequest request) : this(request.Uri, request.Representation, request.ControlData) { }

        public RestRequestArgs(Uri uri, Task<Representation<object>> representation, Task<ControlData> controlData) : base(uri, null)
        {
            RequestRepresentation = representation;
            UpdateExpectedAsync(RequestControlData = controlData);
        }

        private async void UpdateExpectedAsync(Task<ControlData> task)
        {
            var controlData = await task;
            Expected = controlData.TryGetValue(REpresentationalStateTransfer.Rest.CONTENT_TYPE, out var type) ? type : null;
        }

        public Task<bool> Handle(Connector connector, params Func<Representation<object>, Representation<object>>[] converters)
        {
            ResourceNamingAuthority authority = new ResourceNamingAuthority(connector, new RestRequest(Key, RequestControlData, RequestRepresentation));
            return HandleLateBound(authority.Resource, authority.UpdateAsync());
        }

        public Task<bool> Handle(DatastoreRestResponse<Resource> response)
        {
            base.Handle(response);
            return HandleLateBound(() => new Entity[] { new Entity(response.Representation.Result) }, Task.WhenAll(response.Representation, response.ControlData, response.Metadata));
        }

        //public Task<bool> Handle(Task<State> response) => HandleLateBound(() => response.Result, response);

        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is RestRequestArgs args && Equals(Key, args.Key) && Equals(Response, args.Response);
    }

    public class DatastoreRestResponse<T> : DatastoreResponse<Resource>
    {
        public Task<Representation<object>> Representation { get; private set; }
        public Task<ControlData> ControlData { get; private set; }
        public Task<Metadata> Metadata { get; private set; }

        public DatastoreRestResponse(Task<Representation<object>> responseRepresentation, Task<ControlData> responseControlData, Task<Metadata> responseMetadata) : base(() => new Entity[] { })
        {
            Representation = responseRepresentation;
            ControlData = responseControlData;
            Metadata = responseMetadata;
        }
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }

    public static class RestRequestArgsExtensions
    {
        private static Task<bool> Handle(this RestRequestArgs args, Connector connector)
        {
            ResourceNamingAuthority authority = new ResourceNamingAuthority(connector, new RestRequest(args.Key, args.RequestControlData, args.RequestRepresentation));
            return args.HandleLateBound(authority.Resource, authority.UpdateAsync());
        }

        public static Task<bool> Handle(this DatastoreKeyArgs<Uri> args, Connector connector)
        {
            ResourceNamingAuthority authority = new ResourceNamingAuthority(connector, new RestRequest(args.Key, null, null));
            args.Handle(new DatastoreResponse<Resource>(authority.Resource));
            return Task.FromResult(true);
        }

        public static async Task<bool> Handle(this DatastoreArgs args, Task<State> response)
        {
            var value = await response;
            return args.Handle(new DatastoreResponse<Resource>(() => value));
        }
    }
}
