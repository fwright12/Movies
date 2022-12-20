using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Movies
{
    public class Session : BindableDictionary<object>
    {
        public DateTime? LastAccessed
        {
            get => DateTime.TryParse(GetValue(LAST_ACCESSED_KEY) as string, out var value) ? value : (DateTime?)null;
            set => SetValue(value.ToString(), LAST_ACCESSED_KEY);
        }

        public DateTime? DBLastCleaned
        {
            get => GetValue(DB_LAST_CLEANED_KEY) as DateTime?;
            set => SetValue(value, DB_LAST_CLEANED_KEY);
        }

        public Dictionary<string, string> Filters => GetValue(SAVED_FILTERS_KEY) is string filters ? JsonSerializer.Deserialize<Dictionary<string, string>>(filters) : new Dictionary<string, string>();

        private const string SAVED_FILTERS_KEY = "SavedFilters";
        private const string LAST_ACCESSED_KEY = "last accessed";
        private const string DB_LAST_CLEANED_KEY = "last cleaned";

        public Session() : this(new Dictionary<string, object>()) { }

        public Session(IDictionary<string, object> values) : base(values) { }

        public void SaveFilters(string json)
        {
            Cache[SAVED_FILTERS_KEY] = JsonSerializer.Serialize(json);
            OnPropertyChanged(nameof(Filters));
        }
    }
}
