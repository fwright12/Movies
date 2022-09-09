﻿using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

            //Items = SyncEnumerator(Sources.Select(source => new FilteredList(source)), 1, Reverse);

            InitialSync = Sync1();
        }

        private string CleanString(string value) => (value ?? string.Empty).Trim();
        private bool HasNewerUpdates(List master, List remote) => remote.LastUpdated > master.LastUpdated;

        private async Task Sync1()
        {
            var main = Sources.FirstOrDefault();
            var secondary = Sources.Where(source => source != main);

            var add = new List<Task<List<Item>>>();
            var remove = new List<Task<List<Item>>>();

            foreach (var source in secondary)
            {
                if (HasNewerUpdates(main, source))
                {
                    var all = source.ReadAll();

                    add.Add(all);
                    remove.Add(all);
                }
                else
                {
                    //collections.Add(new LazyCollection<Item>(Wrap(source.GetAdditionsAsync(main)))));
                    add.Add(source.GetAdditionsAsync(main));
                }
            }

            var tasks = new List<Task<List<Item>[]>> { Task.WhenAll(add), Task.WhenAll(remove) };

            if (remove.Count > 0)
            {
                tasks.Add(Task.WhenAll(main.ReadAll()));
            }

            var loaded = await Task.WhenAll(tasks);
            var masterItems = loaded.Length == 3 ? loaded[2][0].ToHashSet() : new HashSet<Item>();
            var addItems = loaded[0];
            var removeItems = loaded[1];

            var added = addItems.Select(list => removeItems.Contains(list) ? list.Except(masterItems) : list).SelectMany(list => list);
            var removed = removeItems.SelectMany(list => masterItems.Except(list.ToHashSet()));

            await Sync(added, removed, secondary);
            await Sync(added, removed, main);

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

        private async Task Sync()
        {
            var changed = false;

            if (Sources.Skip(1).Any(source => source.LastUpdated > Sources[0].LastUpdated))
            {
                changed = true;
                await GetAsyncEnumerator().MoveNextAsync();
            }

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

        private const int PageSize = 20;

        private static async Task<List<T>> Buffer<T>(int? count, IAsyncEnumerable<T> source)
        {
            throw new NotImplementedException();
        }

        private async Task Buffer(int? count, Cached source)
        {
            try
            {
                for (int i = 0; !(i >= count) && await source.Itr.MoveNextAsync();)
                {
                    if (source.Cache.Add(source.Itr.Current))
                    {
                        source.Buffer.Add(source.Itr.Current);
                        i++;
                    }
                }
            }
            catch
            {
                source.Itr = null;
                throw;
            }
        }

        private async Task<IList<Item>> Except(Cached source, IEnumerable<Item> other)
        {
            List<Item> result = new List<Item>();

            foreach (var item in other)
            {
                if (source.List.AllowedTypes.HasFlag(item.ItemType) && !source.Cache.Contains(item))
                {
                    var contains = await source.List.ContainsAsync(item);

                    if (!contains.HasValue)
                    {
                        while (true)
                        {
                            try
                            {
                                int count = source.Buffer.Count;
                                await Buffer(PageSize, source);
                                var loaded = source.Buffer.Count - count;

                                if (source.Cache.Contains(item))
                                {
                                    contains = true;
                                }
                                else if (loaded < PageSize)
                                {
                                    contains = false;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            catch { }

                            break;
                        }
                    }

                    if (contains == false)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        private class Cached
        {
            public List List { get; set; }
            public IAsyncEnumerator<Item> Itr { get; set; }
            public List<Item> Buffer { get; set; }
            public HashSet<Item> Cache { get; set; }
        }

        private async IAsyncEnumerable<Item> GetItems(IList<List> sources, int size = 1)
        {
            if (sources.Count == 0)
            {
                yield break;
            }

            List main = sources[0];
            List<Cached> itrs = sources.Where(source => source != main && !(source.LastUpdated <= main.LastUpdated)).Prepend(main).Select(source =>
            {
                source.Reverse = Reverse;
                return new Cached
                {
                    List = source,
                    Itr = source.GetAsyncEnumerator(),
                    Buffer = new List<Item>(),
                    Cache = new HashSet<Item>()
                };
            }).ToList();
            List<Item> allAdditions = new List<Item>();

            if (itrs.Count == 1)
            {
                sources[0].Reverse = Reverse;
                await foreach (var item in sources[0])
                {
                    yield return item;
                }

                yield break;
            }

            var lazyMain = new Lazy<Task<List<Item>>>(() => AsyncEnumerable.ReadAll(main));

            foreach (var itr in itrs.Skip(1))
            {
                try
                {
                    if (main.LastUpdated.HasValue && itr.List.LastUpdated.HasValue)
                    {
                        await Task.WhenAll(lazyMain.Value, Buffer(null, itr));

                        foreach (var item in await Except(itr, lazyMain.Value.Result))
                        {
                            if (itrs[0].Cache.Add(item))
                            {
                                itrs[0].Buffer.Add(item);
                            }
                        }

                        //added = await itr.List.GetAdditionsAsync(main, false);
                        //removed = await main.GetAdditionsAsync(itr.List, false);
                    }
                    else
                    {
                        var added = await itr.List.GetAdditionsAsync(main);

                        foreach (var item in itr.List.Reverse ? added.Reverse<Item>() : added)
                        {
                            if (itr.Cache.Add(item))
                            {
                                itr.Buffer.Add(item);
                            }
                        }
                    }
                }
                catch { }
            }

            for (; ; itrs[0].Buffer.Clear())
            {
                await Buffer(size, itrs[0]);
                bool success = itrs[0].Buffer.Count >= size;

                var diffs = new (List<Item> Add, List<Item> Remove)[itrs.Count];

                for (int i = 1; i < itrs.Count; i++)
                {
                    int? count;
                    if (success)
                    {
                        count = PageSize * itrs[0].Buffer.Sum(item => itrs[i].Cache.Contains(item) ? 0 : 1);
                    }
                    else
                    {
                        count = null;
                    }

                    diffs[i] = (new List<Item>(), new List<Item>());
                    try
                    {
                        // Potentially buffer more stuff to avoid calls to List.ContainsAsync
                        await Buffer(count, itrs[i]);
                        // Find what was removed (may cause more things to be buffered)
                        diffs[i].Remove.AddRange(await Except(itrs[i], itrs[0].Buffer));

                        // Process additions from the buffer
                        while (itrs[i].Buffer.Count > 0)
                        {
                            var added = await Except(itrs[0], itrs[i].Buffer);
                            bool more = added.Count > 0 && itrs[i].Buffer.Count > 0 && added[added.Count - 1] == itrs[i].Buffer[itrs[i].Buffer.Count - 1];

                            diffs[i].Add.AddRange(added);
                            itrs[i].Buffer.Clear();

                            // If the last thing in the buffer is a new addition, check the next page - there may be a block that got split
                            if (more)
                            {
                                await Buffer(PageSize, itrs[i]);
                            }
                        }
                    }
                    catch { }
                }

                var empty = Enumerable.Empty<Item>();
                diffs[0] = (
                    diffs.Skip(1).SelectMany(diff => diff.Add).Except(empty).ToList(),
                    diffs.Skip(1).SelectMany(diff => diff.Remove).Except(empty).ToList());

                IEnumerable<Task> Updates(List list)
                {
                    IEnumerable<Item> add = diffs[0].Add;
                    IEnumerable<Item> remove = diffs[0].Remove;

                    int i = itrs.FindIndex(itr => itr.List == list);
                    if (i != -1)
                    {
                        add = add.Except(itrs[i].Cache);

                        if (list != main)
                        {
                            add = add.Except(diffs[i].Add);
                            remove = remove.Except(diffs[i].Remove);
                        }
                    }

                    yield return list.RemoveAsync(remove);
                    yield return list.AddAsync(list.Reverse ? add.Reverse() : add);
                }

                await Task.WhenAll(sources.Where(source => source != main).SelectMany(Updates));
                await Task.WhenAll(Updates(main));

                var items = itrs[0].Buffer.Except(diffs[0].Remove);
                if (Reverse)
                {
                    items = diffs[0].Add.Concat(items);
                }
                else
                {
                    allAdditions.AddRange(diffs[0].Add);
                }
                Count = sources[0].Count;

                foreach (var item in items)
                {
                    yield return item;
                }

                if (!success)
                {
                    foreach (var item in allAdditions)
                    {
                        yield return item;
                    }

                    break;
                }
            }
        }

        private class OrderedSet<T> : ICollection<T>
        {
            private List<T> List { get; }
            private HashSet<T> Cache { get; }

            public int Count => List.Count;

            public bool IsReadOnly => ((ICollection<T>)List).IsReadOnly;

            public OrderedSet()
            {
                List = new List<T>();
                Cache = new HashSet<T>();
            }

            public void Add(T item)
            {
                List.Add(item);
                Cache.Add(item);
            }

            public void Clear()
            {
                List.Clear();
                Cache.Clear();
            }

            public bool Contains(T item)
            {
                return ((ICollection<T>)List).Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                ((ICollection<T>)List).CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                return ((ICollection<T>)List).Remove(item);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>)List).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)List).GetEnumerator();
            }
        }


        private class LazyCollection<T> : IEnumerable<T>, IAsyncCollection<T>
        {
            public IAsyncEnumerable<T> Source { get; }

            private IAsyncEnumerator<T> Itr { get; }
            private ICollection<T> Collection { get; }

            public LazyCollection(IAsyncEnumerable<T> items) : this(items, new List<T>()) { }

            public LazyCollection(IAsyncEnumerable<T> items, ICollection<T> collection)
            {
                Source = items;
                Collection = collection;
                Itr = items.GetAsyncEnumerator();
            }

            public bool Contains(T item) => Collection.Contains(item);

            public Task<bool?> ContainsAsync(T item) => (Source as IAsyncCollection<T>)?.ContainsAsync(item);

            public async Task<List<T>> LoadMore(int? count = 1)
            {
                var buffer = new List<T>();

                for (int i = 0; !(i >= count) && await Itr.MoveNextAsync(); i++)
                {
                    buffer.Add(Itr.Current);
                    Collection.Add(Itr.Current);
                }

                return buffer;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => Source.GetAsyncEnumerator(cancellationToken);

            public IEnumerator<T> GetEnumerator()
            {
                return Collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)Collection).GetEnumerator();
            }
        }

        private static async Task<bool?> Contains<T>(IAsyncEnumerable<T> source, T item, Queue<T> buffer = null)
        {
            if (source is LazyCollection<T> lazy)
            {
                if (lazy.Contains(item))
                {
                    return true;
                }

                var buffered = await lazy.LoadMore(PageSize);

                if (buffer != null)
                {
                    foreach (var value in buffered)
                    {
                        buffer.Enqueue(value);
                    }
                }

                if (lazy.Contains(item))
                {
                    return true;
                }
            }

            try
            {
                return await (source as IAsyncCollection<T>)?.ContainsAsync(item);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<bool?> Contains<T>(IAsyncCollection<T> source, ICollection<T> batch, T item, Queue<T> buffer = null)
        {
            if (source is LazyCollection<T> lazy)
            {
                if (lazy.Contains(item))
                {
                    return true;
                }

                var buffered = await lazy.LoadMore(PageSize);

                if (buffer != null)
                {
                    foreach (var value in buffered)
                    {
                        buffer.Enqueue(value);
                    }
                }

                if (lazy.Contains(item))
                {
                    return true;
                }
            }

            try
            {
                return await (source as IAsyncCollection<T>)?.ContainsAsync(item);
            }
            catch
            {
                return null;
            }
        }

        private class FilteredList : IAsyncCollection<Item>
        {
            public List List { get; }
            public IAsyncEnumerator<Item> Items { get; }

            public FilteredList(List list)
            {
                List = list;
                Items = list.GetAsyncEnumerator();
            }

            public FilteredList(List list, FilterPredicate filter) : this(list)
            {
                Items = list.GetAsyncEnumerator(filter);
            }

            public Task<bool?> ContainsAsync(Item item) => List.ContainsAsync(item);

            public IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default) => Items;
        }

        public override IAsyncEnumerator<Item> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default) => GetItemsAsync(predicate, cancellationToken).GetAsyncEnumerator();

        public async IAsyncEnumerable<Item> GetItemsAsync(FilterPredicate predicate, CancellationToken cancellationToken = default)
        {
            await InitialSync;

            var masterSource = Sources.FirstOrDefault();
            var secondarySources = Sources.Where(source => source != masterSource && !HasNewerUpdates(masterSource, source));

            var filtered = secondarySources.Prepend(masterSource).Select(source => new LazyCollection<Item>(new FilteredList(source, predicate), new HashSet<Item>()));

            var master = filtered.FirstOrDefault();
            var secondary = filtered.Skip(1).ToList();

            Queue<Item> masterBuffer = new Queue<Item>();
            Queue<Item> batch;
            var defferedAdditions = new List<Item>();
            int? count = 1;

            do
            {
                batch = new Queue<Item>();

                for (int i = 0; !(i >= count) && masterBuffer.TryDequeue(out var item); i++)
                {
                    batch.Enqueue(item);
                }

                foreach (var item in await master.LoadMore(count - batch.Count))
                {
                    batch.Enqueue(item);
                }

                var add = new LinkedList<Item>();
                var remove = new LinkedList<Item>();

                if (batch.Count != count)
                {
                    //await Task.WhenAll(secondary.Select(list => list.LoadMore(null)));
                    await Task.WhenAll(secondary.Select(list => LoadFromRemote(master, list, add, masterBuffer, null)));
                }

                foreach (var item in batch)
                {
                    foreach (var collection in secondary)
                    {
                        bool contains;
                        int total;

                        do
                        {
                            contains = collection.Contains(item);
                            total = add.Count;

                            if (!contains)
                            {
                                await LoadFromRemote(master, collection, add, masterBuffer);
                            }
                        }
                        while (add.Count - total == PageSize);

                        if (!collection.Contains(item) && (await (collection.Source as IAsyncCollection<Item>)?.ContainsAsync(item) == false))
                        {
                            remove.Add(item);
                            break;
                        }
                    }
                }

                /*foreach (var item in batch)
                {
                    foreach (var collection in secondary)
                    {
                        var buffer = new Queue<Item>();
                        bool contains = await Contains(collection, item, buffer) != false;

                        while (buffer.TryDequeue(out var buffered))
                        {
                            if (!add.Contains(buffered) && await Contains(master, buffered) == false)
                            {
                                add.Enqueue(buffered);
                            }
                        }

                        if (!contains)
                        {
                            remove.Add(item);
                            break;
                        }
                    }
                }*/

                await Sync(add, remove, secondarySources);
                await Sync(add, remove, masterSource);

                var items = batch.ToList<Item>().Except(remove);

                if (Reverse)
                {
                    items = add.Concat(items);
                }
                else
                {
                    defferedAdditions.AddRange(add);
                }

                //Count = sources[0].Count;

                foreach (var item in items)
                {
                    yield return item;
                }
            }
            while (batch.Count == count);

            foreach (var item in defferedAdditions)
            {
                yield return item;
            }
        }

        private static async Task LoadMaster()
        {

        }

        private static async Task LoadFromRemote(LazyCollection<Item> master, LazyCollection<Item> remote, LinkedList<Item> add, Queue<Item> masterBuffer = null, int? pageSize = PageSize)
        {
            foreach (var item in await remote.LoadMore(pageSize))
            {
                if (!add.Contains(item) && !master.Contains(item))
                {
                    foreach (var buffered in await master.LoadMore(pageSize))
                    {
                        masterBuffer?.Enqueue(buffered);
                    }

                    if (!master.Contains(item) && (await (master.Source as IAsyncCollection<Item>)?.ContainsAsync(item) == false))
                    {
                        add.AddLast(item);
                    }
                }
            }
        }

        private static async Task GetAsyncEnumerator(Queue<Item> batch, LazyCollection<Item> master, List<LazyCollection<Item>> secondary)
        {
            var buffers = new List<Item>[secondary.Count];
            var add = new List<Item>();
            var remove = await Except(batch, secondary, buffers);

            foreach (var buffer in buffers)
            {
                var temp = new List<Item>[1];
                add.AddRange(await Except(buffer, temp, master));

                foreach (var item in temp[0])
                {
                    batch.Enqueue(item);
                }
            }
        }

        private static Task<List<Item>> Except(IEnumerable<Item> master, List<Item>[] buffers = null, params LazyCollection<Item>[] secondary) => Except(master, secondary, buffers);

        private static async Task<List<Item>> Except(IEnumerable<Item> master, IEnumerable<LazyCollection<Item>> secondary, List<Item>[] buffers = null)
        {
            var result = new List<Item>();
            //var removed = secondary.Select(list => master.Except(list));

            var itr = secondary.GetEnumerator();// removed.Where(items => items.Any()).GetEnumerator();

            for (int i = 0; itr.MoveNext(); i++)
            {
                var removed = master.Except(itr.Current);

                if (removed.Any())
                {
                    var buffer = await itr.Current.LoadMore(PageSize);

                    if (buffer.Count > 0)
                    {
                        removed = master.Except(itr.Current);
                    }

                    if (buffers != null && buffers.Length > i)
                    {
                        buffers[i] = buffer;
                    }
                }

                foreach (var item in removed)
                {
                    if (await itr.Current.ContainsAsync(item) == false)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        private static Task Sync(IEnumerable<Item> add, IEnumerable<Item> remove, params List[] sources) => Sync(add, remove, (IEnumerable<List>)sources);

        private static Task Sync(IEnumerable<Item> add, IEnumerable<Item> remove, IEnumerable<List> sources)
        {
            var updates = new List<Task>();

            foreach (var source in sources)
            {
                updates.Add(source.AddAsync(source.Reverse ? System.Linq.Enumerable.Reverse(add) : add));
                updates.Add(source.RemoveAsync(remove));
            }

            return Task.WhenAll(updates);
        }
    }
}