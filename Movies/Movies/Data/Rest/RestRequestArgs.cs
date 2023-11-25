using REpresentationalStateTransfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ControlData = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>;
using Metadata = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>;

namespace Movies
{
    public class RestRequestArgs : ResourceReadArgs<Uri>
    {
        public string MimeType { get; }

        public Representation<string> Representation { get; set; }
        public IDictionary<string, IEnumerable<string>> ControlData { get; } = new Dictionary<string, IEnumerable<string>>();

        public RestRequestArgs(Uri uri, Type expected = null) : base(uri)
        {
            Expected = expected;
        }

        //public RestRequestArgs(RestRequest request) : this(request.Uri, request.Representation, request.ControlData) { }

        public RestRequestArgs(Uri uri, Representation<string> representation, IDictionary<string, IEnumerable<string>> controlData) : base(uri)
        {
            Representation = representation;
            ControlData = controlData;

            if (controlData.TryGetValue(Rest.CONTENT_TYPE, out var expected))
            {
                Expected = typeof(string);
            }
        }

        //protected override bool Accept(DatastoreResponse response) => response is RestResponse && base.Accept(response);
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public virtual T Value => base.Response?.RawValue is T value ? value : default;

        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }

    public class RestResponse : ResourceResponse
    {
        public override object RawValue => Expected != null && TryGetRepresentation(Entities, Expected, out var value) ? value : Entities.Select(entity => entity.Value).OfType<Representation<object>>().FirstOrDefault()?.Value;
        public IEnumerable<Entity> Entities { get; }
        public object Expected { get; set; }

        public Representation<object> Representation { get; private set; }
        public ControlData ControlData { get; private set; }
        public Metadata Metadata { get; private set; }

        public RestResponse(params Entity[] entities) : this((IEnumerable<Entity>)entities) { }
        public RestResponse(IEnumerable<Entity> entities) : this(entities, null, null) { }

        public RestResponse(Representation<object> representation, ControlData controlData, Metadata metadata) : this(new State(representation.Value), controlData, metadata) { }
        public RestResponse(IEnumerable<Entity> representations, ControlData controlData, Metadata metadata)
        {
            Entities = representations;
            ControlData = controlData ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
            Metadata = metadata ?? Enumerable.Empty<KeyValuePair<string, string>>();
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

    public interface IConverter<T>// : IConverter1
    {
        bool TryConvert(T original, Type targetType, out object converted);
    }

    public class State : IEnumerable, IEnumerable<Representation<object>>, IEnumerable<Entity>
    {
        public int Count => Representations.Count;

        private Dictionary<Type, object> Representations { get; }

        public State()
        {
            Representations = new Dictionary<Type, object>();
        }
        public State(object initial) : this(initial.GetType(), initial) { }
        private State(Type type, object initial)
        {
            Representations = new Dictionary<Type, object>
            {
                { type, initial }
            };
        }

        public static State Create<T>(T value) => value as State ?? new State(typeof(T), value);
        public static State Create(object value) => value as State ?? new State(value);
        public static State Null(Type type) => new State(type, null);

        public bool Add<TExisting, TNew>(IConverter<TExisting> converter) => Add<TExisting>(typeof(TNew), converter);
        public bool Add<T>(Type type, IConverter<T> converter)
        {
            foreach (var value in this.OfType<T>())
            {
                if (converter.TryConvert(value, type, out var converted))
                {
                    Representations.Add(type, converted);
                    return true;
                }
            }

            return false;
        }

        public void Add(Type type, object value)
        {
            Representations.TryAdd(type, value);
        }

        public void Add<T>(T value) => Add(typeof(T), value);

        public void Add(Entity entity)
        {
            if (entity.Value is Representation<object> representation)
            {
                Add(representation.Value.GetType(), representation.Value);
            }
            else
            {
                Add(entity.Value.GetType(), entity.Value);
            }
        }

        public Resource AsResource() => GetResource;

        private ISet<Entity> GetResource() => Representations.Values.Select(representation => new Entity(new ObjectRepresentation<object>(representation))).ToHashSet();

        public bool HasRepresentation<T>() => HasRepresentation(typeof(T));
        public bool HasRepresentation(Type type) => TryGetRepresentation(type, out _);

        public bool TryGetRepresentation(Type type, out object value)
        {
            if (Representations.TryGetValue(type, out value))
            {
                return true;
            }

            foreach (var kvp in Representations)
            {
                if (type.IsAssignableFrom(kvp.Key))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool TryGetRepresentation<T>(out T value)
        {
            if (TryGetRepresentation(typeof(T), out var raw) && raw is T t)
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

        public IEnumerator GetEnumerator() => Representations.Values.GetEnumerator();

        IEnumerator<Representation<object>> IEnumerable<Representation<object>>.GetEnumerator() => Representations.Values.Select(value => new ObjectRepresentation<object>(value)).GetEnumerator();

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => ((IEnumerable<Representation<object>>)this).Select(representation => new Entity(representation)).GetEnumerator();
    }

    public class ObjectRepresentation<T> : Representation<T>
    {
        public T Value { get; }
        public IEnumerable<KeyValuePair<string, string>> Metadata { get; }

        public ObjectRepresentation(T value)
        {
            Value = value;
            Metadata = new Dictionary<string, string>();
        }

        public static implicit operator ObjectRepresentation<T>(T value) => new ObjectRepresentation<T>(value);
    }
}
