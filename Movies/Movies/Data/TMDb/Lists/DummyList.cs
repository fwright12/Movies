#if DEBUG
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        private class DummyList : Models.List
        {
            public override string ID { get; }

            public DummyList(string id = null)
            {
                ID = id ?? Guid.NewGuid().ToString();
                //Description = "cool list";
                Items = Stuff();
            }

            private async IAsyncEnumerable<Models.Item> Stuff()
            {
                //yield break;
                Count = 1;
                yield return new Models.Movie("test").WithID(MockData.IDKey, 3);
            }

            protected override Task<bool> AddAsyncInternal(IEnumerable<Models.Item> items)
            {
                return Task.FromResult(true);
            }

            protected override Task<bool?> ContainsAsyncInternal(Models.Item item)
            {
                return Task.FromResult<bool?>(true);
            }

            public override Task<int> CountAsync()
            {
                throw new NotImplementedException();
            }

            public override Task Delete()
            {
                return Task.CompletedTask;
            }

            protected override Task<bool> RemoveAsyncInternal(IEnumerable<Models.Item> items)
            {
                return Task.FromResult(true);
            }

            public override Task Update()
            {
                return Task.CompletedTask;
            }
        }
    }
}
#endif
