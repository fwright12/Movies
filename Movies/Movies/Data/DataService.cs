using Movies.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public class ItemEventArgs : EventArgs
    {
        public PropertyDictionary Properties { get; }

        public ItemEventArgs(PropertyDictionary properties)
        {
            Properties = properties;
        }
    }

    public class DataService
    {
        public event EventHandler<ItemEventArgs> GetItemDetails;

        private Dictionary<Item, PropertyDictionary> Cache = new Dictionary<Item, PropertyDictionary>();

        //public Task<object> GetValue(Item item, Property property) => GetDetails(item).TryGetValue(property, out var value) ? value : Task.FromResult<object>(null);

        public PropertyDictionary GetDetails(Item item)
        {
            if (!Cache.TryGetValue(item, out var properties))
            {
                Cache.Add(item, properties = new PropertyDictionary());
                var e = new ItemEventArgs(properties);
                GetItemDetails?.Invoke(item, e);
            }

            return properties;
        }
    }
}