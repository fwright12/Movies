using REpresentationalStateTransfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public interface IConverter<T>// : IConverter1
    {
        bool TryConvert(T original, Type targetType, out object converted);
    }

    public class State : IEnumerable, IEnumerable<Representation<object>>, IEnumerable<Entity>
    {
        private Dictionary<Type, object> Representations { get; }

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
            Representations.Add(type, value);
        }

        public void Add<T>(T value) => Add(typeof(T), value);

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