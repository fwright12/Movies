using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class DebouncedTaskCompletionSource<TResult>
    {
        public int Delay { get; }
        public Task<TResult> Task => Source.Task;

        private TResult Result;
        private TaskCompletionSource<TResult> Source;
        private CancellationTokenSource CancelSource;
        private object ResultLock = new object();

        public DebouncedTaskCompletionSource(int delay)
        {
            Delay = delay;
            Source = new TaskCompletionSource<TResult>();
            Reset();
        }

        public void SetResult(TResult result)
        {
            lock (ResultLock)
            {
                Result = result;
            }

            Reset();
        }

        public async void Reset()
        {
            CancellationToken token;

            lock (Source)
            {
                CancelSource?.Cancel();
                CancelSource = new CancellationTokenSource();
                token = CancelSource.Token;
            }

            await System.Threading.Tasks.Task.Delay(Delay);

            if (!token.IsCancellationRequested)
            {
                TResult value;
                lock (ResultLock)
                {
                    value = Result;
                }

                Source.TrySetResult(value);
            }
        }

        public bool TrySetResult(TResult result)
        {
            if (Task.IsCompleted)
            {
                return false;
            }

            SetResult(Result);
            return true;
        }

        public bool TryReset()
        {
            if (Task.IsCompleted)
            {
                return false;
            }

            Reset();
            return true;
        }
    }
}
