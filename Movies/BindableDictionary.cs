using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Movies
{
    public delegate T TryParse<T>(object value, out T parsed);

    public class BindableDictionary<TValue> : BindableViewModel
    {
        protected IDictionary<string, TValue> Cache { get; }

        public BindableDictionary() : this(new Dictionary<string, TValue>()) { }

        public BindableDictionary(IDictionary<string, TValue> cache)
        {
            Cache = cache;
        }

        protected TValue GetValue([CallerMemberName] string propertyName = null, TryParse<TValue> parse = null)
        {
            TValue result = default;

            if (Cache.TryGetValue(propertyName, out var value))
            {
                if (parse != null)
                {
                    parse.Invoke(value, out result);
                }
                else if (value is TValue t)
                {
                    result = t;
                }
            }

            return result;
        }

        protected void SetValue(TValue value, [CallerMemberName] string propertyName = null)
        {
            if (!Cache.TryGetValue(propertyName, out var oldValue) || !Equals(oldValue, value))
            {
                OnPropertyChanging(propertyName);
                var e = new PropertyChangeEventArgs(propertyName, oldValue, Cache[propertyName] = value);
                OnPropertyChanged(propertyName);
                OnPropertyChange(this, e);
            }
        }
    }
}
