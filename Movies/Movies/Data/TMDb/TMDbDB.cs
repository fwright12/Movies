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
                SortBy = POPULARITY;
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
                    return Discover(predicate, types);
                }
                else
                {
                    return Empty<Item>();
                }
            }

            private async Task<Collection> CollectionWithOverview(Collection collection)
            {
                if (collection.TryGetID(ID, out var id) && await GetItem(ItemType.Collection, id) is Collection details)
                {
                    collection.Description = details.Description;
                    collection.Count = details.Count;
                }

                return collection;
            }

            private async IAsyncEnumerable<Collection> SearchCollections(string queryParameter)
            {
                await foreach (var json in FlattenPages<JsonNode>(API.SEARCH.SEARCH_COLLECTIONS, null, queryParameter))
                {
                    if (json.TryGetValue("id", out int id) && await GetItem(ItemType.Collection, id) is Collection collection)
                    {
                        yield return collection;
                    }
                }
            }

            public IAsyncEnumerable<Item> Search(string query, List<Type> types, BooleanExpression filters)
            {
                var queryParameter = $"query={query}";

                if (types.Count > 0)
                {
                    //if (itemType.Type.HasFlag(ItemType.Collection)) results = CollectionWithOverview(FlattenPages(page => Client.SearchCollectionAsync(e.Query, page), GetCacheKey)).Select(value => GetItem(value.Item1, value.Item2, value.Item3));
                    if (types.Contains(typeof(Collection)))
                    {
                        return SearchCollections(queryParameter);
                        //return FlattenPages<Collection>(API.SEARCH.SEARCH_COLLECTIONS, TryParseCollection, queryParameter).SelectAsync(CollectionWithOverview);
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

                return FlattenPages<Item>(API.SEARCH.MULTI_SEARCH, TryParse, queryParameter).WhereAsync(item => types.IndexOf(item.GetType()) >= types.Count - 1);
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

            public IAsyncEnumerable<Item> Discover(FilterPredicate filter, List<Type> types, bool sortAscending = false)
            {
                //var filterType = types.Count > 0;
                //var types = (filterType ? itemType.Value : ItemType.Movie | ItemType.TVShow | ItemType.Person).ToString();

                var sources = new List<IAsyncEnumerable<Item>>();
                var sortParameter = SortOptions.TryGetValue(SortBy, out var sort) ? $"sort_by={sort}.{(sortAscending ? "asc" : "desc")}" : null;

                bool Contains(Type type) => types.IndexOf(type) >= types.Count - 1;

                if (Contains(typeof(Movie)) && MOVIE_PROPERTIES.HasProperty(SortBy))
                {
                    var parameters = GetDiscoverParameters(filter, DiscoverMovieParameters)?.Prepend(sortParameter).ToArray();

                    if (parameters != null)
                    {
                        sources.Add(FlattenPages<Movie>(API.DISCOVER.MOVIE_DISCOVER, TryParseMovie, parameters));
                    }
                }
                if (Contains(typeof(TVShow)) && TVSHOW_PROPERTIES.HasProperty(SortBy))
                {
                    var parameters = GetDiscoverParameters(filter, DiscoverTVParameters)?.Prepend(sortParameter).ToArray();

                    if (parameters != null)
                    {
                        sources.Add(FlattenPages<TVShow>(API.DISCOVER.TV_DISCOVER, TryParseTVShow, parameters));
                    }
                }
                if (filter == FilterPredicate.TAUTOLOGY && Contains(typeof(Person)) && PERSON_PROPERTIES.HasProperty(SortBy))
                {
                    var pages = sortAscending ? Helpers.LazyRange(-1, -1) : Helpers.LazyRange(1, 1);
                    sources.Add(Request<Person>(API.PEOPLE.GET_POPULAR, pages, TryParsePerson));
                }

                return sources.Count == 1 ? sources[0] : Merge(sources, SortBy, sortAscending ? -1 : 1);
            }

            private List<string> GetDiscoverParameters(FilterPredicate predicate, IDictionary<Property, Dictionary<Operators, Parameter>> endpoints)
            {
                var parameters = new List<string>();
                var expression = predicate as BooleanExpression ?? new BooleanExpression { Predicates = { predicate } };
                var filters = expression.Predicates;

                var values = new Dictionary<Property, Dictionary<Operators, List<object>>>();

                for (int i = 0; i < filters.Count; i++)
                {
                    var filter = filters[i];

                    if (filter is OperatorPredicate op && op.LHS is Property property)
                    {
                        if (endpoints == DiscoverMovieParameters)
                        {
                            if (property == TVShow.GENRES)
                            {
                                property = Movie.GENRES;
                            }
                            else if (property == TVShow.WATCH_PROVIDERS)
                            {
                                property = Movie.WATCH_PROVIDERS;
                            }
                        }
                        else if (endpoints == DiscoverTVParameters)
                        {
                            if (property == Movie.GENRES)
                            {
                                property = TVShow.GENRES;
                            }
                            else if (property == Movie.WATCH_PROVIDERS)
                            {
                                property = TVShow.WATCH_PROVIDERS;
                            }
                        }

                        var value = op.RHS;

                        if (property != op.LHS)
                        {
                            var other = property.Values.OfType<object>().FirstOrDefault(value => value.ToString() == op.RHS.ToString());

                            if (other != null)
                            {
                                value = other;
                            }
                            else if (filters.OfType<OperatorPredicate>().Any(filter => filter.LHS == property))
                            {
                                continue;
                            }
                            else
                            {
                                property = (Property)op.LHS;
                            }
                        }

                        if (!values.TryGetValue(property, out var dict))
                        {
                            values[property] = dict = new Dictionary<Operators, List<object>>();
                        }
                        if (!dict.TryGetValue(op.Operator, out var list))
                        {
                            dict[op.Operator] = list = new List<object>();
                        }

                        list.Add(value);
                    }
                }

                foreach (var value in values)
                {
                    var property = value.Key;

                    if (!endpoints.TryGetValue(property, out var options))
                    {
                        return null;
                    }
                    else
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
                    if (value is Genre genre)
                    {
                        return genre.Id.ToString();
                    }
                }
                else if (property == Media.KEYWORDS)
                {
                    if (value is Keyword keyword)
                    {
                        return keyword.Id.ToString();
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
                    return provider.Id.ToString();
                }

                return value.ToString();
            }
        }
    }
}
