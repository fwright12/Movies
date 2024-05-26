using Movies.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class CarouselViewMeasureFirstBehavior : Behavior<VisualElement>
    {
        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);
        }
    }

    public class BoolToObjectConverter : IValueConverter
    {
        public object TrueObject { get; set; }
        public object FalseObject { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, true))
            {
                return TrueObject;
            }
            else if (Equals(value, false))
            {
                return FalseObject;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == TrueObject)
            {
                return true;
            }
            else if (value == FalseObject)
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }

    public class IsNotExceptionConverter : IValueConverter
    {
        public static readonly IsNotExceptionConverter Instance = new IsNotExceptionConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Exception)
            {
                return false;
            }
            else if (value == null)
            {
                return null;
            }
            else
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty(nameof(Converters))]
    public class MultiConverter : IValueConverter
    {
        public List<IValueConverter> Converters { get; } = new List<IValueConverter>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var converter in Converters)
            {
                value = converter.Convert(value, targetType, parameter, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var converter in Converters.Reverse<IValueConverter>())
            {
                value = converter.ConvertBack(value, targetType, parameter, culture);
            }

            return value;
        }
    }

    [ContentProperty(nameof(Commands))]
    public class MultiCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public List<ICommand> Commands { get; } = new List<ICommand>();

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            foreach (var command in Commands)
            {
                if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
        }
    }

    public class FocusCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public VisualElement Target { get; set; }

        public bool CanExecute(object parameter) => Target != null;

        public void Execute(object parameter)
        {
            Target.Focus();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public static readonly BoolToColorConverter SuccessFailureConverter = new BoolToColorConverter
        {
            TrueColor = Color.Green,
            FalseColor = Color.Red,
        };

        public Color TrueColor { get; set; }
        public Color FalseColor { get; set; }
        public Color DefaultColor { get; set; } = Color.Default;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, true))
            {
                return TrueColor;
            }
            else if (Equals(value, false))
            {
                return FalseColor;
            }
            else
            {
                return DefaultColor;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueColor))
            {
                return true;
            }
            else if (Equals(value, FalseColor))
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }

    public abstract class SearchViewModel<T> : BindableViewModel
    {
        public string Query
        {
            get => _Query;
            set => UpdateValue(ref _Query, value);
        }

        public AsyncListViewModel<T> Results
        {
            get => _Results;
            set => UpdateValue(ref _Results, value);
        }

        public ICommand SearchCommand { get; }

        private string _Query;
        private AsyncListViewModel<T> _Results;

        public SearchViewModel()
        {
            //SearchCommand = new Command<string>(async query => Search());
        }

        protected abstract IAsyncEnumerable<T> Search();
    }

    public class SearchFuncViewModel<T> : SearchViewModel<T>
    {
        public Func<string, IAsyncEnumerable<T>> SearchFunc { get; }

        public SearchFuncViewModel(Func<string, IAsyncEnumerable<T>> searchFunc)
        {
            SearchFunc = searchFunc;
        }

        protected override IAsyncEnumerable<T> Search() => SearchFunc(Query);
    }

    public class AsyncItemListViewModel : AsyncListViewModel<Item> { public AsyncItemListViewModel(IAsyncEnumerable<Item> source) : base(source) { } }

    public class RatingTemplateCollectionViewModel : CollectionManagerViewModel<RatingTemplate>
    {
        public AsyncItemListViewModel SearchResults
        {
            get => _SearchResults;
            private set => UpdateValue(ref _SearchResults, value);
        }

        public object UrlJavaScriptTestResult
        {
            get => _UrlJavaScriptTestResult;
            set => UpdateValue(ref _UrlJavaScriptTestResult, value);
        }
        public object ScoreJavaScriptTestResult
        {
            get => _ScoreJavaScriptTestResult;
            set => UpdateValue(ref _ScoreJavaScriptTestResult, value);
        }

        public ICommand SearchCommand { get; }
        public ICommand TestCommand { get; }

        private AsyncItemListViewModel _SearchResults;
        private object _UrlJavaScriptTestResult;
        private object _ScoreJavaScriptTestResult;

        public RatingTemplateCollectionViewModel()
        {
            SearchCommand = new Command<string>(async query => await Search(query));
            TestCommand = new Command<Item>(async item => await Test(item));
        }

        public async Task Search(string query)
        {
            var results = TMDB.Database.Instance.Search(query, FilterPredicate.TAUTOLOGY, typeof(Movie), typeof(TVShow));
            SearchResults = new AsyncItemListViewModel(results);
            await SearchResults.LoadMore(20);
        }

        private class JavaScriptEvaluationException : Exception
        {
            public JavaScriptEvaluationException(Exception inner) : base(inner.Message) { }

            public override string ToString() => Message;
        }

        public async Task Test(Item item)
        {
            UrlJavaScriptTestResult = null;
            ScoreJavaScriptTestResult = null;

            if (item == null || SelectedItem?.URLJavaScipt == null || !TryGetItemJson(item, out var jsonTask))
            {
                return;
            }

            string[] urls;
            using (var evaluator = JavaScriptEvaluationService.Factory.Create())
            {
                try
                {
                    var json = await evaluator.Evaluate(string.Join(";", await jsonTask, SelectedItem.URLJavaScipt));
                    urls = ParseUrls(json);
                    UrlJavaScriptTestResult = JsonToString(json);
                }
                catch (Exception e)
                {
                    UrlJavaScriptTestResult = new JavaScriptEvaluationException(e);
                    return;
                }
            }

            if (SelectedItem.ScoreJavaScript == null)
            {
                return;
            }

            Exception exception = null;

            foreach (var url in urls)
            {
                using (var scoreEvaluator = JavaScriptEvaluationService.Factory.Create(url))
                {
                    try
                    {
                        ScoreJavaScriptTestResult = JsonToString(await scoreEvaluator.Evaluate(SelectedItem.ScoreJavaScript));
                        return;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }

            ScoreJavaScriptTestResult = exception == null ? null : new JavaScriptEvaluationException(exception);
        }

        public Task<IEnumerable<Task<Rating>>> ApplyTemplatesAsync(Item item) => ApplyTemplatesAsync(item, Items);

        public static Task<IEnumerable<Task<Rating>>> ApplyTemplatesAsync(Item item, IEnumerable<RatingTemplate> templates) => ApplyTemplatesAsync(item, templates.ToArray());
        public static async Task<IEnumerable<Task<Rating>>> ApplyTemplatesAsync(Item item, params RatingTemplate[] templates)
        {
            if (!TryGetItemJson(item, out var jsonTask))
            {
                return Enumerable.Empty<Task<Rating>>();
            }

            var itemJson = await jsonTask;
            var evaluatorFactory = templates.Length == 1 ? JavaScriptEvaluationService.Factory : new CachedJsEvaluatorFactory(JavaScriptEvaluationService.Factory);

            var result = templates.Select(template => ApplyTemplateAsync(evaluatorFactory, template, itemJson)).ToList();
            _ = Task.WhenAll(result).ContinueWith(_ => (evaluatorFactory as CachedJsEvaluatorFactory)?.Dispose());
            return result;
        }

        private static async Task<Rating> ApplyTemplateAsync(IJavaScriptEvaluatorFactory evaluatorFactory, RatingTemplate template, string itemJson)
        {
            var rating = new Rating
            {
                Company = new Company
                {
                    Name = template.Name,
                    LogoPath = template.LogoURL
                },
            };

            if (template.URLJavaScipt != null && template.ScoreJavaScript != null)
            {
                try
                {
                    await UpdateRatingAsync(rating, template, evaluatorFactory, itemJson);
                }
                catch { }
            }

            return rating;
        }

        private static async Task UpdateRatingAsync(Rating rating, RatingTemplate template, IJavaScriptEvaluatorFactory evaluatorFactory, string itemJson)
        {
            string[] urls;
            using (var evaluator = evaluatorFactory.Create())
            {
                try
                {
                    var urlResponse = await evaluator.Evaluate(string.Join(";", itemJson, template.URLJavaScipt));
                    urls = ParseUrls(urlResponse);
                }
                catch
                {
                    return;
                }
            }

            foreach (var url in urls)
            {
                JsonDocument doc;
                using (var scoreEvaluator = evaluatorFactory.Create(url))
                {
                    try
                    {
                        doc = JsonDocument.Parse(await scoreEvaluator.Evaluate(template.ScoreJavaScript));
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("score", out var scoreElem))
                    {
                        rating.Score = scoreElem.ToString();
                    }
                    if (doc.RootElement.TryGetProperty("logo", out var logoElem))
                    {
                        rating.Company.LogoPath = logoElem.ToString();
                    }
                }
                else
                {
                    rating.Score = doc.RootElement.ToString();
                }

                break;
            }
        }

        public static string JsonToString(string json) => JsonDocument.Parse(json).RootElement.ToString();

        private static bool TryGetItemJson(Item item, out Task<string> jsonTask)
        {
            if (item is Movie || item is TVShow)
            {
                jsonTask = GetItemJsonAsync(item);
                return true;
            }
            else
            {
                jsonTask = Task.FromResult(string.Empty);
                return false;
            }
        }

        private static async Task<string> GetItemJsonAsync(Item item)
        {
            if (item.TryGetID(TMDB.ID, out var tmdbId))
            {
                item = await TMDB.WithExternalIdsAsync(item, tmdbId);
            }

            int? year = null;
            if (item is TVShow show)
            {
                var request = await DataService.Instance.Controller.TryGet<DateTime>(new UniformItemIdentifier(item, TVShow.FIRST_AIR_DATE));

                if (request.IsHandled)
                {
                    year = request.Value.Year;
                }
            }

            return TryGetItemJson(item, year, out var itemJson) ? itemJson : string.Empty;
        }

        public static bool TryGetItemJson(Item item, int? year, out string json)
        {
            string type;

            if (item is Movie movie)
            {
                type = "movie";
                year ??= movie.Year;
            }
            else if (item is TVShow show)
            {
                type = "tv";
            }
            else
            {
                json = default;
                return false;
            }

            json = $"item = {{ type: '{type}', title: '{item.Name}'";
            if (year.HasValue)
            {
                json += $", year: {year}";
            }
            var ids = new Dictionary<string, string>();

            if (item.TryGetID(TMDB.ID, out var tmdbId))
            {
                ids.Add("tmdb", tmdbId.ToString());
            }
            if (item.TryGetID(IMDb.ID, out var imdbId))
            {
                ids.Add("imdb", $"'{imdbId}'");
            }
            if (item.TryGetID(Wikidata.ID, out var wikiId))
            {
                ids.Add("wikidata", $"'{wikiId}'");
            }
            if (item.TryGetID(Facebook.ID, out var fbId))
            {
                ids.Add("facebook", $"'{fbId}'");
            }
            if (item.TryGetID(Instagram.ID, out var igId))
            {
                ids.Add("instagram", $"'{igId}'");
            }
            if (item.TryGetID(Twitter.ID, out var twitterId))
            {
                ids.Add("twitter", $"'{twitterId}'");
            }

            if (ids.Count > 0)
            {
                json += ", id: { ";
                json += string.Join(", ", ids.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                json += " }";
            }

            json += " }";
            return true;
        }

        public static string[] ParseUrls(string json)
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().Select(element => element.ToString()).ToArray()
                : new string[] { doc.RootElement.ToString() };
        }

        private class CachedJsEvaluatorFactory : IJavaScriptEvaluatorFactory, IDisposable
        {
            public IJavaScriptEvaluatorFactory Inner { get; }

            private Dictionary<string, IJavaScriptEvaluator> Evaluators { get; } = new Dictionary<string, IJavaScriptEvaluator>();

            public CachedJsEvaluatorFactory(IJavaScriptEvaluatorFactory inner)
            {
                Inner = inner;
            }

            public IJavaScriptEvaluator Create(string url = null)
            {
                var key = url ?? string.Empty;

                lock (Evaluators)
                {
                    if (!Evaluators.TryGetValue(key, out var evaluator))
                    {
                        Evaluators[key] = evaluator = new JavaScriptEvaluator(Inner.Create(url));
                    }

                    return evaluator;
                }
            }

            public void Dispose()
            {
                foreach (var evaluator in Evaluators.Values)
                {
                    (evaluator as JavaScriptEvaluator).Inner.Dispose();
                }
            }

            private class JavaScriptEvaluator : IJavaScriptEvaluator
            {
                public IJavaScriptEvaluator Inner { get; }

                public JavaScriptEvaluator(IJavaScriptEvaluator inner)
                {
                    Inner = inner;
                }

                public Task<string> Evaluate(string javaScript) => Inner.Evaluate(javaScript);

                public void Dispose() { }
            }
        }
    }
}
