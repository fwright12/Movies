using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        private static async IAsyncEnumerable<Item> Merge(IEnumerable<IAsyncEnumerable<Item>> sources, Property property, int order = -1)
        {
            var itrs = sources.Select(source => source.GetAsyncEnumerator()).ToList();
            var success = await Task.WhenAll(itrs.Select(itr => itr.MoveNextAsync().AsTask()));

            for (int i = success.Length - 1; i >= 0; i--)
            {
                if (!success[i])
                {
                    itrs.RemoveAt(i);
                }
            }

            async Task<object> Wrap(Task<Rating> result, bool score)
            {
                var rating = await result;

                if (score)
                {
                    return rating.Score;
                }
                else
                {
                    return rating.TotalVotes;
                }
            }

            bool TryGetValue(Item item, Property property, out Task<object> value)
            {
                var properties = Data.GetDetails(item);

                if (properties.TryGetValue(property, out value))
                {
                    return true;
                }
                else if ((property == SCORE || property == VOTE_COUNT) && properties.TryGetValue(Media.RATING, out var rating))
                {
                    value = Wrap(rating, property == SCORE);
                    return true;
                }

                value = null;
                return false;
            }

            while (itrs.Count > 0)
            {
                int index = 0;
                for (int i = 1; i < itrs.Count; i++)
                {
                    //var method = typeof(JsonNode).GetMethod(nameof(JsonNode.GetValue)).MakeGenericMethod(typeof(int));

                    IComparable first;
                    IComparable second;

                    if (!TryGetValue(itrs[index].Current, property, out var a) || (first = await a as IComparable) == null)
                    {
                        itrs.RemoveAt(index);
                    }
                    else if (!TryGetValue(itrs[i].Current, property, out var b) || (second = await b as IComparable) == null)
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
            public static readonly Database Instance = new Database();

            private Database()
            {
                SortBy = POPULARITY;
            }

            public IAsyncEnumerator<Item> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                var expression = predicate as BooleanExpression ?? new BooleanExpression { Predicates = { predicate } };
                var types = expression.Predicates
                    .OfType<OperatorPredicate>()
                    .Select(filter => filter.RHS as Type)
                    .Where(type => type != null)
                    .ToList();

                if (expression.Predicates.OfType<SearchPredicate>().FirstOrDefault() is SearchPredicate search && !string.IsNullOrEmpty(search.Query))
                {
                    return Search(search.Query, expression, types).GetAsyncEnumerator();
                }
                else if (typeof(IComparable).IsAssignableFrom(SortBy.Type))
                {
                    return Discover(predicate, types).GetAsyncEnumerator();
                }
                else
                {
                    return Empty<Item>().GetAsyncEnumerator();
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

            public static IAsyncEnumerable<Person> SearchPeople(string query, FilterPredicate predicate = null) => FlattenSearchPages<Person>(API.SEARCH.SEARCH_PEOPLE, query, TryParsePerson);
            public static IAsyncEnumerable<Keyword> SearchKeywords(string query, FilterPredicate predicate = null) => FlattenSearchPages<Keyword>(API.SEARCH.SEARCH_KEYWORDS, query, TryParseKeyword);
            public static async IAsyncEnumerable<Collection> SearchCollections(string query, FilterPredicate predicate = null)
            {
                await foreach (var json in FlattenSearchPages<JsonNode>(API.SEARCH.SEARCH_COLLECTIONS, query))
                {
                    if (json.TryGetValue("id", out int id))
                    {
                        yield return await GetCollection(id);
                    }
                }
            }

            private static IAsyncEnumerable<T> FlattenSearchPages<T>(IPagedRequest request, string query, AsyncEnumerable.TryParseFunc<JsonNode, T> parse = null, params string[] parameters) => Request(request, parse, false, parameters.Prepend($"query={query}").ToArray());// FlattenPages(request, Helpers.LazyRange(0, 1), parse, false, parameters);

            public IAsyncEnumerable<Item> Search(string query, FilterPredicate filters, params Type[] itemTypes) => Search(query, filters, (IEnumerable<Type>)itemTypes);
            public IAsyncEnumerable<Item> Search(string query, FilterPredicate filters, IEnumerable<Type> itemTypes)
            {
                var types = itemTypes.ToList();

                if (types.Count > 0)
                {
                    //if (itemType.Type.HasFlag(ItemType.Collection)) results = CollectionWithOverview(FlattenPages(page => Client.SearchCollectionAsync(e.Query, page), GetCacheKey)).Select(value => GetItem(value.Item1, value.Item2, value.Item3));
                    if (types.Contains(typeof(Collection)))
                    {
                        return SearchCollections(query);
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
                            return FlattenSearchPages<Movie>(API.SEARCH.SEARCH_MOVIES, query, TryParseMovie);
                        }
                        else if (types[0] == typeof(TVShow))
                        {
                            return FlattenSearchPages<TVShow>(API.SEARCH.SEARCH_TV_SHOWS, query, TryParseTVShow);
                        }
                        else if (types[0] == typeof(Person))
                        {
                            return SearchPeople(query, filters);
                        }
                    }
                }

                return FlattenSearchPages<Item>(API.SEARCH.MULTI_SEARCH, query, TryParse).WhereAsync(item => types.Count * types.IndexOf(item.GetType()) >= 0);
            }

            private static readonly Dictionary<Property, string> SortOptions = new Dictionary<Property, string>
            {
                [POPULARITY] = "popularity",
                [Movie.RELEASE_DATE] = "release_date",
                [Movie.REVENUE] = "revenue",
                [Media.ORIGINAL_TITLE] = "original_title",
                [SCORE] = "vote_average",
                [VOTE_COUNT] = "vote_count",
            };

            public IAsyncEnumerable<Item> Discover(FilterPredicate filter, List<Type> types)
            {
                //var filterType = types.Count > 0;
                //var types = (filterType ? itemType.Value : ItemType.Movie | ItemType.TVShow | ItemType.Person).ToString();

                var sources = new List<IAsyncEnumerable<Item>>();
                var sortParameter = SortOptions.TryGetValue(SortBy, out var sort) ? $"sort_by={sort}.{(SortAscending ? "asc" : "desc")}" : null;

                bool Contains(Type type) => types.Count * types.IndexOf(type) >= 0;

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
                if (Contains(typeof(Person)) && PERSON_PROPERTIES.HasProperty(SortBy))
                {
                    var expression = filter as BooleanExpression ?? new BooleanExpression { Predicates = { filter } };

                    if (!expression.Predicates.Any(predicate => (predicate as OperatorPredicate)?.LHS is Property))
                    {
                        var pages = SortAscending ? Helpers.LazyRange(-1, -1) : Helpers.LazyRange(1, 1);
                        sources.Add(Request<Person>(API.PEOPLE.GET_POPULAR, pages, TryParsePerson, SortAscending));
                    }
                }

                return sources.Count == 1 ? sources[0] : Merge(sources, SortBy, SortAscending ? -1 : 1);
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
                            /*else if (filters.OfType<OperatorPredicate>().Any(temp => temp != filter && temp.LHS == op.LHS))
                            {
                                continue;
                            }*/
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
                        if (property == Movie.GENRES) property = TVShow.GENRES;
                        else if (property == TVShow.GENRES) property = Movie.GENRES;
                        else if (property == Movie.WATCH_PROVIDERS) property = TVShow.WATCH_PROVIDERS;
                        else if (property == TVShow.WATCH_PROVIDERS) property = Movie.WATCH_PROVIDERS;
                        else return null;

                        if (!values.ContainsKey(property))
                        {
                            return null;
                        }
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
                                var certification_country = $"certification_country={TMDB.ISO_3166_1}";

                                if (!parameters.Contains(certification_country))
                                {
                                    parameters.Add(certification_country);
                                }
                            }
                            else if (property == Movie.WATCH_PROVIDERS || property == TVShow.WATCH_PROVIDERS || property == ViewModels.CollectionViewModel.MonetizationType)
                            {
                                var watch_region = $"watch_region={TMDB.ISO_3166_1}";

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
                        return runtime.TotalMinutes.ToString();
                    }
                }
                else if (property == Movie.CONTENT_RATING || property == TVShow.CONTENT_RATING)
                {
                    if (value is string str)
                    {
                        return str;
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

                var result = JsonSerializer.Serialize(value, value.GetType());

                if (value is DateTime)
                {
                    result = result.Trim('\"');
                }

                return result;
            }
        }
    }
}