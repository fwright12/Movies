using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class ParameterizedPagedRequest : IPagedRequest
    {
        public IPagedRequest Request { get; }
        public string Endpoint => Request.Endpoint;

        private object[] Parameters;

        public ParameterizedPagedRequest(PagedTMDbRequest request, params object[] parameters)
        {
            Request = request;
            Parameters = parameters;
        }

        public HttpRequestMessage GetRequest(int page, params string[] parameters)
        {
            var request = Request.GetRequest(page, parameters);
            request.RequestUri = new Uri(string.Format(request.RequestUri.ToString(), Parameters), UriKind.RelativeOrAbsolute);
            return request;
        }

        public Task<int?> GetTotalPages(HttpResponseMessage response) => Request.GetTotalPages(response);
    }
}
