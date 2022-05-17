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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new List<object> { value.ToString() };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is IList list && list.Count > 0 && Enum.TryParse(typeof(CollectionViewModel.Layout), list[0].ToString(), out var result) ? result : value;
    }

    public class CollectionViewModel : ItemViewModel
    {
        public enum Layout { Grid, List }

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
        public Layout ListLayout
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
        private Layout _ListLayout;

        public Collection List => (Collection)Item;
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

        //public static readonly Type TimeSpanConstraintType = typeof(Constraint<TimeSpan>);

        public static readonly Type LongPickerType = typeof(SelectorViewModel<long>);
        public static readonly Type DoublePickerType = typeof(SelectorViewModel<double>);
        public static readonly Type TimeSpanPickerType = typeof(SelectorViewModel<TimeSpan>);
        public static readonly Type DateTimePickerType = typeof(SelectorViewModel<DateTime>);
        public static readonly Type StringListType = typeof(SelectorViewModel<string>);
        public static readonly Type WatchProviderListType = typeof(MultiSelectorViewModel<WatchProvider>);
        public static readonly Type PersonListType = typeof(SelectorViewModel<PersonViewModel>);

        public static readonly Property<string> Search = new Property<string>(string.Empty);
        public static readonly Property<MonetizationType?> MonetizationType = new Property<MonetizationType?>("Monetization Type", GetNames<MonetizationType>());
        public static readonly Property<PersonViewModel> People = new Property<PersonViewModel>("People", new PeopleSearch());

        public static IEnumerable<T> GetNames<T>() where T : struct, Enum => Enum.GetNames(typeof(T)).Select(name => Enum.Parse<T>(name));

        public class PeopleSearch : Filterable<PersonViewModel>
        {
            protected override async IAsyncEnumerable<PersonViewModel> GetItems(List<Constraint> filters)
            {
                foreach (var item in await System.Threading.Tasks.Task.FromResult(new List<PersonViewModel> { new PersonViewModel(App.DataManager, MockData.Instance.MatthewM) }))
                {
                    yield return item;
                }
            }
        }

        public IList<FilterViewModel> Filters { get; }
        public FiltersViewModel Filter { get; } = new FiltersViewModel
        {
            Selectors =
            {
                new SelectorViewModel<TimeSpan>(Media.RUNTIME, Operators.GreaterThan, high: TimeSpan.FromMinutes(180))
                {
                    Comparable = true,
                },
                new MultiSelectorViewModel<string>(Media.GENRES),
                new SelectorViewModel<DateTime>(Movie.RELEASE_DATE, Operators.GreaterThan, low: new DateTime(1900, 1, 1), high: DateTime.Now.AddYears(1)),
                new MultiSelectorViewModel<string>(Media.CONTENT_RATING),
                new MultiSelectorViewModel<WatchProvider>(Media.WATCH_PROVIDERS)
                {
                    Presets =
                    {
                        new Preset
                        {
                            Text = "On my services",
                            Value =
                            {
                                new Constraint(Media.WATCH_PROVIDERS)
                                {
                                    Value = MockData.NetflixStreaming,
                                    Comparison = Operators.Equal
                                }
                            }
                        }
                    }
                },
                new MultiSelectorViewModel<MonetizationType?>(MonetizationType),
                new SelectorViewModel<PersonViewModel>(People)
                {
                    IsImmutable = true,
                    Filter = new FiltersViewModel
                    {
                        Selectors =
                        {
                            new SelectorViewModel<string>(Search),
                        }
                    }
                },
                new SelectorViewModel<string>(Media.KEYWORDS)
                {
                    IsImmutable = true,
                    Filter = new FiltersViewModel
                    {
                        Selectors =
                        {
                            new SelectorViewModel<string>(Search),
                        }
                    }
                },
                new SelectorViewModel<long>(Movie.BUDGET, Operators.GreaterThan, high: 2000000000)
                {
                    Step = 100000,
                },
                new SelectorViewModel<long>(Movie.REVENUE, Operators.GreaterThan, high: 2000000000)
                {
                    Step = 100000,
                }
            }
        };

        public ICommand UpdateDetails { get; }
        public IList SortOptions { get; set; }
        public string SortBy
        {
            get => _SortBy;
            set
            {
                if (value != _SortBy)
                {
                    _SortBy = value;
                    UpdateFilters(this, new EventArgs());
                    OnPropertyChanged(nameof(SortBy));
                }
            }
        }
        private string _SortBy;
        public bool SortAscending { get; private set; } = false;
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

        public CollectionViewModel(DataManager dataManager, string name, IAsyncEnumerable<Item> items, ItemType? allowedTypes, Item item) : base(dataManager, item)
        {
            DataManager = dataManager;
            Name = name;
            ListLayout = Layout.List;

            /*ItemList = new ObservableSet<Item>();
            if (items is LazyCollection<Item> lazy)
            {
                ItemList.AddLazy(lazy);
            }*/

            Items = new ObservableCollection<object>();
            ((INotifyCollectionChanged)Items).CollectionChanged += (sender, e) => OnPropertyChanged(nameof(Count));
            //var itr = !(items is LazyCollection<Item>) && items != null ? items.GetAsyncEnumerator() : ItemList.GetAsyncEnumerator();

            LazyLoadMoreCommand = new Lazy<ICommand>(() =>
            {
                UpdateItems(items);
                return new Command<int?>(async count => await Load(count));
            });

            if (allowedTypes.HasValue)
            {
                Filter.ValueChanged += UpdateFilters;

                var types = allowedTypes.ToString().Split(',').Select(type => Enum.Parse<ItemType>(type.Trim()));
                var selector = new MultiSelectorViewModel<ItemType?>(new Property<ItemType?>(nameof(ItemType), types))
                {
                    Name = string.Empty
                };
                Filter.Selectors.Insert(0, selector);

                var itemType = new ItemTypeFilterViewModel(allowedTypes ?? ItemType.Movie | ItemType.TVShow);
                itemType.ValueChanged += (sender, e) =>
                {
                    var type = ((ItemTypeFilterViewModel)sender).Selected;

                    if (type.HasFlag(ItemType.Movie))
                    {

                    }
                    if (type.HasFlag(ItemType.TVShow))
                    {

                    }
                };

                Filters = new ObservableCollection<FilterViewModel>
                {
                    new SearchFilterViewModel
                    {
                        Placeholder = "Search movies, TV, people, and more..."
                    },
                    itemType,
                };

                ToggleSortOrder = new Command(() =>
                {
                    SortAscending = !SortAscending;
                    OnPropertyChanged(nameof(SortAscending));
                });

                foreach (FilterViewModel model in Filters)
                {
                    model.ValueChanged += UpdateFilters;
                }
                PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(SortAscending))
                    {
                        UpdateFilters(this, EventArgs.Empty);
                    }
                };
            }

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

        private static async IAsyncEnumerable<T> Where<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            await foreach (var item in source)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        private async IAsyncEnumerable<Item> FilterAsync(List<Constraint> filters, IAsyncEnumerable<Item> source)
        {
            await foreach (var item in source)
            {
                if (await Allowed(item, filters))
                {
                    yield return item;
                }
            }
        }

        private async Task<bool> Allowed(Item item, IEnumerable<Constraint> filters)
        {
            var details = Data.GetDetails(item);

            foreach (var filter in filters)
            {
                if (filter.Property == null)
                {
                    /*if (!itemType.Value.HasFlag(item.ItemType))
                    {
                        return false;
                    }*/

                    continue;
                }
                else if (filter.Property is ReflectedProperty reflected)
                {
                    reflected.Info.GetValue(item);
                }
                else if (details.TryGetValue(filter.Property, out var task))
                {
                    var value = await task;

                    if (value is IComparable comparable)
                    {
                        if (comparable.CompareTo(filter.Value) != (int)filter.Comparison)
                        {
                            return false;
                        }
                    }
                    else if (filter.Comparison == Operators.Equal)
                    {
                        if (!Equals(value, filter.Value))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void UpdateFilters(object sender, EventArgs e)
        {
            if (Item is Collection collection)
            {
                var constraints = Enumerable.Empty<Constraint>().ToList();// ((BooleanExpression<Item>)Filter.GetConstraints()).Parts.OfType<Constraint>().ToList();
                var items = collection.GetItems(constraints);
                UpdateItems(FilterAsync(constraints, items));

                return;
            }

            string query = "";
            Dictionary<string, object> filters = new Dictionary<string, object>();

            foreach (FilterViewModel model in Filters)
            {
                if (model is SearchFilterViewModel search)
                {
                    query = search.Query;
                }
                else if (model is ItemTypeFilterViewModel types && types.SelectedOptions.Count > 0)
                {
                    filters.Add(nameof(ItemType), types.Selected);
                }
            }

            //UpdateItems(DataManager.Search(query, filters, SortBy, SortAscending));
        }

        public CollectionViewModel(DataManager dataManager, Person person) : this(dataManager, person.Name, PersonViewModel.GetCredits(dataManager.PersonService, person), null, person) { }
        public CollectionViewModel(DataManager dataManager, Collection collection) : this(dataManager, collection.Name, collection, null, collection) { }

        public CollectionViewModel(DataManager dataManager, string name, IAsyncEnumerable<Item> items, ItemType? allowedTypes = null) : this(dataManager, name, items, allowedTypes, null) { }

        protected ItemViewModel Map(Item item)
        {
            if (item is Movie movie) return new MovieViewModel(DataManager, movie);
            else if (item is TVShow show) return new TVShowViewModel(DataManager, show);
            else if (item is TVSeason season) return new TVSeasonViewModel(DataManager, season);
            else if (item is TVEpisode episode) return new TVEpisodeViewModel(DataManager, episode);
            else if (item is Collection collection) return new CollectionViewModel(DataManager, collection) { ListLabel = "Movies" };
            else if (item is Person person) return new PersonViewModel(DataManager, person);
            else return null;
        }
    }
}
