using REpresentationalStateTransfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ControlData = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>>>;
using Metadata = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>;

namespace Movies
{
    public class RestRequestEventArgs : KeyValueReadEventArgs<Uri>
    {
        public Representation<string> Representation { get; set; }
        public IDictionary<string, IEnumerable<string>> ControlData { get; }
        public string MimeType { get; }

        public RestRequestEventArgs(Uri key, Type expected = null) : this(key, null, new Dictionary<string, IEnumerable<string>>(), expected) { }

        public RestRequestEventArgs(Uri uri, Representation<string> representation, IDictionary<string, IEnumerable<string>> controlData, Type expected = null) : base(uri, expected ?? (true == controlData?.TryGetValue(Rest.CONTENT_TYPE, out _) ? typeof(string) : null))
        {
            Representation = representation;
            ControlData = controlData;
        }
    }

    public class RestRequestArgs : ResourceRequestArgs<Uri>
    {
        public new RestRequestEventArgs Request => base.Request as RestRequestEventArgs;

        public RestRequestArgs(Uri uri, Type expected = null) : base(new RestRequestEventArgs(uri, expected)) { }

        public RestRequestArgs(Uri uri, Representation<string> representation, IDictionary<string, IEnumerable<string>> controlData, Type expected = null) : base(new RestRequestEventArgs(uri, representation, controlData, expected ?? (true == controlData?.TryGetValue(Rest.CONTENT_TYPE, out _) ? typeof(string) : null))) { }
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public new T Value => base.Value == null ? default : (T)base.Value;

        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }

    public class RestResponse : ResourceResponse
    {
        public IResource Resource { get; }
        public ControlData ControlData { get; private set; }
        public Metadata Metadata { get; private set; }

        public override int Count => Resource.Get().Count();

        public RestResponse(params Entity[] entities) : this((IEnumerable<Entity>)entities) { }
        public RestResponse(IEnumerable<Entity> entities, Type expected = null) : this(entities, null, null, expected) { }

        public RestResponse(Representation<object> representation, ControlData controlData, Metadata metadata, Type expected = null) : this((IResource)new State(representation), controlData, metadata, expected) { }
        //public RestResponse(State state, ControlData controlData, Metadata metadata, Type expected = null) : this(new StaticResource(state), controlData, metadata, expected) { }
        public RestResponse(IEnumerable<Entity> representations, ControlData controlData, Metadata metadata, Type expected = null) : this(new StaticResource(representations), controlData, metadata, expected) { }
        public RestResponse(IResource resource, ControlData controlData, Metadata metadata, Type expected = null) : this(resource, controlData, metadata, TryGetRepresentation(resource, expected, out var value) ? value : null) { }
        private RestResponse(IResource resource, ControlData controlData, Metadata metadata, object value) : base(value)
        {
            Resource = resource;
            ControlData = controlData ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
            Metadata = metadata ?? Enumerable.Empty<KeyValuePair<string, string>>();
        }

        public static bool TryGetRepresentation(IResource resource, Type type, out object value)
        {
            if (type == null)
            {
                var representation = resource.Get().Select(entity => entity.Value).OfType<Representation<object>>().FirstOrDefault();

                if (representation != null)
                {
                    value = representation.Value;
                    return true;
                }
            }
            else if (resource.Get() is State state && state.TryGetRepresentation(type, out var representation))
            {
                value = representation.Value;
                return true;
            }

            value = default;
            return false;
        }

        //public static RestResponse Create(Type expected, IResource resource, ControlData controlData = null, Metadata metadata = null) => new RestResponse(resource, controlData, metadata, expected);
        public static RestResponse Create(Type expected, IResource resource, ControlData controlData = null, Metadata metadata = null) => TryGetRepresentation(resource, expected, out var value) ? new RestResponse(resource, controlData, metadata, value) : null;

        public override bool TryGetRepresentation(Type type, out object value) => TryGetRepresentation(Resource, type, out value);
    }

    public class State : StaticResource, IEnumerable, IEnumerable<Representation<object>>, IEnumerable<Entity>
    {
        public int Count => Representations.Count;

        private Dictionary<Type, Representation<object>> Representations { get; }

