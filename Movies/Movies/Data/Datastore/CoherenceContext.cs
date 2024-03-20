using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class Sync<TArgs>
    {
        public abstract object GetKey(TArgs args);
        public abstract object GetValue(TArgs args);
        public abstract TArgs GetWriteArgs(TArgs args);
        public abstract bool Handle(TArgs a, TArgs b);
    }

    public class CoherenceContext<TArgs> //where TArgs : EventArgs
    {
        public Sync<TArgs> Sync;

        private Dictionary<object, Task<object>> InTransit { get; }
        private List<IAsyncEventProcessor<TArgs>> Processors { get; }
        private List<IDatastore<TArgs, TArgs>> Datastores { get; }

        public IAsyncEventProcessor<TArgs> Synchronize(IAsyncEventProcessor<TArgs> processor)
        {
            Processors.Add(processor);
            return null;
            //return new SynchronizedProcessor(processor, this);
        }

        public IAsyncEventProcessor<TArgs> Unsynchronize(IAsyncEventProcessor<TArgs> processor)
        {
            if (processor is SynchronizedProcessor syncProcessor)
            {
                Processors.Remove(syncProcessor.Processor);
                return syncProcessor.Processor;
            }
            else
            {
                return processor;
            }
        }

        private class SynchronizedProcessor
        {
            public IAsyncEventProcessor<TArgs> Processor { get; }
            public IDatastore<TArgs, TArgs> Datastore { get; }
            public CoherenceContext<TArgs> Context { get; }

            public SynchronizedProcessor(IAsyncEventProcessor<TArgs> processor, CoherenceContext<TArgs> context)
            {
                Processor = processor;
                Context = context;
            }

            public async Task ReadAsync(TArgs args)
            {
                var key = Context.Sync.GetKey(args);
                Task readTask;

                lock (Context.InTransit)
                {
                    if (Context.InTransit.TryGetValue(key, out var value))
                    {
                        readTask = HandleAsync(args, value);
                    }
                    else
                    {
                        readTask = Context.InTransit[key] = GetValueAsync(Datastore.ReadAsync(args), args);
                    }
                }

                try
                {
                    await readTask;
                }
                finally
                {
                    lock (Context.InTransit)
                    {
                        Context.InTransit.Remove(key);
                    }
                }
            }

            public async Task WriteAsync(TArgs args)
            {
                var key = Context.Sync.GetKey(args);
                var value = Task.FromResult(Context.Sync.GetValue(args));

                lock (Context.InTransit)
                {
                    Context.InTransit.TryAdd(key, value);
                }

                try
                {
                    await Datastore.WriteAsync(args);
                }
                finally
                {
                    lock (Context.InTransit)
                    {
                        if (Context.InTransit.TryGetValue(key, out var updated) && updated == value)
                        {
                            Context.InTransit.Remove(key);
                        }
                    }
                }
            }

            private async Task<object> GetValueAsync(Task task, TArgs args)
            {
                await task;
                return Context.Sync.GetValue(args);
            }

            private async Task HandleAsync(TArgs args, Task<object> response) => (args as EventArgsRequest)?.Handle(await response);
        }
    }
}