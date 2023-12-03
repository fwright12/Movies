using System;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbSQLProcessor : IAsyncEventProcessor<ResourceReadArgs<Uri>>
    {
        public IEventAsyncCache<ResourceReadArgs<Uri>> DAO { get; }

        public TMDbSQLProcessor(IEventAsyncCache<ResourceReadArgs<Uri>> dao)
        {
            DAO = dao;
        }

        public Task<bool> ProcessAsync(ResourceReadArgs<Uri> e) => DAO.Read(e.AsEnumerable());
    }
}
