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

    public class MediaToJsonConverter : IValueConverter
    {
        public static readonly MediaToJsonConverter Instance = new MediaToJsonConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (false == value is Media media)
            {
                return value;
            }

            return HttpUtility.HtmlEncode(JsonSerializer.Serialize(JsonDocument.Parse("\"" + RatingTemplateCollectionViewModel.GetMediaJson(media) + "\""), new JsonSerializerOptions { WriteIndented = true }));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
            TestCommand = new Command<Media>(async media => await Test(media));
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

        public async Task Test(Media media)
        {
            if (SelectedItem == null || media == null)
            {
                return;
            }

            string[] urls = null;

            if (SelectedItem.URLJavaScipt == null)
            {
                UrlJavaScriptTestResult = null;
            }
            else
            {
                if (media.TryGetID(TMDB.ID, out var tmdbId))
                {
                    media = await TMDB.WithExternalIdsAsync(media, tmdbId) as Media ?? media;
                }

                using (var evaluator = JavaScriptEvaluationService.Factory.Create())
                {
                    try
                    {
                        var json = await evaluator.Evaluate(GetUrlJavaScript(SelectedItem, media));
                        urls = ParseUrls(json);
                        UrlJavaScriptTestResult = JsonToString(json);
                    }
                    catch (Exception e)
                    {
                        UrlJavaScriptTestResult = new JavaScriptEvaluationException(e);
                    }
                }
            }

            if (urls == null || SelectedItem.ScoreJavaScript == null)
            {
                ScoreJavaScriptTestResult = null;
            }
            else
            {
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
        }

        private static async Task<string> EvaluateJsSafe(IJavaScriptEvaluator evaluator, string script)
        {
            try
            {
                return await evaluator.Evaluate(script);
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Task<Rating>>> ApplyTemplatesAsync(Media media)
        {
            if (media.TryGetID(TMDB.ID, out var tmdbId))
            {
                media = await TMDB.WithExternalIdsAsync(media, tmdbId) as Media ?? media;
            }

            string[] responses;
            using (var urlEvaluator = JavaScriptEvaluationService.Factory.Create())
            {
                responses = await Task.WhenAll(Items.Select(template => EvaluateJsSafe(urlEvaluator, GetUrlJavaScript(template, media))));
            }

            var evaluators = new Dictionary<string, Lazy<IJavaScriptEvaluator>>();
            var results = new Lazy<IJavaScriptEvaluator>[responses.Length][];

            for (int i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                if (response == null)
                {
                    results[i] = new Lazy<IJavaScriptEvaluator>[0];
                    continue;
                }

                var urls = RatingTemplateCollectionViewModel.ParseUrls(response);
                var result = results[i] = new Lazy<IJavaScriptEvaluator>[urls.Length];

                for (int j = 0; j < urls.Length; j++)
                {
                    var url = urls[j];
                    if (!evaluators.TryGetValue(url, out var evaluator))
                    {
                        evaluators.Add(url, evaluator = new Lazy<IJavaScriptEvaluator>(() => JavaScriptEvaluationService.Factory.Create(url)));
                    }

                    result[j] = evaluator;
                }
            }

            var tasks = Items.Zip(results, GetRatingAsync).ToList();
            DisposeEvaluators(Task.WhenAll(tasks), results.SelectMany(result => result));
            return tasks;
        }

        private static async void DisposeEvaluators(Task task, IEnumerable<Lazy<IJavaScriptEvaluator>> evaluators)
        {
            await task;

            foreach (var evaluator in evaluators)
            {
                if (evaluator.IsValueCreated)
                {
                    evaluator.Value.Dispose();
                }
            }
        }

        private async Task<Rating> GetRatingAsync(RatingTemplate template, IEnumerable<Lazy<IJavaScriptEvaluator>> evaluators)
        {
            var rating = new Rating
            {
                Company = new Company
                {
                    Name = template.Name,
                    LogoPath = template.LogoURL
                },
            };

            foreach (var evaluator in evaluators)
            {
                try
                {
                    var json = await evaluator.Value.Evaluate(template.ScoreJavaScript);
                    var doc = JsonDocument.Parse(json);

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
                catch { }
            }

            return rating;
        }

        public static string JsonToString(string json) => JsonDocument.Parse(json).RootElement.ToString();

        public static string GetUrlJavaScript(RatingTemplate template, Media media) => GetMediaJson(media) + ";" + template.URLJavaScipt;

        public static string GetMediaJson(Media media)
        {
            var json = $"item = {{ title: '{media.Title}', year: {media.Year}";
            var ids = new Dictionary<string, string>();

            if (media.TryGetID(TMDB.ID, out var tmdbId))
            {
                ids.Add("tmdb", tmdbId.ToString());
            }
            if (media.TryGetID(IMDb.ID, out var imdbId))
            {
                ids.Add("imdb", $"'{imdbId}'");
            }
            if (media.TryGetID(Wikidata.ID, out var wikiId))
            {
                ids.Add("wikidata", $"'{wikiId}'");
            }
            if (media.TryGetID(Facebook.ID, out var fbId))
            {
                ids.Add("facebook", $"'{fbId}'");
            }
            if (media.TryGetID(Instagram.ID, out var igId))
            {
                ids.Add("instagram", $"'{igId}'");
            }
            if (media.TryGetID(Twitter.ID, out var twitterId))
            {
                ids.Add("twitter", $"'{twitterId}'");
            }

            if (ids.Count > 0)
            {
                json += ", id: { ";
                json += string.Join(", ", ids.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                json += " }";
            }

            return json + " }";
        }

        public static string[] ParseUrls(string json)
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().Select(element => element.ToString()).ToArray()
                : new string[] { doc.RootElement.ToString() };
        }
    }
}
