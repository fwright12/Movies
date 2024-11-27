using Movies.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        public static bool TryGetParameters(Item item, out List<object> parameters)
        {
            parameters = new List<object>();

            if (item is TVSeason season)
            {
                parameters.Add(season.SeasonNumber);
                item = season.TVShow;
            }
            else if (item is TVEpisode episode)
            {
                parameters.Add(episode.Season.SeasonNumber, episode.EpisodeNumber);
                item = episode.Season?.TVShow;
            }

            if (item == null || !TryGetID(item, out var id))
            {
                return false;
            }

            parameters.Insert(0, id);
            return true;
        }
    }

    public partial class ItemProperties
    {
        public IReadOnlyDictionary<TMDbRequest, List<Parser>> Info { get; }
        public IReadOnlyDictionary<Property, TMDbRequest> PropertyLookup { get; }

        public ItemInfoCache Cache { get; set; }

        private static Dictionary<Property, string> CHANGE_KEY_PROPERTY_MAP = new Dictionary<Property, string>
        {
            [Media.KEYWORDS] = "plot_keywords",
            [Media.POSTER_PATH] = "images",
            [Media.BACKDROP_PATH] = "images",
            [Media.TRAILER_PATH] = "videos",
            [Movie.CONTENT_RATING] = "releases",
            [Movie.RELEASE_DATE] = "releases",
            [TVShow.CONTENT_RATING] = "releases",
            [TVShow.FIRST_AIR_DATE] = "episode",
            [TVShow.LAST_AIR_DATE] = "episode",
            //[TVShow.SEASONS] = "season",
            [Person.PROFILE_PATH] = "images",
        };

        // These are properties that TMDb does not monitor for changes. As a result they won't be cached across sessions, and will request data once EVERY session
        public static HashSet<Property> NO_CHANGE_KEY = new HashSet<Property>
        {
            Media.ORIGINAL_LANGUAGE,
            Media.RECOMMENDED,
            Movie.PARENT_COLLECTION,
            TVShow.SEASONS, // There is a change key for this but I'm not tracking it at this point
            TVShow.NETWORKS,
            Person.GENDER,
            Person.CREDITS,
            TMDB.POPULARITY
        };

        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType UNCACHED_TYPES = ~(ItemType.Movie | ItemType.TVShow | ItemType.Person);

        private List<TMDbRequest> SupportsAppendToResponse = new List<TMDbRequest>();
        private Dictionary<TMDbRequest, List<Property>> AllRequests { get; }
        private Dictionary<Property, string> ChangeKeyLookup { get; }
        private Dictionary<TMDbRequest, List<Parser>> Uncacheable { get; }

        private SemaphoreSlim CacheSemaphore = new SemaphoreSlim(1, 1);

        public ItemProperties(Dictionary<TMDbRequest, List<Parser>> requests)
        {
            var info = new Dictionary<TMDbRequest, List<Parser>>();
            var propertyLookup = new Dictionary<Property, TMDbRequest>();

            foreach (var kvp in requests)
            {
                if (kvp.Key.SupportsAppendToResponse)
                {
                    SupportsAppendToResponse.Add(kvp.Key);
                }

                foreach (var parser in kvp.Value)
                {
                    propertyLookup[parser.Property] = kvp.Key;
                }

                info.Add(kvp);
            }

            Info = info;
            PropertyLookup = propertyLookup;
            //AllRequests = Info.Keys.ToList();
            AllRequests = new Dictionary<TMDbRequest, List<Property>>(Info.Select(kvp => new KeyValuePair<TMDbRequest, List<Property>>(kvp.Key, kvp.Value.Select(parser => parser.Property).ToList())));
            ChangeKeyLookup = new Dictionary<Property, string>();
            Uncacheable = new Dictionary<TMDbRequest, List<Parser>>(Info.Select(kvp => new KeyValuePair<TMDbRequest, List<Parser>>(kvp.Key, kvp.Value.Where(parser => NO_CHANGE_KEY.Contains(parser.Property)).ToList())));

            foreach (var parser in Info.SelectMany(kvp => kvp.Value))
            {
                var property = parser.Property;

                if (CHANGE_KEY_PROPERTY_MAP.TryGetValue(property, out var changeKey) || (changeKey = ((parser as ParserWrapper)?.JsonParser as JsonPropertyParser)?.Property) != null)
                {
                    ChangeKeyLookup[property] = changeKey;
                    //changeKey = jpp.Property;
                }
            }
        }

        public bool HasProperty(Property property)
        {
            for (; property != null; property = property.Parent)
            {
                if (PropertyLookup.ContainsKey(property))
                {
                    return true;
                }
            }

            return false;
        }
    }
}