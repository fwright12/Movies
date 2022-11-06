namespace MoviesTests
{
    public class TestList : List
    {
        public const int SIMULATED_WEB_DELAY = 0;

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

        private static async Task SimulateWebDelay()
        {
            await Task.Delay(SIMULATED_WEB_DELAY);
        }

        private async IAsyncEnumerable<Item> GetItems()
        {
            await SimulateWebDelay();

            if (SIMULATED_WEB_DELAY == 0)
            {
                await Task.Delay(10);
            }

            var items = Reverse ? ItemList.Reverse<Item>().ToList() : ItemList.ToList<Item>();

            foreach (var item in items)
            {
                yield return item;
            }
        }

        public override Task<int> CountAsync() => Task.FromResult(ItemList.Count);

        public override Task Delete() => Task.CompletedTask;

        public override Task Update() => Task.CompletedTask;

        protected override async Task<bool> AddAsyncInternal(IEnumerable<Item> items)
        {
            await SimulateWebDelay();

            int count = ItemList.Count;

            ItemList.AddRange(items);//.Where(item => !ItemList.Contains(item)));

            CallsToAdd++;
            NumAdded += ItemList.Count - count;

            return true;
        }

        protected override async Task<bool?> ContainsAsyncInternal(Item item)
        {
            await SimulateWebDelay();
            CallsToContains++;
            return ItemList.Contains(item);
        }

        protected override async Task<bool> RemoveAsyncInternal(IEnumerable<Item> items)
        {
            await SimulateWebDelay();

            foreach (var item in items)
            {
                ItemList.Remove(item);
                NumRemoved++;
            }

            CallsToRemove++;
            return true;
        }

        public override async Task<List<Item>> GetAdditionsAsync(List list)
        {
            await SimulateWebDelay();

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
