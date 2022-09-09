namespace MoviesTests
{
    public class TestList : List
    {
        public override string ID { get; }

        public List<Item> ItemList { get; }
        public int CallsToContains { get; private set; }

        public TestList(string id) : this(id, new List<Item>()) { }

        public TestList(string id, List<Item> itemList)
        {
            ID = id;
            ItemList = itemList;
            Items = AsyncEnumerable.ToAsyncEnumerable(Task.FromResult<IEnumerable<Item>>(ItemList.ToList<Item>()));
        }

        public override Task<int> CountAsync() => Task.FromResult(ItemList.Count);

        public override Task Delete() => Task.CompletedTask;

        public override Task Update() => Task.CompletedTask;

        protected override Task<bool> AddAsyncInternal(IEnumerable<Item> items)
        {
            ItemList.AddRange(items);
            return Task.FromResult(true);
        }

        protected override Task<bool?> ContainsAsyncInternal(Item item)
        {
            CallsToContains++;
            return Task.FromResult<bool?>(ItemList.Contains(item));
        }

        protected override Task<bool> RemoveAsyncInternal(IEnumerable<Item> items)
        {
            foreach (var item in items)
            {
                ItemList.Remove(item);
            }

            return Task.FromResult(true);
        }

        public override async Task<List<Item>> GetAdditionsAsync(List list)
        {
            var add = new List<Item>();

            foreach (var item in ItemList)
            {
                if (await list.ContainsAsync(item) == false)
                {
                    add.Add(item);
                }
            }

            return add;
        }
    }

    [TestClass]
    public class ListSyncTests
    {
        public static ID<int>.Key TestKey = new ID<int>.Key();
        public static ID<int> TestID = TestKey.ID;

        private IReadOnlyList<Item> EmptyList { get; } = new List<Item>();
        private IReadOnlyList<Item> SmallList { get; }
        private IReadOnlyList<Item> BigList { get; }
        private IReadOnlyList<Item> VeryBigList { get; }

        public ListSyncTests()
        {
            SmallList = Enumerable.Range(0, 5).Select(MovieFromId).ToList<Item>();
            BigList = Enumerable.Range(100, 100).Select(MovieFromId).ToList<Item>();
            VeryBigList = Enumerable.Range(1000, 1000).Select(MovieFromId).ToList<Item>();
        }

        private static Movie MovieFromId(int id) => new Movie(id.ToString()).WithID(TMDB.IDKey, id);

        [TestMethod]
        public async Task SingleList()
        {
            await AssertEqualAfterSync(SmallList, (SmallList, 0));
            await AssertEqualAfterSync(BigList, (BigList, 0));
        }

        [TestMethod]
        public async Task MultipleSmallListsNoChanges()
        {
            await AssertEqualAfterSync(SmallList, (SmallList, 0), (SmallList, 0));
            await AssertEqualAfterSync(SmallList, (SmallList, 0), (SmallList.Reverse(), 0));
            await AssertEqualAfterSync(SmallList, (SmallList, 0), (SmallList, 0), (SmallList, 0));
        }

        [TestMethod]
        public async Task MultipleSmallScrambledListsNoChanges()
        {
            var scrambledSmallList = SmallList.Skip(1).SkipLast(1).Prepend(SmallList.First()).Append(SmallList.Last());

            await AssertEqualAfterSync(SmallList, (SmallList, 0), (scrambledSmallList, 0));
            await AssertEqualAfterSync(SmallList, (SmallList, 0), (scrambledSmallList, 0), (SmallList.Reverse(), 0));
        }

        [TestMethod]
        public async Task MultipleLargeListsNoChanges()
        {
            await AssertEqualAfterSync(BigList, (BigList, 0), (BigList, 0));
            await AssertEqualAfterSync(BigList, BigList, BigList.Reverse());
            await AssertEqualAfterSync(BigList, (BigList, 0), (BigList, 0), (BigList, 0));
            await AssertEqualAfterSync(BigList, BigList, BigList.Reverse(), BigList);
        }

        [TestMethod]
        public async Task MultipleLargeScrambledListsNoChanges()
        {
            var start = BigList.Take(33);
            var middle = BigList.Skip(33).Take(33);
            var end = BigList.Skip(66);

            var chunkScrambledBigList = middle.Concat(start).Concat(end);
            var scrambledBigList = Scramble(new Random(12), BigList);

            await AssertEqualAfterSync(BigList, BigList, chunkScrambledBigList);
            await AssertEqualAfterSync(BigList, BigList, chunkScrambledBigList, end.Concat(start).Concat(middle));
            await AssertEqualAfterSync(BigList, BigList, chunkScrambledBigList, end.Concat(middle).Concat(start));
            await AssertEqualAfterSync(BigList, BigList, scrambledBigList);
        }

        private static IEnumerable<T> Scramble<T>(Random generator, IEnumerable<T> source)
        {
            var list = source.ToList();

            foreach (var _ in source)
            {
                int index = generator.Next(0, list.Count - 1);
                var item = list[index];
                list.RemoveAt(index);

                yield return item;
            }
        }

        [TestMethod]
        public async Task BasicRemovals()
        {
            var removeOrders = new List<IEnumerable<int>>
            {
                Enumerable.Range(0, SmallList.Count).Reverse(),
                Enumerable.Range(0, SmallList.Count),
                new List<int> { 1, 0, 3, 2, 4 }
            };

            foreach (var removeOrder in removeOrders)
            {
                var items = SmallList.ToList<Item>();

                foreach (var item in removeOrder.Select(index => SmallList[index]))
                {
                    items.Remove(item);

                    await AssertEqualAfterSync(items, (SmallList, 0), (items, 0));
                    //await AssertEqualAfterSync(items, ": remove order " + string.Join(',', removeOrder), SmallList, items);
                }
            }
        }

        [TestMethod]
        public async Task MultipleListsSeparateRemovals()
        {
            var remove = new List<Item> { SmallList[1], SmallList.Last() };

            await AssertEqualAfterSync(EmptyList, (SmallList, 0), (SmallList.Take(2), 0), (SmallList.Skip(2), 0));
            await AssertEqualAfterSync(SmallList.Except(remove), (SmallList, 0), (SmallList.Except(remove.Take(1)), 0), (SmallList.Except(remove.Skip(1)), 0));
            await AssertEqualAfterSync(EmptyList, (SmallList, 0), (SmallList, 0), (EmptyList, 0));
        }

        [TestMethod]
        public async Task MultipleListsOverlappingRemovals()
        {
            var remove = new List<Item> { SmallList[1], SmallList.Last() };

            await AssertEqualAfterSync(EmptyList, (SmallList, 0), (SmallList.Skip(3), 0), (SmallList.Take(2), 0));
            await AssertEqualAfterSync(EmptyList, (SmallList, 0), (EmptyList, 0), (EmptyList, 0));
            await AssertEqualAfterSync(SmallList.Except(remove), (SmallList, 0), (SmallList.Except(remove), 0), (SmallList.Except(remove), 0));
            await AssertEqualAfterSync(SmallList.Take(1).Append(SmallList.Last()), (SmallList, 0), (SmallList.Except(SmallList.Skip(1).Take(2)), 0), (SmallList.Except(SmallList.Skip(2).Take(2)), 0));
        }

        [TestMethod]
        public async Task Removals()
        {
            var bigList = BigList.ToList<Item>();

            // Random removals throughout the list
            bigList.RemoveAt(97);
            bigList.RemoveAt(12);
            bigList.RemoveAt(0);
            bigList.Remove(bigList.Last());
            bigList.RemoveAt(45);
            await AssertEqualAfterSync(bigList, BigList, bigList);

            // Random chunks of removals
            bigList.RemoveRange(34, 4);
            bigList.RemoveRange(0, 2);
            await AssertEqualAfterSync(bigList, BigList, bigList);

            // Remove multiple pages from end
            bigList = bigList.Take(66).ToList();
            await AssertEqualAfterSync(bigList, BigList, bigList);

            // Remove multiple pages from beginning
            bigList = bigList.Skip(33).ToList();
            await AssertEqualAfterSync(bigList, BigList, bigList);

            // Clear list
            bigList.Clear();
            await AssertEqualAfterSync(bigList, BigList, bigList);
        }

        [TestMethod]
        public async Task RandomStressTestRemovals()
        {
            var random = new Random(34);
            var lists = Enumerable.Range(0, 5).Select(i => VeryBigList.ToList<Item>()).ToList();
            var removed = new HashSet<Item>();
            return;
            while (!lists.Any(list => list.Count < 100))
            {
                foreach (var list in lists)
                {
                    int count = random.Next(1, 100);
                    int index = random.Next(0, list.Count - 1 - count);
                    
                    for (int i = 0; i < count; i++)
                    {
                        removed.Add(list[index + i]);
                    }
                    list.RemoveRange(index, count);
                }

                await AssertEqualAfterSync(VeryBigList.Except(removed), lists.Prepend(VeryBigList).ToArray());
            }
        }

        [TestMethod]
        public async Task BasicAdditions()
        {
            var addOrders = new List<IEnumerable<int>>
            {
                Enumerable.Range(0, SmallList.Count),
                Enumerable.Range(0, SmallList.Count).Reverse(),
                new List<int> { 1, 0, 3, 2, 4 }
            };

            foreach (var addOrder in addOrders)
            {
                var items = new List<Item>();

                foreach (var item in addOrder.Select(index => SmallList[index]))
                {
                    items.Add(item);

                    await AssertEqualAfterSync(items, (EmptyList, 0), (items, 0));
                }
            }
        }

        [TestMethod]
        public async Task MultipleListsSeparateAdditions()
        {
            var firstAndLast = SmallList.Take(1).Append(SmallList.Last());

            await AssertEqualAfterSync(SmallList, (EmptyList, 0), (SmallList.Take(2), 0), (SmallList.Skip(2), 0));
            await AssertEqualAfterSync(SmallList.Except(firstAndLast).Concat(firstAndLast), (SmallList.Except(firstAndLast), 0), (SmallList.Except(firstAndLast.Skip(1)), 0), (SmallList.Except(firstAndLast.Take(1)), 0));
            await AssertEqualAfterSync(SmallList, (EmptyList, 0), (SmallList, 0), (EmptyList, 0));
        }

        [TestMethod]
        public async Task MultipleListsDuplicateAdditions()
        {
            var firstAndLast = SmallList.Take(1).Append(SmallList.Last());

            await AssertEqualAfterSync(SmallList, (EmptyList, 0), (SmallList.Take(3), 0), (SmallList.Skip(2), 0));
            await AssertEqualAfterSync(SmallList, (EmptyList, 0), (SmallList, 0), (SmallList, 0));
            await AssertEqualAfterSync(SmallList.Except(firstAndLast).Concat(firstAndLast),
                (SmallList.Except(firstAndLast), 0),
                (SmallList, 0),
                (SmallList, 0));
            await AssertEqualAfterSync(firstAndLast.Concat(SmallList.Except(firstAndLast)),
                (firstAndLast, 0),
                (SmallList.Take(3).Append(SmallList.Last()), 0),
                (SmallList.TakeLast(3).Prepend(SmallList.First()), 0));
        }

        [TestMethod]
        public async Task Additions()
        {
            var bigList = BigList.ToList<Item>();

            // Random additions throughout the list
            bigList.Insert(0, SmallList[0]);
            bigList.Insert(12, SmallList[1]);
            bigList.Insert(45, SmallList[2]);
            bigList.Insert(97, SmallList[3]);
            bigList.Add(SmallList[4]);
            await AssertEqualAfterSync(BigList.Concat(SmallList), BigList, bigList);

            // Random chunks of additions
            bigList = BigList.ToList<Item>();
            bigList.InsertRange(0, SmallList.Take(3));
            bigList.InsertRange(34, SmallList.Skip(3));
            await AssertEqualAfterSync(BigList.Concat(SmallList), BigList, bigList);

            // Insert multiple pages beginning, middle, and end
            bigList = BigList.ToList<Item>();
            var insertPoints = new int?[] { 0, 78, null };
            for (int i = 0; i < insertPoints.Length; i++)
            {
                var index = insertPoints[i] ?? bigList.Count;
                bigList.InsertRange(index, VeryBigList.Skip(i * 50).Take(50));

                await AssertEqualAfterSync(BigList.Concat(VeryBigList.Take((i + 1) * 50)), BigList, bigList);
            }

            // Add new list
            await AssertEqualAfterSync(BigList, EmptyList, BigList);
        }

        [TestMethod]
        public async Task RandomStressTestAdditions()
        {
            var random = new Random(34);
            var lists = Enumerable.Range(0, 1).Select(i => VeryBigList.ToList<Item>()).ToList();
            var ans = VeryBigList.ToList<Item>();
            return;
            while (!lists.Any(list => list.Count < 100))
            {
                foreach (var list in lists)
                {
                    int count = random.Next(0, 100);
                    int index = random.Next(0, list.Count - 1 - count);

                    list.GetRange(index, count).ForEach(item => ans.Remove(item));
                    list.RemoveRange(index, count);
                }

                await AssertEqualAfterSync(ans, lists.Prepend(VeryBigList).ToArray());
            }
        }

        private Task AssertEqualAfterSync(IEnumerable<Item> original, params IEnumerable<Item>[] sync) => AssertEqualAfterSync(original, sync.Select(list => (list, (int?)null)), null, string.Empty);
        private Task AssertEqualAfterSync(IEnumerable<Item> original, params (IEnumerable<Item>, int?)[] sync) => AssertEqualAfterSync(original, sync, null, string.Empty);
        //private Task AssertEqualAfterSync(IEnumerable<Item> original, bool? reverse, params IEnumerable<Item>[] sync) => AssertEqualAfterSync(original, sync, reverse, null);
        //private Task AssertEqualAfterSync(IEnumerable<Item> original, string message, params IEnumerable<Item>[] sync) => AssertEqualAfterSync(original, sync, null, message);
        //private Task AssertEqualAfterSync(IEnumerable<Item> original, bool? reverse, string message, params IEnumerable<Item>[] sync) => AssertEqualAfterSync(original, sync, reverse, message);

        private Task AssertEqualAfterSync(IEnumerable<Item> original, IEnumerable<(IEnumerable<Item>, int?)> sync, bool? reverse = null, string message = null)
        {
            var lists = sync.Select(temp => temp.Item1);
            var calls = sync.Select(temp => temp.Item2);

            if (reverse.HasValue)
            {
                return AssertEqualAfterSync(original, lists, calls, reverse.Value, true, message);
            }
            else
            {
                return Task.WhenAll(
                    AssertEqualAfterSync(original, lists, calls, false, true, message),
                    AssertEqualAfterSync(original, lists, calls, false, false, message),
                    AssertEqualAfterSync(original, lists, calls, true, true, message),
                    AssertEqualAfterSync(original, lists, calls, true, false, message));
            }
        }

        private enum SyncOptions
        {
            Forward = 1,
            Backward = 2,
            UpToDate = 4,
            UnresolvedChanges = 8,
            BothDirection = Forward | Backward,
            UpToDateAndNot = UpToDate | UnresolvedChanges,
            All = ~0
        }

        private async Task AssertEqualAfterSync(IEnumerable<Item> expected, IEnumerable<IEnumerable<Item>> sync, IEnumerable<int?> callsToContains, bool reverse = false, bool upToDate = true, string message = null)
        {
            var now = DateTime.Now;
            var lists = sync.Select(items => new TestList(string.Empty, items.ToList())
            {
                LastUpdated = now
            }).ToList();

            if (upToDate)
            {
                lists[0].LastUpdated = null;
            }
            else
            {
                lists[0].LastUpdated = DateTime.Now.AddDays(-1);
            }

            var actual = await new SyncList(lists).ReadAll();
            var info = $"Synced {(reverse ? "Backward" : "Forward")}, {(upToDate ? "Up to date" : "Not up to date")}";
            //Assert.IsTrue(original.SequenceEqual(synced), (reverse ? "Reverse" : "Regular") + message);
            Assert.IsTrue(expected.SequenceEqual(actual), $"\n{string.Join("\n\t", sync.Select(list => "[" + string.Join(',', list) + "]").Prepend(info))}\nExpected: {string.Join(',', expected)}\nActual: {string.Join(',', actual)}");

            var itr = callsToContains.GetEnumerator();
            itr?.MoveNext();

            for (int i = 1; itr?.MoveNext() == true; i++)
            {
                if (itr.Current.HasValue)
                {
                    Assert.AreEqual(itr.Current.Value, lists[i].CallsToContains, reverse.ToString());
                }
                else if (lists[i].CallsToContains > Math.Ceiling(lists[i].ItemList.Count / 20d))
                {
                    Assert.Inconclusive($"{lists[i].CallsToContains} calls to contains method for a list with {lists[i].ItemList.Count} items");
                }
            }
        }
    }
}