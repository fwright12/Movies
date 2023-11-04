using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class BatchEventArgs : EventArgs
    {
        public string Id { get; }

        public BatchEventArgs(string id)
        {
            Id = id;
        }
    }

    public class BatchHandler : DelegatingHandler
    {
        public static event EventHandler<BatchEventArgs> Received;
        private static event EventHandler<BatchEventArgs> BatchEnded;

        public const string BATCH_HEADER = "Batch";

        private Dictionary<string, (TaskCompletionSource<HttpResponseMessage[]> Source, List<HttpRequestMessage> Requests)> Batches = new Dictionary<string, (TaskCompletionSource<HttpResponseMessage[]> Source, List<HttpRequestMessage> Requests)>();

        protected BatchHandler()
        {
            Init();
        }

        protected BatchHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            Init();
        }

        private void Init()
        {
            BatchEnded += HandleBatchEnded;
        }

        private void HandleBatchEnded(object sender, BatchEventArgs e)
        {
            BatchEnd(e.Id);
        }

        public async void BatchEnd(string batchId)
        {
            if (!Batches.Remove(batchId, out var value))
            {
                return;
            }

            var responses = await Task.WhenAll(SendAsync(value.Requests, default));
            value.Source.SetResult(responses);

            Received?.Invoke(this, new BatchEventArgs(batchId));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues(BATCH_HEADER, out var values))
            {
                var batchId = values.FirstOrDefault();

                if (batchId != null)
                {
                    if (!Batches.TryGetValue(batchId, out var batch))
                    {
                        Batches.Add(batchId, batch = (new TaskCompletionSource<HttpResponseMessage[]>(), new List<HttpRequestMessage>()));
                    }

                    batch.Requests.Add(request);
                    Received?.Invoke(this, new BatchEventArgs(batchId));
                    return Get(batch.Source.Task, batch.Requests.Count - 1);
                }

                request.Headers.Remove(BATCH_HEADER);
            }

            return base.SendAsync(request, cancellationToken);
        }

        private static void OnBatchEnded(Guid batchId)
        {
            BatchEnded?.Invoke(null, new BatchEventArgs(batchId.ToString()));
        }

        public static Task<HttpResponseMessage>[] SendAsync(HttpMessageInvoker invoker, IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken)
        {
            var batch = new Batch(requests);
            var responses = requests.Select(request => invoker.SendAsync(request, cancellationToken)).ToArray();

            return batch.SignalWhenReady(responses);
        }

        public class Batch
        {
            public Guid BatchId { get; }
            public IEnumerable<Task<HttpResponseMessage>> Responses { get; private set; }

            private int Remaining;

            public Batch(IEnumerable<HttpRequestMessage> requests)
            {
                BatchId = Guid.NewGuid();
                var value = BatchId.ToString();

                foreach (var request in requests)
                {
                    request.Headers.Add(BatchHandler.BATCH_HEADER, value);
                }

                Received += HandleReceived;
            }

            public Task<HttpResponseMessage>[] SignalWhenReady(IEnumerable<Task<HttpResponseMessage>> responses)
            {
                Responses = responses;
                HandleReceived(responses.Count());

                Await();
                return responses.ToArray();
            }

            private void HandleReceived(object sender, BatchEventArgs e)
            {
                if (e.Id == BatchId.ToString())
                {
                    HandleReceived(-1);
                }
            }

            private void HandleReceived(int count)
            {
                Remaining += count;

                if (Remaining == 0)
                {
                    OnBatchEnded(BatchId);
                }
            }

            private async void Await()
            {
                while (Remaining > 0)
                {
                    await Task.WhenAny(Responses);
                    HandleReceived(-1);
                }
            }
        }

        protected internal virtual Task<HttpResponseMessage>[] SendAsync(IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken)
        {
            var batch = new Batch(requests);
            var responses = requests.Select(request => base.SendAsync(request, cancellationToken));

            return batch.SignalWhenReady(responses);
        }

        private async Task<T> Get<T>(Task<T[]> list, int index) => (await list)[index];
    }
}