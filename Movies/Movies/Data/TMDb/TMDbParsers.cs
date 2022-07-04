﻿using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Async;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
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
                if (TryParseTVShow(json, out var show))
                {
                    item = show;
                }
            }
            else if (type?.TryGetValue<string>() == "person")
            {
                if (TryParsePerson(json, out var person))
                {
                    item = person;
                }
            }

            return item != null;
        }

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

                collection.Items = GetJsonCollectionItems(collection, id, json);

                return true;
            }

            collection = null;
            return false;
        }

        public IAsyncEnumerable<Item> GetJsonCollectionItems(Item item, int id) => GetJsonCollectionItems(item, id, null);
        private static IAsyncEnumerable<Item> GetJsonCollectionItems(Item item, int id, JsonNode json)
        {
            if (item is TVShow show)
            {
                return GetTVItems(show, TVShow.SEASONS);
                var request = string.Format(API.TV.GET_DETAILS.GetURL(), id);
                return GetJsonCollectionItems(request, "seasons", (JsonNode node, out TVSeason temp) => TryParseTVSeason(node, show, out temp), json);

                //return new Items<Item>(GetCollectionItems(async () => (TvShow)await ConstructTVShowInfoRequest(show => show)(new object[] { id }), show => show.Seasons, season => GetItem(season, show), GetCacheKey<Models.TVShow>(id), season => GetCacheKey<Models.TVSeason>(season.Id)));
            }
            else if (item is TVSeason season)
            {
                return GetTVItems(season, TVSeason.EPISODES);
                var request = string.Format(API.TV_SEASONS.GET_DETAILS.GetURL(), id, season.SeasonNumber);
                return GetJsonCollectionItems(request, "episodes", (JsonNode node, out TVEpisode temp) => TryParseTVEpisode(node, season, out temp), json);
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

        private abstract class TVItemsParser
        {
            public Item Parent { get; set; }
        }

        private class TVItemsParser<TParent, TChild> : TVItemsParser, IJsonParser<IEnumerable<TChild>> where TParent : Item where TChild : Item
        {
            public TryParseFunc Parse { get; }
            public string Property { get; }

            public TVItemsParser(string property, TryParseFunc parse)
            {
                Property = property;
                Parse = parse;
            }

            public delegate bool TryParseFunc(JsonNode json, TParent parent, out TChild result);

            public bool TryGetValue(JsonNode json, out IEnumerable<TChild> value) => TryParseCollection(json, new JsonPropertyParser<IEnumerable<JsonNode>>(Property), out value, new JsonNodeParser<TChild>((JsonNode json, out TChild result) => Parse(json, (TParent)Parent, out result)));
        }

        private static async IAsyncEnumerable<T> GetTVItems<T>(Item tv, Property<T> property)
        {
            foreach (var item in await Data.GetDetails(tv).GetMultiple(property))
            {
                yield return item;
            }
        }

        private static async IAsyncEnumerable<T> GetJsonCollectionItems<T>(TMDbRequest request, string collectionProperty, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse, JsonNode json)
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

                    var request = new ParameterizedPagedRequest(ReviewsEndpoint, id);
                    value.Reviews = FlattenPages(request, (JsonNode json, out Review review) =>
                    {
                        review = ParseReview(json);
                        return true;
                    });
                }

                return true;
            }
        }

        private static bool TryParsePersonCredits(JsonNode json, out IEnumerable<Item> result)
        {
            var credits = new List<Item>();

            if (TryParseCollection(json, new JsonPropertyParser<IEnumerable<JsonNode>>("cast"), out var cast, new JsonNodeParser<Item>(TryParse)))
            {
                credits.AddRange(cast);
            }
            if (TryParseCollection(json, new JsonPropertyParser<IEnumerable<JsonNode>>("crew"), out var crew, new JsonNodeParser<Item>(TryParse)))
            {
                credits.AddRange(crew);
            }

            if (credits.Count > 0)
            {
                result = credits;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static bool TryParseCast(JsonNode json, out IEnumerable<Credit> credits) => TryParseCredits(json, "cast", out credits);
        public static bool TryParseCrew(JsonNode json, out IEnumerable<Credit> credits) => TryParseCredits(json, "crew", out credits);
        public static bool TryParseCredits(JsonNode json, string property, out IEnumerable<Credit> credits) => TryParseCollection(json, new JsonPropertyParser<IEnumerable<JsonNode>>(property), out credits, new JsonNodeParser<Credit>(ParseCredit));

        public static Credit ParseCredit(JsonNode json) => new Credit
        {
            Person = TryParsePerson(json, out var person) ? person : null,
            Role = json["character"]?.TryGetValue<string>() ?? json["job"]?.TryGetValue<string>(),
            Department = json["department"]?.TryGetValue<string>(),
        };

        public static bool TryParseTVEpisodeCast(JsonNode json, out IEnumerable<Credit> result)
        {
            if (TryParseCredits(json, "cast", out var cast) && TryParseCredits(json, "guest_stars", out var guest))
            //if (json["cast"] is JsonNode castNode && CREDITS_PARSER.TryGetValue(castNode, out var cast) && json["guest_stars"] is JsonNode guestNode && CREDITS_PARSER.TryGetValue(guestNode, out var guest))
            {
                result = cast.Concat(guest);
                return true;
            }

            result = null;
            return false;
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
                            if (TryParseWatchProvider(provider, out var temp))
                            {
                                temp.Type = type;
                                yield return temp;
                            }
                        }
                    }
                }
            }
        }

        public static bool TryParseWatchProvider(JsonNode json, out WatchProvider provider)
        {
            if (json.TryGetValue("provider_id", out int id))
            {
                provider = new WatchProvider
                {
                    Id = id,
                    Company = new Company
                    {
                        Name = json["provider_name"]?.TryGetValue<string>(),
                        LogoPath = json["logo_path"]?.TryGetValue<string>(),
                    },
                };
                return true;
            }

            provider = null;
            return false;
        }

        public static bool TryParseGenre(JsonNode json, out Genre genre)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("name", out string name))
            {
                genre = new Genre
                {
                    Id = id,
                    Name = name,
                };
                return true;
            }

            genre = null;
            return false;
        }

        public static bool TryParseKeyword(JsonNode json, out Keyword keyword)
        {
            if (json.TryGetValue("id", out int id) && json.TryGetValue("name", out string name))
            {
                keyword = new Keyword
                {
                    Id = id,
                    Name = name,
                };
                return true;
            }

            keyword = null;
            return false;
        }

        public static IAsyncEnumerable<T> ParseRecommended<T>(JsonNode json, System.Linq.Async.Enumerable.TryParseFunc<JsonNode, T> parse) => TryParseCollection(json, new JsonNodeParser<IEnumerable<JsonNode>>(), out var result, new JsonNodeParser<T>(parse)) ? System.Linq.Async.Enumerable.ToAsyncEnumerable(Task.FromResult(result)) : null;

        public class PagedParser<T> : IJsonParser<IAsyncEnumerable<T>>
        {
            public IPagedRequest Request { get; }
            public IJsonParser<IAsyncEnumerable<T>> ItemParser { get; }

            public PagedParser(IPagedRequest request, IJsonParser<IAsyncEnumerable<T>> itemParser)
            {
                Request = request;
                ItemParser = itemParser;
            }

            public bool TryGetValue(JsonNode json, out IAsyncEnumerable<T> value)
            {
                value = SelectMany(json);
                return true;
            }

            private async IAsyncEnumerable<T> SelectMany(JsonNode json)
            {
                var pages = WebClient.GetPagesAsync(Request, Helpers.LazyRange(2, 1)).SelectAsync(GetJson);

                await foreach (var page in Prepend(pages, json))
                {
                    if (ItemParser.TryGetValue(page, out var items))
                    {
                        await foreach (var item in items)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        private static async IAsyncEnumerable<T> Prepend<T>(IAsyncEnumerable<T> source, T item)
        {
            yield return item;

            await foreach (var more in source)
            {
                yield return more;
            }
        }
    }
}