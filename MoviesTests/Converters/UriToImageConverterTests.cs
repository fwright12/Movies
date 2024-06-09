using Microsoft.VisualStudio.TestTools.UnitTesting;
using Movies.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesTests.Converters
{
    [TestClass]
    public class UriToImageSourceConverterTests
    {
        //[TestMethod]
        public void ValidUri()
        {
            var uri = "https://www.example.com";
            var source = UriToImageSourceConverter.Instance.Convert(uri, null, null, null);

            Assert.AreEqual("", source.GetType().ToString());
        }
    }
}
