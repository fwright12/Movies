using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Movies
{
    public class ReadOnlyEventCache<TArgs> : IEventAsyncCache<TArgs>
    {
        public IAsyncEventProcessor<IEnumerable<TArgs>> Processor { get; }

        public ReadOnlyEventCache(IAsyncEventProcessor<IEnumerable<TArgs>> processor)
        {
            Processor = processor;
        }

        public Task<bool> Read(IEnumerable<TArgs> args) => Processor.ProcessAsync(args);

        public Task<bool> Write(IEnumerable<TArgs> args) => Task.FromResult(false);
    }

    public class ResourceBufferedCache1<TKey> : BufferedCache<KeyValueRequestArgs<TKey>>
        where TKey : Uri
    {
        public ResourceBufferedCache1(IEventAsyncCache<KeyValueRequestArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(KeyValueRequestArgs<TKey> e) => e.Request.Key;

        public override void Process(KeyValueRequestArgs<TKey> e, KeyValueRequestArgs<TKey> buffered)
        {
            if (buffered.Response is RestResponse restResponse)
            {
                var response = RestResponse.Create(e.Request.Expected, restResponse.Resource, restResponse.ControlData, restResponse.Metadata);

                if (response != null)
                {
                    e.Handle(response);
                }
            }
            else
            {
                e.Handle(buffered.Response);
            }
        }
    }

    public class ResourceBufferedCache<TKey> : IAsyncCoRProcessor<IEnumerable<KeyValueRequestArgs<TKey>>>, IWriteBackProcessor<KeyValueRequestArgs<TKey>>, IAsyncCacheAsideProcessor<KeyValueRequestArgs<TKey>> // : BufferedCache<KeyValueRequestArgs<TKey>>
        where TKey : Uri
    {
        protected ResourceBufferedCache1<TKey> Cache { get; }

        public ResourceBufferedCache(IEventAsyncCache<KeyValueRequestArgs<TKey>> cache)
        {
            Cache = new ResourceBufferedCache1<TKey>(cache);
        }

        public virtual async Task<bool> ProcessAsync(IEnumerable<KeyValueRequestArgs<TKey>> e, IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<TKey>>> next)
        {
            if (await Cache.Read(e))
            {
                return true;
            }
            else if (next == null)
            {
                return false;
            }
            else
            {
                return await next.ProcessAsync(e);
            }
        }

        public void Write(IEnumerable<KeyValueRequestArgs<TKey>> args, Task task) => Cache.Write(args, task);

        public void Process(KeyValueRequestArgs<TKey> args, KeyValueRequestArgs<TKey> buffered) => Cache.Process(args, buffered);
    }

    public class UriBufferedCache : ResourceBufferedCache<Uri>
    {
        private IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> Processor { get; }

        public UriBufferedCache(IEventAsyncCache<KeyValueRequestArgs<Uri>> cache) : base(cache)
        {
            Processor = new EventCacheReadProcessor<KeyValueRequestArgs<Uri>>(Cache);
        }

        public override Task<bool> ProcessAsync(IEnumerable<KeyValueRequestArgs<Uri>> e, IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> next)
        {
            if (next == null)
            {
                return base.ProcessAsync(e, next);
            }

            var batchedProcessor = new BatchedEventProcessor<KeyValueRequestArgs<Uri>>(e, Processor);
            var batchedNext = new BatchedEventProcessor<KeyValueRequestArgs<Uri>>(e, next);
            var etagProcessor = new EtaggedUriAsyncCoRProcessor(batchedProcessor);

            return etagProcessor.ProcessAllAsync(e, batchedProcessor, batchedNext);
        }
    }

    public abstract class EtaggedAsyncCoRProcessor<T> : IAsyncCoRProcessor<KeyValueRequestArgs<T>>
        where T : Uri
    {
        public IAsyncEventProcessor<KeyValueRequestArgs<T>> Processor { get; }

        protected EtaggedAsyncCoRProcessor(IAsyncEventProcessor<KeyValueRequestArgs<T>> processor)
        {
            Processor = processor;
        }

        public async Task<bool> ProcessAsync(KeyValueRequestArgs<T> e, IAsyncEventProcessor<KeyValueRequestArgs<T>> next)
        {
            if (next == null)
            {
                return await Processor.ProcessAsync(e);
            }

            var e1 = new KeyValueRequestArgs<T>(e.Request);
            var result = await Processor.ProcessAsync(e1);
            KeyValueRequestArgs<T> eNext;

            if (e1.IsHandled)
            {
                eNext = null;

                if (e1.Response is RestResponse restResponse)
                {
                    var headers = new System.Net.Http.HttpResponseMessage().Headers;
                    foreach (var kvp in restResponse.ControlData)
                    {
                        headers.Add(kvp.Key, kvp.Value);
                    }

                    if (headers.ETag?.ToString() is string etag && !isFresh(headers))
                    {
                        eNext = GetEtaggedRequest(e1, etag);
                    }
                }
            }
            else
            {
                eNext = e;
            }

            if (eNext != null)
            {
                try
                {
                    result = await next.ProcessAsync(eNext);
                }
                catch (Exception exception)
                {
                    if (!e1.IsHandled)
                    {
                        throw exception;
                    }
                }
            }

            var response = eNext?.IsHandled == true ? eNext.Response : e1.Response;
            if (response != e.Response)
            {
                e.Handle(response);
            }

            return result;
        }

        protected abstract KeyValueRequestArgs<T> GetEtaggedRequest(KeyValueRequestArgs<T> original, string etag);

        private static bool isFresh(HttpResponseHeaders headers)
        {
            var expirationDate = headers.Date + headers.CacheControl?.MaxAge - (headers.Age ?? TimeSpan.Zero);
            return DateTimeOffset.UtcNow < expirationDate;
        }
    }

    public class EtaggedUriAsyncCoRProcessor : EtaggedAsyncCoRProcessor<Uri>
    {
        public EtaggedUriAsyncCoRProcessor(IAsyncEventProcessor<KeyValueRequestArgs<Uri>> processor) : base(processor) { }

        protected override KeyValueRequestArgs<Uri> GetEtaggedRequest(KeyValueRequestArgs<Uri> original, string etag)
        {
            var controlData = new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.IF_NONE_MATCH] = new List<string> { etag }
            };
            return new KeyValueRequestArgs<Uri>(new RestRequestEventArgs(original.Request.Key, null, controlData, original.Request.Expected ?? (true == controlData?.TryGetValue(Rest.CONTENT_TYPE, out _) ? typeof(string) : null)));
        }
    }

    public static class BatchedProcessorExtensions
    {
        public static Task<bool> ProcessAllAsync<T>(this IAsyncCoRProcessor<T> processor, IEnumerable<T> e, BatchedEventProcessor<T> batchedProcessor, params BatchedEventProcessor<T>[] batchedProcessors)
        {
            BatchedEventProcessor<T> last;

            if (batchedProcessors.Length == 0)
            {
                last = batchedProcessor;
            }
            else
            {
                last = batchedProcessors[batchedProcessors.Length - 1];

                batchedProcessor.SetNext(batchedProcessors[0]);
                for (int i = 0; i < batchedProcessors.Length - 1; i++)
                {
                    batchedProcessors[i].SetNext(batchedProcessors[i + 1]);
                }
            }

            return last.SetNext(e.ToArray().Select(arg => processor.ProcessAsync(arg, last)));
        }

        public static Task<bool> ProcessPartialAsync<TArgs>(this IAsyncEventProcessor<IEnumerable<TArgs>> processor, IEnumerable<TArgs> e) => (e is BulkEventArgs<TArgs> bulkArgs ? new EventProcessor<TArgs>(processor, bulkArgs) : processor).ProcessAsync(e);

        public static Task<bool> ProcessPartialAsync<TArgs>(this IAsyncCoRProcessor<IEnumerable<TArgs>> processor, IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next) => processor.ProcessAsync(e, e is BulkEventArgs<TArgs> bulkArgs ? new EventProcessor<TArgs>(next, bulkArgs) : next);

        private class EventProcessor<TArgs> : IAsyncEventProcessor<IEnumerable<TArgs>>
        {
            public IAsyncEventProcessor<IEnumerable<TArgs>> Processor { get; }
            public BulkEventArgs<TArgs> Original { get; }

            public EventProcessor(IAsyncEventProcessor<IEnumerable<TArgs>> processor, BulkEventArgs<TArgs> original)
            {
                Processor = processor;
                Original = original;
            }

            public async Task<bool> ProcessAsync(IEnumerable<TArgs> e)
            {
                Task<bool> response;

                try
                {
                    try
                    {
                        response = Processor.ProcessAsync(e);
                    }
                    finally
                    {
                        Original.Add(e);
                    }

                    return await response;
                }
                finally
                {
                    Original.Add(e);
                }
            }
        }

        private class CoRProcessorFunc<U> : IAsyncCoRProcessor<U>
        {
            public Func<U, IAsyncEventProcessor<U>, Task<bool>> Func { get; }

            public CoRProcessorFunc(Func<U, IAsyncEventProcessor<U>, Task<bool>> func)
            {
                Func = func;
            }

            public Task<bool> ProcessAsync(U e, IAsyncEventProcessor<U> next) => Func(e, next);
        }
    }

    public class BatchedEventProcessor<T> : IAsyncEventProcessor<T>
    {
        public IAsyncEventProcessor<IEnumerable<T>> Processor { get; }
        public BulkEventArgs<T> Args { get; }
        public IEnumerable<T> Original { get; }

        private List<BatchedEventProcessor<T>> Dependencies = new List<BatchedEventProcessor<T>>();
        private int Waiting;
        private TaskCompletionSource<bool> Source;
        private Task<bool> Request;

        public BatchedEventProcessor(IEnumerable<T> original, IAsyncEventProcessor<IEnumerable<T>> processor)
        {
            Processor = processor;
            Args = new BulkEventArgs<T>();
            Original = original;
            Waiting = original.Count();

            Source = new TaskCompletionSource<bool>();
            Request = Waiting == 0 ? Task.FromResult(true) : ProcessAsync();
        }

        public BatchedEventProcessor<T> SetNext(BatchedEventProcessor<T> processor)
        {
            processor.AddDependency(this);
            return processor;
        }

        public async Task<bool> SetNext(IEnumerable<Task<bool>> tasks1)
        {
            var tasks = new List<Task<bool>>(tasks1);
            bool result = true;

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);

                result &= await task;
                Decrease();
            }

            return result;
        }

        public void AddDependency(BatchedEventProcessor<T> dependency)
        {
            if (dependency == this)
            {
                throw new Exception("Circular dependency detected");
            }

            Dependencies.Add(dependency);
        }

        private object WaitingLock = new object();
        private object ArgsLock = new object();

        public Task<bool> ProcessAsync(T e)
        {
            lock (ArgsLock)
            {
                Args.Add(e);
            }

            Decrease();

            return Request;
        }

        private void Decrease()
        {
            foreach (var dependency in Dependencies)
            {
                dependency.Decrease();
            }

            bool done = false;

            lock (WaitingLock)
            {
                done = --Waiting == 0;
            }

            if (done)
            {
                Source.SetResult(true);
            }
        }

        private async Task<bool> ProcessAsync()
        {
            await Source.Task;
            var task = Processor.ProcessAsync(Args);

            (Original as BulkEventArgs<T>)?.Add(Args);
            var result = await task;
            (Original as BulkEventArgs<T>)?.Add(Args);

            return result;
        }
    }

    public class PartialProcessor1<TArgs> : IAsyncCoRProcessor<IEnumerable<TArgs>>
    {
        public IAsyncCoRProcessor<IEnumerable<TArgs>> Processor { get; }

        public PartialProcessor1(IAsyncCoRProcessor<IEnumerable<TArgs>> processor)
        {
            Processor = processor;
        }

        public Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next) => Processor.ProcessPartialAsync(e, next);
    }

    public class PartialProcessor<TArgs> : IAsyncEventProcessor<IEnumerable<TArgs>>
        where TArgs : EventArgsRequest
    {
        public IAsyncEventProcessor<IEnumerable<TArgs>> First { get; }
        public IAsyncEventProcessor<IEnumerable<TArgs>> Second { get; }

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> e)
        {
            if (await First.ProcessAsync(e))
            {
                return true;
            }

            var nextArgs = new BulkEventArgs<TArgs>(e.Where(arg => !arg.IsHandled));
            Task<bool> response;

            try
            {
                try
                {
                    response = Second.ProcessAsync(nextArgs);
                }
                finally
                {
                    (e as BulkEventArgs<TArgs>)?.Add(nextArgs);
                }

                return await response;
            }
            finally
            {
                (e as BulkEventArgs<TArgs>)?.Add(nextArgs);
            }
        }
    }

    public class EventCacheReadProcessor<T> : IAsyncEventProcessor<IEnumerable<T>>
    {
        public IEventAsyncCache<T> Cache { get; }

        public EventCacheReadProcessor(IEventAsyncCache<T> cache)
        {
            Cache = cache;
        }

        public Task<bool> ProcessAsync(IEnumerable<T> e) => Cache.Read(e);
    }
}