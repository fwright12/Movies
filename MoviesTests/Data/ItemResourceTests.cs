namespace MoviesTests.Data
{
    public interface IProcessorFactory<T>
    {
        public IAsyncEventProcessor<T> Create();
    }

    public class ItemResourceTests
    {
        protected IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> ProcessorFactory { get; }

        public ItemResourceTests(IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> processorFactory)
        {
            ProcessorFactory = processorFactory;
        }
    }
}
