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

    public class AsyncDataViewModel : BindableViewModel
    {
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

        protected bool TryGetValue<T>(Task<T> task, out T value, [CallerMemberName] string property = null)
        {
            if (task != null)
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    value = task.Result;
                    return true;
                }
                else if (!task.IsCompleted)
                {
                    //_ = SetValueAsync(property, task);
                    _ = SetValueFromTask(task, property);
                }
            }

            value = default;
            return false;
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
    }
}
