using System;
using System.Threading.Tasks;

namespace Movies
{
    public interface IEventProcessor<in T>
    {
        bool Process(T e);
    }

    public interface ICoRProcessor<T>
    {
        bool Process(T e, IEventProcessor<T> next);
    }

    public interface ICoRStrictProcessor<in T>
    {
        bool Process(T e, Action next);
    }

    public interface IAsyncEventProcessor<in T>
    {
        Task<bool> ProcessAsync(T e);
    }

    public interface IAsyncCoRProcessor<T> //: IAsyncEventProcessor<T>
    {
        Task<bool> ProcessAsync(T e, IAsyncEventProcessor<T> next);

        //Task<bool> IAsyncEventProcessor<T>.ProcessAsync(T e) => ProcessAsync(e, null);
    }
}