using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesTests
{
    [TestClass]
    public class SessionTests
    {
        [TestMethod]
        public void PersistTests()
        {
            var session = new Session();

            DateTime? date = new DateTime(2000,1,1);

            session.LastAccessed = date;
            session.DBLastCleaned = date;

            Assert.AreEqual(date, session.LastAccessed);
            Assert.AreEqual(date, session.DBLastCleaned);
        }

        [TestMethod]
        public void FiltersTests()
        {
            var storage = new Dictionary<string, object>();

            var session = new Session(storage);
            var predicates = new List<OperatorPredicate>();
            var key = "test";

            session.SaveFilters(key, predicates);
            Assert.IsTrue(session.TryGetFilters(key, out var stored));
            Assert.AreEqual(predicates.Count, stored.Count());

            var otherSession = new Session(storage);
            Assert.AreEqual(1, otherSession.Filters.Count);
            Assert.IsTrue(otherSession.TryGetFilters(key, out stored));
            Assert.AreEqual(predicates.Count, stored.Count());
        }
    }
}
