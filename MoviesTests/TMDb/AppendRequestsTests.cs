using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Movies.TMDbRemoteResources;
using static Movies.TMDbRemoteResources.TMDbHttpRequest;

namespace MoviesTests.TMDb
{
    [TestClass]
    public class AppendRequestsTests
    {
        public static readonly List<TMDbRequest> Appendable = new List<TMDbRequest>
        {
            API.MOVIES.GET_DETAILS,
            API.TV.GET_DETAILS,
            API.PEOPLE.GET_DETAILS,
        };

        private const string APPEND_TO_RESPONSE = "append_to_response";

        [TestMethod]
        public void SingleRequest()
        {
            var uri = new UniformItemIdentifier(Constants.Movie, Media.RUNTIME);
            var resource = AssertRequests(uri)[0];

            Assert.AreEqual("", resource.Path);
            Assert.AreEqual("3/movie/0?language=en-US", resource.Request.GetUrl());
            //Assert.AreEqual(0, resource.Request.Paths.Count);
        }

        [TestMethod]
        public void SingleCompressedRequest()
        {
            var uris = Uiis(Constants.Movie, Media.KEYWORDS, Media.RUNTIME, Media.RECOMMENDED);
            var resources = AssertRequests(uris.ToArray());
            var paths = new string[] { "keywords", "recommendations" };

            Assert.AreEqual("", resources[0].Path);
            AssertUrl("3/movie/0", resources[0].Request.GetUrl(), paths);
            Assert.AreEqual("keywords", resources[1].Path);
            AssertUrl("3/movie/0", resources[1].Request.GetUrl(), paths);
            Assert.AreEqual("recommendations", resources[2].Path);
            AssertUrl("3/movie/0", resources[2].Request.GetUrl(), paths);
        }

        private void AssertUrl(string expectedPath, string actualPath, params string[] expectedPaths)
        {
            var split = actualPath.Split('?');
            var atr = split[1].Split('&').Select(arg => arg.Split('=')).First(args => args[0] == APPEND_TO_RESPONSE);

            Assert.AreEqual(expectedPath, split[0]);
            Assert.IsTrue(expectedPaths.ToHashSet().SetEquals(atr[1].Split(',').ToHashSet()));
        }

        private Resource[] AssertRequests(params Uri[] uris)
        {
            var request = new TMDbHttpRequest();
            return uris.Select(uri =>
            {
                Assert.IsTrue(request.TryAdd(uri, out var resource));
                return resource;
            }).ToArray();
        }

        private IEnumerable<UniformItemIdentifier> Uiis(Item item, params Property[] properties) => properties.Select(property => new UniformItemIdentifier(item, property));
    }
}
