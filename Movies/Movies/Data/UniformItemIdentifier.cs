using Movies.Models;
using System;

namespace Movies
{
    public class UniformItemIdentifier : Uri
    {
        public Item Item { get; }
        public Property Property { get; }

        public UniformItemIdentifier(Item item, Property property) : base($"urn:{item.ItemType}:{(item.TryGetID(TMDB.ID, out var id) ? id.ToString() : item.Name)}:{property.Name}")
        {
            Item = item;
            Property = property;
        }
    }
}