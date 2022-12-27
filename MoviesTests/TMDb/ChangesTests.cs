using System.Collections;
using HttpClient = Movies.HttpClient;

namespace MoviesTests
{
    [TestClass]
    public class ChangesTests
    {
        [TestMethod]
        public async Task ExpireTests()
        {
            var tmdb = TestsConfig.TMDb;

            var date = new DateTime(2000, 1, 1);
            await AssertExpired(date, date);
            await AssertExpired(date.AddDays(-1), date, await GetURL(date.AddDays(-1)));
            await AssertExpired(date.AddDays(-14), date, await GetURL(date.AddDays(-14)));
            await AssertExpired(date.AddDays(-40), date,
                await GetURL(date.AddDays(-40), date.AddDays(-26)),
                await GetURL(date.AddDays(-26), date.AddDays(-12)),
                await GetURL(date.AddDays(-12)));
        }

        private async Task<string> GetURL(DateTime start, DateTime? end = null) => $"3/{API.CHANGES.GET_MOVIE_CHANGE_LIST.Endpoint}?page=1&start_date={start}{(end.HasValue ? $"&end_date={end}" : "")}";

        private string GetStart(DateTime start) => $"start_date={start}";
        private string[] GetArgs(DateTime start, DateTime end) => new string[] { GetStart(start), $"end_date={end}" };

        private async Task AssertExpired(DateTime from, DateTime to, params string[] calls)
        {
            HttpClient.ClearCallHistory();

            //await TMDB.GetExpiredIds(ItemType.Movie, from, to);
            //AssertAreEqual(calls, HttpClient.CallHistory);

            System.Diagnostics.Debug.WriteLine(calls.PrettyPrint());
        }

        public static void AssertAreEqual(IEnumerable expected, IEnumerable actual)
        {
            try
            {
                Assert.AreEqual(expected.OfType<object>().Count(), expected.OfType<object>().Count(), "Sequences of unequal lengths");
            }
            catch (Exception e)
            {
                throw e;
            }

            foreach (var item in expected.OfType<object>().Zip(actual.OfType<object>()))
            {
                Assert.AreEqual(item.First, item.Second);
            }
        }
    }
}
