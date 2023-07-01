using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Movies
{
    public class TMDbBufferedHandler : DelegatingHandler
    {
        private Dictionary<Uri, List<(ISet<string> Appended, Task<HttpResponseMessage> Response)>> Buffer = new Dictionary<Uri, List<(ISet<string> Appended, Task<HttpResponseMessage> Response)>>();
        private readonly object BufferLock = new object();

        public TMDbBufferedHandler() : base() { }
        public TMDbBufferedHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parts = request.RequestUri.ToString().Split('?');
            var query = HttpUtility.ParseQueryString(parts.LastOrDefault() ?? string.Empty);
            var append = query.GetValues(TMDB.APPEND_TO_RESPONSE);

            if (append == null)
            {
                return base.SendAsync(request, cancellationToken);
            }

            var set = append.SelectMany(value => value.Replace(" ", "").Split(",")).ToHashSet();
            query.Remove(TMDB.APPEND_TO_RESPONSE);

            var url = parts[0];
            if (query.Count > 0)
            {
                url += "?" + query.ToString();
            }

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            List<(ISet<string> Appended, Task<HttpResponseMessage> Response)> list;

            lock (BufferLock)
            {
                if (!Buffer.TryGetValue(uri, out list))
                {
                    Buffer.TryAdd(uri, list = new List<(ISet<string> Appended, Task<HttpResponseMessage> Response)>());
                }
            }

            foreach (var pair in list)
            {
                if (set.IsSubsetOf(pair.Appended))
                {
                    return BufferedHandler.RespondBuffered(pair.Response);
                }
            }

            var response = base.SendAsync(request, cancellationToken);
            var value = (set, response);

            list.Add(value);
            response.ContinueWith(res =>
            {
                list.Remove(value);

                if (list.Count == 0)
                {
                    lock (BufferLock)
                    {
                        Buffer.Remove(uri);
                    }
                }
            });

            return response;
        }
    }
}