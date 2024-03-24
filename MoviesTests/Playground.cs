using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MoviesTests
{
    [TestClass]
    public class Playground
    {
        private class a : ByteArrayContent
        {
            public a(byte[] content) : base(content)
            {
            }

            protected override Task<Stream> CreateContentReadStreamAsync()
            {
                return base.CreateContentReadStreamAsync();
            }
        }

        private class http : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                stream.Write(Encoding.UTF8.GetBytes("this is a test"));
                return Task.CompletedTask;
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return true;
            }
        }

        //[TestMethod]
        public async Task something()
        {
            var content = new http();
            var json = await content.ReadAsByteArrayAsync();
            Print.Log(await content.ReadAsStringAsync());
            Print.Log(Encoding.UTF8.GetString(json));
        }

        //[TestMethod]
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
