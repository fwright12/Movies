using Movies.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Movies.ViewModels
{
    public class Group<T> : List<T>
    {
        public string Name { get; }

        public Group() : base() { }

        public Group(string name, IEnumerable<T> items) : base(items)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }

    public abstract class MediaViewModel : ItemViewModel
    {
        public string Title => Name;

        public string Tagline => RequestValue(Media.TAGLINE);
        public string Description => RequestValue(Media.DESCRIPTION);
        public string ContentRating => RequestValue(ContentRatingProperty);
        public TimeSpan? Runtime => TryRequestValue(Media.RUNTIME, out var runtime) ? runtime : (TimeSpan?)null;
        public string OriginalTitle => RequestValue(Media.ORIGINAL_TITLE);
        public Language OriginalLanguage => RequestValue(Media.ORIGINAL_LANGUAGE);
        public IEnumerable<Language> Languages => RequestValue(Media.LANGUAGES);
        public IEnumerable<string> Genres => RequestValue(GenresProperty) is IEnumerable<Genre> genres ? genres.Select(genre => genre.Name) : null;

#if DEBUG
        public override string PrimaryImagePath => null;
#else
        public override string PrimaryImagePath => RequestValue(Media.TRAILER_PATH);
#endif
        public string PosterPath => RequestValue(Media.POSTER_PATH);
        public string BackdropPath => RequestValue(Media.BACKDROP_PATH);
        public string TrailerPath => null;
        //public override string PrimaryImagePath => TrailerPath;

        public IEnumerable<Rating> Ratings => _Ratings ??= GetRatings();
        public List<Group<Credit>> Cast => GetCrew(RequestValue(Media.CAST));
        public List<Group<Credit>> Crew => GetCrew(RequestValue(Media.CREW));
        public IEnumerable<Company> ProductionCompanies => RequestValue(Media.PRODUCTION_COMPANIES);
        public IEnumerable<string> ProductionCountries => RequestValue(Media.PRODUCTION_COUNTRIES);
        public List<Group<WatchProvider>> WatchProviders => Providers(RequestValue(WatchProvidersProperty));
        public IEnumerable<string> Keywords => RequestValue(Movie.KEYWORDS) is IEnumerable<Keyword> keywords ? keywords.Select(keyword => keyword.Name) : null;
        public CollectionViewModel Recommended => _Recommended ??= (RequestValue(Media.RECOMMENDED) is IAsyncEnumerable<Item> items ? new CollectionViewModel("Recommended", items) : null);

        public bool IsVideoPlaying
        {
            get => _IsVideoPlaying;
            set => UpdateValue(ref _IsVideoPlaying, value);
        }

        protected abstract Property<string> ContentRatingProperty { get; }
        protected abstract MultiProperty<Genre> GenresProperty { get; }
        protected abstract MultiProperty<WatchProvider> WatchProvidersProperty { get; }

        private CollectionViewModel _Recommended;
        private bool _IsVideoPlaying = false;
        private IList<Rating> _Ratings;

        protected static CollectionViewModel ForceLoad(CollectionViewModel cvm)
        {
            //cvm.LoadMoreCommand.Execute(null);
            return cvm;
        }

        public MediaViewModel(Item item) : base(item) { }

        private ObservableCollection<Rating> GetRatings()
        {
            string tmdbRating = nameof(tmdbRating);
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != tmdbRating)
                {
                    return;
                }

                var rating = RequestValue(Media.RATING, tmdbRating);
                UpdateRating(0, rating);
            };

            RequestValue(Media.RATING, tmdbRating);

            var itr = App.Current.RatingTemplateManager.Items.GetEnumerator();
            for (int i = 1; itr.MoveNext(); i++)
            {
                try
                {
                    UpdateRatingAsync(i, GetRatingAsync(itr.Current));
                }
                catch { }
            }

            return new ObservableCollection<Rating>(App.Current.RatingTemplateManager.Items.Select(template => new Rating
            {
                Company = new Company
                {
                    Name = template.Name,
                    LogoPath = template.LogoURL
                },
            }).Prepend(TMDB_DUMMY_RATING));
        }

        private static readonly Rating TMDB_DUMMY_RATING = new Rating
        {
            Company = TMDB.TMDb
        };

        private void UpdateRating(int index, Rating rating)
        {
            _Ratings[index] = rating;
        }

        private async void UpdateRatingAsync(int index, Task<Rating> rating)
        {
            try
            {
                UpdateRating(index, await rating);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private static async Task<string> GetMediaJsonAsync(Media media)
        {
            var json = $"item = {{ title: '{media.Title}', year: {media.Year}";

            if (media.TryGetID(TMDB.ID, out var tmdbId))
            {
                media = await TMDB.WithExternalIdsAsync(media, tmdbId) as Media ?? media;
                var ids = new Dictionary<string, string>();
                ids.Add("tmdb", tmdbId.ToString());

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
            }

            return json + " }";
        }

        private async Task<Rating> GetRatingAsync(RatingTemplate template)
        {
            try
            {
                var rating = new Rating
                {
                    Company = new Company
                    {
                        Name = template.Name,
                        LogoPath = template.LogoURL
                    },
                };

                try
                {
                    var json = await GetMediaJsonAsync((Media)Item);
                    var response = await App.JavaScriptEvaluator.Evaluate(json + ";" + template.URLJavaScipt);
                    Print.Log(response);
                    var doc = JsonDocument.Parse(response);
                    var urls = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().Select(element => element.ToString()) : new string[] { doc.RootElement.ToString() };

                    Print.Log("urls: " + string.Join(", ", urls));

                    foreach (var url in urls)
                    {
                        try
                        {
                            var esacpedUrl = new Uri(url.ToLower(), UriKind.RelativeOrAbsolute).ToString();
                            response = await App.JavaScriptEvaluator.Evaluate(template.ScoreJavaScript);
                            doc = JsonDocument.Parse(response);

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
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                    return rating;
                }

                return rating;
            }
            catch (Exception e)
            {
                Print.Log(e);
                return null;
            }
        }

        public static string[] ApplyToTemplate(string template, Media media)
        {
            var urls = new List<string> { template };

            foreach (var match in Regex.Matches(template, "\\${(.+?)}").Reverse())
            {
                var substitutions = Substitute(match.Groups[1].Value, media);
                var temp = new List<string>();

                foreach (var substitute in substitutions)
                {
                    foreach (var url in urls)
                    {
                        temp.Add(url.Substring(0, match.Index) + substitute + url.Substring(match.Index + match.Length));
                    }
                }

                urls = temp;
            }

            return urls.ToArray();
        }

        private static object[] Substitute(string property, Media media)
        {
            if (property == "title") return new object[] { media.Title };
            else if (property == "year") return new object[] { media.Year };
            else if (property.StartsWith("id."))
            {
                property = property.Substring(3);

                if (property == "tmdb")
                {
                    if (media.TryGetID(TMDB.ID, out var tmdbId)) return new object[] { tmdbId };
                }
                else if (property == "imdb")
                {
                    if (media.TryGetID(IMDb.ID, out var imdbId)) return new object[] { imdbId };
                }
                else if (property == "rt")
                {
                    List<string> ids = new List<string>
                    {
                        $"{media.Title}_{media.Year}",
                        $"{media.Title}",
                    };

                    if (media.Title.ToLower().StartsWith("the"))
                    {
                        ids.Insert(1, ids[0]);
                        ids.Insert(3, ids[2]);
                    }

                    return ids.ToArray();
                }
            }

            throw new Exception($"Unknown property {property}");
        }

        public static List<Group<Credit>> GetCrew(IEnumerable<Credit> crew)
        {
            if (crew == null)
            {
                return null;
            }

            var result = new List<Group<Credit>>(GroupSort(crew, credit => credit.Department).Select(group => new Group<Credit>(group.Key, group.Value)));

            if (result.Count > 1 && string.IsNullOrEmpty(result[result.Count - 1].Name))
            {
                result[result.Count - 1] = new Group<Credit>("Other", result[result.Count - 1]);
            }

            var index = result.FindIndex(group => group.Name?.ToLower() == "directing");

            if (index >= 0)
            {
                var directors = result[index];

                result.RemoveAt(index);
                result.Insert(0, directors);
            }

            return result;
        }

        public static IEnumerable<KeyValuePair<TSort, List<TValue>>> GroupSort<TSort, TValue>(IEnumerable<TValue> values, Func<TValue, TSort> sortBy)
        {
            Dictionary<TSort, List<TValue>> types = new Dictionary<TSort, List<TValue>>();
            List<TValue> notGrouped = new List<TValue>();

            foreach (var value in values)
            {
                var key = sortBy(value);

                if (key == null)
                {
                    notGrouped.Add(value);
                }
                else
                {
                    if (!types.TryGetValue(key, out var list))
                    {
                        list = new List<TValue>();
                        types.Add(key, list);
                    }

                    list.Add(value);
                }
            }

            return notGrouped.Count == 0 ? types : types.Concat(new List<KeyValuePair<TSort, List<TValue>>> { new KeyValuePair<TSort, List<TValue>>(default, notGrouped) });
        }

        public static List<Group<WatchProvider>> Providers(IEnumerable<WatchProvider> providers)
        {
            if (providers == null)
            {
                return null;
            }

            /*string TypeName(MonetizationType type)
            {
                if (type == MonetizationType.Ads) return "Free(Ads)";
                else return type.ToString();
            }*/

            return new List<Group<WatchProvider>>(GroupSort(providers, provider => provider.Type).OrderBy(kvp => (int)kvp.Key).Select(type => new Group<WatchProvider>(type.Key.ToString(), type.Value)));
        }
    }
}
