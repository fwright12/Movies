using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public interface ITaskCache<TArgs>
    {
        Task<bool> HandleAsync(Task<TArgs> args);
        bool TryGetTask(TArgs args, out Task task);
        void Add(TArgs args, Task task);
        void Update(TArgs args, Task task);
        bool TryRemove(TArgs arg);
    }

    public class BufferCache<TKey> : ITaskCache<KeyValueRequestArgs<TKey>>
    {
        private Dictionary<TKey, (KeyValueRequestArgs<TKey> Arg, Task Task)> Buffer;

        public void Add(KeyValueRequestArgs<TKey> args, Task task) => Buffer.Add(args.Request.Key, (args, task));

        public async Task<bool> HandleAsync(Task<KeyValueRequestArgs<TKey>> args)
        {
            var arg = await args;
            if (Buffer.TryGetValue(arg.Request.Key, out var buffered))
            {
                arg.Handle(buffered.Item1.Response);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetTask(KeyValueRequestArgs<TKey> args, out Task task)
        {
            if (Buffer.TryGetValue(args.Request.Key, out var value))
            {
                task = value.Task;
                return true;
            }
            else
            {
                task = default;
                return false;
            }
        }

        public bool TryRemove(KeyValueRequestArgs<TKey> arg) => Buffer.TryGetValue(arg.Request.Key, out var buffered) && arg == buffered.Arg;

        public void Update(KeyValueRequestArgs<TKey> args, Task task) => Add(args, task);
    }

    public interface IAsyncDatastore<TReadArgs, TWriteArgs>
    {
        public Task<bool> Read(TReadArgs args);
        public Task<bool> Write(TWriteArgs args);
    }

    public class BufferedDatastore<TArgs> : IEventAsyncCache<TArgs>
    {
        private ITaskCache<TArgs> Buffer { get; }

        private IEventAsyncCache<TArgs> Datastore { get; }
        private BufferedProcessor<TArgs> BufferedProcessor { get; }

        public BufferedDatastore(IEventAsyncCache<TArgs> datastore, ITaskCache<TArgs> buffer)
        {
            Datastore = datastore;
            Buffer = buffer;
            BufferedProcessor = new BufferedProcessor<TArgs>(new EventCacheReadProcessor<TArgs>(datastore), buffer);
        }

        public Task<bool> Read(IEnumerable<TArgs> args) => BufferedProcessor.ProcessAsync(args);

        public Task<bool> WriteAsync(TArgs args) => Write(args.AsEnumerable());
        public async Task<bool> Write(IEnumerable<TArgs> args)
        {
            lock (Buffer)
            {
                foreach (var arg in args)
                {
                    Buffer.Update(arg, Task.CompletedTask);
                }
            }

            try
            {
                return await Datastore.Write(args);
            }
            catch
            {
                return false;
            }
            finally
            {
                lock (Buffer)
                {
                    foreach (var arg in args)
                    {
                        //TryRemove(arg);
                    }
                }
            }
        }

        public void Write(TArgs args, Task task) => Write(args.AsEnumerable(), task);
        public async void Write(IEnumerable<TArgs> args, Task task)
        {
            try
            {
                lock (Buffer)
                {
                    foreach (var arg in args)
                    {
                        Buffer.Update(arg, task);
                    }
                }

                try
                {
                    await task;
                }
                finally
                {
                    await Write(args);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }
    }

    public class BufferedProcessor<TArgs> : IAsyncEventProcessor<IEnumerable<TArgs>>
    {
        public IAsyncEventProcessor<IEnumerable<TArgs>> Datastore { get; }
        private IDictionary<TArgs, Task> Buffer { get; }

        public BufferedProcessor(IAsyncEventProcessor<IEnumerable<TArgs>> datastore, ITaskCache<TArgs> buffer)
        {
            Datastore = datastore;
            //Buffer = buffer;
        }

        public async Task<bool> ProcessAsync(IEnumerable<TArgs> args)
        {
            var buffer = new Task<TArgs>[args.Count()];
            var unbuffered = new BulkEventArgs<TArgs>();
            var source = new TaskCompletionSource<bool>();
            Task<bool> response;
            
            lock (Buffer)
            {
                var itr = args.GetEnumerator();
                for (int i = 0; itr.MoveNext(); i++)
                {
                    var arg = itr.Current;
                    Task task;

                    if (!Buffer.TryGetValue(arg, out task))
                    {
                        Buffer.Add(arg, task = source.Task);
                        unbuffered.Add(arg);
                    }

                    buffer[i] = task.ContinueWith(_ => arg);
                }

                if (unbuffered.Count == 0)
                {
                    response = Task.FromResult(true);
                }
                else
                {
                    try
                    {
                        response = Datastore.ProcessAsync(unbuffered);

                        foreach (var arg in unbuffered)
                        {
                            Buffer[arg] = source.Task;
                        }
                    }
                    finally
                    {
                        (args as BulkEventArgs<TArgs>)?.Add(unbuffered);
                    }
                }
            }

            bool result;
            try
            {
                source.SetResult(result = await response);
            }
            catch (Exception e)
            {
                source.SetException(e);
                result = false;
            }
            finally
            {
                (args as BulkEventArgs<TArgs>)?.Add(unbuffered);
            }

            var tasks = new List<Task>();

            lock (Buffer)
            {
                var itr = args.GetEnumerator();
                for (int i = 0; itr.MoveNext(); i++)
                {
                    var arg = itr.Current;
                    Task<TArgs> task;

                    if (Buffer.TryGetValue(arg, out var task1))
                    {
                        task = task1.ContinueWith(_ => arg);
                    }
                    else
                    {
                        task = buffer[i];
                    }

                    //tasks.Add(Buffer.HandleAsync(task));
                    //Buffer.TryRemove(arg);
                }
            }

            await Task.WhenAll(tasks);
            //Print.Log(unbuffered.Count, string.Join("\n\t", args.Select(arg => arg.ToString() + ", " + ((EventArgsRequest)arg).IsHandled).Prepend(Cache.ToString())));
            return true;// args.All(arg => arg.IsHandled);
        }
    }

    public abstract class BufferedCache<TArgs> : IEventAsyncCache<TArgs> where TArgs : EventArgsRequest
    {
        public IEventAsyncCache<TArgs> Cache { get; }

        protected BufferedCache(IEventAsyncCache<TArgs> cache)
        {
            Cache = cache;
        }

        public abstract void Process(TArgs e, TArgs buffered);
        public virtual object GetKey(TArgs e) => e;

        private Dictionary<object, Value> Buffer = new Dictionary<object, Value>();

        public Task<bool> ReadAsync(TArgs args) => Read(args.AsEnumerable());
        public async Task<bool> Read(IEnumerable<TArgs> args)
        {
            var buffer = new Dictionary<object, Value>();
            var unbuffered = new BulkEventArgs<TArgs>();
            var source = new TaskCompletionSource<bool>();
            Task<bool> response;
            
            lock (Buffer)
            {
                foreach (var arg in args)
                {
                    var key = GetKey(arg);
                    
                    if (!Buffer.TryGetValue(key, out var value))
                    {
                        Buffer[key] = value = new Value(arg, source.Task);
                        unbuffered.Add(arg);
                    }

                    buffer[key] = value;
                }

                if (unbuffered.Count == 0)
                {
                    response = Task.FromResult(true);
                }
                else
                {
                    try
                    {
                        response = Cache.Read(unbuffered);

                        foreach (var arg in unbuffered)
                        {
                            var key = GetKey(arg);

                            if (!Buffer.TryGetValue(key, out var value))
                            {
                                Buffer[key] = value = new Value(arg, source.Task);
                            }
                        }
                    }
                    finally
                    {
                        (args as BulkEventArgs<TArgs>)?.Add(unbuffered);
                    }
                }
            }

            bool result;
            try
            {
                source.SetResult(result = await response);
            }
            catch (Exception e)
            {
                source.SetException(e);
                result = false;
            }
            finally
            {
                (args as BulkEventArgs<TArgs>)?.Add(unbuffered);
            }
            
            var tasks = new List<Task>();
            var itr = args.GetEnumerator();
            
            foreach (var arg in unbuffered)
            {
                var key = GetKey(arg);
            }

            lock (Buffer)
            {
                foreach (var arg in args)
                {
                    var key = GetKey(arg);
                    if (Buffer.TryGetValue(key, out var value) || buffer.TryGetValue(key, out value))
                    {
                        tasks.Add(HandleAsync(arg, value));
                    }

                    TryRemove(arg);
                }
            }

            await Task.WhenAll(tasks);
            //Print.Log(unbuffered.Count, string.Join("\n\t", args.Select(arg => arg.ToString() + ", " + ((EventArgsRequest)arg).IsHandled).Prepend(Cache.ToString())));
            return args.All(arg => arg.IsHandled);
        }

        public Task<bool> WriteAsync(TArgs args) => Write(args.AsEnumerable());
        public async Task<bool> Write(IEnumerable<TArgs> args)
        {
            lock (Buffer)
            {
                UpdateBuffer(Buffer, args, Task.CompletedTask);
            }

            try
            {
                return await Cache.Write(args);
            }
            catch
            {
                return false;
            }
            finally
            {
                lock (Buffer)
                {
                    foreach (var arg in args)
                    {
                        TryRemove(arg);
                    }
                }
            }
        }

        public void Write(TArgs args, Task task) => Write(args.AsEnumerable(), task);
        public async void Write(IEnumerable<TArgs> args, Task task)
        {
            try
            {
                lock (Buffer)
                {
                    UpdateBuffer(Buffer, args, task);
                }

                try
                {
                    await task;
                }
                finally
                {
                    await Write(args);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private async Task HandleAsync(TArgs arg, Value result)
        {
            object handled;
            Task task;

            lock (result)
            {
                handled = result.Arg;
                task = result.Task;
            }
            
            if (object.ReferenceEquals(arg, handled))
            {
                return;
            }

            try
            {
                await task;
            }
            finally
            {
                if (handled is TArgs readArgs)
                {
                    Process(arg, readArgs);
                }
                else if (handled is TArgs writeArgs)
                {
                    Process(arg, writeArgs);
                }
            }
        }

        private void UpdateBuffer(IDictionary<object, Value> buffer, IEnumerable args, Task task)
        {
            foreach (var arg in args)
            {
                var key = GetKey(arg);

                if (Buffer.TryGetValue(key, out var result))
                {
                    result.Update(arg, task);
                }
                else
                {
                    Buffer[key] = new Value(arg, task);
                }
            }
        }

        private bool TryRemove(object arg)
        {
            var key = GetKey(arg);

            if (Buffer.TryGetValue(key, out var latest) && object.ReferenceEquals(latest.Arg, arg))
            {
                return Buffer.Remove(key);
            }
            else
            {
                return false;
            }
        }

        private object GetKey(object arg)
        {
            if (arg is TArgs readArgs)
            {
                return GetKey(readArgs);
            }
            else if (arg is TArgs writeArgs)
            {
                return GetKey(writeArgs);
            }
            else
            {
                throw new Exception($"arg must be either {typeof(TArgs)} or {typeof(TArgs)}!");
            }
        }

        private class Value
        {
            public object Arg { get; private set; }
            public Task Task { get; private set; }

            public Value(object arg, Task task)
            {
                Update(arg, task);
            }

            public void Update(object arg, Task task)
            {
                lock (this)
                {
                    Arg = arg;
                    Task = task;
                }
            }
        }
    }
}