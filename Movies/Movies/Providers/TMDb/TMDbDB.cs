using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq.Async;
using System.Linq;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace Movies
{
    public partial class TMDB
    {
        public class Database : Collection, ISearchable
        {
            private TMDbClient Client;
            private HttpClient WebClient;

            private string Language;
            private string Region;
            private string Adult;

            private string BuildPagedApiCall(string endpoint, params string[] parameters) => BuildApiCall(endpoint, parameters.Prepend("?page={0}").ToArray());
            private string BuildApiCall(string endpoint, params string[] parameters)
            {
                var p = new List<string>(parameters);

                p.Add(string.Format("language={0}", Language));
                p.Add(string.Format("include_adult={0}", Adult));
                p.Add(string.Format("region={0}", Region));

                return string.Format(endpoint + "?" + string.Join('&', p));
            }

            public override async IAsyncEnumerable<Item> GetItems(List<Constraint> filters)
            {
                IAsyncEnumerable<Item> results = null;
                DataManager.SearchEventArgs e = new DataManager.SearchEventArgs();
                string query = null;
                var itemType = filters.OfType<Constraint<ItemType>>().FirstOrDefault();

                if (itemType != null)
                {
                    filters.Remove(itemType);
                }

                if (!string.IsNullOrEmpty(e.Query))
                {
                    string endpoint = "search/multi";
                    Func<IAsyncEnumerable<JsonNode>, IAsyncEnumerable<Item>> parse = items => items.TrySelect<JsonNode, Item>(TryParse);

                    if (itemType != null)
                    {
                        //if (itemType.Type.HasFlag(ItemType.Collection)) results = CollectionWithOverview(FlattenPages(page => Client.SearchCollectionAsync(e.Query, page), GetCacheKey)).Select(value => GetItem(value.Item1, value.Item2, value.Item3));
                        if (itemType.Value.HasFlag(ItemType.Company))
                        {
                            endpoint = "search/company";
                            parse = items => items.TrySelect<JsonNode, Company>(TryParseCompany);
                        }
                        else if (itemType.Value == ItemType.Movie)
                        {
                            endpoint = "search/movie";
                            parse = items => items.TrySelect<JsonNode, Movie>(TryParseMovie);
                        }
                        else if (itemType.Value == ItemType.TVShow)
                        {
                            endpoint = "search/tv";
                            parse = items => items.TrySelect<JsonNode, TVShow>(TryParseTVShow1);
                        }
                        else if (itemType.Value == ItemType.Person)
                        {
                            endpoint = "search/person";
                            parse = items => items.TrySelect<JsonNode, Models.Person>(TryParsePerson);
                        }
                    }

                    results = parse(FlattenPages(WebClient, BuildPagedApiCall(endpoint)));
                }
                else
                {
                    DiscoverMovieSortBy movieSort = DiscoverMovieSortBy.Undefined;
                    DiscoverTvShowSortBy tvSort = DiscoverTvShowSortBy.Undefined;
                    bool personSort = false;
                    Func<object, IComparable> compare = null;

                    if (e.SortBy == "Popularity" || e.SortBy == null)
                    {
                        movieSort = e.SortAscending ? DiscoverMovieSortBy.Popularity : DiscoverMovieSortBy.PopularityDesc;
                        tvSort = e.SortAscending ? DiscoverTvShowSortBy.Popularity : DiscoverTvShowSortBy.PopularityDesc;
                        personSort = true;
                        compare = item => (item as SearchMovieTvBase)?.Popularity ?? (item as PersonResult)?.Popularity;
                    }
                    else if (e.SortBy == "Release Date")
                    {
                        movieSort = e.SortAscending ? DiscoverMovieSortBy.ReleaseDate : DiscoverMovieSortBy.ReleaseDateDesc;
                        tvSort = e.SortAscending ? DiscoverTvShowSortBy.FirstAirDate : DiscoverTvShowSortBy.FirstAirDateDesc;
                        compare = item => item is SearchMovie movie ? movie.ReleaseDate : ((SearchTv)item).FirstAirDate;
                    }
                    else if (e.SortBy == "Revenue")
                    {
                        movieSort = e.SortAscending ? DiscoverMovieSortBy.Revenue : DiscoverMovieSortBy.RevenueDesc;
                    }
                    else if (e.SortBy == "Original Title")
                    {
                        movieSort = e.SortAscending ? DiscoverMovieSortBy.OriginalTitle : DiscoverMovieSortBy.OriginalTitleDesc;
                    }
                    else if (e.SortBy == "Vote Average")
                    {
                        movieSort = e.SortAscending ? DiscoverMovieSortBy.VoteAverage : DiscoverMovieSortBy.VoteAverageDesc;
                        tvSort = e.SortAscending ? DiscoverTvShowSortBy.VoteAverage : DiscoverTvShowSortBy.VoteAverageDesc;
                        compare = item => ((SearchMovieTvBase)item).VoteAverage;
                    }
                    else if (e.SortBy == "Vote Count")
                    {
                        movieSort = e.SortAscending ? DiscoverMovieSortBy.VoteCount : DiscoverMovieSortBy.VoteCountDesc;
                        compare = item => ((SearchMovieTvBase)item).VoteCount;
                    }

                    var filterType = itemType != null;
                    var types = (filterType ? itemType.Value : ItemType.Movie | ItemType.TVShow | ItemType.Person).ToString();

                    var sources = new List<IAsyncEnumerable<object>>();

                    if (!filterType || itemType.Value.HasFlag(ItemType.Movie))
                    {
                        sources.Add(FlattenPages(WebClient, BuildPagedApiCall("discover/movie", GetDiscoverMovieParameters(filters))).TrySelect<JsonNode, Movie>(TryParseMovie));
                    }
                    if (!filterType || itemType.Value.HasFlag(ItemType.TVShow))
                    {
                        sources.Add(FlattenPages(WebClient, BuildPagedApiCall("discover/tv", DiscoverTVParameters(filters))).TrySelect<JsonNode, TVShow>(TryParseTVShow1));
                    }
                    if (!filterType || itemType.Value.HasFlag(ItemType.Movie))
                    {
                        sources.Add(FlattenPages(WebClient, BuildPagedApiCall("person/popular")).TrySelect<JsonNode, Models.Person>(TryParsePerson));
                    }

                    foreach (var type1 in types.Split(","))
                    {
                        var type = type1.Trim();

                        if (type == ItemType.Movie.ToString() && movieSort != DiscoverMovieSortBy.Undefined)
                        {
                            var search = Client.DiscoverMoviesAsync().OrderBy(movieSort);
                            //sources.Add(FlattenPages(page => search.Query(page), GetCacheKey));
                        }
                        else if (type == ItemType.TVShow.ToString() && tvSort != DiscoverTvShowSortBy.Undefined)
                        {
                            var search = Client.DiscoverTvShowsAsync().OrderBy(tvSort);
                            //sources.Add(FlattenPages(page => search.Query(page), GetCacheKey));
                        }
                        else if (type == ItemType.Person.ToString() && personSort)
                        {
                            //sources.Add(FlattenPages(page => Client.GetPersonListAsync(PersonListType.Popular, page), GetCacheKey, e.SortAscending));
                        }
                        else if (filterType)
                        {
                            sources.Clear();
                            break;
                        }
                    }

                    var sorted = sources.Count == 1 ? sources[0] : Merge(sources, compare, e.SortAscending ? -1 : 1);
                    results = sorted.TrySelect<object, Item>((object item, out Item result) =>
                    {
                        if (item is SearchMovie movie) result = GetItem(movie);
                        //else if (item is SearchTv show) result = GetItem(show);
                        else if (item is PersonResult person) result = GetItem(person);
                        else
                        {
                            result = null;
                            return false;
                        }

                        return true;
                    });
                }

                await foreach (var item in results)
                {
                    yield return item;
                }
            }

            private static readonly Dictionary<string, Dictionary<int, string>> DiscoverMovieParameters = new Dictionary<string, Dictionary<int, string>>
            {
                ["Certification Country"] = new Dictionary<int, string>
                {
                    [0] = "certification_country",
                },
                [MovieService.CONTENT_RATING] = new Dictionary<int, string>
                {
                    [-1] = "certification.lte",
                    [0] = "certification",
                    [1] = "certification.gte"
                },
                ["Primary " + MovieService.RELEASE_DATE] = new Dictionary<int, string>
                {
                    [-1] = "primary_release_date.lte",
                    [1] = "primary_release_date.gte"
                },
                [MovieService.RELEASE_DATE] = new Dictionary<int, string>
                {
                    [-1] = "release_date.lte",
                    [1] = "release_date.gte"
                },
                ["Release Type"] = new Dictionary<int, string>
                {
                    [0] = "with_release_type"
                },
                ["Vote Count"] = new Dictionary<int, string>
                {
                    [-1] = "vote_count.lte",
                    [1] = "vote_count.gte"
                },
                ["Vote Average"] = new Dictionary<int, string>
                {
                    [-1] = "vote_average.lte",
                    [1] = "vote_average.gte"
                },
                [MovieService.CAST] = new Dictionary<int, string>
                {
                    [0] = "with_cast",
                },
                [MovieService.CREW] = new Dictionary<int, string>
                {
                    [0] = "with_crew",
                },
                [MovieService.CAST + " + " + MovieService.CREW] = new Dictionary<int, string>
                {
                    [0] = "with_people",
                },
                [MovieService.PRODUCTION_COMPANIES] = new Dictionary<int, string>
                {
                    [-1] = "without_companies",
                    [0] = "with_companies",
                    [1] = "without_companies",
                },
                [MovieService.GENRES] = new Dictionary<int, string>
                {
                    [-1] = "without_genres",
                    [0] = "with_genres",
                    [1] = "without_genres",
                },
                [MovieService.KEYWORDS] = new Dictionary<int, string>
                {
                    [-1] = "without_keywords",
                    [0] = "with_keywords",
                    [1] = "without_keywords",
                },
                [MovieService.RUNTIME] = new Dictionary<int, string>
                {
                    [-1] = "with_runtime.lte",
                    [1] = "with_runtime.gte"
                },
                [MovieService.OriginalLanguage] = new Dictionary<int, string>
                {
                    [0] = "with_original_language",
                },
                [MovieService.WATCH_PROVIDERS] = new Dictionary<int, string>
                {
                    [0] = "with_watch_providers",
                },
                ["Watch Region"] = new Dictionary<int, string>
                {
                    [0] = "watch_region",
                },
                [nameof(MonetizationType)] = new Dictionary<int, string>
                {
                    [0] = "with_watch_monetization_types",
                },
            };

            private string GetDiscoverMovieParameters(List<Constraint> filters)
            {
                var parameters = new List<string>();

                for (int i = 0; i < filters.Count; i++)
                {
                    var filter = filters[i];

                    if (DiscoverMovieParameters.TryGetValue(filter.Name, out var options) && options.TryGetValue((int)filter.Comparison, out var parameter))
                    {
                        parameters.Add(string.Format("{0}={1}", parameter, filter.Value));
                        filters.RemoveAt(i--);
                    }
                }

                return string.Join('&', parameters);
            }

            private string DiscoverTVParameters(List<Constraint> filters)
            {
                return string.Empty;
            }

            public IAsyncEnumerable<Item> Search(string query)
            {
                throw new NotImplementedException();
            }
        }
    }
}
