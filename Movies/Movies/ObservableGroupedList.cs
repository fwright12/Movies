using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Movies
{
    public class ObservableGroup<TKey, TElement> : ObservableCollection<TElement>, IGrouping<TKey, TElement>
    {
        public TKey Key { get; }

        public ObservableGroup(TKey key, IEnumerable<TElement> elements) : base(elements)
        {
            Key = key;
        }

        public ObservableGroup(TKey key)
        {
            Key = key;
        }
    }

    public class ObservableGroupedList<TKey, TElement> : ObservableCollection<ObservableGroup<TKey, TElement>>
    {
        private Func<TElement, TKey> KeySelector;

        public ObservableGroupedList(Func<TElement, TKey> keySelector)
        {
            KeySelector = keySelector;
        }

        public ObservableGroupedList(Func<TElement, TKey> keySelector, IEnumerable<TElement> elements) : base(elements.GroupBy(keySelector, (key, elements) => new ObservableGroup<TKey, TElement>(key, elements)))
        {
            KeySelector = keySelector;
        }

        public void Add(TElement element)
        {
            var key = KeySelector(element);
            var grouping = this.FirstOrDefault(grouping => Equals(key, grouping.Key));

            if (grouping == null)
            {
                Add(grouping = new ObservableGroup<TKey, TElement>(key));
            }

            grouping.Add(element);
        }
    }
}