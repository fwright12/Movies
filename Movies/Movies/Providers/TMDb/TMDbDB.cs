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
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        private static async IAsyncEnumerable<JsonNode> Merge(IEnumerable<IAsyncEnumerable<JsonNode>> sources, string property, Func<JsonNode, IComparable> parse, int order = -1)
        {
            var itrs = sources.Select(source => source.GetAsyncEnumerator()).ToList();
            await Task.WhenAll(itrs.Select(itr => itr.MoveNextAsync().AsTask()));
            //IComparable[] values = await Task.WhenAll(itrs.Select(itr => byValue(itr.Current)));

            while (itrs.Count > 0)
            {
                int index = 0;
                for (int i = 1; i < itrs.Count; i++)
                {
                    //var method = typeof(JsonNode).GetMethod(nameof(JsonNode.GetValue)).MakeGenericMethod(typeof(int));

                    var first = itrs[i].Current[property];
                    var second = itrs[index].Current[property];
                    if (first != null && second != null && parse(first).CompareTo(parse(second)) == order)
                    {
                        index = i;
                    }
                }

                yield return itrs[index].Current;

                if (!await itrs[index].MoveNextAsync())
                {
                    itrs.RemoveAt(index);
                }
            }

        }
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
                ItemType? itemType = null;
                //var itemType = filters.Where(constraint => false).FirstOrDefault();

                if (itemType != null)
                {
                    //filters.Remove(itemType);
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
                            //parse = items => items.Select(TryParseCompany);
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
                    Func<JsonNode, IComparable> parse = null;

                    if (e.SortBy == "Popularity" || e.SortBy == null)
                    {
                        parse = node => node.GetValue<int>();
                    }
                    else if (e.SortBy == "Release Date")
                    {
                        parse = node => node.GetValue<DateTime>();
                    }
                    else if (e.SortBy == "Revenue")
                    {
                        parse = node => node.GetValue<long>();
                    }
                    else if (e.SortBy == "Original Title")
                    {
                        parse = node => node.GetValue<string>();
                    }
                    else if (e.SortBy == "Vote Average")
                    {
                        parse = node => node.GetValue<double>();
                    }
                    else if (e.SortBy == "Vote Count")
                    {
                        parse = node => node.GetValue<int>();
                    }

                    var filterType = itemType != null;
                    var types = (filterType ? itemType.Value : ItemType.Movie | ItemType.TVShow | ItemType.Person).ToString();

                    var sources = new List<IAsyncEnumerable<JsonNode>>();

                    if (GetMovieSortParameter(e.SortBy, e.SortAscending) is string sortMovie && (!filterType || itemType.Value.HasFlag(ItemType.Movie)))
                    {
                        sources.Add(FlattenPages(WebClient, BuildPagedApiCall("discover/movie", sortMovie, GetDiscoverMovieParameters(filters))));
                    }
                    if (!filterType || itemType.Value.HasFlag(ItemType.TVShow))
                    {
                        sources.Add(FlattenPages(WebClient, BuildPagedApiCall("discover/tv", DiscoverTVParameters(filters))));
                    }
                    if (!filterType || itemType.Value.HasFlag(ItemType.Movie))
                    {
                        sources.Add(FlattenPages(WebClient, BuildPagedApiCall("person/popular")));
                    }

                    /*foreach (var type1 in types.Split(","))
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
                    }*/

                    var sorted = sources.Count == 1 ? sources[0] : Merge(sources, e.SortBy, parse, e.SortAscending ? -1 : 1);
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

            private string GetMovieSortParameter(string property, bool ascending)
            {
                return string.Format("{0}={1}.{2}", "sort_by", property, ascending ? "asc" : "desc");
            }

            private static readonly Dictionary<string, Dictionary<int, string>> DiscoverMovieParameters = new Dictionary<string, Dictionary<int, string>>
            {
                ["Certification Country"] = new Dictionary<int, string>
                {
                    [0] = "certification_country",
                },
                [Media.CONTENT_RATING.Name] = new Dictionary<int, string>
                {
                    [-1] = "certification.lte",
                    [0] = "certification",
                    [1] = "certification.gte"
                },
                ["Primary " + Movie.RELEASE_DATE.Name] = new Dictionary<int, string>
                {
                    [-1] = "primary_release_date.lte",
                    [1] = "primary_release_date.gte"
                },
                [Movie.RELEASE_DATE.Name] = new Dictionary<int, string>
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
                [Media.CAST.Name] = new Dictionary<int, string>
                {
                    [0] = "with_cast",
                },
                [Media.CREW.Name] = new Dictionary<int, string>
                {
                    [0] = "with_crew",
                },
                [Media.CAST.Name + " + " + Media.CREW.Name] = new Dictionary<int, string>
                {
                    [0] = "with_people",
                },
                [Media.PRODUCTION_COMPANIES.Name] = new Dictionary<int, string>
                {
                    [-1] = "without_companies",
                    [0] = "with_companies",
                    [1] = "without_companies",
                },
                [Media.GENRES.Name] = new Dictionary<int, string>
                {
                    [-1] = "without_genres",
                    [0] = "with_genres",
                    [1] = "without_genres",
                },
                [Media.KEYWORDS.Name] = new Dictionary<int, string>
                {
                    [-1] = "without_keywords",
                    [0] = "with_keywords",
                    [1] = "without_keywords",
                },
                [Media.RUNTIME.Name] = new Dictionary<int, string>
                {
                    [-1] = "with_runtime.lte",
                    [1] = "with_runtime.gte"
                },
                [Media.ORIGINAL_LANGUAGE.Name] = new Dictionary<int, string>
                {
                    [0] = "with_original_language",
                },
                [Media.WATCH_PROVIDERS.Name] = new Dictionary<int, string>
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

                    if (DiscoverMovieParameters.TryGetValue(filter.Property.Name, out var options) && options.TryGetValue((int)filter.Comparison, out var parameter))
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
