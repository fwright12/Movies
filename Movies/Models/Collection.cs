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
                    _Itr = new WebCollectionEnumerator<Item>(Items);
                }

                return _Itr;
            }
        }
        private IAsyncEnumerator<Item> _Itr;

        private class WebCollectionEnumerator<T> : IAsyncEnumerator<T>
        {
            public T Current => Itr.Current;

            private IAsyncEnumerator<T> Itr { get; }
            private Exception ThrownException;

            public WebCollectionEnumerator(IAsyncEnumerable<T> items)
            {
                Itr = items.GetAsyncEnumerator();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (ThrownException != null)
                {
                    throw ThrownException;
                }

                try
                {
                    return await Itr.MoveNextAsync();
                }
                catch (Exception e)
                {
                    throw ThrownException = e;
                }
            }

            public ValueTask DisposeAsync()
            {
                return Itr.DisposeAsync();
            }
        }

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
        public bool Error { get; private set; }

        public async IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            for (var node = Reverse ? Cached.Last : Cached.First; ;)
            {
                await ItrSemaphore.WaitAsync();

                if (node == Middle)
                {
                    bool moved;

                    try
                    {
                        moved = await Itr.MoveNextAsync();
                    }
                    catch
                    {
                        Error = true;
                        ItrSemaphore.Release();
                        throw;
                    }

                    if (moved)
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
}
