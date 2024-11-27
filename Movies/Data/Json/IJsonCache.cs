using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public interface IJsonCache : IAsyncEnumerable<KeyValuePair<string, JsonResponse>>
    {
        Task AddAsync(string url, JsonResponse response);
        Task Clear();
        Task<bool> Expire(string url);
        Task<bool> IsCached(string url);
        Task<JsonResponse> TryGetValueAsync(string url);
    }
}
