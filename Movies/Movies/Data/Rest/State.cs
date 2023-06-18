using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public class State : IEnumerable
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
    }
}