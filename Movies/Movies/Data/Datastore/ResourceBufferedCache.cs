using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class ResourceBufferedCache<TKey> : BufferedCache<ResourceReadArgs<TKey>>
    {
        public ResourceBufferedCache(IEventAsyncCache<ResourceReadArgs<TKey>> cache) : base(cache) { }

        public override object GetKey(ResourceReadArgs<TKey> e) => e.Key;

        public override void Process(ResourceReadArgs<TKey> e, ResourceReadArgs<TKey> buffered) => e.Handle(buffered.Response);

        public override async Task<bool> ProcessAsync(IEnumerable<ResourceReadArgs<TKey>> e, IAsyncEventProcessor<IEnumerable<ResourceReadArgs<TKey>>> next)
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
        private IAsyncEventProcessor<IEnumerable<ResourceReadArgs<Uri>>> Processor { get; }

        public UriBufferedCache(IEventAsyncCache<ResourceReadArgs<Uri>> cache) : base(cache)
        {
            Processor = new EventCacheReadProcessor<ResourceReadArgs<Uri>>(cache);
        }

        public override Task<bool> ProcessAsync(IEnumerable<ResourceReadArgs<Uri>> e, IAsyncEventProcessor<IEnumerable<ResourceReadArgs<Uri>>> next)
        {
            var batchedProcessor = new BatchedEventProcessor<ResourceReadArgs<Uri>>(e, Processor);
            var batchedNext = new BatchedEventProcessor<ResourceReadArgs<Uri>>(e, next);
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

    public abstract class EtaggedAsyncCoRProcessor<T> : IAsyncCoRProcessor<ResourceReadArgs<T>>
    {
        public IAsyncEventProcessor<ResourceReadArgs<T>> Processor { get; }

        protected EtaggedAsyncCoRProcessor(IAsyncEventProcessor<ResourceReadArgs<T>> processor)
        {
            Processor = processor;
        }

        public async Task<bool> ProcessAsync(ResourceReadArgs<T> e, IAsyncEventProcessor<ResourceReadArgs<T>> next)
        {
            var result = await Processor.ProcessAsync(e);

            if (next == null)
            {
                return result;
            }

            ResourceReadArgs<T> arg;

            if (e.IsHandled)
            {
                if (e.Response is RestResponse restResponse && restResponse.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out var etag) && etag.Count() == 1)
                {
                    arg = GetEtaggedRequest(e, etag.First());
                }
                else
                {
                    return true;
                }
            }
            else
            {
                arg = e;
            }

            result = await next.ProcessAsync(arg);

            if (arg != e && arg.IsHandled)
            {
                e.Handle(arg.Response);
            }

            return result;
        }

        protected abstract ResourceReadArgs<T> GetEtaggedRequest(ResourceReadArgs<T> original, string etag);
    }

    public class EtaggedUriAsyncCoRProcessor : EtaggedAsyncCoRProcessor<Uri>
    {
        public EtaggedUriAsyncCoRProcessor(IAsyncEventProcessor<ResourceReadArgs<Uri>> processor) : base(processor) { }

        protected override ResourceReadArgs<Uri> GetEtaggedRequest(ResourceReadArgs<Uri> original, string etag)
        {
            return new RestRequestArgs(original.Key, null, new Dictionary<string, IEnumerable<string>>
            {
                [REpresentationalStateTransfer.Rest.IF_NONE_MATCH] = new List<string> { etag }
            })
            {
                Expected = original.Expected
            };
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