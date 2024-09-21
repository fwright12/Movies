namespace MoviesTests.Data
{
    public class MovieResourceTests : ItemResourceTests
    {
        public MovieResourceTests(IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> processorFactory) : base(processorFactory) { }

        [TestMethod]
        public async Task RetrieveCast()
        {
            var processor = ProcessorFactory.Create();
            var request = new KeyValueRequestArgs<Uri, IEnumerable<Credit>>(new UniformItemIdentifier(Constants.Movie, Media.CAST));

            await processor.ProcessAsync(request.AsEnumerable());

            ResourceAssert.Success(request);
        }

        [TestMethod]
        public async Task RetrieveRecommended()
        {
            var processor = ProcessorFactory.Create();
            var request = new KeyValueRequestArgs<Uri, IAsyncEnumerable<Item>>(new UniformItemIdentifier(Constants.Movie, Media.RECOMMENDED));

            await processor.ProcessAsync(request.AsEnumerable());

            ResourceAssert.Success(request);
        }

        [TestMethod]
        public async Task RetrieveRuntime()
        {
            var processor = ProcessorFactory.Create();
            var request = new KeyValueRequestArgs<Uri, TimeSpan>(new UniformItemIdentifier(Constants.Movie, Media.RUNTIME));

            await processor.ProcessAsync(request.AsEnumerable());

            ResourceAssert.Success(request);
        }
    }
}
