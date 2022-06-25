using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq.Async;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

namespace Movies
{
    public partial class TMDB
    {
        private static async IAsyncEnumerable<Item> Merge(IEnumerable<IAsyncEnumerable<Item>> sources, Property property, int order = -1)
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

                    IComparable first;
                    IComparable second;

                    if (!Data.GetDetails(itrs[index].Current).TryGetValue(property, out var a) || (first = await a as IComparable) == null)
                    {
                        itrs.RemoveAt(index);
                    }
                    else if (!Data.GetDetails(itrs[i].Current).TryGetValue(property, out var b) || (second = await b as IComparable) == null)
                    {
                        itrs.RemoveAt(i--);
                    }
                    else if (second.CompareTo(first) == order)
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

        public class Database : Collection, IAsyncFilterable<Item>
        {
            public Database()
            {
                SortBy = Movie.REVENUE;
            }

            public IAsyncEnumerable<Item> GetItems(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                var expression = predicate as BooleanExpression ?? new BooleanExpression { Predicates = { predicate } };
                var types = expression.Predicates
                    .OfType<OperatorPredicate>()
                    .Select(filter => filter.RHS as Type)
                    .Where(type => type != null)
                    .ToList();

                if (expression.Predicates.OfType<SearchPredicate>().FirstOrDefault() is SearchPredicate search && !string.IsNullOrEmpty(search.Query))
                {
                    return Search(search.Query, types, expression);
                }
                else if (typeof(IComparable).IsAssignableFrom(SortBy.Type))
                {
                    return Discover(expression, types);
                }
                else
                {
                    return Empty<Item>();
                }
            }

            private async Task<Collection> CollectionWithOverview(Collection collection)
            {
                if (collection.TryGetID(ID, out var id) && await GetItemJson(ItemType.Collection, id) is Collection details)
                {
                    collection.Description = details.Description;
                    collection.Count = details.Count;
                }

                return collection;
            }

            private IAsyncEnumerable<Item> Search(string query, List<Type> types, BooleanExpression filters)
            {
                var queryParameter = $"query={query}";

                if (types.Count > 0)
                {
                    //if (itemType.Type.HasFlag(ItemType.Collection)) results = CollectionWithOverview(FlattenPages(page => Client.SearchCollectionAsync(e.Query, page), GetCacheKey)).Select(value => GetItem(value.Item1, value.Item2, value.Item3));
                    if (types.Contains(typeof(Collection)))
                    {
                        return FlattenPages<Collection>(API.SEARCH.SEARCH_COLLECTIONS, TryParseCollection, queryParameter).Select(CollectionWithOverview);
                    }
                    else if (types.Contains(typeof(Company)))
                    {
                        //endpoint = "search/company";
                        //parse = items => items.Select(TryParseCompany);
                    }
                    else if (types.Count == 1)
                    {
                        if (types[0] == typeof(Movie))
                        {
                            return FlattenPages<Movie>(API.SEARCH.SEARCH_MOVIES, TryParseMovie, queryParameter);
                        }
                        else if (types[0] == typeof(TVShow))
                        {
                            return FlattenPages<TVShow>(API.SEARCH.SEARCH_TV_SHOWS, TryParseTVShow, queryParameter);
                        }
                        else if (types[0] == typeof(Person))
                        {
                            return FlattenPages<Person>(API.SEARCH.SEARCH_PEOPLE, TryParsePerson, queryParameter);
                        }
                    }
                }

                return FlattenPages<Item>(API.SEARCH.MULTI_SEARCH, TryParse, queryParameter).Where(item => types.IndexOf(item.GetType()) >= types.Count - 1);
            }

            private static readonly Property SCORE = new ReflectedProperty("Score", typeof(Rating).GetProperty(nameof(Rating.Score)))
            {
                Parent = Media.RATING
            };
            private static readonly Property VOTE_COUNT = new ReflectedProperty("Vote Count", typeof(Rating).GetProperty(nameof(Rating.TotalVotes)))
            {
                Parent = Media.RATING
            };

            private static readonly Dictionary<Property, string> SortOptions = new Dictionary<Property, string>
            {
                [POPULARITY] = "popularity",
                [Movie.RELEASE_DATE] = "release_date",
                [Movie.REVENUE] = "revenue",
                [Media.ORIGINAL_TITLE] = "original_title",
                [SCORE] = "vote_average",
                [VOTE_COUNT] = "vote_count",
            };

