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

        protected Task RetrieveResource<T>(Item item, MultiProperty<T> property) => RetrieveResource<IEnumerable<T>>(item, property);
        protected Task RetrieveResource<T>(Item item, Property<T> property) => RetrieveResource<T>(item, (Property)property);
        private async Task RetrieveResource<T>(Item item, Property property)
        {
            var processor = ProcessorFactory.Create();
            var request = new KeyValueRequestArgs<Uri, T>(new UniformItemIdentifier(item, property));
            var requests = new[] { request };

            await processor.ProcessAsync(requests);

            ResourceAssert.Success(request);
        }
    }
}
