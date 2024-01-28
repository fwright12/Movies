using System;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbSQLProcessor : IAsyncEventProcessor<ResourceRequestArgs<Uri>>
    {
        public IEventAsyncCache<ResourceRequestArgs<Uri>> DAO { get; }

        public TMDbSQLProcessor(IEventAsyncCache<ResourceRequestArgs<Uri>> dao)
        {
            DAO = dao;
        }

        public Task<bool> ProcessAsync(ResourceRequestArgs<Uri> e) => DAO.Read(e.AsEnumerable());
    }
}
