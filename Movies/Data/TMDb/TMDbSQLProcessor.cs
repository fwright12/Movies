using System;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbSQLProcessor : IAsyncEventProcessor<KeyValueRequestArgs<Uri>>
    {
        public IEventAsyncCache<KeyValueRequestArgs<Uri>> DAO { get; }

        public TMDbSQLProcessor(IEventAsyncCache<KeyValueRequestArgs<Uri>> dao)
        {
            DAO = dao;
        }

        public Task<bool> ProcessAsync(KeyValueRequestArgs<Uri> e) => DAO.Read(e.AsEnumerable());
    }
}
