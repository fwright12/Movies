using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public interface IPagedRequest
    {
        public string Endpoint { get; }

        public HttpRequestMessage GetRequest(int page, params string[] parameters) => new HttpRequestMessage(HttpMethod.Get, TMDB.BuildApiCall(Endpoint, parameters.Prepend($"page={page}")));
        public int? GetTotalPages() => null;
        public Task<int?> GetTotalPages(HttpResponseMessage response);
    }
}