            private IAsyncEnumerable<Item> Discover(BooleanExpression filter, List<Type> types, bool sortAscending = false)
            {
                TMDbRequest personEndpoint = null;
                //var filterType = types.Count > 0;
                //var types = (filterType ? itemType.Value : ItemType.Movie | ItemType.TVShow | ItemType.Person).ToString();

                var sources = new List<IAsyncEnumerable<Item>>();
                var sortParameter = SortOptions.TryGetValue(SortBy, out var sort) ? $"sort_by={sort}.{(sortAscending ? "asc" : "desc")}" : null;

                if (types.IndexOf(typeof(Movie)) >= types.Count - 1)
                {
                    var parameters = GetDiscoverParameters(filter, DiscoverMovieParameters).Prepend(sortParameter).ToArray();
                    sources.Add(FlattenPages<Movie>(API.DISCOVER.MOVIE_DISCOVER, TryParseMovie, parameters));
                }
                if (types.IndexOf(typeof(TVShow)) >= types.Count - 1)
                {
                    var parameters = GetDiscoverParameters(filter, DiscoverTVParameters).Prepend(sortParameter).ToArray();
                    sources.Add(FlattenPages<TVShow>(API.DISCOVER.TV_DISCOVER, TryParseTVShow, parameters));
                }
                if (personEndpoint != null && types.IndexOf(typeof(Person)) >= types.Count - 1)
                {
                    var pages = sortAscending ? Helpers.LazyRange(-1, -1) : Helpers.LazyRange(1, 1);
                    sources.Add(FlattenPages<Person>(personEndpoint, pages, TryParsePerson));
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

                return sources.Count == 1 ? sources[0] : Merge(sources, SortBy, sortAscending ? -1 : 1);
            }

            private List<string> GetDiscoverParameters(BooleanExpression expression, IDictionary<Property, Dictionary<Operators, Parameter>> endpoints)
            {
                var parameters = new List<string>();
                var filters = expression.Predicates;

                var values = new Dictionary<Property, Dictionary<Operators, List<object>>>();

                for (int i = 0; i < filters.Count; i++)
                {
                    var filter = filters[i];

                    if (filter is OperatorPredicate op && op.LHS is Property property)
                    {
                        if (!values.TryGetValue(property, out var dict))
                        {
                            values[property] = dict = new Dictionary<Operators, List<object>>();
                        }
                        if (!dict.TryGetValue(op.Operator, out var list))
                        {
                            dict[op.Operator] = list = new List<object>();
                        }

                        list.Add(op.RHS);
                    }
                }

                foreach (var value in values)
                {
                    var property = value.Key;

                    if (endpoints.TryGetValue(property, out var options))
                    {
                        foreach (var kvp in value.Value)
                        {
                            var op = kvp.Key;
                            var rhs = kvp.Value.Select(value => Serialize(property, value));

                            if (options.TryGetValue(op, out var parameter))
                            {
                                parameters.AddRange(GetParameter(parameter, rhs));
                            }
                            else if (op == Operators.Equal && options.TryGetValue(Operators.LessThan, out var lte) && options.TryGetValue(Operators.GreaterThan, out var gte))
                            {
                                parameters.AddRange(GetParameter(lte, rhs));
                                parameters.AddRange(GetParameter(gte, rhs));
                            }
                            else
                            {
                                continue;
                            }

                            if (property == Movie.CONTENT_RATING)
                            {
                                var certification_country = $"certification_country={TMDbRequest.DEFAULT_REGION}";

                                if (!parameters.Contains(certification_country))
                                {
                                    parameters.Add(certification_country);
                                }
                            }
                            else if (property == Movie.WATCH_PROVIDERS || property == TVShow.WATCH_PROVIDERS || property == ViewModels.CollectionViewModel.MonetizationType)
                            {
                                var watch_region = $"watch_region={TMDbRequest.DEFAULT_ISO_3166_1}";

                                if (!parameters.Contains(watch_region))
                                {
                                    parameters.Add(watch_region);
                                }
                            }

                            //filters.RemoveAt(i--);
                        }
                    }
                }

                return parameters;
            }

            private IEnumerable<string> GetParameter(Parameter parameter, IEnumerable<object> values)
            {
                if (parameter.AllowsMultiple)
                {
                    yield return $"{parameter.Name}={string.Join('|', values)}";
                }
                else
                {
                    foreach (var value in values)
                    {
                        yield return $"{parameter.Name}={value}";
                    }
                }
            }

            private static string Serialize(Property property, object value)
            {
                if (property == Media.RUNTIME)
                {
                    if (value is TimeSpan runtime)
                    {
                        return runtime.Minutes.ToString();
                    }
                }
                else if (property == Movie.GENRES || property == TVShow.GENRES)
                {
                    if (value is string genre)
                    {
                        return genre;
                    }
                }
                else if (property == Media.KEYWORDS)
                {
                    if (value is string keyword)
                    {
                        return keyword;
                    }
                }
                else if (property == ViewModels.CollectionViewModel.MonetizationType)
                {
                    if (value is MonetizationType type && MonetizationTypeMap.FirstOrDefault(kvp => kvp.Value == type) is KeyValuePair<string, MonetizationType> pair)
                    {
                        return pair.Key;
                    }
                }
                else if (value is ViewModels.PersonViewModel pvm && pvm.Item is Person person)
                {
                    if (person.TryGetID(ID, out var id))
                    {
                        return id.ToString();
                    }
                }
                else if (value is WatchProvider provider)
                {
                    return provider.ID;
                }

                return System.Text.Json.JsonSerializer.Serialize(value, value.GetType());
            }
        }
    }
}
