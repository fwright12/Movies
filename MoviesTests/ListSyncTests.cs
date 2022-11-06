namespace MoviesTests
{
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
            SmallList = Enumerable.Range(0, 5).Select(Helpers.ToMovie).ToList<Item>();
            BigList = Enumerable.Range(100, 100).Select(Helpers.ToMovie).ToList<Item>();
            VeryBigList = Enumerable.Range(1000, 1000).Select(Helpers.ToMovie).ToList<Item>();
        }

        [TestMethod]
        public async Task NoLists()
        {
            Assert.AreEqual(0, (await new SyncList(Enumerable.Empty<List>()).ReadAll()).Count());
        }

        [TestMethod]
        public async Task SingleList()
        {
            await AssertEqualAfterSync(SmallList, SmallList);
            await AssertEqualAfterSync(BigList, BigList);
        }

        [TestMethod]
        public async Task MultipleSmallListsNoChanges()
        {
            await AssertEqualAfterSync(SmallList, SmallList, SmallList);
            await AssertEqualAfterSync(SmallList, SmallList, SmallList.Reverse());
            await AssertEqualAfterSync(SmallList, SmallList, SmallList, SmallList);
        }

        [TestMethod]
        public async Task MultipleSmallScrambledListsNoChanges()
        {
            var scrambledSmallList = (ItemList)"41230";

            await AssertEqualAfterSync(SmallList, SmallList, scrambledSmallList);
            await AssertEqualAfterSync(SmallList, SmallList, scrambledSmallList, SmallList.Reverse());
        }

        [TestMethod]
        public async Task MultipleLargeListsNoChanges()
        {
            await AssertEqualAfterSync(BigList, BigList, BigList);
            await AssertEqualAfterSync(BigList, BigList, BigList.Reverse());
            await AssertEqualAfterSync(BigList, BigList, BigList, BigList);
            await AssertEqualAfterSync(BigList, BigList, BigList.Reverse(), BigList);
        }

        [TestMethod]
        public async Task MultipleLargeScrambledListsNoChanges()
        {
            var start = BigList.Take(33);
            var middle = BigList.Skip(33).Take(33);
            var end = BigList.Skip(66);

            var chunkScrambledBigList = middle.Concat(start).Concat(end);
            var scrambledBigList = BigList.Scramble(new Random(12));

            await AssertEqualAfterSync(BigList, BigList, chunkScrambledBigList);
            await AssertEqualAfterSync(BigList, BigList, chunkScrambledBigList, end.Concat(start).Concat(middle));
            await AssertEqualAfterSync(BigList, BigList, chunkScrambledBigList, end.Concat(middle).Concat(start));
            await AssertEqualAfterSync(BigList, BigList, scrambledBigList);
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

                    await AssertEqualAfterSync(items, SmallList, items);
                    //await AssertEqualAfterSync(items, ": remove order " + string.Join(',', removeOrder), SmallList, items);
                }
            }
        }

        [TestMethod]
        public async Task MultipleListsSeparateRemovals()
        {
            var remove = new List<Item> { SmallList[1], SmallList.Last() };

            await AssertEqualAfterSync(EmptyList, SmallList, SmallList.Take(2), SmallList.Skip(2));
            await AssertEqualAfterSync(SmallList.Except(remove), SmallList, SmallList.Except(remove.Take(1)), SmallList.Except(remove.Skip(1)));
            await AssertEqualAfterSync(EmptyList, SmallList, SmallList, EmptyList);
        }

        [TestMethod]
        public async Task MultipleListsOverlappingRemovals()
        {
            var remove = new List<Item> { SmallList[1], SmallList.Last() };

            await AssertEqualAfterSync(EmptyList, SmallList, SmallList.Skip(3), SmallList.Take(2));
            await AssertEqualAfterSync(EmptyList, SmallList, EmptyList, EmptyList);
            await AssertEqualAfterSync(SmallList.Except(remove), SmallList, SmallList.Except(remove), SmallList.Except(remove));
            await AssertEqualAfterSync(SmallList.Take(1).Append(SmallList.Last()), SmallList, SmallList.Except(SmallList.Skip(1).Take(2)), SmallList.Except(SmallList.Skip(2).Take(2)));
        }

        [TestMethod]
        public async Task MultipleLargeScrambledListsRemovals()
        {
            var scrambledBigList = BigList.Scramble(new Random(21));

            await AssertEqualAfterSync(BigList.Skip(1), BigList, scrambledBigList, BigList.Skip(1), BigList.Skip(1).Append(BigList.First()));
            await AssertEqualAfterSync(BigList.Skip(33), BigList, scrambledBigList, BigList.Skip(33), BigList.Skip(33).Concat(BigList.Take(33)));
        }

        [TestMethod]
        public async Task Removals()
        {
            var bigList = BigList.ToList<Item>();
            var random = new Random(320);
            var sync = new List<IEnumerable<Item>> { BigList, BigList.Reverse().ToList(), bigList, BigList.Scramble(random).ToList() };

            // Random removals throughout the list
            bigList.RemoveAt(97);
            bigList.RemoveAt(12);
            bigList.RemoveAt(0);
            bigList.Remove(bigList.Last());
            bigList.RemoveAt(45);
            await AssertEqualAfterSync(bigList, sync);

            // Random chunks of removals
            bigList.RemoveRange(34, 4);
            bigList.RemoveRange(0, 2);
            await AssertEqualAfterSync(bigList, sync);

            // Remove multiple pages from end
            //bigList = bigList.Take(66).ToList();
            bigList.RemoveRange(66, bigList.Count - 66);
            await AssertEqualAfterSync(bigList, sync);

            // Remove multiple pages from beginning
            //bigList = bigList.Skip(33).ToList();
            bigList.RemoveRange(0, 33);
            await AssertEqualAfterSync(bigList, sync);

            // Clear list
            bigList.Clear();
            await AssertEqualAfterSync(bigList, sync);
        }

        [TestMethod]
        public Task RandomStressTestRemovals() => RandomChanges(new Random(34), VeryBigList, false, true);

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

                    await AssertEqualAfterSyncAdditions(items, items.Reverse<Item>(), new List<IEnumerable<Item>> { EmptyList, items });
                }
            }
        }

        private IEnumerable<IEnumerable<Item>> ListsFromIds(params string[] ids) => ids.Select(id => (ItemList)id);

        [TestMethod]
        public async Task MultipleListsSeparateAdditions()
        {
            await AssertEqualAfterSyncAdditions(SmallList, (ItemList)"43210", ListsFromIds("", "01", "234"));

            var sync = ListsFromIds("123", "0123", "1234");
            await AssertEqualAfterSync((ItemList)"12304", sync);
            await AssertEqualAfterSync((ItemList)"04321", sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync((ItemList)"40321", sync, reverse: true, upToDate: false);

            await AssertEqualAfterSyncAdditions(SmallList, SmallList.Reverse(), ListsFromIds("", "01234", ""));

            sync = ListsFromIds("01", "0231", "401");
            await AssertEqualAfterSync((ItemList)"01423", sync, reverse: false, upToDate: null);
            await AssertEqualAfterSync(SmallList, sync, reverse: false, upToDate: false);
            await AssertEqualAfterSync((ItemList)"32410", sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync(SmallList.Reverse(), sync, reverse: true, upToDate: false);
        }

        [TestMethod]
        public async Task MultipleListsDuplicateAdditions()
        {
            await AssertEqualAfterSyncAdditions(SmallList, SmallList.Reverse(), ListsFromIds("", "012", "234"));

            await AssertEqualAfterSyncAdditions(SmallList, SmallList.Reverse(), new List<IEnumerable<Item>> { EmptyList, SmallList, SmallList });

            var sync = ListsFromIds("123", "01234", "01234");
            await AssertEqualAfterSync((ItemList)"12304", sync);
            await AssertEqualAfterSync((ItemList)"04321", sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync((ItemList)"40321", sync, reverse: true, upToDate: false);

            sync = ListsFromIds("01", "02341", "30142");
            await AssertEqualAfterSync((ItemList)"01324", sync, reverse: false, upToDate: null);
            await AssertEqualAfterSync(SmallList, sync, reverse: false, upToDate: false);
            await AssertEqualAfterSync((ItemList)"32410", sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync(SmallList.Reverse(), sync, reverse: true, upToDate: false);
        }

        [TestMethod]
        public async Task MultipleLargeScrambledListsAdditions()
        {
            var scrambledBigList = BigList.Scramble(new Random(72));

            var sync = new List<IEnumerable<Item>> { BigList, scrambledBigList, BigList.Prepend(SmallList.First()) };
            await AssertEqualAfterSync(BigList.Concat(SmallList.Take(1)), sync);
            await AssertEqualAfterSync(BigList.Reverse().Concat(SmallList.Take(1)), sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync(SmallList.Take(1).Concat(BigList.Reverse()), sync, reverse: true, upToDate: false);

            sync = new List<IEnumerable<Item>> { BigList, scrambledBigList, VeryBigList.Take(33).Concat(BigList) };
            await AssertEqualAfterSync(BigList.Concat(VeryBigList.Take(33)), sync);
            await AssertEqualAfterSync(BigList.Reverse().Concat(VeryBigList.Take(33).Reverse()), sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync(VeryBigList.Take(33).Reverse().Concat(BigList.Reverse()), sync, reverse: true, upToDate: false);
        }

        [TestMethod]
        public async Task Additions()
        {
            var bigList = BigList.ToList<Item>();
            var random = new Random(562);
            var sync = new List<IEnumerable<Item>> { BigList, BigList.Reverse().ToList(), bigList, BigList.Scramble(random).ToList() };

            // Random additions throughout the list
            bigList.Insert(45, SmallList[2]);
            await AssertEqualAfterSync(BigList.Reverse().Take(40).Append(SmallList[2]).Concat(BigList.Take(60).Reverse()), sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync(BigList.Append(SmallList[2]).Reverse(), sync, reverse: true, upToDate: false);

            bigList.Insert(0, SmallList[0]);
            bigList.Insert(12, SmallList[1]);
            bigList.Insert(97, SmallList[3]);
            bigList.Add(SmallList[4]);
            await AssertEqualAfterSync(BigList.Concat(SmallList), sync);

            // Random chunks of additions
            bigList.Clear();
            bigList.AddRange(BigList);
            bigList.InsertRange(0, SmallList.Take(3));
            bigList.InsertRange(34, SmallList.Skip(3));
            await AssertEqualAfterSync(BigList.Concat(SmallList), sync);

            // Insert multiple pages beginning, middle, and end
            //bigList = BigList.ToList<Item>();
            bigList.Clear();
            bigList.AddRange(BigList);

            var insertPoints = new int?[] { 0, 78, null };
            for (int i = 0; i < insertPoints.Length; i++)
            {
                var index = insertPoints[i] ?? bigList.Count;
                bigList.InsertRange(index, VeryBigList.Skip(i * 50).Take(50));

                var added = VeryBigList.Take((i + 1) * 50);
                await AssertEqualAfterSync(BigList.Concat(added), sync);
            }

            // Add new list
            sync.Remove(bigList);
            await AssertEqualAfterSync(BigList, sync.Prepend(EmptyList));
        }

        [TestMethod]
        public Task RandomStressTestAdditions() => RandomChanges(new Random(93), BigList, true, false);

        //[TestMethod]
        public async Task AdditionsAndRemovals()
        {

        }

        [TestMethod]
        public Task RandomStressTests() => RandomChanges(new Random(591), Enumerable.Range(2000, 1000).Select(Helpers.ToMovie).ToList<Item>(), true, true);

        public async Task RandomChanges(Random random, IEnumerable<Item> startList, bool add = true, bool remove = true)
        {
            var lists = Enumerable.Range(0, 5).Select(i => startList.ToList()).ToList();
            var ans = startList.ToList();

            //while (ans.Count / VeryBigList.Count < 0.9)
            for (int i = 0; i < 10; i++)
            {
                var added = new HashSet<Item>();
                var removed = new HashSet<Item>();

                if (remove)
                {
                    foreach (var list in lists)
                    {
                        var removals = Helpers.RandomChunks(list, random).SelectMany(items => items).ToList();

                        foreach (var item in removals)
                        {
                            list.Remove(item);
                            removed.Add(item);
                        }
                    }
                }

                if (add)
                {
                    foreach (var list in lists)
                    {
                        foreach (var additions in Helpers.RandomChunks(VeryBigList.Concat(startList.Except(list.ToHashSet())).Except(removed).ToList().Scramble(random).ToList(), random))
                        {
                            list.InsertRange(random.Next(list.Count), additions);
                            added.AddRange(additions);
                        }

                        var temp = list.Distinct().ToList();
                        list.Clear();
                        list.AddRange(temp);
                    }
                }

                if (removed.Overlaps(added))
                {
                    //Assert.Fail($"Bad test case\n{PrintList(removed)}\n{PrintList(added)}");
                    throw new Exception($"Bad test case\n{removed.Intersect(added).PrettyPrint()}");
                }

                var all = lists.Prepend(ans).ToArray();
                // Too hard to predict order for added items
                await AssertEqualAfterSync(!add ? ans.Except(removed).ToList() : null, all);

                foreach (var list in all)
                {
                    var set = list.ToHashSet();

                    foreach (var item in removed)
                    {
                        if (set.Contains(item))
                        {
                            list.Remove(item);
                        }
                    }

                    foreach (var item in added)
                    {
                        if (!set.Contains(item))
                        {
                            list.Add(item);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public async Task TMDbAndTrakt()
        {
            var initial = new List<Item>((ItemList)"123");
            List<Item> localItems = new List<Item>(initial), tmdbItems = new List<Item>(initial), traktItems = new List<Item>(initial);
            var reverse = true;

            var date = DateTime.Now;

            async Task<(SyncChecklist Local, SyncChecklist TMDb, SyncChecklist Trakt)> AssertEqualAfterSync(IEnumerable<Item> expected, bool checkLocal = true, bool checkTMDb = true, bool checkTrakt = true)
            {
                var localTest = new SyncChecklist(new TestList("local", localItems) { LastUpdated = date }, expected);
                var tmdbTest = new SyncChecklist(new TestList("tmdb", tmdbItems) { LastUpdated = null }, expected);
                var traktTest = new SyncChecklist(new TestList("trakt", traktItems) { LastUpdated = date.AddDays(localItems.ToHashSet().SetEquals(traktItems) ? -1 : 1) }, expected);

                await this.AssertEqualAfterSync(expected, new List<TestList> { localTest.List, traktTest.List, tmdbTest.List }, reverse);

                localItems = localTest.List.ItemList.ToList<Item>();
                tmdbItems = tmdbTest.List.ItemList.ToList<Item>();
                traktItems = traktTest.List.ItemList.ToList<Item>();

                if (checkLocal) localTest.AssertAll();
                if (checkTMDb) tmdbTest.AssertAll();
                if (checkLocal) localTest.AssertAll();

                return (localTest, tmdbTest, traktTest);
            }

            var result = await AssertEqualAfterSync(initial.Reverse<Item>());

            // Add only to tmdb
            tmdbItems.AddRange((ItemList)"4567");
            result = await AssertEqualAfterSync((ItemList)"7654321");
            result.TMDb.AssertAll(addCalls: 0, removeCalls: 0);
            result.Trakt.AssertAll(addCalls: 1, removeCalls: 0);

            // Remove non contiguous items
            tmdbItems.RemoveAll(item => ((ItemList)"64").Contains(item));
            result = await AssertEqualAfterSync((ItemList)"75321");
            result.TMDb.AssertAll(addCalls: 0, removeCalls: 0);
            result.Trakt.AssertAll(addCalls: 0, removeCalls: 2);

            // Remove remaining added items
            tmdbItems.RemoveAll(item => ((ItemList)"75").Contains(item));
            result = await AssertEqualAfterSync((ItemList)"321");
            result.TMDb.AssertAll(addCalls: 0, removeCalls: 0);
            result.Trakt.AssertAll(addCalls: 0, removeCalls: 1);

            // Separate additions
            traktItems.AddRange((ItemList)"45");
            tmdbItems.AddRange((ItemList)"67");
            result = await AssertEqualAfterSync((ItemList)"7654321");
            result.TMDb.AssertAll(addCalls: 1, removeCalls: 0);
            result.Trakt.AssertAll(addCalls: 1, removeCalls: 0);

            // Separate removals
            traktItems.RemoveAll(item => ((ItemList)"67").Contains(item));
            tmdbItems.RemoveAll(item => ((ItemList)"45").Contains(item));
            result = await AssertEqualAfterSync((ItemList)"321");
            result.TMDb.AssertAll(addCalls: 0, removeCalls: 1);
            result.Trakt.AssertAll(addCalls: 0, removeCalls: 1);

            // Duplicate additions
            tmdbItems.AddRange((ItemList)"45");
            traktItems.AddRange((ItemList)"45");
            result = await AssertEqualAfterSync((ItemList)"54321");
            result.TMDb.AssertAll(addCalls: 0, removeCalls: 0);
            result.Trakt.AssertAll(addCalls: 0, removeCalls: 0);

            // Duplicate removals
            tmdbItems.RemoveAll(item => ((ItemList)"45").Contains(item));
            traktItems.RemoveAll(item => ((ItemList)"45").Contains(item));
            result = await AssertEqualAfterSync((ItemList)"321", checkTMDb: false);
            // TMDb needs extra removes here because removals from trakt are processed during SyncList creation, then pushed to tmdb without checking for contains (not worth it)
            result.TMDb.AssertAll(numRemoved: 2, addCalls: 0, removeCalls: 1);
            result.Trakt.AssertAll(addCalls: 0, removeCalls: 0);
        }

        private Task AssertEqualAfterSync(IEnumerable<Item> expected, params IEnumerable<Item>[] sync) => AssertEqualAfterSync(expected, sync, null, string.Empty);

        private async Task AssertEqualAfterSyncAdditions(IEnumerable<Item> forward, IEnumerable<Item> backward, IEnumerable<IEnumerable<Item>> sync)
        {
            var first = Enumerable.Repeat(sync.First(), sync.Count());
            await AssertEqualAfterSync(forward, sync, reverse: false, upToDate: false);
            await AssertEqualAfterSync(forward, sync, reverse: false, upToDate: null);
            await AssertEqualAfterSync(sync.First(), first, reverse: false, upToDate: true);
            await AssertEqualAfterSync(backward, sync, reverse: true, upToDate: false);
            await AssertEqualAfterSync(backward, sync, reverse: true, upToDate: null);
            await AssertEqualAfterSync(sync.First().Reverse(), first, reverse: true, upToDate: true);
        }

        private async Task AssertEqualAfterSync(IEnumerable<Item> expected, IEnumerable<IEnumerable<Item>> sync, bool? reverse = null, string message = null)
        {
            if (reverse.HasValue)
            {
                await AssertEqualAfterSync(expected, sync, reverse.Value, true, message);
            }
            else
            {
                var first = Enumerable.Repeat(sync.First(), sync.Count());
                await AssertEqualAfterSync(expected, sync, reverse: false, upToDate: false, message);
                await AssertEqualAfterSync(expected, sync, reverse: false, upToDate: null, message);
                await AssertEqualAfterSync(expected, sync, reverse: false, upToDate: true, message);
                await AssertEqualAfterSync(null, sync, reverse: true, upToDate: false, message);
                await AssertEqualAfterSync(null, sync, reverse: true, upToDate: null, message);
                await AssertEqualAfterSync(null, sync, reverse: true, upToDate: true, message);
            }
        }

        //private Task AssertEqualAfterSync(string expected, IEnumerable<IEnumerable<Item>> sync, bool reverse, bool? upToDate, string message = null) => AssertEqualAfterSync((ItemList)expected, sync, reverse, upToDate, message);

        private async Task AssertEqualAfterSync(IEnumerable<Item> expected, IEnumerable<IEnumerable<Item>> sync, bool reverse, bool? upToDate, string message = null)
        {
            var now = DateTime.Now;
            IEnumerable<DateTime?> timestamps;

            if (upToDate == true)
            {
                // seed with the id of the first item we grab from all items to sync (semi random)
                var seed = sync.SelectMany().FirstOrDefault()?.TryGetID(TMDB.ID, out var id) == true ? id : 0;
                var random = new Random(seed);
                var options = new DateTime?[] { now.AddDays(-1), null, now.AddDays(1) };

                timestamps = sync.Skip(1).Select(list => options[random.Next(options.Length)]).Prepend(now).ToList();
                // make sure lists that are supposed to be up to date actually are
                sync = sync.Zip(timestamps, (list, timestamp) => timestamp < now ? sync.First() : list);
                expected = null;
            }
            else
            {
                DateTime? timestamp = !upToDate.HasValue ? null : now.AddDays(-1);
                timestamps = Enumerable.Repeat<DateTime?>(now, sync.Count() - 1).Prepend(timestamp);
            }

            var lists = sync.Zip(timestamps, (items, timestamp) => new TestList(string.Empty, items.ToList())
            {
                LastUpdated = timestamp
            }).ToList();

            List<(int AddedOffset, int RemovedOffset)> changesOffsets = lists.Select(list => expected == null ? (0, 0) : (list.NumAdded, list.NumRemoved)).ToList();
            var expectedSet = expected?.ToHashSet();
            var checklists = lists.Select(list => expectedSet == null ? new SyncChecklist(list) : new SyncChecklist(list, expectedSet)).ToArray();
            //var inferred = lists.Select(list => expectedSet == null ? default : InferExpectedValues(expectedSet, list.ItemList)).ToArray();

            message += $"Synced {(reverse ? "Backward" : "Forward")}, {(upToDate == null ? "unknown" : (upToDate.Value ? "Up to date" : "Not up to date"))}";
            await AssertEqualAfterSync(expected, lists, reverse, message);

            for (int i = 0; i < lists.Count; i++)
            {
                var list = lists[i];
                var checklist = checklists[i];

                var added = list.NumAdded - changesOffsets[i].AddedOffset;
                var removed = list.NumRemoved - changesOffsets[i].RemovedOffset;

                try
                {
                    checklist.AssertAll(numAdded: added, numRemoved: removed);
                }
                catch (UnitTestAssertException e)
                {
                    Print.Log(message);
                    throw e;
                }
            }
        }

        private async Task AssertEqualAfterSync(IEnumerable<Item> expected, IEnumerable<TestList> tests, bool reverse, string message = null)
        {
            var lists = tests.ToList();

            message += string.Join("\n\t", lists.Select(list => "[" + string.Join(',', list.ItemList) + "]").Prepend(string.Empty));
            var actual = await new SyncList(lists, reverse).ReadAll();
            //Assert.IsTrue(original.SequenceEqual(synced), (reverse ? "Reverse" : "Regular") + message);
            if (expected != null)
            {
                Assert.IsTrue(expected.SequenceEqual(actual), message + $"\nExpected: {string.Join(',', expected)}\nActual: {string.Join(',', actual)}");
            }

            // Assert set equality against first list
            var first = lists.First().ItemList.ToHashSet();

            foreach (var list in lists.Skip(1))//.Where(list => !(list.LastUpdated < lists.First().LastUpdated)))
            {
                var set = list.ItemList.ToHashSet();

                if (!set.SetEquals(first))
                {
                    var added = set.Except(first);
                    var removed = first.Except(set);

                    Assert.Fail(message + string.Join("\n\t", lists.Take(1).Append(list).Select(list => list.ItemList.PrettyPrint()).Prepend(string.Empty)) + $"\nFailed to add: {added.PrettyPrint()}\nFailed to remove: {removed.PrettyPrint()}");
                }
            }
        }
    }
}