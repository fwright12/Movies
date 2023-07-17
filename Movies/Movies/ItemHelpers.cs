using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Models
{
    public static class ItemHelpers
    {
        public static List<Type> RemoveTypes(FilterPredicate predicate, out FilterPredicate remaining)
        {
            var predicates = predicate is BooleanExpression temp && temp.IsAnd ? temp.Predicates : new List<FilterPredicate> { predicate };
            var expression = new BooleanExpression();
            var types = new List<Type>();

            foreach (var value in predicates)
            {
                var inner = value is BooleanExpression temp1 && !temp1.IsAnd ? temp1.Predicates : new List<FilterPredicate> { value };
                var innerTypes = new List<Type>();
                var itr = inner.GetEnumerator();

                while (itr.MoveNext() && itr.Current is OperatorPredicate op && Equals(op.LHS, ViewModels.CollectionViewModel.ITEM_TYPE) && op.RHS is Type type)
                {
                    innerTypes.Add(type);
                }

                if (inner.Count == innerTypes.Count)
                {
                    types.AddRange(innerTypes);
                }
                else
                {
                    expression.Predicates.Add(value);
                }
            }

            if (types.Count == 0)
            {
                remaining = predicate;
            }
            else if (expression.Predicates.Count == 0)
            {
                remaining = FilterPredicate.TAUTOLOGY;
            }
            else
            {
                remaining = expression;
            }

            return types;
        }

        public static List<Type> RemoveTypes1(FilterPredicate predicate, out FilterPredicate remaining)
        {
            var predicates = (predicate as BooleanExpression)?.Predicates ?? new List<FilterPredicate> { predicate };
            var expression = new BooleanExpression();
            var types = new List<Type>();

            foreach (var value in predicates)
            {
                if (value is OperatorPredicate op && Equals(op.LHS, ViewModels.CollectionViewModel.ITEM_TYPE) && op.RHS is Type type)
                {
                    types.Add(type);
                }
                else
                {
                    expression.Predicates.Add(predicate);
                }
            }

            remaining = expression;
            return types;
        }

        private static async Task<List<T>> Buffer<T>(IAsyncEnumerator<T> itr, int? count, CancellationToken cancellationToken = default)
        {
            var buffer = new List<T>();

            for (int i = 0; !(i >= count) && !cancellationToken.IsCancellationRequested && await itr.MoveNextAsync(); i++)
            {
                buffer.Add(itr.Current);
            }

            return buffer;
        }

        public static async IAsyncEnumerable<Item> Filter(IAsyncEnumerable<Item> items, FilterPredicate filter, CancellationToken cancellationToken = default)
        {
            var itr = items.GetAsyncEnumerator(cancellationToken);
            List<Item> buffer;
            var size = 10;
#if DEBUG
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int count = 0;
#endif

            do
            {
                buffer = await Buffer(itr, size, cancellationToken);
#if DEBUG
                count += buffer.Count;
                if (count % 100 == 0) Print.Log($"Loaded {count}");
#endif
                var evaluated = await Task.WhenAll(buffer.Select(item => ItemHelpers.Evaluate(item, filter)));

                for (int i = 0; i < buffer.Count && !cancellationToken.IsCancellationRequested; i++)
                {
                    if (evaluated[i])
                    {
                        yield return buffer[i];
                    }
                }
            }
            while (buffer.Count == size && !cancellationToken.IsCancellationRequested);

#if DEBUG
            watch.Stop();
            if (Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.iOS && watch.ElapsedMilliseconds > 1000 && count > 0)
            {
                App.Message($"flushed {count} items in {watch.Elapsed}");
            }
            else
            {
                Print.Log($"flushed {count} items in {watch.Elapsed}");
            }
#endif
        }

        public static IJsonCache PersistentCache { get; set; }

        public static Task<bool> Evaluate(Item item, FilterPredicate filter) => Evaluate(DataService.Instance.Controller, item, filter);

#if true
        public static async Task<bool> Evaluate(ChainLink<MultiRestEventArgs> controller, Item item, FilterPredicate filter)
        {
            var predicates = DefferedPredicates(item, filter, DataService.Instance.ResourceCache, PersistentCache).GetAsyncEnumerator();

            object lhs = ViewModels.CollectionViewModel.ITEM_TYPE;
            object value = item.GetType();

            if (filter is BooleanExpression exp)
            {
                filter = ViewModels.ExpressionBuilder.FormatFilters(exp.Predicates);
            }
            //var types = RemoveTypes(filter, out filter);

            while (true)
            {
                filter = Reduce(filter as BooleanExpression ?? new BooleanExpression { Predicates = { filter } }, lhs, value);

                if (filter == FilterPredicate.TAUTOLOGY)
                {
                    return true;
                }
                else if (filter == FilterPredicate.CONTRADICTION)
                {
                    return false;
                }

                if (await predicates.MoveNextAsync() && predicates.Current.LHS is Property property)
                {
                    lhs = property;

                    try
                    {
                        property = FixProperty(item, property);
                        var request = new RestRequestArgs(new UniformItemIdentifier(item, property), property.FullType);
                        await controller.Get(request);

                        if (!request.Handled || !request.Response.TryGetRepresentation(property.FullType, out value))
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static async IAsyncEnumerable<OperatorPredicate> DefferedPredicates(Item item, FilterPredicate predicate, UiiDictionaryDatastore datastore = null, IJsonCache cache = null)
        {
            var cachedInMemory = new Queue<OperatorPredicate>();
            var cachedPersistent = new Queue<OperatorPredicate>();
            var notCached = new Queue<OperatorPredicate>();

            foreach (var child in Flatten(predicate))
            {
                if (child is OperatorPredicate op)
                {
                    if (op.LHS is Property property)
                    {
                        if (datastore != null && datastore.ReadAsync(new UniformItemIdentifier(item, property)) != null)
                        {
                            cachedInMemory.Enqueue(op);
                        }
                        else
                        {
                            cachedPersistent.Enqueue(op);
                        }
                    }
                }
                else //if (!Equals(op.LHS, ViewModels.CollectionViewModel.ITEM_TYPE))
                {
                    yield break;
                }
            }

            foreach (var value in cachedInMemory)
            {
                yield return value;
            }

            foreach (var value in cachedPersistent)
            {
                if (value.LHS is Property property && cache != null && TryGetRequest(property, out var request) == true && await cache.IsCached(request.GetURL()))
                {
                    yield return value;
                }
                else
                {
                    notCached.Enqueue(value);
                }
            }

            foreach (var value in notCached)
            {
                yield return value;
            }
        }
#else
        public static async Task<bool> Evaluate(ChainLink<MultiRestEventArgs> controller, Item item, FilterPredicate filter)//, PropertyDictionary properties = null, ItemInfoCache cache = null)
        {
            var details = new Lazy<PropertyDictionary>(() => DataService.Instance.GetDetails(item));
            var predicates = DefferedPredicates(filter, DataService.Instance.GetDetails(item), PersistentCache).GetAsyncEnumerator();

            object lhs = ViewModels.CollectionViewModel.ITEM_TYPE;
            object value = item.GetType();

            if (filter is BooleanExpression exp)
            {
                filter = ViewModels.ExpressionBuilder.FormatFilters(exp.Predicates);
            }
            //var types = RemoveTypes(filter, out filter);

            while (true)
            {
                filter = Reduce(filter as BooleanExpression ?? new BooleanExpression { Predicates = { filter } }, lhs, value);

                if (filter == FilterPredicate.TAUTOLOGY)
                {
                    return true;
                }
                else if (filter == FilterPredicate.CONTRADICTION)
                {
                    return false;
                }

                if (await predicates.MoveNextAsync() && predicates.Current.LHS is Property property && details.Value.TryGetValue(FixProperty(item, property), out var task))
                {
                    lhs = property;
                    try
                    {
                        property = FixProperty(item, property);
                        value = await task;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static async IAsyncEnumerable<OperatorPredicate> DefferedPredicates(FilterPredicate predicate, PropertyDictionary properties = null, IJsonCache cache = null)
        {
            var cachedInMemory = new Queue<OperatorPredicate>();
            var cachedPersistent = new Queue<OperatorPredicate>();
            var notCached = new Queue<OperatorPredicate>();

            foreach (var child in Flatten(predicate))
            {
                if (child is OperatorPredicate op)
                {
                    if (op.LHS is Property property)
                    {
                        if (properties != null && properties.ValueCount(property) > 0)
                        {
                            cachedInMemory.Enqueue(op);
                        }
                        else
                        {
                            cachedPersistent.Enqueue(op);
                        }
                    }
                }
                else //if (!Equals(op.LHS, ViewModels.CollectionViewModel.ITEM_TYPE))
                {
                    yield break;
                }
            }

            foreach (var value in cachedInMemory)
            {
                yield return value;
            }

            foreach (var value in cachedPersistent)
            {
                if (value.LHS is Property property && cache != null && TryGetRequest(property, out var request) == true && await cache.IsCached(request.GetURL()))
                {
                    yield return value;
                }
                else
                {
                    notCached.Enqueue(value);
                }
            }

            foreach (var value in notCached)
            {
                yield return value;
            }
        }
#endif

        private static Property FixProperty(Item item, Property property)
        {
            if (item is Movie)
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
            else if (item is TVShow)
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

            return property;
        }

        private static bool TryGetRequest(Property property, out TMDbRequest request)
        {
            foreach (var properties in TMDB.ITEM_PROPERTIES.Values)
            {
                if (properties.PropertyLookup.TryGetValue(property, out request))
                {
                    return true;
                }
            }

            request = null;
            return false;
        }

        public static IEnumerable<FilterPredicate> Flatten(FilterPredicate predicate)
        {
            if (predicate is BooleanExpression expression)
            {
                foreach (var child in expression.Predicates.SelectMany(predicate => Flatten(predicate)))
                {
                    yield return child;
                }
            }
            else
            {
                yield return predicate;
            }
        }

        private static FilterPredicate Reduce(BooleanExpression expression, object lhs = null, object value = null)
        {
            var result = new BooleanExpression
            {
                IsAnd = expression.IsAnd
            };
            var and = expression.IsAnd;

            foreach (var predicate in expression.Predicates)
            {
                var reduced = predicate;

                if (predicate is BooleanExpression inner)
                {
                    reduced = Reduce(inner, lhs, value);
                }
                else if (predicate is OperatorPredicate op)
                {
                    FilterPredicate current = null;

                    if (Equals(op.LHS, lhs))
                    {
                        IEnumerable values;

                        if (lhs is Property property && property.AllowsMultiple && value is IEnumerable collection)
                        {
                            values = collection;
                        }
                        else
                        {
                            values = new List<object> { value };
                        }

                        BooleanExpression exp = new BooleanExpression
                        {
                            IsAnd = false
                        };

                        foreach (var temp in values)
                        {
                            exp.Predicates.Add(new OperatorPredicate
                            {
                                LHS = temp,
                                Operator = op.Operator,
                                RHS = op.RHS
                            });
                        }

                        current = exp;
                    }
                    else if (!(op.LHS is Property))
                    {
                        current = op;
                    }

                    if (current != null)
                    {
                        reduced = current.Evaluate() ? FilterPredicate.TAUTOLOGY : FilterPredicate.CONTRADICTION;
                    }
                }

                if (reduced == FilterPredicate.CONTRADICTION)
                {
                    if (and)
                    {
                        return reduced;
                    }
                }
                else if (reduced == FilterPredicate.TAUTOLOGY)
                {
                    if (!and)
                    {
                        return reduced;
                    }
                }
                else
                {
                    result.Predicates.Add(reduced);
                }
            }

            if (result.Predicates.Count == 0)
            {
                return and ? FilterPredicate.TAUTOLOGY : FilterPredicate.CONTRADICTION;
            }
            else
            {
                return result;
            }
        }

        public class FilterableWrapper<T> : IAsyncEnumerable<T>, IAsyncFilterable<T> where T : Item
        {
            private IAsyncEnumerable<T> Items { get; }

            public FilterableWrapper(IAsyncEnumerable<T> items)
            {
                Items = items;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => Items.GetAsyncEnumerator();

            public IAsyncEnumerator<T> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default) => Items.WhereAsync(item => ItemHelpers.Evaluate(item, predicate)).GetAsyncEnumerator();
        }
    }
}
