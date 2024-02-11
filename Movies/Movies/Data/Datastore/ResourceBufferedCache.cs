using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class ResourceBufferedCache<TKey> : BufferedCache<ResourceRequestArgs<TKey>>
        where TKey : Uri
    {
        public ResourceBufferedCache(IEventAsyncCache<ResourceRequestArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(ResourceRequestArgs<TKey> e) => e.Request.Key;

        public override void Process(ResourceRequestArgs<TKey> e, ResourceRequestArgs<TKey> buffered)
        {
            if (buffered.Response is RestResponse restResponse && restResponse.TryGetRepresentation(e.Request.Expected, out var value))
            {
                e.Handle(new RestResponse(restResponse.Resource, restResponse.ControlData, restResponse.Metadata, e.Request.Expected));
            }
            else
            {
                e.Handle(buffered.Response);
            }
        }

        public override async Task<bool> ProcessAsync(IEnumerable<ResourceRequestArgs<TKey>> e, IAsyncEventProcessor<IEnumerable<ResourceRequestArgs<TKey>>> next)
        {
            if (await Read(e))
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
    }

    public class UriBufferedCache : ResourceBufferedCache<Uri>
    {
        private IAsyncEventProcessor<IEnumerable<ResourceRequestArgs<Uri>>> Processor { get; }

        public UriBufferedCache(IEventAsyncCache<ResourceRequestArgs<Uri>> cache) : base(cache)
        {
            Processor = new EventCacheReadProcessor<ResourceRequestArgs<Uri>>(cache);
        }

        public override Task<bool> ProcessAsync(IEnumerable<ResourceRequestArgs<Uri>> e, IAsyncEventProcessor<IEnumerable<ResourceRequestArgs<Uri>>> next)
        {
            var batchedProcessor = new BatchedEventProcessor<ResourceRequestArgs<Uri>>(e, Processor);
            var batchedNext = new BatchedEventProcessor<ResourceRequestArgs<Uri>>(e, next);
            var etagProcessor = new EtaggedUriAsyncCoRProcessor(batchedProcessor);

            return etagProcessor.ProcessAllAsync(e, batchedProcessor, batchedNext);
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

    public abstract class EtaggedAsyncCoRProcessor<T> : IAsyncCoRProcessor<ResourceRequestArgs<T>>
        where T : Uri
    {
        public IAsyncEventProcessor<ResourceRequestArgs<T>> Processor { get; }

        protected EtaggedAsyncCoRProcessor(IAsyncEventProcessor<ResourceRequestArgs<T>> processor)
        {
            Processor = processor;
        }

        public async Task<bool> ProcessAsync(ResourceRequestArgs<T> e, IAsyncEventProcessor<ResourceRequestArgs<T>> next)
        {
            if (next == null)
            {
                return await Processor.ProcessAsync(e);
            }

            var e1 = new ResourceRequestArgs<T>(e.Request);
            var result = await Processor.ProcessAsync(e1);
            ResourceRequestArgs<T> eNext;

            if (e1.IsHandled)
            {
                if (e1.Response is RestResponse restResponse && restResponse.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out var etag) && etag.Count() == 1)
                {
                    eNext = GetEtaggedRequest(e1, etag.First());
                }
                else
                {
                    eNext = null;
                }
            }
            else
            {
                eNext = e;
            }

            if (eNext != null)
            {
                result = await next.ProcessAsync(eNext);
            }

            var response = eNext?.Response ?? e1.Response;
            if (response != e.Response)
            {
                e.Handle(response);
            }

            return result;
        }

        protected abstract ResourceRequestArgs<T> GetEtaggedRequest(ResourceRequestArgs<T> original, string etag);
    }

    public class EtaggedUriAsyncCoRProcessor : EtaggedAsyncCoRProcessor<Uri>
    {
        public EtaggedUriAsyncCoRProcessor(IAsyncEventProcessor<ResourceRequestArgs<Uri>> processor) : base(processor) { }

        protected override ResourceRequestArgs<Uri> GetEtaggedRequest(ResourceRequestArgs<Uri> original, string etag)
        {
            return new RestRequestArgs(original.Request.Key, null, new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.IF_NONE_MATCH] = new List<string> { etag }
            }, original.Request.Expected);
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
}