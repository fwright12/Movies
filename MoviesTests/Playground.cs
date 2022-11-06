using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesTests
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public async Task FilterStressTest()
        {
            var list = new TestList("test", Enumerable.Range(0, 1000).Select(Helpers.ToMovie));

            var watch = System.Diagnostics.Stopwatch.StartNew();
            int count = 0;

            var itr = list.GetAsyncEnumerator(new OperatorPredicate
            {
                LHS = Movie.WATCH_PROVIDERS,
                Operator = Operators.Equal,
                RHS = null
            });
            
            while (await itr.MoveNextAsync())
            {
                count++;
            }

            watch.Stop();
            Print.Log($"loaded {count} items in {watch.Elapsed}");
        }
    }
}
