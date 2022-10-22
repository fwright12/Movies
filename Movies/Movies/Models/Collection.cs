using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Models
{
    public interface ISearchable
    {
        IAsyncEnumerable<Item> Search(string query);
    }

    public interface IFilterable<T> : IEnumerable<T>
    {
        IEnumerable<T> GetItems(FilterPredicate predicate);
    }

    public interface IAsyncFilterable<T> : IAsyncEnumerable<T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default);
    }

    public abstract class AsyncFilterable<T> : IAsyncFilterable<T>
    {
        public abstract IAsyncEnumerator<T> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default);
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => GetAsyncEnumerator(FilterPredicate.TAUTOLOGY);
    }

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

        public static async Task<bool> Evaluate(Item item, FilterPredicate filter, PropertyDictionary properties = null, ItemInfoCache cache = null)
        {
            var details = new Lazy<PropertyDictionary>(() => DataService.Instance.GetDetails(item));
            var predicates = DefferedPredicates(filter).GetAsyncEnumerator();

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

        public static async IAsyncEnumerable<OperatorPredicate> DefferedPredicates(FilterPredicate predicate, PropertyDictionary properties = null, ItemProperties lookup = null, IJsonCache cache = null)
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
                if (value.LHS is Property property && cache != null && lookup?.PropertyLookup.TryGetValue(property, out var request) == true && await cache.IsCached(request.GetURL()))
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

    public class Collection : Item, IAsyncEnumerable<Item>
    {
        public string Description { get; set; }
        public string PosterPath { get; set; }
        public int? Count { get; set; }
        public IAsyncEnumerable<Item> Items { protected get; set; }
        public bool IsFullyLoaded { get; private set; }
        public Property SortBy { get; set; }
        public bool SortAscending { get; set; }

        private LinkedList<Item> Cached;
        private ICollection<LinkedListNode<Item>> Removed;
        private ICollection<Item> WillRemove;
        private HashSet<Item> Cache;
        private readonly LinkedListNode<Item> Middle;

        private IAsyncEnumerator<Item> Itr
        {
            get
            {
                if (_Itr == null)
                {
                    _Itr = Items.GetAsyncEnumerator();
                }

                return _Itr;
            }
        }
        private IAsyncEnumerator<Item> _Itr;

        public Collection()
        {
            Middle = new LinkedListNode<Item>(null);
            Reset();
        }

        public Collection(IAsyncEnumerable<Item> items, int? count = null) : this()
        {
            Items = items;
            Count = count;
        }

        public void Reset()
        {
            Cached = new LinkedList<Item>();
            Cache = new HashSet<Item>();
            Removed = new List<LinkedListNode<Item>>();
            WillRemove = new HashSet<Item>();

            Middle.List?.Remove(Middle);
            Cached.AddFirst(Middle);

            _Itr = null;
            IsFullyLoaded = false;
        }

        protected bool Add(Item item)
        {
            if (Insert(item) != null)
            {
                Count = (Count ?? 0) + 1;
                return true;
            }

            return false;
        }

        private LinkedListNode<Item> Insert(Item item, LinkedListNode<Item> node = null, bool before = false)
        {
            if (Cache.Add(item))
            {
                if (node == null)
                {
                    return Cached.AddLast(item);
                }
                else if (before)
                {
                    return Cached.AddBefore(node, item);
                }
                else
                {
                    return Cached.AddAfter(node, item);
                }
            }

            return null;
        }

        protected bool Remove(Item item)
        {
            if (Cache.Remove(item))
            {
                Removed.Add(Cached.Find(item));
            }
            else if (!IsFullyLoaded)
            {
                WillRemove.Add(item);
            }
            else
            {
                return false;
            }

            Count = (Count ?? 1) - 1;
            return true;
        }

        protected bool Contains(Item item) => Cache.Contains(item);

        private SemaphoreSlim ItrSemaphore = new SemaphoreSlim(1, 1);
        public bool Reverse { get; set; } = false;

        public async IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            for (var node = Reverse ? Cached.Last : Cached.First; ;)
            {
                await ItrSemaphore.WaitAsync();

                if (node == Middle)
                {
                    if (await Itr.MoveNextAsync())
                    {
                        LinkedListNode<Item> added = null;

                        if (!WillRemove.Remove(Itr.Current))
                        {
                            added = Insert(Itr.Current, Middle, !Reverse);
                        }

                        if (added != null)
                        {
                            Count = Math.Max(Count ?? 0, Cache.Count);
                            node = added;
                        }
                        else
                        {
                            ItrSemaphore.Release();
                            continue;
                        }
                    }
                }

                if (node == null)
                {
                    Count = Cache.Count;
                    ItrSemaphore.Release();
                    IsFullyLoaded = true;
                    break;
                }

                bool removed = Removed.Contains(node) || node == Middle;
                var value = node.Value;

                ItrSemaphore.Release();

                if (!removed)
                {
                    yield return value;
                }

                node = Reverse ? node.Previous : node.Next;
            }
        }
    }

    public interface IAsyncCollection<T> : IAsyncEnumerable<T>
    {
        Task<bool?> ContainsAsync(T item);
    }

    public abstract class List : Collection, IAsyncCollection<Item>, IAsyncFilterable<Item>
    {
        public abstract string ID { get; }

        public ItemType AllowedTypes { get; set; } = ItemType.All;
        public string Author { get; set; }
        public bool Public { get; set; }
        public bool Editable { get; set; } = true;
        public DateTime? LastUpdated { get; set; }

        private Dictionary<Item, Task<bool?>> Cache = new Dictionary<Item, Task<bool?>>();

        public List()
        {
            Count = 0;
        }
        //public List(IAsyncEnumerable<Item> items, int? count = null) : base(items, count) { }

        /*public virtual async Task<(IEnumerable<Item> Added, IEnumerable<Item> Removed)> DiffAsync(List other)
        {
            var diff = (new List<Item>(), new List<Item>());

            foreach (var item in items[0])
            {
                if (await other.ContainsAsync(item) != true)
                {
                    diff.Item1.Insert(0, item);
                }
            }

            foreach (var item in items[1])
            {
                if (await ContainsAsync(item) != true)
                {
                    diff.Item2.Add(item);
                }
            }

            return diff;
        }*/

        public abstract Task Update();
        public abstract Task Delete();

        public Task<bool> AddAsync(params Item[] items) => AddAsync((IEnumerable<Item>)items);
        public Task<bool> AddAsync(IEnumerable<Item> items)
        {
            var list = items.Where(item => Add(item)).ToList();

            if (list.Count > 0)
            {
                return AddAsyncInternal(list);
            }
            else
            {
                return Task.FromResult(true);
            }
        }
        protected abstract Task<bool> AddAsyncInternal(IEnumerable<Item> items);

        public Task<bool> RemoveAsync(params Item[] items) => RemoveAsync((IEnumerable<Item>)items);
        public Task<bool> RemoveAsync(IEnumerable<Item> items)
        {
            if (items.Any())
            {
                foreach (var item in items)
                {
                    Remove(item);
                    Cache[item] = Task.FromResult<bool?>(false);
                }

                return RemoveAsyncInternal(items);
            }
            else
            {
                return Task.FromResult(true);
            }
        }
        protected abstract Task<bool> RemoveAsyncInternal(IEnumerable<Item> items);

        public abstract Task<int> CountAsync();
        protected abstract Task<bool?> ContainsAsyncInternal(Item item);

        public Task<bool?> ContainsAsync(Item item)
        {
            if (Contains(item))
            {
                return Task.FromResult<bool?>(true);
            }
            else if (IsFullyLoaded || !AllowedTypes.HasFlag(item.ItemType))
            {
                return Task.FromResult<bool?>(false);
            }
            else if (Cache.TryGetValue(item, out var contains))
            {
                return contains;
            }
            else
            {
                return Cache[item] = ContainsAsyncInternal(item);
            }
        }

        public virtual Task<List<Item>> GetAdditionsAsync(List list) => Task.FromResult(new List<Item>());

        public virtual async IAsyncEnumerator<Item> GetAsyncEnumerator(FilterPredicate filter, CancellationToken cancellationToken = default)
        {
            await foreach (var item in this)
            {
                if (await ItemHelpers.Evaluate(item, filter))
                {
                    yield return item;
                }
            }
        }
    }
}
