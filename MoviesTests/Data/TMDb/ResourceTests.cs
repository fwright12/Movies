using System.Collections;
using System.Text;

namespace MoviesTests.Data.TMDb
{
    internal class TMDbProcessorFactory : IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>>
    {
        public IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> Create()
        {
            return ServiceFactory.CreateMockTMDb(out _).Processor;
        }
    }

    [TestClass]
    public class MovieResourceTests : Data.MovieResourceTests
    {
        public MovieResourceTests() : base(new TMDbProcessorFactory()) { }
    }
}
