using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Movies
{
    public class Session : BindableDictionary<string>
    {
        public DateTime? LastAccessed
        {
            get => DateTime.TryParse(GetValue(LAST_ACCESSED_KEY) as string, out var value) ? value : (DateTime?)null;
            set => SetValue(value.ToString(), LAST_ACCESSED_KEY);
        }

        public DateTime? DBLastCleaned
        {
            get => GetValue(DB_LAST_CLEANED_KEY) is string str ? JsonSerializer.Deserialize<DateTime>(str) : null;
            set => SetValue(JsonSerializer.Serialize(value), DB_LAST_CLEANED_KEY);
        }

        public Dictionary<string, string> Filters
        {
            get
            {
                if (_Filters == null)
                {
                    try
                    {
                        _Filters = JsonSerializer.Deserialize<Dictionary<string, string>>((string)GetValue(SAVED_FILTERS_KEY));
                    }
                    catch { }

                    _Filters ??= new Dictionary<string, string>();
                }

                return _Filters;
            }
        }

        private const string SAVED_FILTERS_KEY = "SavedFilters";
        private const string LAST_ACCESSED_KEY = "last accessed";
        private const string DB_LAST_CLEANED_KEY = "last cleaned";

        private readonly JsonSerializerOptions FilterSerializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new ItemTypeConverter(),
                new PredicateConverter(),
                new PropertyConverter(),
                new PersonConverter()
            }
        };
        private Dictionary<string, string> _Filters;

        public Session() : this(new Dictionary<string, string>()) { }

        public Session(IDictionary<string, string> values) : base(values) { }

        public bool TryGetFilters(string key, out IEnumerable<OperatorPredicate> filters)
        {
            if (Filters.TryGetValue(key, out var savedFilters))
            {
                filters = JsonSerializer.Deserialize<IEnumerable<OperatorPredicate>>(savedFilters, FilterSerializerOptions);
                return true;
            }
            else
            {
                filters = null;
                return false;
            }
        }

        public void SaveFilters(string key, IEnumerable<OperatorPredicate> predicates)
        {
            Filters[key] = System.Text.Json.JsonSerializer.Serialize(predicates, FilterSerializerOptions);
            Cache[SAVED_FILTERS_KEY] = System.Text.Json.JsonSerializer.Serialize(Filters);
            //OnPropertyChanged(nameof(Filters));
        }
    }
}
