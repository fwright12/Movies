using MoviesTests.Data.TMDb;

namespace MoviesTests.ViewModels
{
    public class ItemViewModelTests
    {
        public static int DEFAULT_TIMEOUT = 5000;

        public ItemViewModelTests()
        {
            var handlers = new ResourceTests.HandlerChain();
            DataService.Instance.Controller.SetNext(new AsyncCacheAsideProcessor<ResourceRequestArgs<Uri>>(new ResourceBufferedCache<Uri>(handlers.LocalTMDbCache)))
                    .SetNext(handlers.RemoteTMDbProcessor);
        }

        protected static Task<T> GetValue<T>(ItemViewModel model, string propertyName)
        {
            var source = new TaskCompletionSource<T>();

            model.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == propertyName)
                {
                    source.TrySetResult((T)model.GetType().GetProperty(propertyName).GetValue(model));
                }
            };

            Timeout(source);
            return source.Task;
        }

        private static void Timeout<T>(TaskCompletionSource<T> source) => Timeout(source, DEFAULT_TIMEOUT);
        private static async void Timeout<T>(TaskCompletionSource<T> source, int timeout)
        {
            await Task.Delay(timeout);
            source.TrySetCanceled();
        }
    }
}
