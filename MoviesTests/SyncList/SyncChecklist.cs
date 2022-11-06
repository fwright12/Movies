using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesTests
{
    public class SyncChecklist
    {
        public TestList List { get; }

        private IEnumerable<Item> Expected;
        private HashSet<Item> ActualSet;

        public SyncChecklist(TestList list)
        {
            List = list;
            ActualSet = List.ItemList.ToHashSet();
        }

        public SyncChecklist(TestList list, IEnumerable<Item> expected) : this(list)
        {
            Expected = expected;
        }

        public void AssertAll(int? contains = null, int? numAdded = null, int? numRemoved = null, int? addCalls = null, int? removeCalls = null)
        {
            var lazyExpectedSet = new Lazy<HashSet<Item>>(() => Expected as HashSet<Item> ?? Expected.ToHashSet());
            var lazyAdded = new Lazy<int>(() => lazyExpectedSet.Value.Except(ActualSet).Count());
            var lazyRemoved = new Lazy<int>(() => ActualSet.Except(lazyExpectedSet.Value).Count());

            if (numAdded.HasValue || Expected != null)
            {
                Assert.AreEqual(numAdded ?? lazyAdded.Value, List.NumAdded);
            }
            if (numRemoved.HasValue || Expected != null)
            {
                Assert.AreEqual(numRemoved ?? lazyRemoved.Value, List.NumRemoved);
            }

            var lazyContains = new Lazy<int>(() => (int)Math.Ceiling(Math.Max(ActualSet.Count, ActualSet.Count + lazyAdded.Value - lazyRemoved.Value) / 20d));

            if ((contains.HasValue || Expected != null) && List.CallsToContains > (contains ?? lazyContains.Value))
            {
                Assert.Inconclusive($"{List.CallsToContains} calls to contains method for a list with {List.ItemList.Count} items\n");
            }

            if (addCalls.HasValue)
            {
                Assert.AreEqual(addCalls.Value, List.CallsToAdd);
            }
            if (removeCalls.HasValue)
            {
                Assert.AreEqual(removeCalls.Value, List.CallsToRemove);
            }
        }
    }
}
