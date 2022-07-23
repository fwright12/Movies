using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Movies
{
    public class BindableDictionary<TKey, TValue> : BindableViewModel
    {
        private IDictionary<TKey, TValue> Cache { get; }

        public BindableDictionary(IDictionary<TKey, TValue> cache)
        {
            Cache = cache;
        }

        protected T GetValue<T>(TKey key) => Cache.TryGetValue(key, out var value) && value is T t ? t : default;

        protected virtual bool UpdateValue(TKey key, TValue value, [CallerMemberName] string propertyName = null)
        {
            if (!Cache.TryGetValue(key, out var cached) || !Equals(cached, value))
            {
                OnPropertyChanging(propertyName);
                Cache[key] = value;
                //var e = new PropertyChangeEventArgs(propertyName, cached, cached = value);
                OnPropertyChanged(propertyName);
                //OnPropertyChange(this, e);

                return true;
            }

            return false;
        }
    }

    public class UserPrefs : BindableDictionary<string, object>
    {
        public const string LANGUAGE_KEY = "language";
        public const string REGION_KEY = "region";

        public string Language
        {
            get => GetValue<string>(LANGUAGE_KEY);
            set => UpdateValue(LANGUAGE_KEY, value);
        }

        public string Region
        {
            get => GetValue<string>(REGION_KEY);
            set => UpdateValue(REGION_KEY, value);
        }

        public UserPrefs(IDictionary<string, object> cache) : base(cache) { }
    }
}
