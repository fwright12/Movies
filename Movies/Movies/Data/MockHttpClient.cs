#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Movies
{
    public class Router
    {
        public List<(Regex Route, string Response)> Routes { get; }

        public Router(IEnumerable<(string, string)> routes)
        {
            Routes = new List<(Regex route, string content)>(routes.Select(route => (new Regex("^" + route.Item1 + "$"), route.Item2)).OrderByDescending(route => route.Item1.ToString().Length));
        }

        public string Route(Uri uri)
        {
            string path = (uri.IsAbsoluteUri ? uri.AbsolutePath : uri.ToString().Split("?").FirstOrDefault().Split(".").LastOrDefault()).Trim('/');
            foreach (var route in Routes)
            {
                if (route.Route.IsMatch(path))
                {
                    return route.Item2;
                }
            }

            return null;
        }
    }

    public partial class MockHandler : HttpClientHandler
    {
        public static readonly string DEFAULT_ETAG = "\"lkjsdflkjasdflkjsdflkjsdaflkjsadf\"";

        public static List<string> CallHistory = new List<string>();
        public List<string> LocalCallHistory = new List<string>();
        public bool Connected { get; private set; } = true;

        public bool Reconnect() => Connected = true;
        public bool Disconnect() => Connected = false;

        public static void ClearCallHistory() => CallHistory.Clear();

        private static Dictionary<object, string> DetailsRoutes = new Dictionary<object, string>
        {
            [API.MOVIES.GET_DETAILS] = DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_RESPONSE,
            [API.TV.GET_DETAILS] = DUMMY_TMDB_DATA.THE_OFFICE_RESPONSE,
            [API.TV_SEASONS.GET_DETAILS] = DUMMY_TMDB_DATA.THE_OFFICE_SEASON_3_RESPONSE,
            [API.TV_EPISODES.GET_DETAILS] = DUMMY_TMDB_DATA.THE_OFFICE_SEASON_3_EPISODE_20_RESPONSE,
            [API.PEOPLE.GET_DETAILS] = DUMMY_TMDB_DATA.JESSICA_CHASTAIN_RESPONSE,
            [API.COLLECTIONS.GET_DETAILS] = DUMMY_TMDB_DATA.HARRY_POTTER_COLLECTION_RESPONSE,
        };

        private static Dictionary<object, string> TMDbRoutes = new Dictionary<TMDbRequest, string>
        {
            [string.Format(API.TRENDING.GET_TRENDING.Endpoint, "movie", "[^/]*")] = DUMMY_TMDB_DATA.TRENDING_MOVIES_RESPONSE,
            [API.DISCOVER.MOVIE_DISCOVER] = DUMMY_TMDB_DATA.TRENDING_MOVIES_RESPONSE,
            [string.Format(API.TRENDING.GET_TRENDING.Endpoint, "tv", "[^/]*")] = DUMMY_TMDB_DATA.TRENDING_TV_RESPONSE,
            [API.DISCOVER.TV_DISCOVER] = DUMMY_TMDB_DATA.TRENDING_TV_RESPONSE,
            [string.Format(API.TRENDING.GET_TRENDING.Endpoint, "person", "[^/]*")] = DUMMY_TMDB_DATA.TRENDING_PEOPLE_RESPONSE,
            [API.PEOPLE.GET_POPULAR] = DUMMY_TMDB_DATA.TRENDING_PEOPLE_RESPONSE,

            [API.V4.ACCOUNT.GET_MOVIES] = DUMMY_TMDB_LISTS.TMDB_PERSONAL_WATCHLIST_MOVIES_RESPONSE,
            [API.V4.ACCOUNT.GET_TV_SHOWS] = DUMMY_TMDB_LISTS.TMDB_ACCOUNT_FAVORITE_TV_RESPONSE,
            [new TMDbRequest("account/{0}/lists") { Version = 4 }] = DUMMY_TMDB_LISTS.TMDB_ACCOUNT_LISTS_RESPONSE,
            [API.V3.ACCOUNT.ADD_TO_LIST] = DUMMY_TMDB_LISTS.TMDB_WATCHED_LIST_RESPONSE,

            [API.CONFIGURATION.GET_API_CONFIGURATION] = DUMMY_TMDB_CONFIG.CONFIGURATION,
            [API.GENRES.GET_MOVIE_LIST] = DUMMY_TMDB_CONFIG.MOVIE_GENRE_VALUES,
            [API.GENRES.GET_TV_LIST] = DUMMY_TMDB_CONFIG.TV_GENRE_VALUES,
            [API.CERTIFICATIONS.GET_MOVIE_CERTIFICATIONS] = DUMMY_TMDB_CONFIG.MOVIE_CERTIFICATION_VALUES,
            [API.CERTIFICATIONS.GET_TV_CERTIFICATIONS] = DUMMY_TMDB_CONFIG.TV_CERTIFICATION_VALUES,
            [API.WATCH_PROVIDERS.GET_MOVIE_PROVIDERS] = DUMMY_TMDB_CONFIG.MOVIE_WATCH_PROVIDER_VALUES,
            [API.WATCH_PROVIDERS.GET_TV_PROVIDERS] = DUMMY_TMDB_CONFIG.TV_WATCH_PROVIDER_VALUES,
            [API.CONFIGURATION.GET_COUNTRIES] = DUMMY_TMDB_CONFIG.COUNTRIES_VALUES,
            [API.CONFIGURATION.GET_API_CONFIGURATION] = DUMMY_TMDB_CONFIG.CONFIGURATION,

            [API.SEARCH.MULTI_SEARCH] = DUMMY_TMDB_DATA.TRENDING_MOVIES_RESPONSE
        }.ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);

        private static Dictionary<object, string> TraktRoutes = new Dictionary<object, string>
        {
            ["users/me/lists"] = TRAKT_ACCOUNT_LISTS_RESPONSE,
            ["users/me/lists/{0}"] = TRAKT_ACCOUNT_FAVORITES_RESPONSE,
            //["users/me/lists"] = TRAKT_ACCOUNT_FAVORITES_RESPONSE,
            ["sync/last_activities"] = "{}",
        };

        private static Router Router { get; }

        static MockHandler()
        {
            Router = new Router(DetailsRoutes.Concat(TMDbRoutes).Concat(TraktRoutes).Select<KeyValuePair<object, string>, (string, string)>(kvp =>
            {
                string url;

                if (kvp.Key is TMDbRequest tmdb)
                {
                    url = tmdb.GetRequest().RequestUri.OriginalString.Split('?').FirstOrDefault();
                }
                else
                {
                    url = kvp.Key?.ToString() ?? string.Empty;
                }

                return (new Regex("{\\d+}").Replace(url, "[^/]*"), kvp.Value);
            }));
        }

        //new public Task<HttpResponseMessage> GetAsync(Uri requestUri) => SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri));
        //new public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) => SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
        //new public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => SendAsync(request, new CancellationToken());

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;

            if (request.Headers.TryGetValues(REpresentationalStateTransfer.Rest.IF_NONE_MATCH, out var etags) && !etags.Skip(1).Any() && etags.FirstOrDefault() == DEFAULT_ETAG)
            {
                response = new HttpResponseMessage(HttpStatusCode.NotModified);
            }
            else
            {
                var endpoint = request.RequestUri.ToString();
                string content;

                if (endpoint.Contains("changes"))
                {
                    content = "{}";
                }
                else
                {
                    content = Router.Route(request.RequestUri);
                }

                if (!DebugConfig.AllowLiveRequests)
                {
                    content ??= "{}";
                }

                if (!Connected)
                {
                    throw new Exception("Mock HTTP handler is not connected");
                }

                if (content != null)
                {
                    response = new HttpResponseMessage
                    {
                        Content = new StringContent(content),
                        RequestMessage = request,
                    };

                    response.Headers.Add(REpresentationalStateTransfer.Rest.ETAG, DEFAULT_ETAG);
                }
            }

            string url = request.RequestUri.IsAbsoluteUri ? request.RequestUri.PathAndQuery.TrimStart('/') : request.RequestUri.ToString();
            CallHistory.Add(url);
            LocalCallHistory.Add(url);
            bool isLive = false;

            if (DebugConfig.AllowLiveRequests && response == null)
            {
                //throw new Exception();
                response = await base.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Print.Log(json);
                }

                isLive = true;
            }
            else if (DebugConfig.SimulatedDelay > 0)
            {
                await Task.Delay(DebugConfig.SimulatedDelay);
            }

            if (DebugConfig.LogWebRequests)
            {
                Print.Log($"web request{(!isLive ? " (mock)" : string.Empty)} ({response?.StatusCode}): {request.RequestUri}");
            }
            DebugConfig.Breakpoint();

            return response;
        }
    }
}
#endif