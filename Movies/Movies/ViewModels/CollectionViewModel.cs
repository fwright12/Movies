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

                return (Source.Items as IList)?.Count ?? 0;
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
        public MultiEditor Filters { get; }

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

        private string _Name;

        public static readonly TreeToListConverter<object> TreeToListConverter = new TreeToListConverter<object>();

        public static readonly int NextYear = DateTime.Now.AddYears(1).Year;
        public static readonly Type FilterListType = typeof(FilterListViewModel<object>);
        public static readonly Type PredicateTreeType = typeof(ObservableNode<object>);

        public static readonly Predicate<object> EqualsZeroPredicate = value => value is int i && i == 0;
        public static readonly Predicate<object> GreaterThanOnePredicate = value => value is int i && i > 1;

        public class ItemWrapper : IAsyncFilterable<object>
        {
            private IAsyncEnumerable<Item> Items;
            private CollectionViewModel Collection;

            public ItemWrapper(IAsyncEnumerable<Item> items, CollectionViewModel collection)
            {
                Items = items;
                Collection = collection;
            }

            public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default) => GetAsyncEnumerator(FilterPredicate.TAUTOLOGY, cancellationToken);

            public async IAsyncEnumerator<object> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                var itr = (Items as IAsyncFilterable<Item>)?.GetAsyncEnumerator(predicate, cancellationToken) ?? Items.GetAsyncEnumerator(cancellationToken);

                while (await itr.MoveNextAsync())
                {
                    yield return Collection.Map(itr.Current);
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

            //Items = new ObservableCollection<object>();
            //var itr = !(items is LazyCollection<Item>) && items != null ? items.GetAsyncEnumerator() : ItemList.GetAsyncEnumerator();

            ToggleListLayoutCommand = new Command(() => ListLayout = ListLayout == ListLayouts.Grid ? ListLayouts.List : ListLayouts.Grid);

            var wrapper = new ItemWrapper(items, this);
            Source = new FilterListViewModel<object>(wrapper);
            Filters = new MultiEditor();//(types)

            var predicate = new ExpressionBuilder();
            // Not currently allowing these types
            allowedTypes &= ~(ItemType.TVSeason | ItemType.TVEpisode | ItemType.List | ItemType.AllCompanies);
            var types = Enum.GetValues(typeof(ItemType))
                .OfType<ItemType>()
                .Where(type => allowedTypes?.HasFlag(type) == true)
                .TrySelect<ItemType, Type>(App.TypeMap.TryGetValue)
                .Distinct()
                .ToArray();

            if (types.Length > 1)
            {
                var propertyEditor = new PropertyEditorFilter(Filters, types);
                propertyEditor.TypeSelector.Select(predicate.Root);

                predicate.Editor = propertyEditor;
                Source.Predicate = predicate;
            }
            else
            {
                predicate.Editor = Filters;

                if (Filters.Editors is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += (sender, e) =>
                    {
                        var filters = (IList)sender;
                        Source.Predicate = filters.Count == 0 ? null : predicate;
                    };
                }
            }

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

            if (Source.Items is INotifyCollectionChanged observable1)
            {
                observable1.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(Count));
            }

            //Items = items?.Select(Map) ?? new List<ItemViewModel>();
            //if (this is TVSeriesViewModel)
            //Items = new List<ItemViewModel>(Items);
        }

        private void Refresh() => Source.Reset();

        public CollectionViewModel(Person person) : this(person.Name, new ItemHelpers.FilterableWrapper<Item>(PersonViewModel.GetCredits(person)), ItemType.Movie | ItemType.TVShow, person) { }
        public CollectionViewModel(Collection collection) : this(collection.Name, collection, (collection as List)?.AllowedTypes, collection) { }

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