        public State(params Representation<object>[] representations) : this((IEnumerable<Representation<object>>)representations) { }
        public State(IEnumerable<Representation<object>> representations)
        {
            Representations = new Dictionary<Type, Representation<object>>();

            foreach (var representation in representations)
            {
                AddRepresentation(representation.Type, representation);
            }

            Set(this);
        }

        private State(Type type, object initial)
        {
            Representations = new Dictionary<Type, Representation<object>>
            {
                { type, new ObjectRepresentation<object>(initial) }
            };
            Set(this);
        }

        public static State Create<T>(T value) => value as State ?? new State(typeof(T), value);
        public static State Create(object value) => value as State ?? new State(value.GetType(), value);
        public static State Null(Type type) => new State(type, null);

        public void Add<T>(T value) => Add(typeof(T), value);
        public void Add(Type type, object value) => AddRepresentation(type, value as Representation<object> ?? new ObjectRepresentation<object>(value));
        public void AddRepresentation<T>(Representation<T> representation) where T : class => AddRepresentation(typeof(T), representation);
        public void AddRepresentation(Type type, Representation<object> representation)
        {
            Representations.TryAdd(type, representation);
        }

        public bool HasRepresentation<T>() => HasRepresentation(typeof(T));
        public bool HasRepresentation(Type type) => TryGetValue(type, out _);

        public override bool TryGetRepresentation(object key, out Representation<object> representation)
        {
            if (false == key is Type type)
            {
                return base.TryGetRepresentation(key, out representation);
            }

            if (type != null)
            {
                if (TryGetValue(type, out var value))
                {
                    representation = new ObjectRepresentation<object>(value);
                    return true;
                }
                else
                {
                    representation = default;
                    return false;
                }
            }
            else if (Representations.Count > 0)
            {
                representation = ((IEnumerable<Entity>)this).Select(entity => entity.Value).OfType<Representation<object>>().FirstOrDefault();
                return true;
            }
            else
            {
                representation = default;
                return false;
            }
        }

        public bool TryGetValue(Type type, out object value)
        {
            if (Representations.TryGetValue(type, out var representation))
            {
                value = representation.Value;
                return true;
            }

            foreach (var kvp in Representations)
            {
                if (type.IsAssignableFrom(kvp.Key))
                {
                    value = kvp.Value.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool TryGetValue<T>(out T value)
        {
            if (TryGetValue(typeof(T), out var raw) && raw is T t)
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

        IEnumerator<Representation<object>> IEnumerable<Representation<object>>.GetEnumerator() => Representations.Values.GetEnumerator();

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => ((IEnumerable<Representation<object>>)this).Select(representation => new Entity(representation)).GetEnumerator();
    }

    public abstract class ObjectRepresentation
    {
        public abstract Type Type { get; }
    }

    public class ObjectRepresentation<T> : ObjectRepresentation, Representation<T>
    {
        public T Value { get; }
        public IEnumerable<KeyValuePair<string, string>> Metadata { get; }

        public override Type Type => typeof(T);

        public ObjectRepresentation(T value)
        {
            Value = value;
            Metadata = new Dictionary<string, string>();
        }

        public static implicit operator ObjectRepresentation<T>(T value) => new ObjectRepresentation<T>(value);
    }

    public abstract class LazilyConvertedRepresentation<T> : Representation<T>
    {
        public T Value => LazyValue.Value;
        public IEnumerable<KeyValuePair<string, string>> Metadata { get; set; }

        public Representation<object> SourceRepresentation { get; }
        public Func<object, T> Converter { get; }

        private Lazy<T> LazyValue;

        protected LazilyConvertedRepresentation(Representation<object> sourceRepresentation, Func<object, T> converter)
        {
            SourceRepresentation = sourceRepresentation;
            Converter = converter;

            LazyValue = new Lazy<T>(() => Converter(SourceRepresentation.Value));
        }
    }

    public class LazilyConvertedRepresentation<TSource, TTarget> : LazilyConvertedRepresentation<TTarget> where TSource : class
    {
        public LazilyConvertedRepresentation(Representation<TSource> sourceRepresentation, Func<TSource, TTarget> converter) : base(sourceRepresentation, source => source is TSource value ? converter(value) : default)
        {
        }
    }
}
