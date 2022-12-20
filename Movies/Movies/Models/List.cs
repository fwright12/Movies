using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Models
{
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

        public IAsyncEnumerator<Item> GetAsyncEnumerator(FilterPredicate filter, CancellationToken cancellationToken = default)
        {
            var filtered = TryFilter(filter, out filter, cancellationToken);
            return ItemHelpers.Filter(CacheItems(filtered), filter, cancellationToken).GetAsyncEnumerator();
        }

        private async IAsyncEnumerable<Item> CacheItems(IAsyncEnumerable<Item> items)
        {
            await foreach (var item in items)
            {
                Cache[item] = Task.FromResult<bool?>(true);
                yield return item;
            }
        }

        public virtual IAsyncEnumerable<Item> TryFilter(FilterPredicate full, out FilterPredicate partial, CancellationToken cancellationToken = default)
        {
            partial = full;
            return this;
        }
    }
}
