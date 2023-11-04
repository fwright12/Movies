using System;
using System.Threading.Tasks;

namespace Movies
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, EventArgsAsyncWrapper<TEventArgs> args);

    public static class AsyncEventHandlerExtensions
    {
        public static void Invoke<T>(this EventHandler<EventArgsAsyncWrapper<T>> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static Task InvokeAsync<T>(this AsyncEventHandler<T> handler, object sender, T e) where T : EventArgs => handler.Invoke(sender, new EventArgsAsyncWrapper<T>(e));

        public static EventHandler<EventArgsAsyncWrapper<T>> Create<T>(AsyncEventHandler<T> handler) where T : EventArgs => (sender, e) => e.ExecuteInvoke(sender, handler);
    }
}