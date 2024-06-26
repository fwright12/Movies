﻿using Movies.Models;
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

        public IEnumerable<Rating> Ratings => RequestValue(Media.RATING) is Rating rating ? new List<Rating> { rating } : null;
        public IEnumerable<Rating> AllRatings => _Ratings ??= GetRatings();
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
            UpdateRatings(App.Current.RatingTemplateManager);

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

        private async void UpdateRatings(RatingTemplateCollectionViewModel templates)
        {
            try
            {
                await UpdateRatingsAsync(templates);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private async Task UpdateRatingsAsync(RatingTemplateCollectionViewModel templates)
        {
            await BatchRequest;

            var tasks = new List<Task<Rating>>(await templates.ApplyTemplatesAsync(Item));
            var remaining = new List<Task<Rating>>(tasks);
            while (remaining.Count > 0)
            {
                var task = await Task.WhenAny(remaining);
                remaining.Remove(task);

                UpdateRating(tasks.IndexOf(task) + 1, await task);
            }
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
