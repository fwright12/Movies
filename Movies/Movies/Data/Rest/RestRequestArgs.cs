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
        RestResponse Send(Uri identifer, ControlData controlData, Task<Representation<object>> representation = null);
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
        public ControlData ControlData { get; }

        public RestRequest(Uri uri, ControlData controlData, Task<Representation<object>> representation = null)
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
    public class DatastoreArgs : EventArgsRequest
    {
        public object Operation { get; set; }
    }

    public class KeyValueDatastoreArgs<TKey, TValue> : DatastoreArgs
    {
        public TKey Key { get; }
        public TValue Value { get; private set; }

        public KeyValueDatastoreArgs(TKey key)
        {
            Key = key;
        }

        public virtual bool Handle(TValue value)
        {
            Value = value;
            Handle();
            return true;
        }
    }

    public class EventArgsRequest : EventArgs
    {
        public bool IsHandled { get; private set; }

        protected void Handle()
        {
            IsHandled = true;
        }
    }

    public interface IAsyncEventArgsRequest
    {
        public Task<bool> IsHandledAsync { get; }
    }

    public interface IAsyncResponse
    {
        Task Response { get; }
    }

    public sealed class EventArgsAsyncWrapper<T> : EventArgs where T : EventArgs
    {
        public T Args { get; }
        public Task Task { get; private set; }

        public EventArgsAsyncWrapper(T args)
        {
            Args = args;
        }

        public void ExecuteInvoke(object sender, AsyncEventHandler<T> handler)
        {
            Task = handler.Invoke(sender, this);
        }

        //public bool Manage(Task task)
        //{
        //    Unwrap(task);
        //    return false;
        //}

        //public Task<bool> ManageAsync(Task task) => Task = UnwrapAsync(task);

        //private async void Unwrap(Task task) => await ManageAsync(task);
        private async Task<bool> UnwrapAsync(Task task)
        {
            await task;
            return Args is EventArgsRequest request && request.IsHandled;
        }
    }

    public class ResourceArgs : KeyValueDatastoreArgs<Uri, Resource>, IAsyncResponse
    {
        Task IAsyncResponse.Response => LateBindingDelay;

        public object Expected { get; }
        public Metadata Metadata { get; private set; }

        private Task LateBindingDelay;

        public ResourceArgs(Uri uri, object expected) : base(uri)
        {
            Expected = expected;
            Metadata = new Dictionary<string, string>();
        }

        public override bool Handle(Resource value)
        {
            if (value().HasRepresentation(Expected))
            {
                return base.Handle(value);
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

        public async Task UpdateAsync()
        {
            var response = Connector.Send(Request);
            var representation = await response.Representation;

            Entities = new Entity[] { new Entity(representation) };
        }

        public IEnumerable<Entity> Resource()
        {
            foreach (var entity in Entities)
            {
                yield return entity;
            }
        }
    }

    public class RestArgs : ResourceArgs
    {
        public RestRequest Request { get; }
        public RestResponse Response { get; private set; }

        public RestArgs(RestRequest request) : base(request.Uri, request.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.CONTENT_TYPE, out var type) ? type : null)
        {
            Request = request;
        }

        public Task<bool> Handle(Connector connector)
        {
            ResourceNamingAuthority authority = new ResourceNamingAuthority(connector, Request);
            return HandleLateBound(authority.Resource, authority.UpdateAsync());
        }
    }

    public class RestRequestArgs : AsyncChainEventArgs
    {
        public Uri Uri { get; }
        public State Body { get; }
        public Task<State> ResponseAsync { get; private set; }
        public State Response { get; private set; }
        public Type Expected { get; }

        public RestRequestArgs(Uri uri, Type expected = null)
        {
            Uri = uri;
            Expected = expected;
        }

        public RestRequestArgs(Uri uri, State body)
        {
            Uri = uri;
            Body = body;
        }

        private static async Task<State> Convert<T>(Task<T> value) => State.Create(await value);

        public async Task<bool> Handle<T>(Task<T> response)
        {
            await Handle(Convert(response));
            return true;
        }

        public bool Handle<T>(T response) => Handle(response as State ?? State.Create<T>(response));

        public bool Handle<T>(IConverter<T> converter)
        {
            if (Expected != null && Expected != typeof(T) && Response?.Add(Expected, converter) == true)
            {
                Handle();
                return true;
            }

            return false;
        }

        public bool Handle(State response)
        {
            Response = response;

            if (Expected == null || response.HasRepresentation(Expected))
            {
                Handle();
                return true;
            }

            return false;
        }

        public Task Handle(Task<State> response)
        {
            var result = HandleAsync(response);
            RequestSuspension(response);
            return response;
        }

        private async Task<bool> HandleAsync(Task<State> response) => Handle(await response);
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }
}
