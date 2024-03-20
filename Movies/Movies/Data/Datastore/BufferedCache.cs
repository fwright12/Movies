using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
#if true
    public abstract class BufferedCache<TArgs> : IEventAsyncCache<TArgs>, IAsyncCoRProcessor<IEnumerable<TArgs>> where TArgs : EventArgsRequest
    {
        public IEventAsyncCache<TArgs> Cache { get; }

        protected BufferedCache(IEventAsyncCache<TArgs> cache)
        {
            Cache = cache;
        }

        public abstract void Process(TArgs e, TArgs buffered);
        public virtual object GetKey(TArgs e) => e;

        public virtual async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
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
                return await Cache.Write(args.Where(arg => arg.IsHandled));
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
#else
    public abstract class BufferedCache<TArgs> : IEventAsyncCache<TArgs>, IAsyncCoRProcessor<IEnumerable<TArgs>>
    {
        public IEventAsyncCache<TArgs> Cache { get; }

        protected BufferedCache(IEventAsyncCache<TArgs> cache)
        {
            Cache = cache;
        }

        public abstract void Process(TArgs e, TArgs buffered);
        public virtual object GetKey(TArgs e) => e;

        public Task<bool> Read(IEnumerable<TArgs> args)
        {
            return Cache.Read(args);
        }

        public Task<bool> Write(IEnumerable<TArgs> args)
        {
            return Cache.Write(args);
        }

        public virtual async Task<bool> ProcessAsync(IEnumerable<TArgs> e, IAsyncEventProcessor<IEnumerable<TArgs>> next)
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
#endif
}