using System;
using System.Threading.Tasks;

namespace Movies
{
    public interface IAsyncResponse
    {
        Task Response { get; }
    }

    public sealed class EventArgsAsyncWrapper<T> : EventArgs, IAsyncResponse
    {
        public T Args { get; }
        public Task Task { get; private set; }

        public Task Response => Task;

        public EventArgsAsyncWrapper(T args)
        {
            Args = args;
        }

        public void ExecuteInvoke(object sender, AsyncEventHandler<T> handler)
        {
            Task = handler.Invoke(sender, this);
        }
    }
}
