using Movies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ControlData = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>;
using Metadata = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>;

namespace REpresentationalStateTransfer
{
    public interface IResource
    {
        IEnumerable<Entity> Get() => Get(DateTime.Now);
        IEnumerable<Entity> Get(DateTime time);
    }

    public class StaticResource : IResource
    {
        public IEnumerable<Entity> Entities { get; private set; }

        public StaticResource(params Entity[] entities) : this((IEnumerable<Entity>)entities) { }

        public StaticResource(IEnumerable<Entity> entities)
        {
            Entities = entities;
        }

        protected void Set(IEnumerable<Entity> entities)
        {
            Entities = entities;
        }

        public IEnumerable<Entity> Get(DateTime time) => Entities;

        public virtual bool TryGetRepresentation(object key, out Representation<object> representation)
        {
            foreach (Entity entity in Entities)
            {
                if (false == entity.Value is Representation<object> rep)
                {
                    continue;
                }
                else if (key is Type type)
                {
                    if (false == ((rep as ObjectRepresentation)?.Type ?? entity.Value.GetType()).IsAssignableFrom(type))
                    {
                        continue;
                    }
                }
                else if (key != null)
                {
                    continue;
                }

                representation = rep;
                return true;
            }

            representation = default;
            return false;
        }
    }

    public interface Connector
    {
        RestResponse Send(Uri identifer, Task<ControlData> controlData, Task<Representation<object>> representation = null);
    }

    public interface Representation<out T>
    {
        T Value { get; }
        Metadata Metadata { get; }
        Type Type => typeof(T);
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
        public const string ETAG = "ETag";
        public const string AGE = "age";
        public const string DATE = "date";
        public const string CACHE_CONTROL = "cache-control";
        public const string IF_NONE_MATCH = "If-None-Match";

        public static IResource EmptyResource = new StaticResource();
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
