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
            var key = request.RequestUri;
            Task<HttpResponseMessage> response;
            TaskCompletionSource<HttpResponseMessage> source;

            lock (BufferLock)
            {
                if (Buffer.TryGetValue(key, out response))
                {
                    source = null;
                }
                else
                {
                    source = new TaskCompletionSource<HttpResponseMessage>();
                    Buffer[key] = response = source.Task;
                }
            }

            if (source != null)
            {
                SetResultAsync(source, key, () => base.SendAsync(request, cancellationToken));
            }
            else
            {
                response = RespondBuffered(response);
            }

            return response;
        }

        private async void SetResultAsync(TaskCompletionSource<HttpResponseMessage> source, Uri key, Func<Task<HttpResponseMessage>> request)
        {
            try
            {
                source.SetResult(await request());
            }
            catch (Exception e)
            {
                source.SetException(e);
            }
            finally
            {
                lock (BufferLock)
                {
                    Buffer.Remove(key);
                }
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