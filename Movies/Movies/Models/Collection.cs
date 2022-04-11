using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Models
{
    public class Collection : Item, IAsyncEnumerable<Item>
    {
        public string Description { get; set; }
        public string PosterPath { get; set; }
        public int? Count { get; set; }
        public IAsyncEnumerable<Item> Items { private get; set; }
        public bool IsFullyLoaded { get; private set; }

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

    public abstract class List : Collection
    {
        public abstract string ID { get; }

        public ItemType AllowedTypes { get; set; } = ItemType.All;
        public string Author { get; set; }
        public bool Public { get; set; }
        public bool Editable { get; set; } = true;

        private Dictionary<Item, Task<bool?>> Cache = new Dictionary<Item, Task<bool?>>();

        public List()
        {
            Count = 0;
        }
        //public List(IAsyncEnumerable<Item> items, int? count = null) : base(items, count) { }

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
    }
}
