using Movies.Models;
using System;

namespace Movies
{
    public class UniformItemIdentifier : Uri
    {
        public Item Item { get; }
        public Property Property { get; }

        public UniformItemIdentifier(Item item, Property property) : this(item, property, null) { }
        public UniformItemIdentifier(Item item, Property property, string query) : base(BuildUri(item, property, query))
        {
            Item = item;
            Property = property;
        }

        private static string BuildUri(Item item, Property property, string query)
        {
            var parts = new object[]
            {
                "urn",
                item.ItemType,
                item.TryGetID(TMDB.ID, out var id) ? id.ToString() : item.Name,
                property.Name
            };
            var uri = string.Join(":", parts);
            if (!string.IsNullOrEmpty(query))
            {
                uri += "?" + query;
            }

            return uri;
        }
    }
}