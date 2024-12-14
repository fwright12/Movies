namespace MoviesTests.ViewModels
{
    [TestClass]
    public class RatingTemplateCollectionViewModelTests
    {
        private static readonly string IMBD_ID = "tt00000";
        private static readonly string WIKIDATA_ID = "wikidata_id";
        private static readonly string FACEBOOK_ID = "facebook_id";
        private static readonly string INSTAGRAM_ID = "instagram_id";
        private static readonly string TWITTER_ID = "twitter_id";

        private static readonly Movie DUMMY_MOVIE = new Movie("test")
            .WithID(TMDB.IDKey, 0)
            .WithID(IMDb.IDKey, IMBD_ID)
            .WithID(Wikidata.IDKey, WIKIDATA_ID)
            .WithID(Facebook.IDKey, FACEBOOK_ID)
            .WithID(Instagram.IDKey, INSTAGRAM_ID)
            .WithID(Twitter.IDKey, TWITTER_ID);

        private static readonly string DUMMY_URL = "https://www.test.com";
        private static readonly string BAD_URL = "blahblah.com";
        private static readonly string INVALID_JS = "return 100";

        private static readonly RatingTemplate DUMMY_TEMPLATE = new RatingTemplate
        {
            Name = "Test",
            LogoURL = "https://www.logo.com",
            URLJavaScipt = "\"" + DUMMY_URL + "\"",
            ScoreJavaScript = "\"100\""
        };

        private static ICollection<JavaScriptEvaluatorFactory.JavaScriptEvaluator> EvaluatorsCreated => (JavaScriptEvaluationService.Factory as JavaScriptEvaluatorFactory)?.Instances!;
        private static int TotalQueries => EvaluatorsCreated.Sum(evaluator => evaluator.Queries.Count);

        [TestInitialize]
        public void Init()
        {
            JavaScriptEvaluationService.Register(new JavaScriptEvaluatorFactory
            {
                { null!, INVALID_JS, new Exception("'return' not inside function") },
                { DUMMY_URL, INVALID_JS, new Exception("'return' not inside function") },
                { BAD_URL, ".*", new Exception("site does not exist") }
            });
        }

        [TestMethod]
        public async Task ApplySingleTemplate()
        {
            var model = new RatingTemplateCollectionViewModel { Items = { DUMMY_TEMPLATE } };
            var rating = (await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie)))[0];

            Assert.AreEqual(DUMMY_TEMPLATE.Name, rating.Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, rating.Company.LogoPath);
            Assert.AreEqual("100", rating.Score);
        }

        [TestMethod]
        public async Task ApplyTemplateURLArray()
        {
            var template = new RatingTemplate
            {
                URLJavaScipt = $"[\"{BAD_URL}\", {DUMMY_TEMPLATE.URLJavaScipt}]",
                ScoreJavaScript = DUMMY_TEMPLATE.ScoreJavaScript
            };
            var model = new RatingTemplateCollectionViewModel { Items = { template } };
            var rating = (await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie)))[0];

            Assert.AreEqual("100", rating.Score);
            Assert.AreEqual(3, EvaluatorsCreated.Count);
            Assert.AreEqual(3, TotalQueries);
        }

        [TestMethod]
        public async Task ApplyTemplateScoreObject()
        {
            var template = new RatingTemplate
            {
                URLJavaScipt = DUMMY_TEMPLATE.URLJavaScipt,
                ScoreJavaScript = $"{{ \"score\" : 100, \"logo\": \"{DUMMY_TEMPLATE.LogoURL}\" }}"
            };
            var model = new RatingTemplateCollectionViewModel { Items = { template } };
            var rating = (await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie)))[0];

            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, rating.Company.LogoPath);
            Assert.AreEqual("100", rating.Score);
        }

        [TestMethod]
        public async Task ApplyTemplateWithInvalidURLJs()
        {
            var template = new RatingTemplate { Name = DUMMY_TEMPLATE.Name, LogoURL = DUMMY_TEMPLATE.LogoURL, URLJavaScipt = INVALID_JS };
            var model = new RatingTemplateCollectionViewModel { Items = { template } };
            var rating = (await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie)))[0];

            Assert.AreEqual(DUMMY_TEMPLATE.Name, rating.Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, rating.Company.LogoPath);
            Assert.AreEqual(null, rating.Score);
        }

        [TestMethod]
        public async Task ApplyTemplateWithInvalidScoreJs()
        {
            var template = new RatingTemplate { Name = DUMMY_TEMPLATE.Name, LogoURL = DUMMY_TEMPLATE.LogoURL, URLJavaScipt = DUMMY_TEMPLATE.URLJavaScipt, ScoreJavaScript = INVALID_JS };
            var model = new RatingTemplateCollectionViewModel { Items = { template } };
            var rating = (await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie)))[0];

            Assert.AreEqual(DUMMY_TEMPLATE.Name, rating.Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, rating.Company.LogoPath);
            Assert.AreEqual(null, rating.Score);
        }

        [TestMethod]
        public async Task ApplyEmptyTemplate()
        {
            var model = new RatingTemplateCollectionViewModel { Items = { new RatingTemplate() } };
            var rating = (await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie)))[0];

            Assert.AreEqual(null, rating.Company.Name);
            Assert.AreEqual(null, rating.Company.LogoPath);
            Assert.AreEqual(null, rating.Score);
        }

        [TestMethod]
        public async Task ApplyMultipleTemplates()
        {
            var dummy_url2 = DUMMY_URL + "/page";
            var model = new RatingTemplateCollectionViewModel
            {
                Items =
                {
                    DUMMY_TEMPLATE,
                    new RatingTemplate { Name = DUMMY_TEMPLATE.Name, LogoURL = DUMMY_TEMPLATE.LogoURL, URLJavaScipt = "\"" + dummy_url2 + "\"", ScoreJavaScript = DUMMY_TEMPLATE.ScoreJavaScript },
                }
            };
            var ratings = await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie));

            Assert.AreEqual(DUMMY_TEMPLATE.Name, ratings[0].Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, ratings[0].Company.LogoPath);
            Assert.AreEqual("100", ratings[0].Score);

            Assert.AreEqual(DUMMY_TEMPLATE.Name, ratings[1].Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, ratings[1].Company.LogoPath);
            Assert.AreEqual("100", ratings[1].Score);

            // 1 extra for evaluating url javascript
            Assert.AreEqual(3, EvaluatorsCreated.Count);
            Assert.AreEqual(4, TotalQueries);
        }

        [TestMethod]
        public async Task ApplyMultipleTemplatesWithIssues()
        {
            var model = new RatingTemplateCollectionViewModel
            {
                Items =
                {
                    DUMMY_TEMPLATE,
                    new RatingTemplate { Name = DUMMY_TEMPLATE.Name, LogoURL = DUMMY_TEMPLATE.LogoURL, URLJavaScipt = INVALID_JS, ScoreJavaScript = DUMMY_TEMPLATE.ScoreJavaScript },
                    new RatingTemplate { Name = DUMMY_TEMPLATE.Name, LogoURL = DUMMY_TEMPLATE.LogoURL, URLJavaScipt = DUMMY_TEMPLATE.URLJavaScipt, ScoreJavaScript = INVALID_JS },
                    new RatingTemplate()
                }
            };
            var ratings = await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie));

            Assert.AreEqual(DUMMY_TEMPLATE.Name, ratings[0].Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, ratings[0].Company.LogoPath);
            Assert.AreEqual("100", ratings[0].Score);

            Assert.AreEqual(DUMMY_TEMPLATE.Name, ratings[1].Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, ratings[1].Company.LogoPath);
            Assert.AreEqual(null, ratings[1].Score);

            Assert.AreEqual(DUMMY_TEMPLATE.Name, ratings[2].Company.Name);
            Assert.AreEqual(DUMMY_TEMPLATE.LogoURL, ratings[2].Company.LogoPath);
            Assert.AreEqual(null, ratings[2].Score);

            Assert.AreEqual(null, ratings[3].Company.Name);
            Assert.AreEqual(null, ratings[3].Company.LogoPath);
            Assert.AreEqual(null, ratings[3].Score);

            // 1 for evaluating url javascript + 1 unique score urls
            Assert.AreEqual(2, EvaluatorsCreated.Count, string.Join(", ", EvaluatorsCreated.Select(evaluator => evaluator.URL)));
            // 4 templates * (1 url javascript + 1 score javascript) - 1 with invalid url javascript - 1 with invalid score javascript
            Assert.AreEqual(5, TotalQueries, string.Join(", ", EvaluatorsCreated.SelectMany(evaluator => evaluator.Queries)));
        }

        [TestMethod]
        public async Task ApplyMultipleTemplatesSameURL()
        {
            var dummy_url2 = DUMMY_URL + "/page";
            var model = new RatingTemplateCollectionViewModel
            {
                Items =
                {
                    new RatingTemplate
                    {
                        //URLJavaScipt = $"[\"https://www.test1.com\", \"{BAD_URL}\", {DUMMY_TEMPLATE.URLJavaScipt}]",
                        URLJavaScipt = $"[\"{BAD_URL}\", {DUMMY_TEMPLATE.URLJavaScipt}, \"https://www.test1.com\"]",
                        ScoreJavaScript = DUMMY_TEMPLATE.ScoreJavaScript
                    },
                    new RatingTemplate { Name = DUMMY_TEMPLATE.Name, LogoURL = DUMMY_TEMPLATE.LogoURL, URLJavaScipt = "\"" + dummy_url2 + "\"", ScoreJavaScript = DUMMY_TEMPLATE.ScoreJavaScript },
                    DUMMY_TEMPLATE,
                    new RatingTemplate
                    {
                        URLJavaScipt = $"[{DUMMY_TEMPLATE.URLJavaScipt}, \"{BAD_URL}\", \"{dummy_url2}\"]",
                        ScoreJavaScript = DUMMY_TEMPLATE.ScoreJavaScript
                    }
                }
            };
            await Task.WhenAll(await model.ApplyTemplatesAsync(Constants.Movie));
            return;
            // 1 for evaluating url javascript + 3 unique score urls
            Assert.AreEqual(4, EvaluatorsCreated.Count);
            Assert.AreEqual(8, TotalQueries);
        }

        [TestMethod]
        public void GetMediaJson()
        {
            Assert.IsTrue(RatingTemplateCollectionViewModel.TryGetItemJson(DUMMY_MOVIE, null, out var movieJson));
            Assert.AreEqual(@"item = { type: 'movie', title: 'test', id: { tmdb: 0, imdb: ""tt00000"", wikidata: ""wikidata_id"", facebook: ""facebook_id"", instagram: ""instagram_id"", twitter: ""twitter_id"" } }", movieJson);

            Assert.IsTrue(RatingTemplateCollectionViewModel.TryGetItemJson(Constants.TVShow, 2024, out var tvJson));
            Assert.AreEqual(@"item = { type: 'tv', title: 'test', year: 2024, id: { tmdb: 0 } }", tvJson);
        }

        private class JavaScriptFailedException : Exception { }
    }

    public static class RatingTemplateCollectionViewModelHelpers
    {
        public static Task<Rating> GetRatingAsync(this RatingTemplateCollectionViewModel obj) => (Task<Rating>)obj.Invoke(nameof(GetRatingAsync), typeof(RatingTemplate), typeof(IEnumerable<Lazy<IJavaScriptEvaluator>>));
    }
}
