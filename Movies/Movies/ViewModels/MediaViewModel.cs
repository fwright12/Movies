using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Movies.Models;

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
        public MediaViewModel(DataManager dataManager, Item item) : base(dataManager, item) { }

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

    public abstract class MediaViewModel<TItem> : MediaViewModel where TItem : Item
    {
        public string Title => Name;

        protected abstract MediaService<TItem> MediaService { get; }

        public string Tagline => RequestSingle(MediaService.TaglineRequested);
        public string Description => RequestSingle(MediaService.DescriptionRequested);
        public string ContentRating => RequestSingle(MediaService.ContentRatingRequested);
        public TimeSpan? Runtime => RequestSingle(MediaService.RuntimeRequested);
        public string OriginalTitle => RequestSingle(MediaService.OriginalTitleRequested);
        public string OriginalLanguage => RequestSingle(MediaService.OriginalLanguageRequested);
        public IEnumerable<string> Languages => RequestSingle(MediaService.LanguagesRequested);
        public IEnumerable<string> Genres => RequestSingle(MediaService.GenresRequested);

        public override string PrimaryImagePath => RequestSingle(MediaService.TrailerPathRequested);
        public string PosterPath => RequestSingle(MediaService.PosterPathRequested);
        public string BackdropPath => RequestSingle(MediaService.BackdropPathRequested);
        public string TrailerPath => RequestSingle(MediaService.TrailerPathRequested);
        //public override string PrimaryImagePath => TrailerPath;

        public IEnumerable<Rating> Ratings => RequestSingle(MediaService.RatingRequested) is Rating rating ? new List<Rating> { rating } : null;
        public List<Group<Credit>> Cast => GetCrew(RequestSingle(MediaService.CastRequested));
        public List<Group<Credit>> Crew => GetCrew(RequestSingle(MediaService.CrewRequested));
        public IEnumerable<Company> ProductionCompanies => RequestSingle(MediaService.ProductionCompaniesRequested);
        public IEnumerable<string> ProductionCountries => RequestSingle(MediaService.ProductionCountriesRequested);
        public List<Group<WatchProvider>> WatchProviders => Providers(RequestSingle(MediaService.WatchProvidersRequested));
        public IEnumerable<string> Keywords => RequestSingle(MediaService.KeywordsRequested);
        public CollectionViewModel Recommended => _Recommended ??= (RequestSingle(MediaService.RecommendedRequested) is IAsyncEnumerable<Item> items ? ForceLoad(new CollectionViewModel(DataManager, "Recommended", items)) : null);

        private CollectionViewModel _Recommended;

        protected static CollectionViewModel ForceLoad(CollectionViewModel cvm)
        {
            //cvm.LoadMoreCommand.Execute(null);
            return cvm;
        }

        public MediaViewModel(DataManager dataManager, Item item) : base(dataManager, item) { }
    }
}
