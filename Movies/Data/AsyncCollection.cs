using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public interface IAsyncCollection<T> : ICollection<T>, IAsyncEnumerable<T>
    {
        Task<int> CountAsync { get; }

        bool IsReadOnly { get; }

        Task AddAsync(T item);

        Task ClearAsync();

        Task<bool> ContainsAsync(T item);

        Task CopyToAsync(T[] array, int arrayIndex);

        Task<bool> RemoveAsync(T item);
    }

    public class HashSetAsyncWrapper<T> : AsyncCollection<HashSet<T>, T> { public HashSetAsyncWrapper(Task<IEnumerable<T>> items) : base(items) { } }
    public class ListAsyncWrapper<T> : AsyncCollection<List<T>, T> { public ListAsyncWrapper(Task<IEnumerable<T>> items) : base(items) { } }

    public class AsyncCollection<TCollection, T> : IAsyncCollection<T>
        where TCollection : ICollection<T>, new()
    {
        public int Count => Collection.Count;
        public bool IsReadOnly => Collection.IsReadOnly;
        public Task<int> CountAsync => throw new NotImplementedException();

        public TCollection Collection { get; }
        public Task Load { get; }

        public AsyncCollection(Task<IEnumerable<T>> items)
        {
            Collection = new TCollection();
            Load = AddWhenReady(items);
        }

        private async Task AddWhenReady(Task<IEnumerable<T>> items)
        {
            foreach (var item in await items)
            {
                Collection.Add(item);
            }
        }

        public void Add(T item) => Collection.Add(item);

        public void Clear() => Collection.Clear();

        public bool Contains(T item) => Collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => Collection.CopyTo(array, arrayIndex);

        public bool Remove(T item) => Collection.Remove(item);

        public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Collection).GetEnumerator();

        public async Task AddAsync(T item)
        {
            await Load;
            Add(item);
        }

        public async Task ClearAsync()
        {
            await Load;
            Clear();
        }

        public async Task<bool> ContainsAsync(T item)
        {
            await Load;
            return Contains(item);
        }

        public async Task CopyToAsync(T[] array, int arrayIndex)
        {
            await Load;
            CopyTo(array, arrayIndex);
        }

        public async Task<bool> RemoveAsync(T item)
        {
            await Load;
            return Remove(item);
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Load;

            foreach (var item in this)
            {
                yield return item;
            }
        }
    }
}
