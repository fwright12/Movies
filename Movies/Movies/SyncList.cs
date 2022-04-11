﻿using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

        public SyncList(IEnumerable<List> sources)
        {
            Sources = new List<List>(sources);
            Items = GetItems(Sources);
            Count = Sources.FirstOrDefault()?.Count;

            if (SyncDetails())
            {
                _ = Update();
            }
        }

        public bool SyncDetails()
        {
            var changed = false;
            var values = new object[Properties.Count];//.Select(property => property.GetValue(main)).ToArray();

            for (int i = 0; i < Properties.Count; i++)
            {
                var property = Properties[i];

                foreach (var list in Sources)
                {
                    var other = property.GetValue(list);

                    if (!Equals(values[i], other))
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

            return changed;
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
            var buffer = await Task.WhenAll(System.Linq.Async.Enumerable.ReadAll(this), System.Linq.Async.Enumerable.ReadAll(list));

            if (Reverse)
            {
                buffer[0].Reverse();
            }
            if (list.Reverse)
            {
                buffer[1].Reverse();
            }

            await Task.WhenAll(AddAsync(buffer[1]), list.AddAsync(buffer[0]), Update(list));

            Sources.Add(list);
        }

        public void RemoveSource(List list)
        {
            Sources.Remove(list);
        }

        protected override async Task<bool> AddAsyncInternal(IEnumerable<Item> items) => !(await Task.WhenAll(Sources.Select(source => source.AddAsync(items)))).Contains(false);

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

                if (!contains.HasValue)
                {
                    return null;
                }
                else if (!contains.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public override Task<int> CountAsync() => Sources.FirstOrDefault()?.CountAsync() ?? Task.FromResult(0);

        public override Task Delete() => Task.WhenAll(Sources.Select(source => source.Delete()));

        protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Item> items) => !(await Task.WhenAll(Sources.Select(source => source.RemoveAsync(items)))).Contains(false);

        public override Task Update() => Update(Sources);

        private Task Update(params List[] lists) => Update((IEnumerable<List>)lists);
        private Task Update(IEnumerable<List> lists)
        {
            foreach (var list in lists)
            {
                foreach (var property in Properties)
                {
                    property.SetValue(list, property.GetValue(this));
                }

                AllowedTypes |= list.AllowedTypes;
            }

            //Author = string.Join(", ", lists.Select(list => list.Author).Prepend(Author).Where(author => !string.IsNullOrEmpty(author)));

            return Task.WhenAll(lists.Select(list => list.Update()));
        }

        public void DiscardEdits()
        {
            CopyChanges(Properties, Sources[0], this);
        }

        private const int PageSize = 20;

        private async Task Buffer(int count, Cached source)
        {
            try
            {
                for (int i = 0; i < count && await source.Itr.MoveNextAsync(); i++)
                {
                    source.Buffer.Add(source.Itr.Current);
                    source.Cache.Add(source.Itr.Current);
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
            if (sources.Count <= 1)
            {
                if (sources.Count == 1)
                {
                    sources[0].Reverse = Reverse;
                    await foreach (var item in sources[0])
                    {
                        yield return item;
                    }
                }

                yield break;
            }

            List<Cached> itrs = sources.Select(source =>
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

            while (true)
            {
                itrs[0].Buffer.Clear();
                await Buffer(size, itrs[0]);
                bool success = itrs[0].Buffer.Count == size;

                var diffs = new (List<Item> Add, List<Item> Remove)[sources.Count];

                for (int i = 1; i < sources.Count; i++)
                {
                    int count;
                    if (success)
                    {
                        count = PageSize * itrs[0].Buffer.Sum(item => itrs[i].Cache.Contains(item) ? 0 : 1);
                    }
                    else
                    {
                        count = int.MaxValue;
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
                            var additions = await Except(itrs[0], itrs[i].Buffer);
                            bool more = additions.Count > 0 && itrs[i].Buffer.Count > 0 && additions[additions.Count - 1] == itrs[i].Buffer[itrs[i].Buffer.Count - 1];

                            diffs[i].Add.AddRange(additions);
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

                var updates = new List<Task>();

                for (int i = 0; i < diffs.Length; i++)
                {
                    var add = diffs[0].Add.Except(itrs[i].Cache);
                    IEnumerable<Item> remove = diffs[0].Remove;

                    if (i > 0)
                    {
                        add = add.Except(diffs[i].Add);
                        remove = remove.Except(diffs[i].Remove);
                    }

                    updates.Add(sources[i].RemoveAsync(remove));
                    updates.Add(sources[i].AddAsync(itrs[i].List.Reverse ? add.Reverse() : add));
                }

                await Task.WhenAll(updates);

                //Count = sources[0].Count;
                var items = itrs[0].Buffer.Except(diffs[0].Remove);
                if (Reverse)
                {
                    items = diffs[0].Add.Concat(items);
                }
                else
                {
                    allAdditions.AddRange(diffs[0].Add);
                }

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
    }
}