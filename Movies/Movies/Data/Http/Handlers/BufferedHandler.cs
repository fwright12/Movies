using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class BufferedHandler : DelegatingHandler
    {
        private Dictionary<Uri, Task<HttpResponseMessage>> Buffer = new Dictionary<Uri, Task<HttpResponseMessage>>();

        public BufferedHandler() : base() { }
        public BufferedHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        private readonly object BufferLock = new object();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            Task<HttpResponseMessage> response;
            bool buffered;

            lock (BufferLock)
            {
                buffered = Buffer.TryGetValue(uri, out response);

                if (!buffered)
                {
                    Buffer.TryAdd(uri, response = base.SendAsync(request, cancellationToken));
                }
            }

            if (buffered)
            {
                return RespondBuffered(response);
            }
            else
            {
                response.ContinueWith(res =>
                {
                    lock (BufferLock)
                    {
                        Buffer.Remove(uri);
                    }
                });
                return response;
            }
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