using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class PropertyEventArgs : EventArgs
    {
        public IEnumerable<Property> Properties { get; }

        public PropertyEventArgs(params Property[] properties) : this((IEnumerable<Property>)properties) { }
        public PropertyEventArgs(IEnumerable<Property> properties)
        {
            Properties = properties;
        }
    }

    public abstract class PropertyValuePair
    {
        public Property Property { get; }
        public object Value { get; }

        public PropertyValuePair(Property property, object value)
        {
            Property = property;
            Value = value;
        }
    }

    public class PropertyValuePair<T> : PropertyValuePair
    {
        public PropertyValuePair(Property<T> property, Task<T> value) : base(property, value) { }
        public PropertyValuePair(MultiProperty<T> property, Task<IEnumerable<T>> value) : base(property, value) { }
    }

    public class PropertyDictionary : IReadOnlyCollection<PropertyValuePair>
    {
        public event EventHandler<PropertyEventArgs> PropertyAdded;

        public int Count => Properties.Count;
        public Task<object> this[Property property] => GetSingle<object>(property);

        private Dictionary<Property, IList<object>> Properties = new Dictionary<Property, IList<object>>();

        public bool TryGetValue(Property key, out Task<object> value)
        {
            value = GetSingle<object>(key);
            return true;
        }

        public Task<IEnumerable<T>> GetMultiple<T>(Property<T> key, string source = null) => GetMultiple<T>((Property)key, source);

        public Task<T> GetSingle<T>(Property<T> key, string source = null) => GetSingle<T>((Property)key, source);

        private Task<IEnumerable<T>> GetMultiple<T>(Property key, string source = null)
        {
            var values = AddProperty(key);

            foreach (var value in values)
            {
                if (TryCastTask<IEnumerable<T>>(value, out var multiple))
                {
                    return multiple;
                }
                else if (TryCastTask<T>(value, out var single))
                {
                    return FlattenTasks(single);
                }
            }

            return Task.FromResult<IEnumerable<T>>(new List<T>());
        }

        private Task<IEnumerable<T>> FlattenTasks<T>(params Task<T>[] tasks) => FlattenTasks((IEnumerable<Task<T>>)tasks);
        private async Task<IEnumerable<T>> FlattenTasks<T>(IEnumerable<Task<T>> tasks)
        {
            var values = new List<T>();

            foreach (var task in tasks)
            {
                values.Add(await task);
            }

            return values;
        }

        private Task<T> GetSingle<T>(Property key, string source = null)
        {
            var values = AddProperty(key);

            foreach (var value in values)
            {
                if (TryCastTask<T>(value, out var result))
                {
                    return result;
                }
            }

            return Task.FromResult<T>(default);
        }

        private bool TryCastTask<T>(object untyped, out Task<T> typed)
        {
            if (untyped is Task<T> task)
            {
                typed = task;
                return true;
            }
            else if (untyped is Task task1)
            {
                try
                {
                    typed = CastTask<T>(task1);
                    return true;
                }
                catch { }
            }

            typed = null;
            return false;
        }

        private async Task<T> CastTask<T>(Task task) => (T)await (dynamic)task;

        public void AddProperty<T>(Property<T> property) => AddProperty((Property)property);
        private IList<object> AddProperty(Property property)
        {
            if (!Properties.TryGetValue(property, out var values))
            {
                Properties.Add(property, values = new List<object>());
                PropertyAdded?.Invoke(this, new PropertyEventArgs(property));
            }

            return values;
        }

        public void Add<T>(Property<T> key, Task<T> value) => Add(key, value);
        public void Add<T>(MultiProperty<T> key, Task<IEnumerable<T>> value) => Add(key, value);

        public void Add(PropertyValuePair pair)
        {
            Add(pair.Property, pair.Value);
        }

        private void Add(Property property, object value)
        {
            if (!Properties.TryGetValue(property, out var values))
            {
                Properties.Add(property, values = new List<object>());
            }

            values.Add(value);
        }

        public IEnumerator<PropertyValuePair> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}