using System.Threading.Tasks;

namespace Movies
{
    public interface IDataStore<TKey, TValue>
    {
        Task<bool> CreateAsync(TKey key, TValue value);
        Task<TValue> ReadAsync(TKey key);
        Task<bool> UpdateAsync(TKey key, TValue updatedValue);
        Task<TValue> DeleteAsync(TKey key);
    }
}