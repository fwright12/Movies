using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Movies
{
    public class HttpResource
    {
        public Type Type { get; }
        private Task Task { get; }

        public HttpResource(Type type, Task task)
        {
            Type = type;
            Task = task;
        }

        public static HttpResource Create<T>(IHttpConverter<T> converter) => null;

        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
    }

    public class HttpResource<T> : HttpResource
    {
        private Task<T> Task { get; }

        public HttpResource(Task<T> task) : base(typeof(T), task)
        {
            Task = task;
        }

        new public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();
    }
}