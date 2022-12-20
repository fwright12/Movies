using System;
using System.Threading.Tasks;

namespace Movies
{
    public partial class Trakt
    {
        public abstract class BaseNamedList : BaseList
        {
            protected override string ItemsEndpoint => ID;
            protected override string AddEndpoint => ID;
            protected override string RemoveEndpoint => string.Format("{0}/remove", ID);
            protected override string ReorderEndpoint => string.Format("{0}/reorder", ID);

            public BaseNamedList(Trakt trakt, string endpoint, int? pageSize = null) : base(trakt, endpoint, pageSize)
            {
                AllowedTypes = ItemType.AllMedia;
                Client.BaseAddress = new Uri("https://api.trakt.tv/sync/");
            }

            public override Task Delete() => Task.CompletedTask;

            public override Task Update() => Task.CompletedTask;
        }

        public class NamedList : BaseNamedList
        {
            public NamedList(Trakt trakt, string endpoint, int? pageSize = null) : base(trakt, endpoint, pageSize) { }
        }
    }
}
