using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Movies.Models;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class SingleItemListConverter : IValueConverter
    {
        public static readonly SingleItemListConverter Instance = new SingleItemListConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new List<object> { value };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is IList list && list.Count == 1 ? list[0] : null;
    }

    public enum ListLayouts { Grid, List }

    public class CollectionViewModel : ItemViewModel
    {
        public override string Name
        {
            get => base.Name ?? _Name;
            set
            {
                if (value == Name)
                {
                    return;
                }

                _Name = value;
                if (Item != null)
                {
                    Item.Name = _Name;
                }
                OnPropertyChanged();
            }
        }
        public override string PrimaryImagePath => List?.PosterPath;
        public virtual string ListLabel { get; set; }
        public virtual string DescriptionLabel { get; set; } = "Description";
        public virtual string Description => List?.Description;
        public virtual bool Editable => false;
        public ListLayouts ListLayout
        {
            get => _ListLayout;
            set
            {
                if (value != _ListLayout)
                {
                    _ListLayout = value;
                    OnPropertyChanged();
                }
            }
        }
        private ListLayouts _ListLayout;

        public Collection List => Item as Collection;
        public int? Count
        {
            get
            {
                if (Item is Collection collection)
                {
                    if (collection.Count.HasValue)
                    {
                        return collection.Count.Value;
                    }
                    else if (collection is List list)
                    {
                        _ = UpdateCount(list);
                    }
                }

                return (Items as IList)?.Count ?? 0;
            }
        }

        private async Task UpdateCount(List list)
        {
            list.Count = 0;
            list.Count = await list.CountAsync();
            OnPropertyChanged(nameof(Count));
        }

        //public int? Count => (Item as Collection)?.Count ?? (Items as IList)?.Count ?? 0;// Item is List list ? GetValue(list.CountAsync()) : (int?)null;
        //public int? Count => Math.Max((Item as Collection)?.Count ?? 0, (Items as IList)?.Count ?? 0) ;// Item is List list ? GetValue(list.CountAsync()) : (int?)null;
        //public int? Count => Item is List list ? GetValue(list.CountAsync()) : ((Item as Collection)?.Items is IAsyncEnumerable<Item> items ? GetValue(CountAsync(items)) : (int?)null);

        private static async Task<int> CountAsync<T>(IAsyncEnumerable<T> source)
        {
            int count = 0;

            await foreach (var item in source)
            {
                count++;
            }

            return count;
        }

        public IEnumerable Items { get; }
        public ICommand LoadMoreCommand => LazyLoadMoreCommand?.Value;
        public ICommand ToggleSortOrder { get; }
        public ICommand ToggleListLayoutCommand { get; }

        //public static readonly Type TimeSpanPropertyType = typeof(Property<TimeSpan>);

        public static readonly string ITEM_TYPE = nameof(object.GetType);
        public static readonly Property<MonetizationType?> MonetizationType = new Property<MonetizationType?>("Monetization Type", GetNames<MonetizationType>());
        public static readonly Property<PersonViewModel> People = new Property<PersonViewModel>("People", new FilterListViewModel<PersonViewModel>(new App.PeopleSearch())
        {
            Predicate = new SearchPredicateBuilder()
        });

        public static IEnumerable<T> GetNames<T>() where T : struct, Enum => Enum.GetNames(typeof(T)).Select(name => Enum.Parse<T>(name));

        public FilterListViewModel<object> Source { get; }

        public ICommand UpdateDetails { get; }

        public bool SortAscending => List?.SortAscending ?? false;
        public IList<Property> SortOptions { get; set; }
        public Property SortBy
        {
            get => List?.SortBy;
            set
            {
                if (Item is Collection collection && value != collection.SortBy)
                {
                    collection.SortBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Loading
        {
            get => _Loading;
            private set
            {
                if (value != _Loading)
                {
                    _Loading = value;
                    OnPropertyChanged();
                }
            }
        }

        private Lazy<ICommand> LazyLoadMoreCommand;
        private string _Name;
        private bool _Loading;

        public static readonly TreeToListConverter<object> TreeToListConverter = new TreeToListConverter<object>();

        public static readonly int NextYear = DateTime.Now.AddYears(1).Year;
        public static readonly Type FilterListType = typeof(FilterListViewModel<object>);
        public static readonly Type PredicateTreeType = typeof(ObservableNode<object>);

        public static readonly Predicate<object> EqualsZeroPredicate = value => value is int i && i == 0;
        public static readonly Predicate<object> GreaterThanOnePredicate = value => value is int i && i > 1;

        private OperatorEditor GetComparableEditor(params Property[] properties) => GetComparableEditor(properties.FirstOrDefault()?.Values.OfType<object>().FirstOrDefault(), properties);
        private OperatorEditor GetComparableEditor(object defaultValue, params Property[] properties) => GetEditor(properties, defaultValue, null, Operators.GreaterThan, Operators.Equal, Operators.LessThan);
        private OperatorEditor GetEqualityEditor(params Property[] properties) => GetEqualityEditor(null, properties);
        private OperatorEditor GetEqualityEditor(ObservableNode<object> selected, params Property[] properties) => GetEditor(properties, null, selected, Operators.Equal);

        private OperatorEditor GetEditor(IEnumerable<Property> properties, object defaultValue, ObservableNode<object> selected, params Operators[] operators)
        {
            var editor = new OperatorEditor
            {
                LHSOptions = new ObservableCollection<object>(properties),
                OperatorOptions = operators.ToList<Operators>(),
                RHSOptions = !properties.Skip(1).Any() ? properties.FirstOrDefault()?.Values : properties.SelectMany(property => property.Values.OfType<object>()),
                DefaultLHS = properties.FirstOrDefault(),
                DefaultOperator = operators?.FirstOrDefault() ?? Operators.Equal,
                DefaultRHS = defaultValue
            };

            if (selected != null)
            {
                editor.Select(selected);
            }
            else
            {
                editor.AddNew();
            }

            return editor;
        }

        public class ItemWrapper : IAsyncFilterable<object>
        {
            private IAsyncEnumerable<Item> Items;
            private CollectionViewModel Collection;

            public ItemWrapper(IAsyncEnumerable<Item> items, CollectionViewModel collection)
            {
                Items = items;
                Collection = collection;
            }

            public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default) => GetItems(FilterPredicate.TAUTOLOGY, cancellationToken).GetAsyncEnumerator();

            public async IAsyncEnumerable<object> GetItems(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                var filtered = (Items as IAsyncFilterable<Item>)?.GetItems(predicate) ?? Items;

                await foreach (var item in filtered)
                {
                    yield return Collection.Map(item);
                }
            }
        }

        public CollectionViewModel(string name, IAsyncEnumerable<Item> items, ItemType? allowedTypes, Item item) : base(item)
        {
            Name = name;
            ListLayout = ListLayouts.List;

            /*ItemList = new ObservableSet<Item>();
            if (items is LazyCollection<Item> lazy)
            {
                ItemList.AddLazy(lazy);
            }*/

            Items = new ObservableCollection<object>();
            //var itr = !(items is LazyCollection<Item>) && items != null ? items.GetAsyncEnumerator() : ItemList.GetAsyncEnumerator();

            LazyLoadMoreCommand = new Lazy<ICommand>(() =>
            {
                UpdateItems(items);
                return new Command<int?>(async count => await Load(count));
            });
            ToggleListLayoutCommand = new Command(() => ListLayout = ListLayout == ListLayouts.Grid ? ListLayouts.List : ListLayouts.Grid);

            if (allowedTypes.HasValue)
            {
                /*if (Movie.WATCH_PROVIDERS.Values is IList list)
                {
                    list.Clear();
                    list.Add(MockData.NetflixStreaming);
                }*/

                var types = new Type[]
                {
                    typeof(Movie),
                    typeof(TVShow),
                    typeof(Person),
                    typeof(Collection),
                };

                var predicate = new ExpressionBuilder();
                var wrapper = new ItemWrapper(items, this);
                Source = new FilterListViewModel<object>(wrapper)
                {
                    Predicate = predicate
                };

                var editor = new MultiEditor//(types)
                {
                    GetComparableEditor(new DateTime(1900, 1, 1), Movie.RELEASE_DATE),
                    GetComparableEditor(Media.RUNTIME),
                    GetEqualityEditor(Movie.CONTENT_RATING),// TVShow.CONTENT_RATING),
                    GetEqualityEditor(predicate.Root, Movie.GENRES, TVShow.GENRES),
                    GetComparableEditor(TMDB.SCORE),
                    GetEqualityEditor(predicate.Root, Movie.WATCH_PROVIDERS, TVShow.WATCH_PROVIDERS),
                    GetEqualityEditor(predicate.Root, MonetizationType),
                    GetEqualityEditor(People),
                    //GetComparableEditor(Movie.BUDGET),
                    //GetComparableEditor(Movie.REVENUE),
                    GetEqualityEditor(Media.KEYWORDS),
                };

                var propertyEditor = new PropertyEditorFilter(editor, types);
                propertyEditor.TypeSelector.Select(predicate.Root);

                predicate.Editor = propertyEditor;

                Items = Source.Items;
                LazyLoadMoreCommand = new Lazy<ICommand>(() =>
                {
                    return Source.LoadMoreCommand;
                });

                PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(SortBy))
                    {
                        List.SortBy = SortBy;
                        Refresh();
                    }
                };

                ToggleSortOrder = new Command(() =>
                {
                    List.SortAscending = !List.SortAscending;
                    OnPropertyChanged(nameof(SortAscending));
                    Refresh();
                });
            }

            ((INotifyCollectionChanged)Items).CollectionChanged += (sender, e) => OnPropertyChanged(nameof(Count));

            //Items = items?.Select(Map) ?? new List<ItemViewModel>();
            //if (this is TVSeriesViewModel)
            //Items = new List<ItemViewModel>(Items);
        }

        private IAsyncEnumerator<Item> Itr;

        public async Task Load(int? count = null)
        {
            if (Loading)
            {
                return;
            }

            Loading = true;
            try
            {
                for (int i = 0; i < (count ?? 1) && (await Itr.MoveNextAsync()); i++)
                {
                    if (Itr.Current == null)
                    {
                        i--;
                        continue;
                    }

                    (Items as IList)?.Add(Map(Itr.Current));
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                ;
#endif
            }

            Loading = false;
        }

        protected void UpdateItems(IAsyncEnumerable<Item> items)
        {
            if (items == null)
            {
                return;
            }

            (Items as IList)?.Clear();
            Itr = items.GetAsyncEnumerator();
            _ = Load(10);
        }

        private void Refresh() => Source.Refresh();

        public CollectionViewModel(Person person) : this(person.Name, PersonViewModel.GetCredits(person), null, person) { }
        public CollectionViewModel(Collection collection) : this(collection.Name, collection, null, collection) { }

        public CollectionViewModel(string name, IAsyncEnumerable<Item> items, ItemType? allowedTypes = null) : this(name, items, allowedTypes, null) { }

        protected ItemViewModel Map(Item item)
        {
            if (item is Movie movie) return new MovieViewModel(movie);
            else if (item is TVShow show) return new TVShowViewModel(show);
            else if (item is TVSeason season) return new TVSeasonViewModel(season);
            else if (item is TVEpisode episode) return new TVEpisodeViewModel(episode);
            else if (item is Collection collection) return new CollectionViewModel(collection) { ListLabel = "Movies" };
            else if (item is Person person) return new PersonViewModel(person);
            else return null;
        }
    }
}
