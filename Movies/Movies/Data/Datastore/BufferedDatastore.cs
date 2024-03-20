using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class KeyValueWriteArgs : EventArgs
    {
        public object Key { get; }
        public object Value { get; }

        public KeyValueWriteArgs(object key, object value)
        {
            Key = key;
            Value = value;
        }
    }

    public class KeyValueEventArgsFactory : IEventArgsFactory<KeyValueRequestArgs<Uri>, KeyValueWriteArgs>
    {
        public KeyValueWriteArgs CreateWriteArgs(KeyValueRequestArgs<Uri> args) => new KeyValueWriteArgs(args.Request.Key, args.Response);

        public object GetKey(KeyValueRequestArgs<Uri> args) => args.Request.Key;

        public object GetKey(KeyValueWriteArgs args) => args.Key;

        public bool Handle(KeyValueRequestArgs<Uri> args, KeyValueRequestArgs<Uri> handled) => args.Handle(handled.Response);

        public bool Handle(KeyValueRequestArgs<Uri> args, KeyValueWriteArgs handled) => args.Handle(handled.Value);
    }

    public interface IEventArgsFactory<TReadArgs, TWriteArgs>
    {
        public object GetKey(TReadArgs args);
        public object GetKey(TWriteArgs args);

        public TWriteArgs CreateWriteArgs(TReadArgs args);
        public bool Handle(TReadArgs args, TReadArgs handled);
        public bool Handle(TReadArgs args, TWriteArgs handled);
    }

    public interface IDatastore<in TReadArgs, in TWriteArgs>
    {
        Task<bool> ReadAsync(TReadArgs e);
        Task<bool> WriteAsync(TWriteArgs e);
    }

    public sealed class ReadOnlyDatastore<TReadArgs, TWriteArgs> : IDatastore<TReadArgs, TWriteArgs>
    {
        public IAsyncEventProcessor<TReadArgs> Processor { get; }

        public ReadOnlyDatastore(IAsyncEventProcessor<TReadArgs> processor)
        {
            Processor = processor;
        }

        public Task<bool> ReadAsync(TReadArgs e) => Processor.ProcessAsync(e);
        public Task<bool> WriteAsync(TWriteArgs e) => Task.FromResult(false);
    }

    public class BufferedDatastore<TReadArgs, TWriteArgs> : IDatastore<TReadArgs, TWriteArgs>, IDatastore<IEnumerable<TReadArgs>, IEnumerable<TWriteArgs>>
    {
        public IDatastore<IEnumerable<TReadArgs>, IEnumerable<TWriteArgs>> Datastore { get; }
        public IEventArgsFactory<TReadArgs, TWriteArgs> ArgsFactory { get; }

        private Dictionary<object, Result> Buffer;

        public BufferedDatastore(IDatastore<TReadArgs, TWriteArgs> datastore) { }
        public BufferedDatastore(IDatastore<IEnumerable<TReadArgs>, IEnumerable<TWriteArgs>> datastore)
        {
            Datastore = datastore;
        }

        public Task<bool> ReadAsync(TReadArgs args) => ReadAsync(args.AsEnumerable());
        public async Task<bool> ReadAsync(IEnumerable<TReadArgs> args)
        {
            var results = new List<Result>();
            var unbuffered = new BulkEventArgs<TReadArgs>();
            var source = new TaskCompletionSource<bool>();

            lock (Buffer)
            {
                foreach (var arg in args)
                {
                    var key = ArgsFactory.GetKey(arg);

                    if (!Buffer.TryGetValue(key, out var result))
                    {
                        Buffer[key] = result = new Result(arg, source.Task);
                        unbuffered.Add(arg);
                    }

                    results.Add(result);
                }
            }

            bool response;

            if (unbuffered.Count > 0)
            {
                try
                {
                    source.SetResult(response = await Datastore.ReadAsync(unbuffered));
                }
                catch (Exception e)
                {
                    source.SetException(e);
                    response = false;
                }
            }
            else
            {
                response = true;
            }

            var tasks = new List<Task>();
            var itr = args.GetEnumerator();

            lock (Buffer)
            {
                for (int i = 0; itr.MoveNext(); i++)
                {
                    var arg = itr.Current;
                    var key = ArgsFactory.GetKey(arg);

                    if (!Buffer.TryGetValue(key, out var result))
                    {
                        result = results[i];
                    }

                    tasks.Add(HandleAsync(arg, result));
                    TryRemove(arg);
                }
            }

            await Task.WhenAll(tasks);
            return response;
        }

        public Task<bool> WriteAsync(TWriteArgs args) => WriteAsync(args.AsEnumerable());
        public async Task<bool> WriteAsync(IEnumerable<TWriteArgs> args)
        {
            lock (Buffer)
            {
                UpdateBuffer(Buffer, args, Task.CompletedTask);
            }

            try
            {
                return await Datastore.WriteAsync(args);
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

        public void Write(TReadArgs args, Task task) => Write(args.AsEnumerable(), task);
        public async void Write(IEnumerable<TReadArgs> args, Task task)
        {
            try
            {
                lock (Buffer)
                {
                    UpdateBuffer(Buffer, args, task);
                }

                await task;
                await WriteAsync(args.Select(arg => ArgsFactory.CreateWriteArgs(arg)));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private async Task HandleAsync(TReadArgs args, Result result)
        {
            object handled;
            Task task;

            lock (result)
            {
                handled = result.Arg;
                task = result.Task;
            }

            await task;
            if (handled is TReadArgs readArgs)
            {
                ArgsFactory.Handle(args, readArgs);
            }
            else if (handled is TWriteArgs writeArgs)
            {
                ArgsFactory.Handle(args, writeArgs);
            }
        }

        private void UpdateBuffer(IDictionary<object, Result> buffer, IEnumerable args, Task task)
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
                    Buffer[key] = new Result(arg, task);
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
            if (arg is TReadArgs readArgs)
            {
                return ArgsFactory.GetKey(readArgs);
            }
            else if (arg is TWriteArgs writeArgs)
            {
                return ArgsFactory.GetKey(writeArgs);
            }
            else
            {
                throw new Exception($"arg must be either {typeof(TReadArgs)} or {typeof(TWriteArgs)}!");
            }
        }

        private class Result
        {
            public object Arg { get; private set; }
            public Task Task { get; private set; }

            public Result(object arg, Task task)
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