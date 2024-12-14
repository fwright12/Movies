using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public class UniformItemIdentifier : Uri
    {
        public Item Item { get; }
        public ItemType ItemType { get; }
        public Property Property { get; }

        public Language Language { get; }
        public Region Region { get; }
        public bool? IncludeAdult { get; }

        public UniformItemIdentifier(Item item, Property property, Language language = null, Region region = null, bool? includeAdult = null) : this(item.ItemType, item.TryGetID(TMDB.ID, out var id) ? id.ToString() : item.Name, property) //this(item, property, "")//, BuildQuery(language, region, includeAdult))
        {
            Item = item;
            Property = property;
            ItemType = Item.ItemType;

            Language = language;
            Region = region;
            IncludeAdult = includeAdult;
        }

        public UniformItemIdentifier(ItemType type, string id, Property property) : base(BuildUri(type, id, property, "")) { }

        private UniformItemIdentifier(Item item, Property property, string query) : base("")
        {
            Item = item;
            Property = property;
            ItemType = Item.ItemType;

            return;
            var queryParts = query.Split("&").Select(part => part.Split("=")).Where(kvp => kvp.Length == 2).ToDictionary(kvp => kvp[0], kvp => kvp[1]);
            if (queryParts.TryGetValue("language", out var iso_639))
            {
                Language = new Language(iso_639);
            }
            if (queryParts.TryGetValue("region", out var iso_3166))
            {
                Region = new Region(iso_3166);
            }
            IncludeAdult = queryParts.ContainsKey("adult");
        }

        private UniformItemIdentifier(Item item, ItemType itemType, Property property, Language language, Region region, bool? includeAdult, string uriString) : base(uriString)
        {
            Item = item;
            ItemType = itemType;
            Property = property;
            Language = language;
            Region = region;
            IncludeAdult = includeAdult;
        }

        public static bool TryParse(Uri uri, out UniformItemIdentifier uii)
        {
            uii = uri as UniformItemIdentifier;
            if (uii != null)
            {
                return true;
            }

            var parts = GetUrnParts(uri);

            if (!TryParseItemType(parts, out var itemType))
            {
                return false;
            }

            uii = new UniformItemIdentifier(null, itemType, null, null, null, null, uri.OriginalString);
            return true;
        }

        private static string[] GetUrnParts(Uri uri)
        {
            var urn = uri.ToString();
            return urn.Split(":");
        }

        private static bool TryParseItemInfo(Uri uri, out ItemType type, out int id, out string propertyName)
        {
            var parts = GetUrnParts(uri);

            if (parts.Length > 2)
            {
                propertyName = parts[2];
                return TryParseItemType(parts, out type) & int.TryParse(parts[1], out id);
            }
            else
            {
                type = default;
                id = default;
                propertyName = default;
                return false;
            }
        }

        private static bool TryParseItemType(string[] urnParts, out ItemType type)
        {
            var typeStr = urnParts[0].ToLower();

            if (typeStr == "movie") type = ItemType.Movie;
            else if (typeStr == "tvshow") type = ItemType.TVShow;
            else if (typeStr == "person") type = ItemType.Person;
            else
            {
                type = default;
                return false;
            }

            return true;
        }

        private static string BuildUri(ItemType itemType, string id, Property property, string query)
        {
            var parts = new object[]
            {
                "urn",
                itemType,
                id,
                property.Name
            };

            var uri = string.Join(":", parts);
            if (!string.IsNullOrEmpty(query))
            {
                uri += "?" + query;
            }

            return uri;
        }

        private static string BuildQuery(Language language, Region region, bool? adult)
        {
            var parameters = new List<KeyValuePair<string, string>>();

            if (language != null)
            {
                parameters.Add(new KeyValuePair<string, string>("language", language.Iso_639));
            }
            if (region != null)
            {
                parameters.Add(new KeyValuePair<string, string>("region", region.Iso_3166));
            }
            if (adult.HasValue)
            {
                parameters.Add(new KeyValuePair<string, string>("adult", adult.Value.ToString()));
            }

            return string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
    }
}