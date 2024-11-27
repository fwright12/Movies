using System;
using System.Collections.Generic;
using System.Linq;

namespace Movies.Models
{
    public abstract class Item
    {
        public string Name { get; set; }
        public ItemType ItemType
        {
            get
            {
                if (this is Movie) return ItemType.Movie;
                else if (this is TVShow) return ItemType.TVShow;
                else if (this is TVSeason) return ItemType.TVSeason;
                else if (this is TVEpisode) return ItemType.TVEpisode;
                else if (this is Person) return ItemType.Person;
                else if (this is List) return ItemType.List;
                else if (this is Collection) return ItemType.Collection;
                else if (this is Company) return ItemType.Company;
                else return default;
            }
        }

        private IDictionary<object, object> IDs;

        public Item()
        {
            IDs = new Dictionary<object, object>();
        }

        public bool TryGetID<T>(ID<T> key, out T id)
        {
            if (IDs.TryGetValue(key, out var idObj) && idObj is T t)
            {
                id = t;
                return true;
            }

            id = default;
            return false;
        }

        public void SetID<T>(ID<T>.Key key, T value) => IDs[key.ID] = value;

        public override int GetHashCode() => TryGetID(TMDB.ID, out var id) ? id : (Name?.GetHashCode() ?? base.GetHashCode());
        public override bool Equals(object obj)
        {
            if (!(obj is Item item) || item.GetType() != GetType())
            {
                return false;
            } 
            
            if (item.IDs.Any(kvp => IDs.TryGetValue(kvp.Key, out var value) && value.Equals(kvp.Value)))
            {
                return true;
            }

            return Equals(item.Name, Name);
        }

        public override string ToString() => Name ?? base.ToString();
    }
}
