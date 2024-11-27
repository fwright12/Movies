using System.Collections.Generic;
using System.Net.Http;

namespace Movies
{
    public class TMDbApi
    {
        public static readonly Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> AutoAppend = new Dictionary<TMDbRequest, IEnumerable<TMDbRequest>>
        {
            [API.MOVIES.GET_DETAILS] = new List<TMDbRequest>
            {
                API.MOVIES.GET_CREDITS,
                API.MOVIES.GET_EXTERNAL_IDS,
                API.MOVIES.GET_KEYWORDS,
                API.MOVIES.GET_RECOMMENDATIONS,
                API.MOVIES.GET_RELEASE_DATES,
                API.MOVIES.GET_VIDEOS,
                API.MOVIES.GET_WATCH_PROVIDERS
            },
            [API.TV.GET_DETAILS] = new List<TMDbRequest>
            {
                API.TV.GET_AGGREGATE_CREDITS,
                API.TV.GET_CONTENT_RATINGS,
                API.TV.GET_EXTERNAL_IDS,
                API.TV.GET_KEYWORDS,
                API.TV.GET_RECOMMENDATIONS,
                API.TV.GET_VIDEOS,
                API.TV.GET_WATCH_PROVIDERS
            },
            [API.TV_SEASONS.GET_DETAILS] = new List<TMDbRequest>
            {
                API.TV_SEASONS.GET_AGGREGATE_CREDITS,
            },
            [API.TV_EPISODES.GET_DETAILS] = new List<TMDbRequest>
            {
                API.TV_EPISODES.GET_CREDITS,
            },
            [API.PEOPLE.GET_DETAILS] = new List<TMDbRequest>
            {
                API.PEOPLE.GET_COMBINED_CREDITS,
            }
        };

        public TMDbApi(HttpClient client, TMDbResolver resolver) { }
    }
}
