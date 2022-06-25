using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Async;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        public static bool TryParseMovie(JsonNode json, out Movie movie)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("title", out string title) && json.TryGetValue("release_date", out string releaseDate))
            {
                movie = new Movie(title, DateTime.TryParse(releaseDate, out var year) ? (int?)year.Year : null).WithID(IDKey, id);
                return true;
            }

            movie = null;
            return false;
        }

        public static bool TryParseTVShow1(JsonNode json, out TVShow show)
        {
            throw new NotImplementedException();
        }

        public static bool TryParsePerson(JsonNode json, out Person person)
        {
            if (json.TryGetValue("name", out string name))
            {
                person = new Person(name);

                if (json.TryGetValue("id", out int id))
                {
                    person.SetID(IDKey, id);
                }

                return true;
            }

            person = null;
            return false;
        }

        public static bool TryParseTVShow(JsonNode json, out TVShow show)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("name", out string name))
            {
                show = new TVShow(name)
                {
                    Description = json["overview"]?.TryGetValue<string>(),
                    Count = json["number_of_seasons"]?.TryGetValue<int>(),
                }.WithID(IDKey, id);

                if (json.TryGetValue("poster_path", out string poster_path))
                {
                    show.PosterPath = BuildImageURL(poster_path, POSTER_SIZE);
                }

                show.Items = GetJsonCollectionItems(show, id, json);

                return true;
            }

            show = null;
            return false;
        }

        public static bool TryParseTVSeason(JsonNode json, TVShow show, out TVSeason season)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("season_number", out int season_number))
            {
                season = new TVSeason(show, season_number)
                {
                    //Name = json["name"]?.TryGetValue<string>(),
                    Description = json["overview"]?.TryGetValue<string>(),
                    Count = json["episode_count"]?.TryGetValue<int>(),
                }.WithID(IDKey, id);

                if (json.TryGetValue("name", out string name))
                {
                    season.Name = name;
                }
                if (json.TryGetValue("poster_path", out string poster_path))
                {
                    season.PosterPath = BuildImageURL(poster_path, POSTER_SIZE);
                }

                season.Items = GetJsonCollectionItems(season, id, json);

                return true;
            }

            season = null;
            return false;
        }

        public static bool TryParseTVEpisode(JsonNode json, TVSeason season, out TVEpisode episode)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("episode_number", out int episode_number) && json.TryGetValue("name", out string name))
            {
                episode = new TVEpisode(season, name, episode_number).WithID(IDKey, id);
                return true;
            }

            episode = null;
            return false;
        }

        public static bool TryParseCollection(JsonNode json, out Collection collection)
        {
            if (json.TryGetValue("id", out int id))
            {
                collection = new Collection
                {
                    //Name = json["name"]?.TryGetValue<string>(),
                    Description = json["overview"]?.TryGetValue<string>(),
                }.WithID(IDKey, id);

                if (json.TryGetValue("name", out string name))
                {
                    collection.Name = name;
                }
                if (json.TryGetValue("poster_path", out string poster_path))
                {
                    collection.PosterPath = BuildImageURL(poster_path, POSTER_SIZE);
                }

                //collection.Items = GetJsonCollectionItems(collection, id, json);

                return true;
            }

            collection = null;
            return false;
        }

        private static async IAsyncEnumerable<T> Unwrap<T>(Func<Task<IEnumerable<T>>> items)
        {
            foreach (var item in await items())
            {
                yield return item;
            }
        }

        private static async Task<IEnumerable<T>> GetItemsAsync<T>(TMDbRequest request, IJsonParser<IEnumerable<T>> parser, JsonNode json = null)
        {
            if (json != null && parser.TryGetValue(json, out var items))
            {
                return items;
            }
            else if (request != null)
            {
                var response = await WebClient.TryGetAsync(request.GetURL());

                if (response?.IsSuccessStatusCode == true)
                {
                    json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    return await GetItemsAsync(null, parser, json);
                }
            }

            return System.Linq.Enumerable.Empty<T>();
        }

        public IAsyncEnumerable<Item> GetJsonCollectionItems(Item item, int id) => GetJsonCollectionItems(item, id, null);
        private static IAsyncEnumerable<Item> GetJsonCollectionItems(Item item, int id, JsonNode json)
        {
            if (item is TVShow show)
            {
                var request = string.Format(API.TV.GET_DETAILS.GetURL(), id);
                return GetJsonCollectionItems(request, "seasons", (JsonNode node, out TVSeason temp) => TryParseTVSeason(json, show, out temp), json);

                //return new Items<Item>(GetCollectionItems(async () => (TvShow)await ConstructTVShowInfoRequest(show => show)(new object[] { id }), show => show.Seasons, season => GetItem(season, show), GetCacheKey<Models.TVShow>(id), season => GetCacheKey<Models.TVSeason>(season.Id)));
            }
            else if (item is TVSeason season)
            {
                var request = string.Format(API.TV_SEASONS.GET_DETAILS.GetURL(), id, season.SeasonNumber);
                return GetJsonCollectionItems(request, "episodes", (JsonNode node, out TVEpisode temp) => TryParseTVEpisode(json, season, out temp), json);
            }
            else if (item is Collection collection)
            {
                var request = string.Format(API.COLLECTIONS.GET_DETAILS.GetURL(), id);
                return GetJsonCollectionItems<Movie>(request, "parts", TryParseMovie, json);

                /*return new Items<Models.Item>(GetCollectionItems(() => Client.GetCollectionAsync(id), collection1 =>
                {
                    collection.PosterPath = BuildImageURL(collection1.PosterPath, POSTER_SIZE);
                    return collection1.Parts;
                }, GetItem, GetCacheKey<Models.Collection>(id), GetCacheKey));*/
            }
            else
            {
                return Empty<Item>();
            }
        }

        private static async IAsyncEnumerable<T> GetJsonCollectionItems<T>(TMDbRequest request, string collectionProperty, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse, JsonNode json) where T : Item
        {
            //return Unwrap(() => GetItemsAsync(request, parser, json));

            var parseItems = new JsonPropertyParser<IEnumerable<JsonNode>>(collectionProperty);
            var jsonParser = new JsonNodeParser<T>(parse);
            IEnumerable<T> items;

            if (json != null && parseItems.TryGetValue(json, out var cached))
            {
                items = ParseCollection(cached, jsonParser);
            }
            else
            {
                items = await Request(request, parseItems, jsonParser);
            }

            foreach (var temp in items)
            {
                yield return temp;
            }
        }

        public static bool TryParse(JsonNode json, out Item item)
        {
            item = null;
            var type = json["media_type"];

            if (type?.TryGetValue<string>() == "movie")
            {
                if (TryParseMovie(json, out var movie))
                {
                    item = movie;
                }
            }
            else if (type?.TryGetValue<string>() == "tv")
            {
                if (TryParseTVShow1(json, out var show))
                {
                    item = show;
                }
            }

            return item != null;
        }

        public static string ParseMovieCertification(JsonNode json)
        {
            if (json is JsonArray regionalReleases)
            {
                foreach (var regionalRelease in regionalReleases)
                {
                    if (regionalRelease["iso_3166_1"].TryGetValue<string>() == ISO_3166_1 && regionalRelease["release_dates"] is JsonArray releases)
                    {
                        foreach (var release in releases)
                        {
                            if (release.TryGetValue("type", out int type) && type == 3 && release.TryGetValue("certification", out string certification))
                            {
                                return certification;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static string ParseTVCertification(JsonNode json)
        {
            if (json is JsonArray releases)
            {
                foreach (var release in releases)
                {
                    if (release["iso_3166_1"].TryGetValue<string>() == ISO_3166_1 && release.TryGetValue("rating", out string rating))
                    {
                        return rating;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<JsonNode> ForRegion(JsonArray array, string iso_3661_1 = null, string iso_639_1 = null)
        {
            foreach (var item in array)
            {
                if ((string.IsNullOrEmpty(iso_3661_1) || item["iso_3166_1"].TryGetValue<string>() == iso_3661_1 && (string.IsNullOrEmpty(iso_639_1) || item["iso_639_1"].TryGetValue<string>() == iso_639_1)))
                {
                    yield return item;
                }
            }
        }

        public static string ParseTrailerPath(JsonNode json)
        {
            if (json is JsonArray results)
            {
                var videos = ForRegion(results, ISO_3166_1, ISO_639_1)
                    .Concat(ForRegion(results, "US", "en"));

                var video = videos.FirstOrDefault();

                foreach (var temp in videos)
                {
                    if (temp.TryGetValue("official", out bool official) && official)
                    {
                        video = temp;
                        break;
                    }
                }

                if (video != null && video.TryGetValue("site", out string site) && video.TryGetValue("key", out string key))
                {
                    return BuildVideoURL(site, key);
                }
            }

            return null;
        }

        public static Rating ParseRating(JsonNode json) => new Rating
        {
            Company = TMDb,
            Score = json["vote_average"]?.TryGetValue<double>(),
            TotalVotes = json["vote_count"]?.TryGetValue<double>() ?? 0,
        };

        public static Review ParseReview(JsonNode json) => new Review
        {
            Author = json["author"]?.TryGetValue<string>(),
            Content = json["content"]?.TryGetValue<string>(),
        };

        public class RatingParser : IJsonParser<Rating>
        {
            public static TMDB TMDb { get; set; }
            public PagedTMDbRequest ReviewsEndpoint { get; set; }

            public bool TryGetValue(JsonNode json, out Rating value)
            {
                value = ParseRating(json);

                if (json.TryGetValue("id", out int id))
                {
                    var appended = API.MOVIES.GET_REVIEWS.Endpoint.Replace(API.MOVIES.GET_DETAILS.Endpoint, string.Empty).TrimStart('/');
                    if (json[appended] is JsonNode reviews)
                    {
                        
                    }

                    value.Reviews = FlattenPages<Review>(ReviewsEndpoint, (JsonNode json, out Review review) =>
                    {
                        review = ParseReview(json);
                        return true;
                    }, id.ToString());
                }

                return true;
            }
        }

        public static Credit ParseCredit(JsonNode json) => new Credit
        {
            Person = TryParsePerson(json, out var person) ? person : null,
            Role = json["character"]?.TryGetValue<string>() ?? json["job"]?.TryGetValue<string>(),
            Department = json["department"]?.TryGetValue<string>(),
        };

        public static IEnumerable<Credit> ParseTVEpisodeCast(JsonNode json)
        {
            var result = System.Linq.Enumerable.Empty<Credit>();

            if (json["cast"] is JsonNode cast && CREDITS_PARSER.TryGetValue(cast, out var cast1))
            {
                result = result.Concat(cast1);
            }
            if (json["guest_stars"] is JsonNode guest && CREDITS_PARSER.TryGetValue(guest, out var guest1))
            {
                result = result.Concat(guest1);
            }

            return result;
        }

        public static Company ParseCompany(JsonNode json) => new Company
        {
            Name = json["name"]?.TryGetValue<string>(),
            LogoPath = json["logo_path"].TryGetValue<string>()
        };

        private static readonly Dictionary<string, MonetizationType> MonetizationTypeMap = new Dictionary<string, MonetizationType>
        {
            ["flatrate"] = MonetizationType.Subscription,
            ["rent"] = MonetizationType.Rent,
            ["buy"] = MonetizationType.Buy,
            ["free"] = MonetizationType.Free,
            ["ads"] = MonetizationType.Ads,
        };

        public static IEnumerable<WatchProvider> ParseWatchProviders(JsonNode json)
        {
            if (json is JsonObject array && array[ISO_3166_1] is JsonObject providers)
            {
                foreach (var monetizationGroup in providers)
                {
                    if (MonetizationTypeMap.TryGetValue(monetizationGroup.Key, out var type))
                    {
                        foreach (var provider in monetizationGroup.Value.AsArray())
                        {
                            yield return new WatchProvider
                            {
                                Company = new Company
                                {
                                    Name = provider["provider_name"]?.TryGetValue<string>(),
                                    LogoPath = provider["logo_path"]?.TryGetValue<string>(),
                                },
                                Type = type
                            };
                        }
                    }
                }
            }
        }

        public static IAsyncEnumerable<Item> ParseMovieRecommended(JsonNode json) => ParseRecommended<Movie>(json, API.MOVIES.GET_RECOMMENDATIONS, TryParseMovie);
        public static async IAsyncEnumerable<T> ParseRecommended<T>(JsonNode json, PagedTMDbRequest request, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse)
        {
            foreach (var recommended in ParseCollection(json, PageParser, new JsonNodeParser<T>(parse)))
            {
                yield return recommended;
            }

            await foreach (var item in Request(request, Helpers.LazyRange(2), parse))
            {
                yield return item;
            }
        }
    }
}