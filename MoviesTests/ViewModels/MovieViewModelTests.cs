namespace MoviesTests.ViewModels
{
    [TestClass]
    public class MovieViewModelTests : ItemViewModelTests
    {
        [TestMethod]
        public async Task ValueRetrievedAsynchronously()
        {
            var model = new MovieViewModel(Constants.Movie);

            Assert.AreEqual(default, model.Runtime);
            var value = await GetValue<TimeSpan>(model, nameof(MediaViewModel.Runtime));
            Assert.AreEqual(Constants.HARRY_POTTER_RUNTIME, value);
            Assert.AreEqual(Constants.HARRY_POTTER_RUNTIME, model.Runtime);
        }

        [TestMethod]
        public async Task ValueRetrievedInBatch()
        {
            var model = new MovieViewModel(Constants.Movie);

            DataService.Instance.BatchBegin();

            Assert.AreEqual(default, model.Runtime);
            var task = GetValue<TimeSpan>(model, nameof(MediaViewModel.Runtime));

            DataService.Instance.BatchEnd();

            var value = await task;
            Assert.AreEqual(Constants.HARRY_POTTER_RUNTIME, value);
            Assert.AreEqual(Constants.HARRY_POTTER_RUNTIME, model.Runtime);
        }
    }

    [TestClass]
    public class TVSeasonViewModelTests : ItemViewModelTests
    {
        //[TestMethod]
        public async Task ValueRetrievedAsynchronously()
        {
            var model = new TVSeasonViewModel(Constants.TVSeason);

            await model.Source.LoadMore(100);
            Assert.AreEqual(24, model.Source.Items.Count);
        }
    }

    [TestClass]
    public class PersonViewModelTests : ItemViewModelTests
    {
        //[TestMethod]
        public async Task ValueRetrievedAsynchronously()
        {
            var model = new PersonViewModel(Constants.Person);

        }
    }
}
