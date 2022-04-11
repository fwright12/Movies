using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Movies.ViewModels
{
    public delegate void AsyncDataRequestEventHandler<T>(object sender, AsyncEventArgs<T> e);

    public class AsyncEventArgs<T> : EventArgs
    {
        public string PropertyName { get; }
        public object Value { get; private set; }
        public string Source { get; private set; }

        public AsyncEventArgs(string propertyName) => PropertyName = propertyName;

        public async Task<T> GetValueAsync() => Value is Task<T> task ? await task : (T)Value;

        public void SetValue(T value, string source) => SetValueInner(value, source);
        public void SetValue(Task<T> value, string source) => SetValueInner(value, source);

        private void SetValueInner(object value, string source)
        {
            Value = value;
            Source = source;
        }
    }

    public class WaitingEventArgs<T> : EventArgs
    {
        public event EventHandler<EventArgs<T>> ValueChanged;

        public T Value
        {
            get => _Value;
            set
            {
                if (!value.Equals(_Value))
                {
                    _Value = value;
                    ValueChanged?.Invoke(this, new EventArgs<T>(value));
                }
            }
        }

        private T _Value;

        public async Task UpdateValue(Task<T> task)
        {
            Value = await task;
        }
    }

    public class AsyncEventArgs1<T>
    {
        public Task<T> Task => Value.Task;
        public Datum Value { private get; set; }

        public class Datum
        {
            public Task<T> Task { get; private set; }

            public Datum(T value) => Task = System.Threading.Tasks.Task.FromResult(value);
            public Datum(Task<T> task) => Task = task;

            public static implicit operator Datum(T value) => new Datum(value);
            public static implicit operator Datum(Task<T> task) => new Datum(task);
        }
    }

    public interface IRequestInfo
    {
        event AsyncDataRequestEventHandler<object> InfoRequested;
    }

    public class AsyncDataViewModel : INotifyPropertyChanged, IRequestInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event AsyncDataRequestEventHandler<object> InfoRequested;

        private Dictionary<string, object> CachedValues;
        private List<Task> BatchedTasks = new List<Task>();

        public AsyncDataViewModel(bool cached = true)
        {
            if (cached)
            {
                CachedValues = new Dictionary<string, object>();
            }
        }

        private async Task SetValueFromTask(Task task, string property)
        {
            try
            {
                await task;
                OnPropertyChanged(property);
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                //throw e;
#endif
            }
        }

        protected T GetValue<T>(Task<T> task, [CallerMemberName] string property = null)
        {
            if (task != null)
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    return task.Result;
                }
                else
                {
                    //_ = SetValueAsync(property, task);
                    _ = SetValueFromTask(task, property);
                }
            }

            return default;
        }

        public T RequestSingle<T>(string source = null, [CallerMemberName] string name = null) => (T)RequestSingle(InfoRequested, source, name);

        public T RequestSingle<T>(AsyncDataRequestEventHandler<T> handler, string source = null, [CallerMemberName] string name = null)
        {
            object value;
            if (CachedValues.TryGetValue(name, out object cachedValue))
            {
                value = cachedValue;
            }
            else
            {
                /*var args = new AsyncEventArgs<T>();

                EventHandler<EventArgs<T>> temp = null;
                temp = (sender, e) =>
                {
                    CachedValues[name] = e.Value;
                    OnPropertyChanged(name);
                    args.ValueChanged -= temp;
                };

                args.ValueChanged += temp;*/

                AsyncEventArgs<T> args = new AsyncEventArgs<T>(name);
                //handler?.GetInvocationList().FirstOrDefault()?.DynamicInvoke(this, args);
                handler?.Invoke(this, args);
                //OnInfoRequested(args);
                value = args.Value;
            }

            T result = default;

            if (value is T t)
            {
                result = t;
            }
            else if (value is Task<T> task)
            {
                if (task.Status == TaskStatus.Created)
                {
                    _ = SetValueAsync(name, task);
                }
                else if (task.Status == TaskStatus.RanToCompletion)
                {
                    value = result = task.Result;
                }
            }
            else if (value != null)
            {
                System.Diagnostics.Debug.WriteLine("Request value: Could not convert type " + value.GetType() + " to " + typeof(T) + ", target property: '" + name + "'");
            }

            if (value != null && value != cachedValue)
            {
                CachedValues[name] = value;
            }

            return result;
        }

        private async Task SetValueAsync<T>(string name, Task<T> task) => SetValue(name, await task);

        private void SetValue(string name, object value)
        {
            if (!CachedValues.TryGetValue(name, out object cachedValue) || !cachedValue.Equals(value))
            {
                CachedValues[name] = value;
                OnPropertyChanged(name);
            }
        }

        public void ClearCache()
        {
            CachedValues.Clear();
        }

        public async Task BatchEnd()
        {
            while (BatchedTasks.Count > 0)
            {
                BatchedTasks.Remove(await Task.WhenAny(BatchedTasks));
            }
        }

        protected virtual void OnInfoRequested(AsyncEventArgs<object> args)
        {
            InfoRequested?.Invoke(this, args);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
