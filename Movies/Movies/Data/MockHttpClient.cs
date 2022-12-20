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

        public Router(IEnumerable<(string, string)> routes) : this(routes.Select(route => (new Regex(route.Item1), route.Item2))) { }
        public Router(IEnumerable<(Regex, string)> routes)
        {
            Routes = new List<(Regex route, string content)>(routes.OrderByDescending(route => route.Item1.ToString().Length));
        }

        public string Route(string url)
        {
            foreach (var route in Routes)
            {
                if (route.Route.IsMatch(url))
                {
                    return route.Item2;
                }
            }

            return null;
        }
    }

    public partial class HttpClient
    {
        public static bool AllowLiveRequests = true;
        public static bool BreakOnRequest = true;
        public static int SimulatedDelay = 0;
    }

    public partial class HttpClient : System.Net.Http.HttpClient
    {
        public static List<string> CallHistory = new List<string>();

        public static void ClearCallHistory() => CallHistory.Clear();

        private Dictionary<object, string> DetailsRoutes = new Dictionary<object, string>
        {
            [API.MOVIES.GET_DETAILS] = HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_RESPONSE,
            [API.TV.GET_DETAILS] = THE_OFFICE_RESPONSE,
            [API.TV_SEASONS.GET_DETAILS] = THE_OFFICE_SEASON_3_RESPONSE,
            [API.TV_EPISODES.GET_DETAILS] = THE_OFFICE_SEASON_3_EPISODE_20_RESPONSE,
            [API.PEOPLE.GET_DETAILS] = JESSICA_CHASTAIN_RESPONSE,
            [API.COLLECTIONS.GET_DETAILS] = HARRY_POTTER_COLLECTION_RESPONSE,
        };

        private Dictionary<object, string> TMDbRoutes = new Dictionary<TMDbRequest, string>
        {
            [string.Format(API.TRENDING.GET_TRENDING.Endpoint, "movie", ".*")] = TRENDING_MOVIES_RESPONSE,
            [API.DISCOVER.MOVIE_DISCOVER] = TRENDING_MOVIES_RESPONSE,
            [string.Format(API.TRENDING.GET_TRENDING.Endpoint, "tv", ".*")] = TRENDING_TV_RESPONSE,
            [API.DISCOVER.TV_DISCOVER] = TRENDING_TV_RESPONSE,
            [string.Format(API.TRENDING.GET_TRENDING.Endpoint, "person", ".*")] = TRENDING_PEOPLE_RESPONSE,
            [API.PEOPLE.GET_POPULAR] = TRENDING_PEOPLE_RESPONSE,

            [API.V4.ACCOUNT.GET_MOVIES] = TMDB_ACCOUNT_FAVORITE_MOVIES_RESPONSE,
            [API.V4.ACCOUNT.GET_TV_SHOWS] = TMDB_ACCOUNT_FAVORITE_TV_RESPONSE,
            [new TMDbRequest("account/{0}/lists") { Version = 4 }] = TMDB_ACCOUNT_LISTS_RESPONSE,
            [API.V3.ACCOUNT.ADD_TO_LIST] = TMDB_WATCHED_LIST_RESPONSE,

            [API.CONFIGURATION.GET_API_CONFIGURATION] = CONFIGURATION,
            [API.GENRES.GET_MOVIE_LIST] = HttpClient.MOVIE_GENRE_VALUES,
            [API.GENRES.GET_TV_LIST] = HttpClient.TV_GENRE_VALUES,
            [API.CERTIFICATIONS.GET_MOVIE_CERTIFICATIONS] = HttpClient.MOVIE_CERTIFICATION_VALUES,
            [API.CERTIFICATIONS.GET_TV_CERTIFICATIONS] = HttpClient.TV_CERTIFICATION_VALUES,
            [API.WATCH_PROVIDERS.GET_MOVIE_PROVIDERS] = HttpClient.MOVIE_WATCH_PROVIDER_VALUES,
            [API.WATCH_PROVIDERS.GET_TV_PROVIDERS] = HttpClient.TV_WATCH_PROVIDER_VALUES,
            [API.CONFIGURATION.GET_COUNTRIES] = HttpClient.COUNTRIES_VALUES,
            [API.CONFIGURATION.GET_API_CONFIGURATION] = HttpClient.CONFIGURATION,
        }.ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);

        private Dictionary<object, string> TraktRoutes = new Dictionary<object, string>
        {
            ["users/me/lists"] = TRAKT_ACCOUNT_LISTS_RESPONSE,
            ["users/me/lists/{0}"] = TRAKT_ACCOUNT_FAVORITES_RESPONSE,
            //["users/me/lists"] = TRAKT_ACCOUNT_FAVORITES_RESPONSE,
            ["sync/last_activities"] = "{}",
        };

        private Router Router { get; }

        public HttpClient()
        {
            Router = new Router(DetailsRoutes.Concat(TMDbRoutes).Concat(TraktRoutes).Select<KeyValuePair<object, string>, (Regex, string)>(kvp =>
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

                return (new Regex(new Regex("{\\d+}").Replace(url, ".*") + ".*"), kvp.Value);
            }));
        }

        new public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default) => SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
        new public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => SendAsync(request, new CancellationToken());

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //return await base.SendAsync(request, cancellationToken);

            var endpoint = request.RequestUri.ToString();
            string content;

            if (AllowLiveRequests)
            {
                if (endpoint.Contains("changes"))
                {
                    content = "{}";
                }
                else
                {
                    content = Router.Route(endpoint);
                }
            }
            else
            {
                content = "{}";
            }

            if (DetailsRoutes.Values.Contains(content))
            {
                if (BreakOnRequest)
                    ;
            }

            //throw new HttpRequestException();

            /*if (endpoint.StartsWith("3/movie") && !endpoint.Contains("account_states") && !endpoint.Contains("changes"))
            {
                //await Task.Delay(5000);
                //throw new System.Net.Http.HttpRequestException();

                Breakpoint();
                //content = HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_PARTIAL_RESPONSE;
                content = HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_RESPONSE;
                //content = null;
                //content = endpoint.Contains("reviews") ? null : content;
            }
            else if (endpoint.StartsWith("3/tv") && !endpoint.Contains("account_states") && !endpoint.Contains("changes"))
            {
                if (endpoint.Contains("episode"))
                {
                    Breakpoint();
                    content = THE_OFFICE_SEASON_3_EPISODE_20_RESPONSE;
                }
                else if (endpoint.Contains("season"))
                {
                    Breakpoint();
                    content = THE_OFFICE_SEASON_3_RESPONSE;
                }
                else
                {
                    Breakpoint();
                    content = THE_OFFICE_RESPONSE;
                }
                //content = null;
            }
            else if (endpoint.StartsWith("3/person") && !endpoint.Contains("changes"))
            {
                Breakpoint();
                content = JESSICA_CHASTAIN_RESPONSE;
                //content = STEPHEN_WOOLFENDEN_RESPONSE;
                //await Task.Delay(2000);
                //content = null;
            }
            else if (endpoint.StartsWith("3/collection"))
            {
                Breakpoint();
                content = HARRY_POTTER_COLLECTION_RESPONSE;
                //content = null;
            }
            else if (endpoint.Contains("changes"))
            {
                content = "{}";
            }
            else if (BaseAddress.ToString().Contains("themoviedb"))
            {
                if (endpoint.Contains("account"))
                {
                    if (endpoint.Contains("movie"))
                    {
                        content = TMDB_ACCOUNT_FAVORITE_MOVIES_RESPONSE;
                    }
                    else if (endpoint.Contains("tv"))
                    {
                        content = TMDB_ACCOUNT_FAVORITE_TV_RESPONSE;
                    }
                    else
                    {
                        content = TMDB_ACCOUNT_LISTS_RESPONSE;
                    }
                }
                else if (endpoint.StartsWith("4/list"))
                {
                    content = TMDB_WATCHED_LIST_RESPONSE;
                }

                //content = null;
            }
            else if (BaseAddress.ToString().Contains("trakt"))
            {
                if (BaseAddress.ToString().Contains("sync"))
                {
                    if (endpoint.StartsWith("watchlist"))
                    {
                        content = TRAKT_ACCOUNT_FAVORITES_RESPONSE;
                    }
                }
                else
                {
                    if (endpoint.Contains("sync/last_activities"))
                    {
                        content = "{}";
                    }
                    else if (endpoint.StartsWith("users/me/lists"))
                    {
                        if (endpoint.Contains("favorites"))
                        {
                            content = TRAKT_ACCOUNT_FAVORITES_RESPONSE;
                        }
                        else
                        {
                            content = TRAKT_ACCOUNT_LISTS_RESPONSE;
                        }
                    }
                }

                //content = null;
            }*/

            Print.Log($"web request{(content != null ? " (mock)" : string.Empty)}: " + endpoint);
            CallHistory.Add(endpoint);

            if (SimulatedDelay > 0)
            {
                await Task.Delay(SimulatedDelay);
            }

            if (content != null)
            {
                return new HttpResponseMessage
                {
                    Content = new StringContent(content)
                };
            }
            else
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Print.Log(json);
                }

                return response;
            }
        }
    }
}
#endif