using System.Text;

namespace MoviesTests.Data
{
    [TestClass]
    public class JsonIndexTests
    {
        [TestMethod]
        public void RemoveAppendedProperties()
        {
            var index = new JsonIndex(Encoding.UTF8.GetBytes(DUMMY_TMDB_DATA.HARRY_POTTER_AND_THE_DEATHLY_HALLOWS_PART_2_RESPONSE));

            foreach (var kvp in index)
            {
                ;
            }
        }
    }
}
