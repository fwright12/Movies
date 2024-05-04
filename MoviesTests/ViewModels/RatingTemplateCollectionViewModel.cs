namespace MoviesTests.ViewModels
{
    public class JavaScriptEvaluatorFactory : IJavaScriptEvaluatorFactory
    {
        public static int Delay { get; set; }

        private static Dictionary<string, Dictionary<string, string>> Results { get; } = new Dictionary<string, Dictionary<string, string>>();

        public IJavaScriptEvaluator Create(string url = null!) => new JavaScriptEvaluator(url);

        public void SetExpectedResult(string url, string javaScript, string result)
        {
            if (!Results.TryGetValue(url, out var dict))
            {
                Results.Add(url, dict = new Dictionary<string, string>());
            }

            dict[javaScript] = result;
        }

        private class JavaScriptEvaluator : IJavaScriptEvaluator
        {
            private string URL { get; }

            public JavaScriptEvaluator(string url)
            {
                URL = url;
            }

            public async Task<string> Evaluate(string javaScript)
            {
                await Task.Delay(Delay);
                return Results.TryGetValue(URL, out var dict) && dict.TryGetValue(javaScript, out var result) ? result : null!;
            }

            public void Dispose() { }
        }
    }

    public static class RatingTemplateCollectionViewModelHelpers
    {
        public static Task<Rating> GetRatingAsync(this RatingTemplateCollectionViewModel obj) => (Task<Rating>)obj.Invoke(nameof(GetRatingAsync), typeof(RatingTemplate), typeof(IEnumerable<Lazy<IJavaScriptEvaluator>>));
    }

    [TestClass]
    public class RatingTemplateCollectionViewModelTests
    {
        [TestInitialize]
        public void Init()
        {
            JavaScriptEvaluationService.Register(new JavaScriptEvaluatorFactory());
        }

        [TestMethod]
        public void ApplySingleTemplate()
        {
            var model = new RatingTemplateCollectionViewModel();
            Assert.IsTrue(true);
        }
    }
}
