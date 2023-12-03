using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public interface IAsyncCache<TKey, TValue>
    {
        Task<TValue> Read(TKey key);
        Task<bool> Write(TKey key, TValue value);
    }

    public interface IEventAsyncCache<TArgs>
    {
        Task<bool> Read(IEnumerable<TArgs> args);
        Task<bool> Write(IEnumerable<TArgs> args);
    }
}