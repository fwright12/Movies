using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class BufferedHandler : DelegatingHandler
    {
        private ConcurrentDictionary<Uri, Task<HttpResponseMessage>> Buffer = new ConcurrentDictionary<Uri, Task<HttpResponseMessage>>();

        public BufferedHandler() : base() { }
        public BufferedHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (Buffer.TryGetValue(uri, out var buffered))
            {
                return RespondBuffered(buffered);
            }

            var response = base.SendAsync(request, cancellationToken);
            Buffer.TryAdd(uri, response);
            response.ContinueWith(res => Buffer.Remove(uri, out _));
            return response;
        }

        public static async Task<HttpResponseMessage> RespondBuffered(Task<HttpResponseMessage> buffered)
        {
            var response = await buffered;
            var request = response.RequestMessage;
            HttpContent content;

            if (request.Method == HttpMethod.Get)
            {
                content = response.Content;
            }
            else
            {
                content = request.Content;
            }

            return new HttpResponseMessage(response.StatusCode)
            {
                Content = content,
            };
        }
    }
}