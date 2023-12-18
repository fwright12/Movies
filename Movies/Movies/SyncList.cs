using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Movies
{
    public class SyncList : List
    {
        private static readonly List<PropertyInfo> Properties = new List<PropertyInfo>(new string[]
        {
            nameof(Name),
            nameof(Description),
            nameof(PosterPath),
            //nameof(Models.List.Count),
            //nameof(Models.List.AllowedTypes),
            //nameof(Models.List.Author),
            nameof(Public),
            //nameof(Models.List.Editable)
        }.Select(name => typeof(SyncList).GetProperty(name)));

        public override string ID => Sources.FirstOrDefault()?.ID;
        private IList<List> Sources;
        private Task InitialSync { get; }

        public SyncList(IEnumerable<List> sources, bool reverse = false)
        {
            Sources = new List<List>(sources);
            Items = GetItemsAsync(FilterPredicate.TAUTOLOGY);
            //Items = GetItems(Sources);
            Count = Sources.FirstOrDefault()?.Count;
            Reverse = reverse;

            foreach (var source in Sources)
            {
                source.Reverse = Reverse;
            }

            //Items = SyncEnumerator(Sources.Select(source => new FilteredList(source)), 1, Reverse);

            InitialSync = Sync();
        }

        private string CleanString(string value) => (value ?? string.Empty).Trim();
        private bool HasNewerUpdates(List master, List remote) => remote.LastUpdated > master.LastUpdated;

        private async Task PushChanges(List master, IEnumerable<List> secondary, IEnumerable<IEnumerable<Item>> additions, IEnumerable<IEnumerable<Item>> removals)
        {
            var allAdditions = additions.SelectMany().Distinct().ToList();
            var allRemovals = removals.SelectMany().ToHashSet();

            await Task.WhenAll(secondary.Zip(removals, (list, removed) => list.RemoveAsync(allRemovals.Except(removed.ToHashSet()))));
            await Task.WhenAll(secondary.Zip(additions, (list, added) => list.AddAsync(allAdditions.Except(added.ToHashSet()))));

            if (Reverse)
            {
                //allAdditions.Reverse();
            }

            await master.RemoveAsync(allRemovals);
            await master.AddAsync(allAdditions);
        }

        private static async Task<IEnumerable<TItem>> ExceptAsync<TItem, TSource>(Task<TSource> first, Task<TSource> second) where TSource : IEnumerable<TItem> => (await first).Except(await second);
        private static async Task<IEnumerable<T>> ReverseAsync<T>(Task<IEnumerable<T>> source) => (await source).Reverse();

        private static async Task<IEnumerable<T>> ToEnumerable<T>(Task<List<T>> source) => await source;
        private static async Task<IEnumerable<Item>> GetAdditions(List master, List source, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                return await source.GetAdditionsAsync(master);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task Sync()
        {
            if (Sources.Count > 0)
            {
                var master = Sources.FirstOrDefault();
                var secondary = Sources.Where(source => source != master).ToList();

                var lazyMaster = new Lazy<Task<List<Item>>>(() => master.ReadAll());
                var masterSemaphore = new SemaphoreSlim(1, 1);
                var additions = new List<Task<IEnumerable<Item>>>();
                var removals = new List<Task<IEnumerable<Item>>>();

                try
                {
                    foreach (var source in secondary)
                    {
                        Task<IEnumerable<Item>> add;
                        Task<IEnumerable<Item>> remove;

                        if (HasNewerUpdates(master, source))
                        {
                            var items = source.ReadAll();

                            add = ExceptAsync<Item, List<Item>>(items, lazyMaster.Value);
                            remove = ExceptAsync<Item, List<Item>>(lazyMaster.Value, items);

                            if (source.Reverse)
                            {
                                add = ReverseAsync(add);
                            }
                        }
                        else
                        {
                            remove = Task.FromResult<IEnumerable<Item>>(new List<Item>());

                            if (master.LastUpdated == null || source.LastUpdated == null)
                            {
                                //add = ToEnumerable(source.GetAdditionsAsync(master));
                                add = GetAdditions(master, source, masterSemaphore);
                            }
                            else
                            {
                                add = remove;
                            }
                        }

                        additions.Add(add);
                        removals.Add(remove);
                    }

                    await PushChanges(master, secondary, await Task.WhenAll(additions), await Task.WhenAll(removals));
                }
                catch { }
            }

            var changed = false;
            var values = new object[Properties.Count];//.Select(property => property.GetValue(main)).ToArray();

            for (int i = 0; i < Properties.Count; i++)
            {
                var property = Properties[i];

                foreach (var list in Sources)
                {
                    var other = property.GetValue(list);

                    if (!Equals(values[i], other) && !(property.PropertyType == typeof(string) && Equals(CleanString((string)values[i]), CleanString((string)other))))
                    {
                        values[i] = other;

                        if (list != Sources[0])
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                property.SetValue(this, values[i]);
            }

            if (changed)
            {
                await Update();
            }
        }

        private void CopyChanges(IEnumerable<PropertyInfo> changes, List from, params List[] to) => CopyChanges(changes, from, (IEnumerable<List>)to);
        private void CopyChanges(IEnumerable<PropertyInfo> changes, List from, IEnumerable<List> to)
        {
            foreach (var list in to)
            {
                foreach (var property in changes)
                {
                    property.SetValue(list, property.GetValue(from));
                }
            }
        }

        public async Task AddSourceAsync(List list)
        {
            var buffer = await Task.WhenAll(AsyncEnumerable.ReadAll(this), AsyncEnumerable.ReadAll(list));

            if (Reverse)
            {
                buffer[0].Reverse();
            }
            if (list.Reverse)
            {
                buffer[1].Reverse();
            }

            await Update(list);
            await Task.WhenAll(list.AddAsync(buffer[0]), AddAsync(buffer[1]));

            Sources.Add(list);
            await Task.WhenAll(Deffered.Select(list => list.Update()));

            list.Reverse = Reverse;
        }

        public void RemoveSource(List list)
        {
            Sources.Remove(list);
        }

        protected override async Task<bool> AddAsyncInternal(IEnumerable<Item> items) => !(await ExecuteDeffered(source => source.AddAsync(items))).Contains(false);

        protected override async Task<bool?> ContainsAsyncInternal(Item item)
        {
            var sources = Sources.Where(source => source.AllowedTypes.HasFlag(item.ItemType));

            if (sources.Count() == 0)
            {
                return false;
            }

            foreach (var source in sources)
            {
                var contains = await source.ContainsAsync(item);

                if (contains != true)
                {
                    return contains;
                }
            }

            return true;
        }

        public override Task<int> CountAsync() => Sources.FirstOrDefault()?.CountAsync() ?? Task.FromResult(0);

        public override Task Delete() => Task.WhenAll(Sources.Select(source => source.Delete()));

        protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Item> items) => !(await ExecuteDeffered(source => source.RemoveAsync(items))).Contains(false);

        private IEnumerable<List> Deffered => Sources.OfType<Views.Database.LocalList>();

        public override Task Update() => ExecuteDeffered(source => Update(source));

        //private Task Update(params List[] lists) => Update((IEnumerable<List>)lists);
        private Task Update(List list)
        {
            foreach (var property in Properties)
            {
                property.SetValue(list, property.GetValue(this));
            }

            AllowedTypes |= list.AllowedTypes;

            //Author = string.Join(", ", lists.Select(list => list.Author).Prepend(Author).Where(author => !string.IsNullOrEmpty(author)));

            return list.Update();
            //return ExecuteDeffered(list => list.Update(), lists, Deffered.ToList());
            //return Task.WhenAll(lists.Select(list => list.Update()));
        }

        private Task ExecuteDeffered(Func<List, Task> execute) => ExecuteDeffered(execute, Sources, Deffered.ToList());
        private async Task ExecuteDeffered<T>(Func<T, Task> execute, IEnumerable<T> source, IEnumerable<T> deffered)
        {
            await Task.WhenAll(source.Except(deffered).Select(execute));
            await Task.WhenAll(deffered.Select(execute));
        }

        private Task<T[]> ExecuteDeffered<T>(Func<List, Task<T>> execute) => ExecuteDeffered(execute, Sources, Deffered.ToList());
        private async Task<TResult[]> ExecuteDeffered<TSource, TResult>(Func<TSource, Task<TResult>> execute, IEnumerable<TSource> source, IEnumerable<TSource> deffered)
        {
            var first = (await Task.WhenAll(source.Except(deffered).Select(execute))).OfType<TResult>().GetEnumerator();
            var second = (await Task.WhenAll(deffered.Select(execute))).OfType<TResult>().GetEnumerator();

            TResult[] result = new TResult[Sources.Count];

            for (int i = 0; i < result.Count(); i++)
            {
                IEnumerator<TResult> itr = deffered.Contains(source.ElementAt(i)) ? second : first;

                itr.MoveNext();
                result[i] = itr.Current;
            }

            return result;
        }

        public void DiscardEdits()
        {
            CopyChanges(Properties, Sources[0], this);
        }

        public override IAsyncEnumerable<Item> TryFilter(FilterPredicate filter, out FilterPredicate partial, CancellationToken cancellationToken = default)
        {
            partial = filter;// FilterPredicate.TAUTOLOGY;
            return GetItemsAsync(filter, cancellationToken);
        }

        private async IAsyncEnumerable<Item> GetItemsAsync(FilterPredicate predicate, CancellationToken cancellationToken = default)
        {
            var itr = new Enumerator(this, predicate, cancellationToken);

            while (await itr.MoveNextAsync())
            {
                yield return itr.Current;
            }
        }

        private static Task Sync(IEnumerable<Item> add, IEnumerable<Item> remove, params List[] sources) => Sync(add, remove, (IEnumerable<List>)sources);

        private static Task Sync(IEnumerable<Item> add, IEnumerable<Item> remove, IEnumerable<List> sources, IEnumerable<IEnumerable<Item>> cached = null)
        {
            var updates = new List<Task>();

            foreach (var source in sources)
            {
                updates.Add(source.AddAsync(source.Reverse ? add.Reverse() : add));
                updates.Add(source.RemoveAsync(remove));
            }

            return Task.WhenAll(updates);
        }

        private class Enumerator : IAsyncEnumerator<Item>
        {
            private const int BATCH_SIZE = 1;
            private const int PAGE_SIZE = 20;

            public Item Current => _Current;

            private SyncList List { get; }
            private List Origin { get; }
            private List<List> Remote { get; }
            private FilterPredicate Filter { get; }

            private IAsyncEnumerator<Item>[] Itrs;
            private HashSet<int> RemoteIndexMap;
            private Dictionary<Item, bool?>[] ListItems;
            private Queue<Item> Batch = new Queue<Item>();
            private List<Item> Buffer = new List<Item>();
            private Item _Current;

            public Enumerator(SyncList list, CancellationToken cancellationToken = default) : this(list, FilterPredicate.TAUTOLOGY) { }

            public Enumerator(SyncList list, FilterPredicate filter, CancellationToken cancellationToken = default)
            {
                List = list;

                Origin = List.Sources.FirstOrDefault();
                Remote = List.Sources.Where(source => source != Origin).ToList();
                Filter = filter;

                var remoteIndexMap = Enumerable.Range(0, Remote.Count);
                if (Origin?.LastUpdated != null)
                {
                    remoteIndexMap = remoteIndexMap.Where(i => Remote[i].LastUpdated == null).ToList();
                }

                RemoteIndexMap = new HashSet<int>(remoteIndexMap);
                //Itrs = Origin == null ? new IAsyncEnumerator<Item>[0] : remoteIndexMap.Select(i => Remote[i]).Prepend(Origin).Select(source => filter == FilterPredicate.TAUTOLOGY ? source.GetAsyncEnumerator(cancellationToken) : source.TryFilter(filter, out _, cancellationToken).GetAsyncEnumerator(cancellationToken)).ToArray();
                if (Origin == null)
                {
                    Itrs = new IAsyncEnumerator<Item>[0];
                }
                else
                {
                    Itrs = new IAsyncEnumerator<Item>[RemoteIndexMap.Count + 1];
                    Itrs[0] = Origin.TryFilter(filter, out _, cancellationToken).GetAsyncEnumerator(cancellationToken);

                    var itr = remoteIndexMap.GetEnumerator();
                    for (int i = 1; itr.MoveNext(); i++)
                    {
                        var source = Remote[itr.Current];
                        Itrs[i] = source.TryFilter(filter, out _, cancellationToken).GetAsyncEnumerator(cancellationToken);
                    }
                }

                ListItems = Itrs.Select(_ => new Dictionary<Item, bool?>()).ToArray();
            }

            public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

            public async ValueTask<bool> MoveNextAsync()
            {
                await List.InitialSync;

                if (List.Sources.Count == 0)
                {
                    return false;
                }

                var forEach = Enumerable.Range(1, RemoteIndexMap.Count);
                var additions = forEach.Select(_ => Enumerable.Empty<Item>());
                var removals = forEach.Select(_ => Enumerable.Empty<Item>());

                while (Batch.Count == 0)
                {
                    // Pad the buffer with items from the source if necessary
                    await LoadMore(0, BATCH_SIZE - Buffer.Count);
                    var batch = RemoveRange(Buffer, 0, Math.Min(Buffer.Count, BATCH_SIZE));
                    //var batch = await TakeFiltered(BATCH_SIZE, Filter);

                    List<Item>[] added;
                    List<Item>[] removed;

                    try
                    {
                        added = await Task.WhenAll(forEach.Select(i => ExceptAsync(i, batch)));
                        removed = await Task.WhenAll(forEach.Select(i => ExceptAsync(batch, i)));
                    }
                    catch
                    {
                        added = new List<Item>[0];
                        removed = new List<Item>[0];
                    }

                    var allAdded = added.SelectMany().Distinct();

                    if (List.Reverse || batch.Count == 0)
                    {
                        foreach (var item in allAdded)
                        {
                            Batch.Enqueue(item);
                        }
                    }
                    else
                    {
                        Itrs[0] = ConcatAsync(Itrs[0], allAdded);
                        //Deffered.AddRange(added.SelectMany());
                    }

                    foreach (var item in batch.Except(removed.SelectMany().ToHashSet()))
                    {
                        Batch.Enqueue(item);
                    }

                    additions = additions.Zip(added, Enumerable.Concat);
                    removals = removals.Zip(removed, Enumerable.Concat);

                    if (batch.Count == 0)
                    {
                        break;
                    }
                }

                await List.PushChanges(Origin, Remote, PrepareChanges(additions), PrepareChanges(removals));

                return Batch.TryDequeue(out _Current);
            }

            private IEnumerable<IEnumerable<Item>> PrepareChanges(IEnumerable<IEnumerable<Item>> items)
            {
                List<IEnumerable<Item>> result = new List<IEnumerable<Item>>();
                var itr = items.GetEnumerator();

                for (int i = 0; i < Remote.Count; i++)
                {
                    IEnumerable<Item> next;

                    if (RemoteIndexMap.Contains(i) && itr.MoveNext())
                    {
                        next = itr.Current;

                        if (Remote[i].Reverse)
                        {
                            next = next.Reverse();
                        }
                    }
                    else
                    {
                        next = Enumerable.Empty<Item>();
                    }

                    result.Add(next);
                }

                return result;
            }

            private async Task<List<Item>> ExceptAsync(int index, IEnumerable<Item> source)
            {
                var result = new List<Item>();
                List<Item> buffer;
                int total;

                do
                {
                    total = result.Count;

                    if (!source.Any())
                    {
                        buffer = await LoadMore(index, null);
                    }
                    else
                    {
                        buffer = await PreemptiveBuffer(index, source);
                        await PreemptiveBuffer(0, buffer);
                    }

                    result.AddRange(await ExceptAsync(buffer, 0));
                }
                // Everything was an addition
                while (buffer.Count > 0 && result.Count - total == buffer.Count);

                return result;
            }

            private async Task<List<Item>> ExceptAsync(IEnumerable<Item> source, int index)
            {
                var result = new List<Item>();

                foreach (var item in source)
                {
                    if (!Contains(index, item) && await ContainsAsync(index, item) == false)
                    {
                        result.Add(item);
                    }
                }

                return result;
            }

            private async Task<List<Item>> PreemptiveBuffer(int index, IEnumerable<Item> items)
            {
                var misses = items.Sum(item => Contains(index, item) ? 0 : 1);
                return await LoadMore(index, misses * PAGE_SIZE);
            }

            private async Task<List<T>> LoadMore<T>(IAsyncEnumerator<T> itr, int? count = 1)
            {
                var buffer = new List<T>();

                for (int i = 0; !(i >= count) && await itr.MoveNextAsync(); i++)
                {
                    buffer.Add(itr.Current);
                }

                return buffer;
            }

            private SemaphoreSlim ListItemsSemaphore = new SemaphoreSlim(1, 1);
            private SemaphoreSlim BufferSemaphore = new SemaphoreSlim(1, 1);
            private SemaphoreSlim ItrSemaphore = new SemaphoreSlim(1, 1);

            private async Task<List<Item>> LoadMore(int index, int? count = 1)
            {
                List<Item> buffer;

                await ItrSemaphore.WaitAsync();
                try
                {
                    buffer = await LoadMore(Itrs[index], count);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    ItrSemaphore.Release();
                }

                await ListItemsSemaphore.WaitAsync();

                foreach (var item in buffer)
                {
                    ListItems[index][item] = true;
                }

                ListItemsSemaphore.Release();

                if (index == 0)
                {
                    await BufferSemaphore.WaitAsync();
                    Buffer.AddRange(buffer);
                    BufferSemaphore.Release();
                }

                return buffer;
            }

            private bool Contains(int index, Item item) => ListItems[index].TryGetValue(item, out var contains) && contains == true;

            private async Task<bool?> ContainsAsync(int index, Item item)
            {
                // index is relative to the list of remote sources with unknown update date
                var list = index == 0 ? Origin : Remote[RemoteIndexMap.OrderBy(i => i).ElementAt(index - 1)];

                bool? contains = null;
                await ListItemsSemaphore.WaitAsync();

                try
                {
                    contains = ListItems[index][item] = await list.ContainsAsync(item);
                }
                finally
                {
                    ListItemsSemaphore.Release();
                }

                return contains;
            }

            private static List<T> RemoveRange<T>(List<T> list, int index, int count)
            {
                var result = list.GetRange(index, count).ToList<T>();
                list.RemoveRange(index, count);
                return result;
            }

            private static async IAsyncEnumerator<T> ConcatAsync<T>(IAsyncEnumerator<T> itr, IEnumerable<T> items)
            {
                while (await itr.MoveNextAsync())
                {
                    yield return itr.Current;
                }

                foreach (var item in items)
                {
                    yield return item;
                }
            }
        }
    }
}