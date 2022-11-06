using System.Collections;

namespace MoviesTests
{
    public class ItemList : IEnumerable<Item>
    {
        private IEnumerable<Item> Items { get; }

        public ItemList(IEnumerable<Item> items)
        {
            Items = items;
        }

        public static implicit operator ItemList(string ids) => new ItemList(ids.Select(id => int.Parse(id.ToString()).ToMovie()).ToList<Item>());

        public static ItemList operator +(IEnumerable<Item> items, ItemList list) => new ItemList(items.Concat(list.Items));

        public static ItemList operator -(IEnumerable<Item> items, ItemList list) => new ItemList(items.Except(list.Items));

        //public void Add(int id) => Items.Add(MovieFromId(id));

        public IEnumerator<Item> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
