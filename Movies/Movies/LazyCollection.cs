using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public interface IReadOnlyLazyCollection<T> : IAsyncEnumerable<T>
    {
        Task<int> AsyncCount { get; }
        Task<bool> ContainsAsync(T item);
    }

    public interface ILazyCollection<T> : IReadOnlyLazyCollection<T>
    {
        void Add(T item);
        //void AddLazy(IAsyncEnumerable<T> lazyCollection);
        //void Insert(int index, T item);
        bool Remove(T item);
        void Clear();
    }

    public static class LazyExtensions
    {
        public static ILazyCollection<object> MakeLazy(this IEnumerable collection) => collection as ILazyCollection<object> ?? new LazyCollection<object>(System.Linq.Enumerable.OfType<object>(collection));
        public static ILazyCollection<T> MakeLazy<T>(this IEnumerable<T> collection) => collection as ILazyCollection<T> ?? new LazyCollection<T>(collection);
    }

    public class SkipListNode<T>
    {
        public List<SkipListNode<T>> Next;
        public List<SkipListNode<T>> Previous;
    }

    public class SkipList<T>
    {
        
    }

    public class LazyCollection<T> : ILazyCollection<T>
    {
        public Task<int> AsyncCount => Task.FromResult(List.Count + RemainingCount);
        private async Task<int> GetCount()
        {
            int count = List.Count;

            foreach (var remaining in Additions)
            {
                //count += await remaining.Item1.AsyncCount;
            }

            await Task.CompletedTask;

            return count;
        }

        public bool HasLoadedCompletely => Additions.Count == 0;

        private IList<T> List;
        //private Queue<(IReadOnlyLazyCollection<T> Collection, IAsyncEnumerator<T> Itr)> Remaining;
        private List<SmartItr> Additions;
        //private SortedDictionary<int, object> Inserts;
        private IAsyncEnumerator<T> Itr => Additions[0];
        private int RemainingCount;

        private class SmartItr : IAsyncEnumerator<T>
        {
            public IAsyncEnumerable<T> Source { get; }
            public int Count { get; private set; }

            public T Current => Itr.Current;

            private IAsyncEnumerator<T> Itr;

            public SmartItr(IAsyncEnumerable<T> source)
            {
                Source = source;
                Itr = source.GetAsyncEnumerator();
            }

            public ValueTask DisposeAsync() => Itr.DisposeAsync();

            public ValueTask<bool> MoveNextAsync()
            {
                Count++;
                return Itr.MoveNextAsync();
            }
        }

        private readonly SemaphoreSlim RemainingSemaphore = new SemaphoreSlim(1, 1);
        private readonly object ListLock = new object();

        protected LazyCollection(IList<T> list)
        {
            List = list;
            Additions = new List<SmartItr>();
        }

        public LazyCollection() : this(new List<T>()) { }

        public LazyCollection(IAsyncEnumerable<T> lazyCollection, int? count = null) : this()
        {
            AddLazy(lazyCollection);
            RemainingCount = count ?? 0;
        }

        public LazyCollection(IEnumerable<T> enumerable) : this(new List<T>(enumerable)) { }

        public static LazyCollection<T> FromList(IList<T> list) => new LazyCollection<T>(list);

        public void Add(T item)
        {
            lock (ListLock)
            {
                List.Add(item);
            }
        }

        public void AddLazy(IAsyncEnumerable<T> items)
        {
            RemainingSemaphore.Wait();
            Additions.Add(new SmartItr(items));
            RemainingSemaphore.Release();

            //Task.Run(async () => RemainingCount += await lazyCollection.AsyncCount);
        }

        public void Insert(int index, T item)
        {
            lock (ListLock)
            {
                if (index <= List.Count)
                {
                    List.Insert(index, item);
                    return;
                }
            }

#if DEBUG
            throw new NotImplementedException("Lazy insert");
#endif
        }

        public bool Remove(T item)
        {
            lock (ListLock)
            {
                if (List.Remove(item))
                {
                    return true;
                }
            }

            throw new NotImplementedException("Lazy remove");
        }

        public void Clear()
        {
            lock (ListLock)
            {
                List.Clear();
            }

            RemainingSemaphore.Wait();

            Additions.Clear();
            RemainingCount = 0;

            RemainingSemaphore.Release();
        }

        public async IAsyncEnumerable<object> SmartEnumerate()
        {
            Queue<int> deferred = new Queue<int>();

            for (int index = 0; ; index++)
            {
                IReadOnlyLazyCollection<T> lazy;

                if (index < Additions.Count)
                {
                    var temp = Additions[index];

                    if (temp is IReadOnlyLazyCollection<T> t)
                    {
                        lazy = t;
                    }
                    else
                    {
                        deferred.Enqueue(index);
                        continue;
                    }
                }
                else
                {
                    break;
                }

                yield return lazy;
            }

            var test = new LazyCollection<T>(this);
            //test.Itr = Itr;

            while (deferred.Count > 0)
            {
                //test.AddLazy(Additions[deferred.Dequeue()]);
            }

            for (int offset = 0; deferred.Count > 0;)
            {
                int index = deferred.Dequeue();
                var temp = Additions[index];

                if (index - offset == 0)
                {
                    while (temp == Additions[0] && await LoadMore() == 1)
                    {
                        yield return List[List.Count - 1];
                    }
                }
                else
                {
                    //yield return Additions[index] = new LazyCollection<T>(temp);
                }
            }
        }

        public async Task<int> CountAsync1()
        {
            int count;

            count = List.Count;

            await foreach (var part in SmartEnumerate())
            {
                if (part is IReadOnlyLazyCollection<T> lazy)
                {
                    count += await lazy.AsyncCount;
                }
                else
                {
                    count++;
                }
            }

            return count;
        }

        public async Task<bool> ContainsAsync1(T item)
        {
            var temp = new LazyCollection<T>();

            foreach (object add in Additions)
            {
                if (add is IReadOnlyLazyCollection<T> lazy && await lazy.ContainsAsync(item))
                {
                    return true;
                }
                else if (add is IAsyncEnumerable<T> items)
                {
                    temp.AddLazy(items);
                }
                else if (add is T t)
                {
                    temp.Add(t);
                }
            }

            int count = 0;
            while (true)
            {
                if (temp.Additions[0] == Additions[count])
                {
                    //temp.Additions[0] = this;
                }
                else
                {
                    //temp.Additions[0] = new LazyCollection<T>(temp.Additions[0] as IAsyncEnumerable<T>);
                }

                if (await temp.LoadMore() < 1)
                {
                    break;
                }
                else if (temp.List[temp.List.Count - 1].Equals(item))
                {
                    return true;
                }
            }

            await foreach (var part in SmartEnumerate())
            {
                if (part is IReadOnlyLazyCollection<T> lazy)
                {
                    if (await lazy.ContainsAsync(item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static async Task<bool> ContainsAsync(IAsyncEnumerable<T> source, T item)
        {
            await foreach(T member in source)
            {
                if (item.Equals(member))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> ContainsAsync(T item)
        {
            lock (ListLock)
            {
                if (List.Contains(item))
                {
                    return true;
                }
            }

            Queue<int> deferred = new Queue<int>();

            await RemainingSemaphore.WaitAsync();

            for (int i = 0; ; i++)
            {
                if (i < Additions.Count)
                {
                    if (Additions[i] is IReadOnlyLazyCollection<T> lazy)
                    {
                        if (await lazy.ContainsAsync(item))
                        {
                            RemainingSemaphore.Release();
                            return true;
                        }
                    }
                    else
                    {
                        deferred.Enqueue(i);
                    }
                }
                else if (deferred.Count > 0)
                {
                    var index = deferred.Dequeue();
                    var lazy = new LazyCollection<T>();// Additions[index]);
                    //Additions[index] = lazy;

                    while (await lazy.LoadMore() == 1)
                    {
                        if (List[List.Count - 1].Equals(item))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    RemainingSemaphore.Release();
                    return false;
                }
            }
        }
        
        public async Task<int> LoadMore(int count = 1)
        {
            List<T> buffer = new List<T>();

            for (int i = 0; ;)
            {
                if (i >= count || HasLoadedCompletely)
                {
                    break;
                }

                if (await Itr.MoveNextAsync())
                {
                    buffer.Add(Itr.Current);
                    i++;
                    RemainingCount--;
                }
                else
                {
                    await RemainingSemaphore.WaitAsync();

                    Additions.RemoveAt(0);
                    if (Additions.Count > 0)
                    {
                        //Itr = Additions[0].GetAsyncEnumerator();
                    }

                    RemainingSemaphore.Release();
                }
            }

            lock (ListLock)
            {
                foreach (T item in buffer)
                {
                    Add(item);
                }
            }

            return buffer.Count;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < List.Count || await LoadMore() == 1; i++)
            {
                yield return List[i];
            }
        }

        private class Enumerator : IAsyncEnumerator<T>
        {
            public T Current => List.List[Index];

            private LazyCollection<T> List;
            //private IAsyncEnumerator<T> Itr;
            private int Index;

            public Enumerator(LazyCollection<T> list)
            {
                List = list;
            }

            public ValueTask DisposeAsync()
            {
                throw new NotImplementedException();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (++Index <= List.List.Count)
                {
                    return true;
                }

                return await List.LoadMore() == 1;
            }
        }
    }
}
