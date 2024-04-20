namespace MoviesTests.ViewModels
{
    [TestClass]
    public class MediaViewModelTests
    {
        public static readonly Movie INTERSTELLAR = new Movie("Interstellar", 2014)
            .WithID(TMDB.IDKey, 157336)
            .WithID(IMDb.IDKey, "tt0816692");

        [TestMethod]
        public void ApplyTMDbTemplate()
        {
            var template = "www.themoviedb.org/movie/${item.id.tmdb}-${item.title}";
            var urls = Apply(template, INTERSTELLAR);

            Assert.AreEqual(1, urls.Length);
            Assert.IsTrue(urls.Contains("www.themoviedb.org/movie/157336-interstellar"));
        }

        [TestMethod]
        public void ApplyIMDbTemplate()
        {
            var template = "www.imdb.com/title/${id.imdb}";
            var urls = Apply(template, INTERSTELLAR);

            Assert.AreEqual(1, urls.Length);
            Assert.IsTrue(urls.Contains("www.imdb.com/title/tt0816692"));
        }

        [TestMethod]
        public void ApplyRottenTomatoesTemplateBasic()
        {
            var template = "www.rottentomatoes.com/m/${title}_${year}";
            var urls = Apply(template, INTERSTELLAR);

            Assert.AreEqual(1, urls.Length);
            Assert.IsTrue(urls.Contains("www.rottentomatoes.com/m/interstellar_2014"));
        }

        private static string[] Apply(string template, Media media) => MediaViewModel.ApplyToTemplate(template, media).Select(url => url.ToLower()).ToArray();
    }
}
