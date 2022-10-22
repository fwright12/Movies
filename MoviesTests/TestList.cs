namespace MoviesTests
{
    public class TestList : List
    {
        public override string ID { get; }

        public List<Item> ItemList { get; }
        public int CallsToContains { get; private set; }
        public int CallsToAdd { get; private set; }
        public int CallsToRemove { get; private set; }
        public int NumAdded { get; private set; }
        public int NumRemoved { get; private set; }

        public TestList(string id) : this(id, new List<Item>()) { }

        public TestList(string id, IEnumerable<Item> itemList)
        {
            ID = id;
            ItemList = itemList.ToList();
            //Items = AsyncEnumerable.ToAsyncEnumerable(Task.FromResult<IEnumerable<Item>>(ItemList.ToList<Item>()));
            Items = GetItems();
        }

        public TestList AddItems(IEnumerable<Item> items) => new TestList(ID, ItemList.Concat(items).ToList())
        {
            LastUpdated = LastUpdated
        };

        public TestList RemoveItems(IEnumerable<Item> items) => new TestList(ID, ItemList.Except(items).ToList())
        {
            LastUpdated = LastUpdated
        };

        //public static TestList operator +(TestList list, IEnumerable<Item> items) => 

        private async IAsyncEnumerable<Item> GetItems()
        {
            var items = Reverse ? ItemList.Reverse<Item>().ToList() : ItemList.ToList<Item>();

            // Simulate delay from web api
            //await Task.Delay(Random.Next(0, 5) * 10);
            await Task.Delay(10);

            foreach (var item in items)
            {
                yield return item;
            }
        }

        public override Task<int> CountAsync() => Task.FromResult(ItemList.Count);

        public override Task Delete() => Task.CompletedTask;

        public override Task Update() => Task.CompletedTask;

        protected override Task<bool> AddAsyncInternal(IEnumerable<Item> items)
        {
            int count = ItemList.Count;
            ItemList.AddRange(items);//.Where(item => !ItemList.Contains(item)));
            CallsToAdd++;
            NumAdded += ItemList.Count - count;
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
                NumRemoved++;
            }

            CallsToRemove++;
            return Task.FromResult(true);
        }

        public override async Task<List<Item>> GetAdditionsAsync(List list)
        {
            var add = new List<Item>();
            var itr = GetItems().GetAsyncEnumerator();
            var backup = (list as TestList)?.CallsToContains;

            while (await itr.MoveNextAsync() && await list.ContainsAsync(itr.Current) == false)
            {
                add.Add(itr.Current);
            }

            if (backup.HasValue)
            {
                ((TestList)list).CallsToContains = backup.Value;
            }

            if (Reverse)
            {
                add.Reverse();
            }

            return add;
        }
    }
}
