using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Movies
{
    public class AsyncChainEventArgs : ChainEventArgs
    {
        //public Task<bool> HandledAsync => HandledSource?.Task ?? Task.FromResult(false);

        private TaskCompletionSource<bool> HandledSource;
        private List<Task> Work = new List<Task>();
        public Task RequestedSuspension => _RequestedSuspension ?? Task.CompletedTask;
        private Task _RequestedSuspension;

        public void RequestSuspension(Task suspension)
        {
            _RequestedSuspension = suspension;
        }

        private void AddWork(Task work)
        {
            if (Handled)
            {
                return;
            }
            else if (Work.Count == 0)
            {
                HandledSource = new TaskCompletionSource<bool>();
            }
            
            Work.Add(work);
            _ = DoWork();
        }

        private async Task DoWork()
        {
            while (!Handled && Work.Count > 0)
            {
                var finished = await Task.WhenAny(Work);
                Work.Remove(finished);
            }

            HandledSource.TrySetResult(Handled);
        }
    }
}