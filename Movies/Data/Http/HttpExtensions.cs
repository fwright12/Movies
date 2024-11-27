using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public static class HttpExtensions
    {
        public static HttpMessageHandler BuildHandlerChain(HttpMessageHandler final, params DelegatingHandler[] handlers)
        {
            for (int i = 1; i < handlers.Length; i++)
            {
                handlers[i - 1].InnerHandler = handlers[i];
            }

            HttpMessageHandler start;

            if (handlers.Length == 0)
            {
                start = final;
            }
            else
            {
                start = handlers[0];
                handlers[handlers.Length - 1].InnerHandler = final;
            }

            return start;
        }

        public static Task<HttpResponseMessage>[] SendAsync(this HttpMessageInvoker invoker, IEnumerable<HttpRequestMessage> requests) => SendAsync(invoker, requests, default);
        public static Task<HttpResponseMessage>[] SendAsync(this HttpMessageInvoker invoker, IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken) => BatchHandler.SendAsync(invoker, requests, cancellationToken);

        public static Task<HttpResponseMessage[]> SendAsync(BatchHandler invoker, IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken, out Guid batchId)
        {
            batchId = Guid.NewGuid();
            var tasks = new List<Task<HttpResponseMessage>>();
            var itr = requests.GetEnumerator();

            if (itr.MoveNext())
            {
                while (true)
                {
                    var request = itr.Current;
                    var value = batchId.ToString();

                    if (!itr.MoveNext())
                    {
                        value += " final";
                    }

                    request.Headers.Add(BatchHandler.BATCH_HEADER, value);
                    //tasks.Add(invoker.SendAsync(request, cancellationToken));
                }
            }

            return Task.WhenAll(tasks);
        }
    }
}