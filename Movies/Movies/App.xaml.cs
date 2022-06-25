using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Input;
using Movies.Models;
using Movies.ViewModels;
using Movies.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: ExportFont("Ionicons.ttf")]
[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Movies
{
    [Flags]
    public enum ItemType
    {
        Movie = 1,
        TVShow = 2,
        TVSeason = 4,
        TVEpisode = 8,
        Person = 16,
        Collection = 32,
        List = 64,
        Company = 128,
        Network = 256,
        WatchProvider = 512,
        AllMedia = Movie | TVShow | TVSeason | TVEpisode,
        AllCollections = Collection | List,
        AllCompanies = Company | Network | WatchProvider,
        All = ~0,
    }

    /* Changelog:
     * 
     * 
     * 
     */

    public partial class App : Application
    {
        private enum ServiceName
        {
            Local,
            TMDb,
            Trakt
        }

        public static readonly string[] AdKeywords = "movie,tv,shows,tv show,series,streaming,entertainment,watch,list,film,actor,guide,library,theater".Split(',').ToArray();

        private static readonly object HISTORY_ID = new string("History");
        private static readonly object WATCHLIST_ID = new string("Watchlist");
        private static readonly object FAVORITES_ID = new string("Favorites");

        public static readonly DataManager DataManager = new DataManager();

        public static readonly ImageSource TMDbAttribution = ImageSource.FromResource("Movies.Logos.TMDbAttribution.png");
        public static readonly ImageSource TraktAttributionLight = ImageSource.FromResource("Movies.Logos.TraktAttributionLight.png");
        public static readonly ImageSource TraktAttributionDark = ImageSource.FromResource("Movies.Logos.TraktAttributionDark.png");
        public static readonly ImageSource JustWatchAttribution = ImageSource.FromResource("Movies.Logos.JustWatchAttribution.png");

        //public IReadOnlyList<IDataSource> DataSources { get; }
        //public List<Group<CollectionViewModel>> Lists { get; private set; }
        public ListViewModel Watchlist => ValueFromTask(LazyWatchlist.Value);
        public ListViewModel History => ValueFromTask(LazyHistory.Value);
        public ListViewModel Favorites => ValueFromTask(LazyFavorites.Value);
        public IList<ListViewModel> CustomLists { get; }

        private Lazy<Task<ListViewModel>> LazyWatchlist;
        private Lazy<Task<ListViewModel>> LazyHistory;
        private Lazy<Task<ListViewModel>> LazyFavorites;

        private HashSet<Task> QueuedTasks = new HashSet<Task>();
        private T ValueFromTask<T>(Task<T> task, [System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
            if (task.IsCompleted)
            {
                return task.Result;
            }
            else if (QueuedTasks.Add(task))
            {
                _ = NotifyWhenTaskCompleted(task, property);
            }

            return default;
        }

        private async Task NotifyWhenTaskCompleted(Task task, string property)
        {
            await task;
            QueuedTasks.Remove(task);
            OnPropertyChanged(property);
        }

        public IList<object> MovieExplore { get; }
        public IList<object> TVExplore { get; }
        public IList<object> PeopleExplore { get; }
        public CollectionViewModel Popular => _Popular ??= GetPopular();
        //public List<WatchProvider> MyServices { get; } = null;// new List<WatchProvider>(MockData.Interstellar.WatchProviders);

        public IEnumerable<AccountViewModel> Accounts { get; }

        public static ICommand FocusCommand { get; } = new Command<VisualElement>(visualElement => visualElement.Focus());

        public ICommand AddSyncSourceCommand { get; }
        public ICommand LoadListsCommand { get; }
        public ICommand CreateListCommand { get; }
        public ICommand DeleteListCommand { get; }

        private Dictionary<ServiceName, IService> Services;
        private Database LocalDatabase;

        private CollectionViewModel _Popular;

        public App()
        {
#if DEBUG
            if (Device.RuntimePlatform == Device.iOS)
            {
                Properties[GetLoginCredentialsKey(ServiceName.TMDb)] = TMDB_LOGIN_INFO;
                Properties[GetLoginCredentialsKey(ServiceName.Trakt)] = TRAKT_LOGIN_INFO;
            }
#endif

            TMDB tmdb = new TMDB(TMDB_API_KEY, TMDB_V4_BEARER);
            Trakt trakt = new Trakt(tmdb, TRAKT_CLIENT_ID, TRAKT_CLIENT_SECRET);

            tmdb.Company.LogoPath ??= ImageSource.FromResource("Movies.Logos.TMDbLogo.png");
            trakt.Company.LogoPath ??= ImageSource.FromResource("Movies.Logos.TraktLogo.png");

#if DEBUG && false
            LocalDatabase = new Database(MockData.Instance, MockData.IDKey);

            DataManager.AddDataSource(MockData.Instance);
            //DataManager.AddDataSource(LocalDatabase);
            
            MovieExplore = new List<object>()
            {
                new CollectionViewModel(DataManager, "Trending", MockData.Instance.GetTrending()),
                new CollectionViewModel(DataManager, "Recommended", MockData.Instance.GetRecommended()),
                new CollectionViewModel(DataManager, "In Theaters", MockData.Instance.GetInTheaters()),
                new CollectionViewModel(DataManager, "Upcoming", MockData.Instance.GetUpcoming())
            };

            foreach (var collection in MovieExplore)
            {
                //CustomLists.Add(new List { Name = collection.Name, Items = collection.Items });
            }
#else
            LocalDatabase = new Database(tmdb, TMDB.IDKey);
            DataManager.AddDataSource(tmdb);

#if DEBUG
            MovieExplore = new List<object>
            {
                new CollectionViewModel(DataManager, "Trending Movies", tmdb.GetTrendingMoviesAsync()),
                new CollectionViewModel(DataManager, "Trending TV", tmdb.GetTrendingTVShowsAsync()),
                new CollectionViewModel(DataManager, "Trending People", tmdb.GetTrendingPeopleAsync()),
            };
#else
            MovieExplore = new ObservableCollection<object>
            {
                new CollectionViewModel(DataManager, "Trending", tmdb.GetTrendingMoviesAsync()),
                new CollectionViewModel(DataManager, "Most Anticipated", trakt.GetAnticpatedMoviesAsync()),
                new CollectionViewModel(DataManager, "In Theaters", tmdb.GetNowPlayingMoviesAsync()),
                new CollectionViewModel(DataManager, "Upcoming", tmdb.GetUpcomingMoviesAsync()),
                new CollectionViewModel(DataManager, "Top Rated", tmdb.GetTopRatedMoviesAsync()),
            };

            TVExplore = new ObservableCollection<object>
            {
                new CollectionViewModel(DataManager, "Trending", tmdb.GetTrendingTVShowsAsync()),
                new CollectionViewModel(DataManager, "Most Anticipated", trakt.GetAnticipatedTVAsync()),
                new CollectionViewModel(DataManager, "Currently Airing", tmdb.GetTVOnAirAsync()),
                new CollectionViewModel(DataManager, "Airing Today", tmdb.GetTVAiringTodayAsync()),
                new CollectionViewModel(DataManager, "Top Rated", tmdb.GetTopRatedTVShowsAsync()),
            };

            PeopleExplore = new ObservableCollection<object>
            {
                new CollectionViewModel(DataManager, "Trending", tmdb.GetTrendingPeopleAsync()),
                //new CollectionViewModel(DataManager, "Popular", tmdb.GetPopularPeopleAsync())
            };
#endif
#endif
            Services = new Dictionary<ServiceName, IService>
            {
                [ServiceName.Local] = LocalDatabase,
                [ServiceName.TMDb] = tmdb,
                [ServiceName.Trakt] = trakt,
            };

            QueuedProviders = new Queue<(IListProvider Provider, IAsyncEnumerator<List> Itr)>(LoggedInListProviders().Select(provider => (provider, provider.GetAllListsAsync().GetAsyncEnumerator())));
            LoadListsCommand = new Command<object>(parameter => _ = LoadLists(parameter != null && int.TryParse(parameter.ToString(), out var count) ? count : (int?)null));

            Accounts = Services.Where(kvp => kvp.Value is IAccount).Select(kvp =>
            {
                var account = (IAccount)kvp.Value;

                var model = new AccountViewModel(account);
                model.LoggedIn += (sender, e) => _ = Login(((AccountViewModel)sender).Account, e.Credentials);
                model.LoggedOut += (sender, e) => _ = Logout(((AccountViewModel)sender).Account);

                return model;
            }).ToList();

            if (Accounts.FirstOrDefault(account => account.Account is Trakt) is AccountViewModel traktAvm)
            {
                trakt.RedirectUri = traktAvm.RedirectUri;
                TraktSetup(traktAvm);
            }
            if (Accounts.FirstOrDefault(account => account.Account is TMDB) is AccountViewModel tmdbAvm)
            {
                TMDBSetup(tmdbAvm);
            }

            // Try to login in to accounts with stored credentials
            foreach (var account in Accounts.Reverse())
            {
                if (TryGetProviderName(account.Account, out var name) && Properties.TryGetValue(GetLoginCredentialsKey(name), out var credentials))
                {
                    _ = account.Login(credentials);
                }
            }

            LazyWatchlist = new Lazy<Task<ListViewModel>>(GetWatchlist);
            LazyFavorites = new Lazy<Task<ListViewModel>>(GetFavorites);
            LazyHistory = new Lazy<Task<ListViewModel>>(GetHistory);
            var customLists = new ObservableCollection<ListViewModel>();

            customLists.CollectionChanged += CustomListsChanged;

            CustomLists = customLists;

            CreateListCommand = new Command<ElementTemplate>(template => _ = CreateList(template));
            AddSyncSourceCommand = new Command<ListViewModel>(model => _ = AddSyncSource(model), list => list != null && list.SyncWith.Count != LoggedInListProviders().Count());
            DeleteListCommand = new Command<ListViewModel>(list => _ = DeleteList(list));

            PageDisappearing += (sender, e) =>
            {
                if (e.BindingContext?.GetType() == typeof(ListViewModel))
                {
                    var list = (ListViewModel)e.BindingContext;
                    if (list.Editing)// && !MainPage.Navigation.NavigationStack.Any(page => page.BindingContext == e.BindingContext)))
                    {
                        list.Editing = false;
                    }
                    else if (!CustomLists.Contains(list))
                    {
                        customLists.CollectionChanged -= CustomListsChanged;
                        CustomLists.Insert(0, list);
                        customLists.CollectionChanged += CustomListsChanged;
                    }
                }
            };

            InitializeComponent();

            MainPage = new MainPage();
        }

        public static readonly BindableProperty AutoWireFromItemProperty = BindableProperty.CreateAttached("AutoWireFromItem", typeof(Item), typeof(BindableObject), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            Item item = (Item)newValue;
            object context = null;

            if (item is Movie movie) context = new MovieViewModel(DataManager, movie);
            else if (item is TVShow show) context = new TVShowViewModel(DataManager, show);
            else if (item is TVSeason season) context = new TVSeasonViewModel(DataManager, season);
            else if (item is TVEpisode episode) context = new TVEpisodeViewModel(DataManager, episode);
            else if (item is Collection collection) context = new CollectionViewModel(DataManager, collection);
            else if (item is Person person) context = new PersonViewModel(DataManager, person);

            if (context != null)
            {
                bindable.BindingContext = context;
            }
        });

        public static Item GetAutoWireFromItem(BindableObject bindable) => (Item)bindable.GetValue(AutoWireFromItemProperty);
        public static void SetAutoWireFromItem(BindableObject bindable, Item value) => bindable.SetValue(AutoWireFromItemProperty, value);

        private async Task CreateList(ElementTemplate template)
        {
            var source = Services.Values.OfType<IListProvider>().FirstOrDefault();

            if (source == null)
            {
                return;
            }

            var model = new ListViewModel(DataManager)
            {
                Editing = true
            };

            model.AddSync(new ListViewModel.SyncOptions
            {
                Provider = source,
                List = source.CreateList()
            });

            if (model.Item is List list)
            {
                list.Name = "New List";
            }

            CustomListsChanged(CustomLists, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<ListViewModel> { model }));

            var content = template.CreateContent();
            var page = content as Page ?? new ContentPage { Content = content as View };
            page.BindingContext = model;

            await Shell.Current.CurrentPage.Navigation.PushAsync(page);
        }

        private async Task AddSyncSource(ListViewModel model)
        {
            if (model == null)
            {
                return;
            }

            var options = LoggedInListProviders().Except(model.SyncWith.Select(sync => sync.Provider)).ToList();
            var cancel = "Cancel";
            var choice = await Shell.Current.CurrentPage.DisplayActionSheet("Add Sync Source", cancel, null, options.Select(provider => provider.Name).ToArray());

            if (choice != cancel)
            {
                var provider = options.FirstOrDefault(provider => provider.Name == choice);

                var id = GetNamedId(model);
                var list = await GetList(provider, id);

                if (id != null && list == null)
                {
                    await foreach (var temp in provider.GetAllListsAsync())
                    {
                        if (temp.Name == model.Name)
                        {
                            list = temp;

                            /*for (int i = 0; i < CustomLists.Count; i++)
                            {
                                if (CustomLists[i].SyncWith.FirstOrDefault(sync => Equals(sync.List.ID, temp.ID)) is ListViewModel.SyncOptions sync)
                                {
                                    if (!CustomLists[i].RemoveSync(sync))
                                    {
                                        CustomLists.RemoveAt(i);
                                    }
                                    break;
                                }
                            }*/

                            break;
                        }
                    }
                }

                model.AddSync(new ListViewModel.SyncOptions
                {
                    Provider = provider,
                    List = list ?? provider.CreateList()
                });
            }
        }

        private async Task DeleteList(ListViewModel list)
        {
            if (list?.GetType() == typeof(ListViewModel) && await Shell.Current.CurrentPage.DisplayAlert("Are you sure?", "This will permanently delete this list and all synced lists. This action cannot be undone", "Delete", "Cancel"))
            {
                await Current.MainPage.Navigation.PopAsync();
                CustomLists.Remove(list);
            }
        }

#if DEBUG
        private void TMDBSetup(AccountViewModel tmdb) { }
        private void TraktSetup(AccountViewModel tmdb) { }
#else
        private void AddFirst(IList<object> list, CollectionViewModel model)
        {
            if (model.Items is INotifyCollectionChanged observable)
            {
                NotifyCollectionChangedEventHandler handler = null;
                handler = (sender, e) =>
                {
                    if ((sender as IList)?.Count != 0)
                    {
                        ((INotifyCollectionChanged)sender).CollectionChanged -= handler;
                        list.Insert(0, model);
                    }
                };

                observable.CollectionChanged += handler;
                _ = model.LoadMoreCommand;
            }
        }

        private void TMDBSetup(AccountViewModel tmdb)
        {
            string recommended = "Recommended by TMDb";

            tmdb.LoggedIn += (sender, e) =>
            {
                var account = (AccountViewModel)sender;

                if (account.Account is TMDB tmdb)
                {
                    AddFirst(MovieExplore, new CollectionViewModel(DataManager, recommended, tmdb.GetRecommendedMoviesAsync()));
                    AddFirst(TVExplore, new CollectionViewModel(DataManager, recommended, tmdb.GetRecommendedTVShowsAsync()));
                }
            };

            tmdb.LoggedOut += (sender, e) =>
            {
                foreach (var list in new List<IList<object>> { MovieExplore, TVExplore })
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if ((list[i] as CollectionViewModel)?.Name == recommended)
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }
            };
        }

        private void TraktSetup(AccountViewModel trakt)
        {
            string recommended = "Recommended by Trakt";

            trakt.LoggedIn += (sender, e) =>
            {
                var account = (AccountViewModel)sender;

                if (account.Account is Trakt trakt)
                {
                    AddFirst(MovieExplore, new CollectionViewModel(DataManager, recommended, trakt.GetRecommendedMoviesAsync()));
                    AddFirst(TVExplore, new CollectionViewModel(DataManager, recommended, trakt.GetRecommendedTVAsync()));
                }
            };

            trakt.LoggedOut += (sender, e) =>
            {
                foreach (var list in new List<IList<object>> { MovieExplore, TVExplore })
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if ((list[i] as CollectionViewModel)?.Name == recommended)
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }
            };
        }
#endif

        public const string OAUTH_SCHEME = "nexus";
        public const string OAUTH_HOST = "gmlmovies.page.link";
        public static readonly Uri BASE_OAUTH_REDIRECT_URI = new Uri((Device.RuntimePlatform == Device.iOS ? "https" : OAUTH_SCHEME) + "://" + OAUTH_HOST + "/oauth/");

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            string url = uri.ToString();
            if (url.StartsWith(BASE_OAUTH_REDIRECT_URI.ToString()))
            {
                var service = BASE_OAUTH_REDIRECT_URI.MakeRelativeUri(uri).ToString();
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    service = service.Replace(uri.Query, string.Empty);
                }
                service = service.Split('/').FirstOrDefault();

                await Accounts.FirstOrDefault(account => account.Account.Name.ToLower() == service)?.Login(uri);
            }
        }

        private static bool TryDeserializeConstraints(JsonNode json, out FilterPredicate predicate)
        {
            if (json is JsonArray array)
            {
                var expression = new BooleanExpression();
                predicate = expression;

                foreach (var item in array)
                {
                    if (TryDeserializeConstraints(item, out var predicate1))
                    {
                        expression.Predicates.Add(predicate1);
                    }
                }

                return true;
            }
            else if (json["lhs"] is JsonNode lhs && json.TryGetValue("operate", out int op) && (json["rhs"] is JsonNode rhsNode))
            {
                var types = new Type[]
                {
                    typeof(Media),
                    typeof(Movie),
                    typeof(TVShow),
                };
                
                var properties = new List<Property>();

                foreach (var type in types)
                {
                    foreach (var field in type.GetFields())//System.Reflection.BindingFlags.Static))
                    {
                        var value = field.GetValue(null);

                        if (value is Property temp && temp.Name == lhs.ToString())
                        {
                            properties.Add(temp);
                        }
                    }

                    /*foreach (var property in type.GetProperties())
                    {
                        Print.Log(property);
                        list.Add(new ReflectedProperty(property));
                    }*/
                }

                object rhs = null;

                foreach (var property in properties)
                {
                    if (PrimitiveJsonTypes.Contains(property.Type))
                    {
                        var method = typeof(JsonNode).GetMethod(nameof(JsonNode.GetValue));
                        method = method.MakeGenericMethod(property.Type);

                        try
                        {
                            rhs = method.Invoke(rhsNode, null);
                        }
                        catch { }
                    }
                    else if (property.Values != null)
                    {
                        var valueStr = rhsNode.TryGetValue<string>();
                        rhs = property.Values.OfType<object>().FirstOrDefault(value => value.ToString() == valueStr);
                    }

                    if (rhs != null)
                    {
                        predicate = new OperatorPredicate
                        {
                            LHS = property,
                            Operator = (Operators)op,
                            RHS = rhs
                        };
                        return true;
                    }
                }
            }

            predicate = FilterPredicate.TAUTOLOGY;
            return false;
        }

        private static object SerializeFilters(FilterPredicate predicate)
        {
            if (predicate is OperatorPredicate op)
            {
                return new
                {
                    lhs = (op.LHS as Property)?.ToString() ?? op.LHS,
                    operate = op.Operator,
                    rhs = op.RHS
                };
            }
            else if (predicate is BooleanExpression expression)
            {
                return expression.Predicates.Select(part => SerializeFilters(part));
            }
            else
            {
                return predicate;
            }
        }

        private static readonly Type[] PrimitiveJsonTypes = new Type[]
        {
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(bool),
            typeof(string),
            typeof(DateTime)
        };

        private static readonly string POPULAR_FILTERS = "PopularFilters";

        private CollectionViewModel GetPopular()
        {
#if DEBUG
            var db = new TMDB.Database();
            var collection = new CollectionViewModel(DataManager, null, db, ItemType.Movie | ItemType.TVShow | ItemType.Person | ItemType.Collection, db)// | ItemType.Company)
            {
                ListLayout = ListLayouts.Grid,
                SortOptions = new List<string> { "Popularity", "Release Date", "Revenue", "Original Title", "Vote Average", "Vote Count" },
            };

            if (collection.Source.Predicate is ExpressionBuilder builder && builder.Editor is MultiEditor editor)
            {
                var search = new Editor<SearchPredicateBuilder>();
                search.AddNew();
                editor.Editors.Insert(0, search);

                if (Properties.TryGetValue(POPULAR_FILTERS, out var value) && value is string filters)
                {
                    try
                    {
                        var json = JsonNode.Parse(filters);

                        if (TryDeserializeConstraints(json, out var temp))
                        {
                            var expression = temp as BooleanExpression ?? new BooleanExpression { Predicates = { temp } };

                            foreach (var predicate in expression.Predicates)
                            {
                                //builder.Add(predicate);
                            }
                        }
                    }
                    catch { }

                    /*if (json["presets"] is System.Text.Json.Nodes.JsonArray presets)
                    {
                        foreach (var text in presets.Select(preset => preset.AsValue().TryGetValue<string>()))
                        {
                            foreach (var preset in collection.Filter.Presets.Where(preset => preset.Text == text))
                            {
                                preset.IsActive = true;
                            }
                        }
                    }*/
                }
            }

            collection.Source.Predicate.PredicateChanged += (sender, e) =>
            {
                var builder = (IPredicateBuilder)sender;

                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        /*presets = collection.Filter.Presets.Where(preset => preset.IsActive).Select(preset => preset.Text),
                        constraints = e.Value.Select(constraint => new
                        {
                            property = constraint.Property.Name,
                            comparison = constraint.Comparison,
                            value = PrimitiveJsonTypes.Contains(constraint.Value?.GetType()) ? constraint.Value : constraint.Value?.ToString()
                        })*/
                    });
                    json = System.Text.Json.JsonSerializer.Serialize(SerializeFilters(builder.Predicate));

                    Print.Log(json);

                    //Properties[POPULAR_FILTERS] = json;
                    _ = SavePropertiesAsync();
                }
                catch { }
            };

            return collection;
#else
            return new CollectionViewModel(DataManager, null, DataManager.Search(), ItemType.Movie | ItemType.TVShow | ItemType.Person | ItemType.Collection)// | ItemType.Company)
            {
                ListLayout = CollectionViewModel.Layout.Grid,
                SortOptions = new List<string> { "Popularity", "Release Date", "Revenue", "Original Title", "Vote Average", "Vote Count" },
                /*Filters =
                {
                    new StringFilterViewModel
                    {
                        Options = new List<string> { "Action", "Adventure", "Romance", "Comedy", "Thriller", "Mystery", "Sci-Fi", "Horror", "Documentary" }
                    },
                    new StringFilterViewModel
                    {
                        Options = new List<string> { "G", "PG", "PG-13", "R", "NC-17" }
                    }
                }*/
            };
#endif
        }

        private bool TryGetProvider<T>(string name, out T provider)
        {
            if (Enum.TryParse(typeof(ServiceName), name, out var value) && value is ServiceName providerName && Services.TryGetValue(providerName, out var value1) && value1 is T tempProvider)
            {
                provider = tempProvider;
                return true;
            }

            provider = default;
            return false;
        }

        private bool TryGetProviderName(IService provider, out ServiceName name)
        {
            foreach (var kvp in Services)
            {
                if (kvp.Value == provider)
                {
                    name = kvp.Key;
                    return true;
                }
            }

            name = default;
            return false;
        }

        public IEnumerable<IListProvider> LoggedInListProviders() => Services.Values.OfType<IListProvider>().Where(provider => !(provider is IAccount account) || Accounts?.FirstOrDefault(temp => temp.Account == account)?.IsLoggedIn == true);

        private static string GetLoginCredentialsKey(ServiceName name) => name.ToString() + " login credentials";

        public async Task Login(IAccount account, object credentials = null)
        {
#if !DEBUG
            if (account is IListProvider listProvider)
            {
                QueuedProviders.Enqueue((listProvider, listProvider.GetAllListsAsync().GetAsyncEnumerator()));
            }
#endif

            if (TryGetProviderName(account, out var name) && credentials != null)
            {
                var key = GetLoginCredentialsKey(name);

                if (!Properties.TryGetValue(key, out var stored) || !Equals(stored, credentials))
                {
                    Properties[key] = credentials;
                    await SavePropertiesAsync();
                }
            }
        }

        public async Task Logout(IAccount account)
        {
            if (account is IListProvider listProvider)
            {
                QueuedProviders = new Queue<(IListProvider Provider, IAsyncEnumerator<List> Itr)>(QueuedProviders.Where(provider => provider.Provider != listProvider));
                var remove = new Stack<ListViewModel>();

                foreach (var list in await AllLists())
                {
                    if (list.SyncWith.FirstOrDefault(sync => sync.Provider == listProvider) is ListViewModel.SyncOptions options)
                    {
                        EventHandler<ListViewModel.SavedEventArgs> handler = null;
                        if (list is NamedListViewModel)
                        {
                            if (list.SyncWith.Count == 1)
                            {
                                list.AddSync(await TryParseSync(ServiceName.Local.ToString(), GetNamedId(list)));
                            }
                        }
                        else
                        {
                            handler = RenameOldLists;
                        }

                        if (!list.RemoveSync(options))
                        {
                            remove.Push(list);
                        }

                        list.Saved -= handler;
                        await list.Save();
                        list.Saved += handler;
                    }
                }

                var customLists = CustomLists as ObservableCollection<ListViewModel>;

                if (customLists != null)
                {
                    customLists.CollectionChanged -= CustomListsChanged;
                }

                while (remove.Count > 0)
                {
                    CustomLists.Remove(remove.Pop());
                }

                if (customLists != null)
                {
                    customLists.CollectionChanged += CustomListsChanged;
                }
            }

#if DEBUG
            if (Device.RuntimePlatform == Device.iOS)
            {
                return;
            }
#endif

            if (TryGetProviderName(account, out var name))
            {
                Properties.Remove(GetLoginCredentialsKey(name));
                await SavePropertiesAsync();
            }
        }

        private object GetNamedId(ListViewModel list)
        {
            if (list == Watchlist)
            {
                return WATCHLIST_ID;
            }
            else if (list == History)
            {
                return HISTORY_ID;
            }
            else if (list == Favorites)
            {
                return FAVORITES_ID;
            }
            else
            {
                return null;
            }
        }

        private Task<List> GetList(IListProvider provider, object id)
        {
            if (id == WATCHLIST_ID)
            {
                return provider.GetWatchlist();
            }
            else if (id == FAVORITES_ID)
            {
                return provider.GetFavorites();
            }
            else if (id == HISTORY_ID)
            {
                return provider.GetHistory();
            }
            else if (id != null)
            {
                return provider.GetListAsync(id.ToString());
            }
            else
            {
                return Task.FromResult<List>(null);
            }
        }

        private async Task<List<ListViewModel.SyncOptions>> GetSyncOptionsAsync(ServiceName name, object id)
        {
            await LocalDatabase.Init;

            var result = new List<ListViewModel.SyncOptions>();

            foreach (var sync in await LocalDatabase.GetSyncsWithAsync(name.ToString(), id.ToString()))
            {
                try
                {
                    ListViewModel.SyncOptions options = null;

                    if (id == WATCHLIST_ID || id == HISTORY_ID || id == FAVORITES_ID)
                    {
                        options = await TryParseSync(sync.Name, id);
                    }

                    if (options == null)
                    {
                        options = await TryParseSync(sync.Name, sync.ID);
                    }

                    if (options == null)
                    {
                        await Task.WhenAll(
                            LocalDatabase.RemoveSyncsWithAsync(sync.Name, sync.ID, name.ToString(), id.ToString()),
                            LocalDatabase.RemoveSyncsWithAsync(name.ToString(), id.ToString(), sync.Name, sync.ID));
                    }
                    else
                    {
                        result.Add(options);
                    }
                }
                catch { }
            }

            return result;
        }

        private async Task<List<ListViewModel.SyncOptions>> GetNamedSyncList(object name)
        {
            var result = await GetSyncOptionsAsync(ServiceName.Local, name);

            if (result.Count == 0)
            {
                result.Add(new ListViewModel.SyncOptions
                {
                    Provider = LocalDatabase,
                    List = await GetList(LocalDatabase, name)
                });
                //result.Add((ServiceName.Local.ToString(), null));
            }

            return result;
        }

        /*private async Task<List<ListViewModel.SyncOptions>> GetNamedSyncList(object name)
        {
            await LocalDatabase.Init;

            var syncs = await LocalDatabase.GetSyncsWithAsync(ServiceName.Local.ToString(), name.ToString());
            var result = new List<ListViewModel.SyncOptions>();

            foreach (var sync in syncs)
            {
                try
                {
                    if ((await TryParseSync(sync.Name, name) ?? await TryParseSync(sync.Name, sync.ID)) is ListViewModel.SyncOptions options)
                    {
                        result.Add(options);
                    }
                }
                catch { }
            }

            return result;
        }*/

        private async Task<ListViewModel> GetWatchlist() => WithHandlers(new NamedListViewModel(DataManager, "Watchlist", await GetNamedSyncList(WATCHLIST_ID), ItemType.Movie | ItemType.TVShow));
        private async Task<ListViewModel> GetFavorites() => WithHandlers(new NamedListViewModel(DataManager, "Favorites", await GetNamedSyncList(FAVORITES_ID)));
        private async Task<ListViewModel> GetHistory() => WithHandlers(new NamedListViewModel(DataManager, "Watched", await GetNamedSyncList(HISTORY_ID), ItemType.Movie | ItemType.TVShow));

        private ListViewModel WithHandlers(ListViewModel list)
        {
            CustomListsChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<ListViewModel> { list }));
            return list;
        }

        private Queue<(IListProvider Provider, IAsyncEnumerator<List> Itr)> QueuedProviders;
        //private Dictionary<ServiceName, HashSet<object>> lists = new Dictionary<ServiceName, HashSet<object>>();
        private async Task<IEnumerable<ListViewModel>> AllLists()
        {
            await Task.WhenAll(LazyFavorites.Value, LazyWatchlist.Value, LazyHistory.Value);
            return CustomLists.Prepend(Favorites).Prepend(Watchlist).Prepend(History);
        }

        private async Task<bool> IsListInUse(IListProvider provider, List list) => (await AllLists()).Any(lvm => lvm.SyncWith.Any(source => source.Provider == provider && source.List.ID.Equals(list.ID)));

        private bool LoadingLists;

        private async Task LoadLists(int? count = null)
        {
            if (LoadingLists)
            {
                return;
            }
            LoadingLists = true;

            await LocalDatabase.Init;

            //while (QueuedProviders.Count > 0)
            for (int i = 0; i < (count ?? 1) && QueuedProviders.Count > 0;)
            {
                if (!await QueuedProviders.Peek().Itr.MoveNextAsync())
                {
                    QueuedProviders.Dequeue();
                }
                else
                {
                    var source = QueuedProviders.Peek().Provider;
                    var list = QueuedProviders.Peek().Itr.Current;

                    if (!TryGetProviderName(source, out var name) || await IsListInUse(source, list))// || (lists.TryGetValue(name, out var cache) && cache.Contains(list.ID)))
                    {
                        continue;
                    }

                    /*if (list.Name.ToLower() == "tmdb list" && TryGetProvider<TMDB>(ServiceName.TMDb.ToString(), out var tmdb))
                    {
                        var itr = tmdb.GetTrendingMoviesAsync().GetAsyncEnumerator();
                        var items = new List<Item>();
                        for (int i = 0; i < 50 && await itr.MoveNextAsync(); i++)
                        {
                            items.Add(itr.Current);
                        }

                        await list.AddAsync(items);
                    }*/

                    //var syncs = await LocalDatabase.GetSyncsWithAsync(name.ToString(), list.ID);
                    var sources = (await GetSyncOptionsAsync(name, list.ID)).Prepend(new ListViewModel.SyncOptions
                    {
                        Provider = source,
                        List = list
                    });

                    /*foreach (var synced in sync)
                    {
                        if (!TryGetProviderName(synced.Provider, out var otherName))
                        {
                            continue;
                        }

                        if (!lists.TryGetValue(otherName, out var l))
                        {
                            lists[otherName] = l = new HashSet<object>();
                        }

                        l.Add(synced.List.ID);
                    }*/

                    var lvm = new ListViewModel(DataManager, sources);
                    CustomLists.Add(lvm);
                    i++;
                }
            }

            LoadingLists = false;
        }

        private async void CustomListsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var list in e.OldItems.OfType<ListViewModel>())
                {
                    var args = new ListViewModel.SavedEventArgs
                    {
                        OldSources = new List<ListViewModel.SyncOptions>(list.SyncWith),
                        NewSources = new List<ListViewModel.SyncOptions>()
                    };

                    list.SyncWith.Clear();
                    UpdateSyncTable(list, args);
                    //await Task.WhenAll(list.SyncWith.Select(sync => sync.List.Delete()));
                    await (list.Item as List)?.Delete();
                }
            }

            if (e.NewItems != null)
            {
                foreach (var list in e.NewItems.OfType<ListViewModel>())
                {
                    list.Saved += UpdateSyncTable;
                    if (!(list is NamedListViewModel))
                    {
                        list.Saved += RenameOldLists;
                    }
                    //lvm.SyncWith.CollectionChanged += UpdateSyncTable;
                    list.SyncChanged += (sender, e) => CoerceSyncList(((ListViewModel)sender).SyncWith);
                    list.SyncWith.CollectionChanged += (sender, e) => (AddSyncSourceCommand as Command)?.ChangeCanExecute();
                    CoerceSyncList(list.SyncWith);
                }
            }
        }

        private async void CoerceSyncList(IList<ListViewModel.SyncOptions> list)
        {
            if (list.Count > 1 && list[0].Provider != LocalDatabase)
            {
                int i;
                for (i = 0; i < list.Count; i++)
                {
                    if (list[i].Provider == LocalDatabase)
                    {
                        if (list is ObservableCollection<ListViewModel.SyncOptions> observable)
                        {
                            observable.Move(i, 0);
                        }
                        else
                        {
                            list.Insert(0, list[i]);
                            list.RemoveAt(i + 1);
                        }

                        break;
                    }
                }

                if (i == list.Count)
                {
                    list.Insert(0, new ListViewModel.SyncOptions
                    {
                        Provider = LocalDatabase,
                        List = LocalDatabase.CreateList()
                    });
                    await Shell.Current.CurrentPage.DisplayAlert("", "If this list syncs with two or more remote sources, a local list must be the first source", "OK");
                }
            }
        }

        /*private Task<IEnumerable<ListViewModel.SyncOptions>> ParseSync(params (string Name, object ID)[] syncs) => ParseSync((IEnumerable<(string, object)>)syncs);
        private async Task<IEnumerable<ListViewModel.SyncOptions>> ParseSync(IEnumerable<(string Name, object ID)> syncs)
        {
            var parsed = new List<ListViewModel.SyncOptions>();

            foreach (var sync in syncs)
            {
                if ((await TryParseSync(sync.Name, sync.ID)) is ListViewModel.SyncOptions options)
                {
                    parsed.Add(options);
                }
            }

            return parsed;
        }*/

        private async Task<ListViewModel.SyncOptions> TryParseSync(string name, object id)
        {
            if (TryGetProvider<IListProvider>(name, out var provider))
            {
                List list = await GetList(provider, id);

                if (list != null)
                {
                    return new ListViewModel.SyncOptions
                    {
                        Provider = provider,
                        List = list
                    };
                }
            }

            return null;
        }

        private IEnumerable<(string Name, string ID)> ParseSync(IEnumerable<ListViewModel.SyncOptions> sources)
        {
            foreach (var source in sources)
            {
                if (TryGetProviderName(source.Provider, out var name))
                {
                    yield return (name.ToString(), source.List.ID);
                }
            }
        }

        private async void RenameOldLists(object sender, ListViewModel.SavedEventArgs e)
        {
            foreach (var sync in e.OldSources)
            {
                sync.List.Name += string.Format(" [{0}]", sync.Provider.Name);
                await sync.List.Update();
                CustomLists.Add(new ListViewModel(DataManager, sync));
            }
        }

        private async void UpdateSyncTable(object sender, ListViewModel.SavedEventArgs e)
        {
            var list = (ListViewModel)sender;
            var oldSources = ParseSync(e.OldSources);
            var newSources = ParseSync(e.NewSources);
            var current = ParseSync(list.SyncWith);

            if (GetNamedId(list) is string namedList)
            {
                current = current.Prepend((ServiceName.Local.ToString(), namedList));
            }

            var past = current.Except(newSources).Concat(oldSources).ToList();

            foreach (var sync in oldSources)
            {
                foreach (var other in past.Intersect(await LocalDatabase.GetSyncsWithAsync(sync.Name, sync.ID)))
                {
                    if (other != sync)
                    {
                        await Task.WhenAll(
                            LocalDatabase.RemoveSyncsWithAsync(sync.Name, sync.ID, other.Name, other.ID),
                            LocalDatabase.RemoveSyncsWithAsync(other.Name, other.ID, sync.Name, sync.ID));
                    }
                }
            }

            foreach (var sync in current)
            {
                var locus = current.FirstOrDefault();

                if (sync != locus)
                {
                    await Task.WhenAll(
                        LocalDatabase.SetSyncsWithAsync(locus.Name, locus.ID, sync.Name, sync.ID),
                        LocalDatabase.SetSyncsWithAsync(sync.Name, sync.ID, locus.Name, locus.ID));
                }
            }
        }

        private static bool Paused;

        public static async Task Return()
        {
            while (Paused)
            {
                await Task.Delay(100);
            }
        }

        public static Task<bool> TryOpenUrlAsync(string url)
        {
            Paused = true;
            return Xamarin.Essentials.Launcher.TryOpenAsync(url);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            Paused = true;
            Print.Log("sleep");
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            Paused = false;
            Print.Log("resume");
            // Handle when your app resumes
        }
    }
}

namespace Movies.Views
{
    public class AutoSizeBehavior : Behavior<View>
    {
        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            bindable.WidthRequest = bindable.HeightRequest = 0;
            AddHandlers(bindable);
            //bindable.BatchCommitted += (sender, e) => Print.Log("batch committed");
            //bindable.SizeChanged += (sender, e) => Print.Log("size changed", ((View)sender).Width, ((View)sender).Height, ((View)sender).WidthRequest, ((View)sender).HeightRequest, sender.GetHashCode());
            //bindable.MeasureInvalidated += (sender, e) => Print.Log("measure invalidated");
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);
            RemoveHandlers(bindable);
        }

        private void AddHandlers(View view)
        {
            //view.SizeChanged += Update;
            view.PropertyChanged += WidthChanged;
            view.PropertyChanged += HeightChanged;
            view.MeasureInvalidated += Update;
        }

        private void WidthChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.WidthProperty.PropertyName && sender is VisualElement visualElement && (visualElement.Width != visualElement.WidthRequest || visualElement.WidthRequest == 0))
            {
                Update(sender, EventArgs.Empty);
            }
        }

        private void HeightChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.HeightProperty.PropertyName && sender is VisualElement visualElement && (visualElement.Height != visualElement.HeightRequest || visualElement.HeightRequest == 0))
            {
                Update(sender, EventArgs.Empty);
            }
        }

        private void RemoveHandlers(View view)
        {
            view.MeasureInvalidated -= Update;
            view.PropertyChanged -= HeightChanged;
            view.PropertyChanged -= WidthChanged;
            //view.SizeChanged -= Update;
        }

        private void Update(object sender, EventArgs e)
        {
            var view = (View)sender;
            if (view.Width < 0 && view.Height < 0)
            {
                //return;
            }

            //Print.Log("size changed", view.Bounds.Size);

            bool autoWidth = Math.Max(0, view.Width) == view.WidthRequest;
            bool autoHeight = Math.Max(0, view.Height) == view.HeightRequest;
            Size size = default;

            RemoveHandlers(view);
            view.BatchBegin();
            view.IsVisible = false;

            if (autoWidth || autoHeight)
            {
                double widthConstraint = autoWidth ? double.PositiveInfinity : view.Width;
                double heightConstraint = autoHeight ? double.PositiveInfinity : view.Height;

                view.ClearValue(VisualElement.WidthRequestProperty);
                view.ClearValue(VisualElement.HeightRequestProperty);
                size = view.Measure(widthConstraint, heightConstraint).Request;

                //Print.Log(view.WidthRequest, view.HeightRequest, autoWidth, autoHeight, widthConstraint, heightConstraint, size);
            }

            view.WidthRequest = autoWidth ? size.Width : 0;
            view.HeightRequest = autoHeight ? size.Height : 0;

            EventHandler<Xamarin.Forms.Internals.EventArg<VisualElement>> handler = null;
            handler = (sender, e) =>
            {
                var view = (View)sender;

                if (!view.Batched)
                {
                    view.BatchCommitted -= handler;
                    AddHandlers(view);
                }
            };

            view.IsVisible = true;
            view.BatchCommitted += handler;
            view.BatchCommit();
        }
    }

    public class PreloadItemsBehavior : Behavior<CollectionView>
    {
        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            bindable.PropertyChanged += ThresholdCommandChanged;
            ThresholdCommandChanged(bindable, new PropertyChangedEventArgs(ItemsView.RemainingItemsThresholdReachedCommandProperty.PropertyName));
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.PropertyChanged -= ThresholdCommandChanged;
        }

        private void ThresholdCommandChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != ItemsView.RemainingItemsThresholdReachedCommandProperty.PropertyName)
            {
                return;
            }

            var collectionView = (CollectionView)sender;
            collectionView.RemainingItemsThresholdReachedCommand?.Execute(collectionView.RemainingItemsThresholdReachedCommandParameter);
        }
    }

    public class ImageSizeConverter : IValueConverter
    {
        public static readonly ImageSizeConverter Instance = new ImageSizeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is string str ? TMDB.GetFullSizeImage(str) : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DecoratorDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Main { get; set; }
        public DataTemplate Decorator { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var main = (Main as DataTemplateSelector)?.SelectTemplate(item, container) ?? Main;
            var decorator = ((Decorator as DataTemplateSelector)?.SelectTemplate(item, container) ?? Decorator)?.CreateContent() as ContentView;

            if (decorator != null && main?.CreateContent() is View view)
            {
                decorator.Content = view;

                return new DataTemplate(() => decorator);
            }
            else
            {
                return main;
            }
        }
    }

    public class ListEditView : ContentView
    {
        public static readonly BindableProperty SelectedProperty = BindableProperty.Create(nameof(Selected), typeof(bool), typeof(ListEditView), false);

        public bool Selected
        {
            get => (bool)GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }

        public readonly ICommand ToggleSelectedCommand;

        public ListEditView()
        {
            ToggleSelectedCommand = new Command(() => Selected = !Selected);
        }
    }

    public class SelectionModeToBoolConverter : IValueConverter
    {
        public static readonly SelectionModeToBoolConverter Instance = new SelectionModeToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is SelectionMode mode && mode != SelectionMode.None;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSelectionModeConverter : IValueConverter
    {
        public static readonly BoolToSelectionModeConverter Instance = new BoolToSelectionModeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, true) ? (Equals(parameter, true) ? SelectionMode.Single : SelectionMode.Multiple) : SelectionMode.None;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime Date
        {
            get => _Date;
            set
            {
                if (value != _Date)
                {
                    _Date = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Year
        {
            get => _Year;
            set
            {
                if (value != _Year)
                {
                    _Year = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Month
        {
            get => _Month;
            set
            {
                if (value != _Month)
                {
                    _Month = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Day
        {
            get => _Day;
            set
            {
                if (value != _Day)
                {
                    _Day = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DaysInMonth
        {
            get => _DaysInMonth;
            private set
            {
                if (value != _DaysInMonth)
                {
                    _DaysInMonth = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _Date;
        private int _Year;
        private int _Month;
        private int _Day;
        private int _DaysInMonth;

        public DateTimeViewModel()
        {
            UpdateComponents();

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Date))
                {
                    UpdateComponents();

                    OnPropertyChanged(nameof(Year));
                    OnPropertyChanged(nameof(Month));
                    OnPropertyChanged(nameof(Day));
                }
                else if (e.PropertyName == nameof(Year) || e.PropertyName == nameof(Month) || e.PropertyName == nameof(Day))
                {
                    if (e.PropertyName != nameof(Day))
                    {
                        DaysInMonth = DateTime.DaysInMonth(Year, Month);
                    }

                    UpdateDate();
                }
            };
        }

        private void UpdateDate()
        {
            Date = new DateTime(Year, Month, Day);
        }

        private void UpdateComponents()
        {
            _Year = Date.Year;
            _Month = Date.Month;
            _Day = Date.Day;
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public class DateTimeComponentConverter : IMultiValueConverter
    {
        public static readonly DateTimeComponentConverter Instance = new DateTimeComponentConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditDataTemplateSelector : DecoratorDataTemplateSelector
    {
        public CollectionView CollectionView { get; private set; }
        public ControlTemplate SwipeTemplate
        {
            get => SafeSwipeTemplate?.Value;
            set => SafeSwipeTemplate = new Lazy<ControlTemplate>(() =>
            {
                var swipe = value?.CreateContent() as SwipeView;

                if (swipe.Content is ContentPresenter)
                {
                    return value;
                }
                else
                {
                    return new ControlTemplate(() =>
                    {
                        var swipe = value?.CreateContent() as SwipeView;
                        swipe.Content = new ContentPresenter();
                        return swipe;
                    });
                }
            });
        }
        public ControlTemplate EditTemplate { get; set; }

        private List<Element> Children = new List<Element>();
        private Lazy<ControlTemplate> SafeSwipeTemplate;

        public EditDataTemplateSelector()
        {
            Decorator = new DataTemplate(() =>
            {
                var content = new ListEditView();
                UpdateTemplates(content);
                return content;
            });
        }

        public void UpdateCollectionView(CollectionView collectionView)
        {
            CollectionView = collectionView;

            CollectionView.PropertyChanged += ToggleEditMode;
            CollectionView.ChildAdded += (sender, e) =>
            {
                Children.Add(e.Element);
            };
            CollectionView.ChildRemoved += (sender, e) =>
            {
                Children.Remove(e.Element);
            };
        }

        private void ToggleEditMode(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != SelectableItemsView.SelectionModeProperty.PropertyName)
            {
                return;
            }
            Print.Log(sender.GetHashCode(), Children.Count, ((CollectionView)sender).SelectionMode);
            UpdateTemplates();
        }

        public void UpdateTemplates() => UpdateTemplates(Children.OfType<TemplatedView>());
        private void UpdateTemplates(params TemplatedView[] items) => UpdateTemplates((IEnumerable<TemplatedView>)items);
        private void UpdateTemplates(IEnumerable<TemplatedView> items)
        {
            var editing = CollectionView?.SelectionMode != SelectionMode.None;
            ControlTemplate template = editing ? EditTemplate : SwipeTemplate;

            foreach (var item in items)
            {
                item.ControlTemplate = template;
            }
        }
    }

    public static class CollectionViewExt
    {
        public static readonly ICommand SelectAllCommand = new Command<CollectionView>(SelectAll);

        public static readonly ICommand DeselectAllCommand = new Command<CollectionView>(DeselectAll);

        public static readonly ICommand DeleteSelectedCommand = new Command<CollectionView>(DeleteSelected);

        public static void SelectAll(this CollectionView collectionView)
        {
            collectionView.SelectedItems = new List<object>(collectionView.ItemsSource.OfType<object>());
            collectionView.SetIsAllSelected(true);

            if (collectionView.ItemsSource is INotifyCollectionChanged observable)
            {
                var handler = (NotifyCollectionChangedEventHandler)collectionView.GetValue(AllSelectedHandlerProperty);

                // Make sure we don't add it multiple times
                observable.CollectionChanged -= handler;
                observable.CollectionChanged += handler;
            }
        }

        public static void DeselectAll(this CollectionView collectionView)
        {
            collectionView.SelectedItems = null;

            if (collectionView.ItemsSource is INotifyCollectionChanged observable)
            {
                var handler = (NotifyCollectionChangedEventHandler)collectionView.GetValue(AllSelectedHandlerProperty);

                observable.CollectionChanged -= handler;
            }
        }

        public static void DeleteSelected(this CollectionView collectionView)
        {
            if (collectionView.SelectionMode == SelectionMode.Single)
            {
                if (collectionView.SelectedItem != null && collectionView.ItemsSource is IList list)
                {
                    list.Remove(collectionView.SelectedItem);
                }

                collectionView.SelectedItem = null;
            }
            else if (collectionView.SelectionMode == SelectionMode.Multiple)
            {
                if (collectionView.SelectedItems != null && collectionView.ItemsSource is IList list)
                {
                    foreach (var item in collectionView.SelectedItems)
                    {
                        list.Remove(item);
                    }
                }

                collectionView.SelectedItems = null;
            }
        }

        private static readonly BindableProperty AllSelectedHandlerProperty = BindableProperty.CreateAttached("AllSelectedHandler", typeof(NotifyCollectionChangedEventHandler), typeof(CollectionView), null, defaultValueCreator: bindable =>
        {
            var collectionView = (CollectionView)bindable;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.NewItems != null)
                {
                    if (collectionView.SelectedItems == null)
                    {
                        //collectionView.SelectedItems = new List<object>();
                    }

                    foreach (var item in e.NewItems)
                    {
                        collectionView.SelectedItems?.Add(item);
                    }
                }
            };

            return (NotifyCollectionChangedEventHandler)collectionChanged;
        });

        public static readonly BindableProperty IsAllSelectedProperty = BindableProperty.CreateAttached("IsAllSelected", typeof(bool), typeof(CollectionView), false, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var collectionView = (CollectionView)bindable;

            if (Equals(newValue, true))
            {
                //collectionView.SelectAll();
            }
        }, defaultValueCreator: bindable =>
        {
            ((CollectionView)bindable).SelectionChanged += (sender, e) =>
            {
                var collectionView = (CollectionView)sender;

                if (e.CurrentSelection.Count < e.PreviousSelection.Count)
                {
                    collectionView.SetIsAllSelected(false);
                }
            };

            return false;
        });

        private static bool IsAllSelected(this CollectionView collectionView) => collectionView.SelectedItems != null && collectionView.ItemsSource is IList items && collectionView.SelectedItems.Count == items.Count;

        public static bool GetIsAllSelected(this CollectionView collectionView) => (bool)collectionView.GetValue(IsAllSelectedProperty);
        public static void SetIsAllSelected(this CollectionView collectionView, bool value) => collectionView.SetValue(IsAllSelectedProperty, value);

        public static readonly ICommand ToggleIsEditingCommand = new Command<CollectionView>(collection => collection.SetIsEditing(!collection.GetIsEditing()));

        public static readonly ICommand ToggleSelectionModeCommand = new Command<CollectionView>(collection => collection.SelectionMode = collection.SelectionMode == SelectionMode.None ? SelectionMode.Multiple : SelectionMode.None);

        public static readonly BindableProperty IsEditingProperty = BindableProperty.CreateAttached("IsEditing", typeof(bool), typeof(CollectionView), false, propertyChanged: (bindable, oldValue, newValue) =>
        {
            CollectionView collection = (CollectionView)bindable;
            var template = collection.ItemTemplate;
        }, defaultValueCreator: bindable =>
        {
            SetupEditCollectionView((CollectionView)bindable);
            return false;
        });

        private static void SetupEditCollectionView(CollectionView collectionView)
        {
            collectionView.PropertyChanged += ItemTemplateChanged;
            ItemTemplateChanged(collectionView, new PropertyChangedEventArgs(ItemsView.ItemTemplateProperty.PropertyName));
        }

        private static void ItemTemplateChanged(object sender, PropertyChangedEventArgs e)
        {
            CollectionView collection = (CollectionView)sender;

            if (collection.IsSet(ItemsView.ItemTemplateProperty) && collection.ItemTemplate is EditDataTemplateSelector editTemplate)
            {
                editTemplate.UpdateCollectionView(collection);
            }
        }

        public static bool GetIsEditing(this CollectionView collectionView) => (bool)collectionView.GetValue(IsEditingProperty);
        public static void SetIsEditing(this CollectionView collectionView, bool value) => collectionView.SetValue(IsEditingProperty, value);
    }

    public static class Extensions
    {
        public static readonly BindableProperty YearProperty = BindableProperty.CreateAttached(nameof(DateTime.Year), typeof(int), typeof(DatePicker), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var picker = (DatePicker)bindable;
            picker.Date = new DateTime((int)newValue, picker.Date.Month, picker.Date.Day);
        });

        public static int GetYear(this DatePicker bindable) => (int)bindable.GetValue(YearProperty);
        public static void SetYear(this DatePicker bindable, int value) => bindable.SetValue(YearProperty, value);

        public static readonly BindableProperty ContentProperty = BindableProperty.CreateAttached("Content", typeof(View), typeof(ScrollView), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var scrollView = (ScrollView)bindable;
            var content = (View)newValue;
            
            scrollView.Content = content;
        });

        public static View GetContent(this ScrollView bindable) => (View)bindable.GetValue(ContentProperty);
        public static void SetContent(this ScrollView bindable, object value) => bindable.SetValue(ContentProperty, value);

        public static readonly BindableProperty ChildCountProperty = BindableProperty.CreateAttached("ChildCount", typeof(int), typeof(Layout), 0, coerceValue: (bindable, value) =>
        {
            if (bindable is ContentView contentView)
            {
                return contentView.Content == null ? 0 : 1;
            }

            return (bindable as Layout<View>)?.Children.Count ?? value;
        }, defaultValueCreator: bindable =>
         {
             if (bindable is ContentView)
             {
                 bindable.PropertyChanged += (sender, e) =>
                 {
                     if (e.PropertyName == ContentView.ContentProperty.PropertyName)
                     {
                         ((Layout)sender).SetChildCount(0);
                     }
                 };
             }
             else if (bindable is Layout<View> layout && layout.Children is INotifyCollectionChanged observable)
             {
                 observable.CollectionChanged += (sender, e) =>
                 {
                     layout.SetChildCount(0);
                 };
             }

             return 0;
         });

        public static int GetChildCount(this Layout bindable) => (int)bindable.GetValue(ChildCountProperty);
        public static void SetChildCount(this Layout bindable, int value) => bindable.SetValue(ChildCountProperty, value);

        public static readonly BindableProperty StepProperty = BindableProperty.CreateAttached("Step", typeof(double), typeof(Slider), null, defaultValueCreator: bindable =>
        {
            ((Slider)bindable).DragStarted += (sender, e) => ((Slider)sender).ValueChanged += CoerceSliderValue;
            ((Slider)bindable).DragCompleted += (sender, e) => ((Slider)sender).ValueChanged -= CoerceSliderValue;
            return 1.0;
        });

        private static void CoerceSliderValue(object sender, ValueChangedEventArgs e)
        {
            Slider slider = (Slider)sender;
            var step = slider.GetStep();

            slider.Value = Math.Round(e.NewValue / step) * step;
        }

        public static double GetStep(this Slider bindable) => (double)bindable.GetValue(StepProperty);
        public static void SetStep(this Slider bindable, double value) => bindable.SetValue(StepProperty, value);

        public static readonly BindableProperty SelectedItemChangedCommandProperty = BindableProperty.CreateAttached("SelectedItemChangedCommand", typeof(ICommand), typeof(Picker), null, propertyChanged: (bindable, oldValue, newValue) =>
        {

        }, defaultValueCreator: bindable =>
        {
            ((Picker)bindable).SelectedIndexChanged += (sender, e) =>
            {
                var picker = (Picker)sender;
                var command = picker.GetSelectedItemChangedCommand();
                var parameter = picker.GetSelectedItemChangedCommandParameter();

                if (command != null && command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            };

            return null;
        });
        public static readonly BindableProperty SelectedItemChangedCommandParameterProperty = BindableProperty.CreateAttached("SelectedItemChangedCommandParameter", typeof(object), typeof(Picker), null);

        public static ICommand GetSelectedItemChangedCommand(this Picker picker) => (ICommand)picker.GetValue(SelectedItemChangedCommandProperty);
        public static object GetSelectedItemChangedCommandParameter(this Picker picker) => picker.GetValue(SelectedItemChangedCommandParameterProperty);
        public static void SetSelectedItemChangedCommand(this Picker picker, ICommand value) => picker.SetValue(SelectedItemChangedCommandProperty, value);
        public static void SetSelectedItemChangedCommandParameter(this Picker picker, ICommand value) => picker.SetValue(SelectedItemChangedCommandParameterProperty, value);

        private class Proxy : BindableObject { }

        public static readonly BindableProperty BatchProperty = BindableProperty.CreateAttached("Batch", typeof(object), typeof(BindableObject), null, propertyChanging: (bindable, oldValue, newValue) =>
        {
            Print.Log("batch changed", bindable.GetHashCode(), oldValue, newValue);
            if (newValue == null)
            {
                return;
            }

            bindable.BindingContextChanged -= EndBatch;
            bindable.BindingContextChanged += EndBatch;

            //Print.Log("batch property changed", bindable, newValue);
            Print.Log("\tbatch begin", bindable.GetType());//, (bindable as VisualElement)?.Style?.Behaviors.Count);
            App.DataManager.BatchBegin();

            //bindable.BindingContextChanged -= EndBatch;
            //bindable.BindingContextChanged += EndBatch;
        });

        public static void EndBatch(object sender, EventArgs e)
        {
            if (((BindableObject)sender).BindingContext == null)
            {
                return;
            }
            Print.Log("\tbatch will end", sender.GetHashCode(), ((BindableObject)sender)?.BindingContext?.GetType(), ((BindableObject)sender)?.BindingContext);
            App.DataManager.BatchEnd();
        }

        public static object GetBatch(this BindableObject bindable) => bindable.GetValue(BatchProperty);
        public static void SetBatch(this BindableObject bindable, object value) => bindable.SetValue(BatchProperty, value);

        public static readonly BindableProperty AspectRequestProperty = BindableProperty.CreateAttached("AspectRequest", typeof(double), typeof(VisualElement), null, defaultValueCreator: bindable =>
        {
            ((VisualElement)bindable).SizeChanged += AdjustAspect;
            return null;
        });

        private static void AdjustAspect(object sender, EventArgs e)
        {
            var visualElement = (VisualElement)sender;

            double aspect = visualElement.Width / visualElement.Height;
            double request = GetAspectRequest(visualElement);

#if DEBUG
            if (sender is ImageView image && image.AltText?.ToLower() == "m. m.")
            //if (request == 5)
            {
                Print.Log(visualElement.IsSet(VisualElement.WidthRequestProperty), visualElement.IsSet(VisualElement.HeightRequestProperty));
                ;
            }
#endif

            if (request > aspect && !visualElement.IsSet(VisualElement.WidthRequestProperty))
            {
                visualElement.WidthRequest = request * visualElement.Height;
            }
            else if (request < aspect && !visualElement.IsSet(VisualElement.HeightRequestProperty))
            {
                visualElement.HeightRequest = visualElement.Width / request;
            }
            else if (request < aspect)
            {
                visualElement.WidthRequest = request * visualElement.Height;
            }
            else if (request > aspect)
            {
                visualElement.HeightRequest = visualElement.Width / request;
            }
            return;

            Size size = new Size();

            if (!visualElement.IsSet(VisualElement.WidthRequestProperty) || !visualElement.IsSet(VisualElement.HeightRequestProperty))
            {
                visualElement.WidthRequest = visualElement.Width;
                visualElement.HeightRequest = visualElement.Width / request;
            }

            //visualElement.WidthRequest = visualElement.IsSet(VisualElement.WidthRequestProperty) ? (aspect < request ? visualElement.Width : request * visualElement.Height) : Math.Max(visualElement.Width, request * visualElement.Height);
            //visualElement.HeightRequest = visualElement.IsSet(VisualElement.HeightRequestProperty) ? (aspect < request ? visualElement.Width / request : visualElement.Height) : Math.Max(visualElement.Height, visualElement.Width / request);

            /*if (!visualElement.IsSet(VisualElement.WidthRequestProperty) || !visualElement.IsSet(VisualElement.HeightRequestProperty))
            {
                visualElement.WidthRequest = request * visualElement.Height;
                visualElement.HeightRequest = visualElement.Width / request;
            }
            else*/
            else if (aspect < request)// && visualElement.Width < visualElement.Height))
            {
                visualElement.WidthRequest = visualElement.Width;
                visualElement.HeightRequest = visualElement.Width / request;
            }
            else if (aspect > request)// && visualElement.Height < visualElement.Width))
            {
                visualElement.WidthRequest = request * visualElement.Height;
                visualElement.HeightRequest = visualElement.Height;
            }
        }

        public static double GetAspectRequest(this VisualElement visualElement) => (double)visualElement.GetValue(AspectRequestProperty);
        public static void SetAspectRequest(this VisualElement visualElement, double value) => visualElement.SetValue(AspectRequestProperty, value);

        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.CreateAttached("ItemsSource", typeof(object), typeof(ItemsView), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            ItemsView items = (ItemsView)bindable;

            if (newValue is IAsyncEnumerable<object> asyncEnumerable)
            {
                ICollection<object> itemsSource = asyncEnumerable is ICollection<object> asyncCollection && asyncEnumerable is INotifyCollectionChanged ? asyncCollection : new ObservableCollection<object>();
                IAsyncEnumerator<object> itr = asyncEnumerable.GetAsyncEnumerator();
                bool loading = false;

                var command = new Command<int?>(async count =>
                {
                    if (loading)
                    {
                        return;
                    }
                    loading = true;

                    try
                    {
                        for (int i = 0; i < (count ?? 1) && (await itr.MoveNextAsync()); i++)
                        {
                            itemsSource.Add(itr.Current);
                        }
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Print.Log(e);
#endif
                    }

                    loading = false;
                });

                command.Execute(10);

                items.RemainingItemsThresholdReachedCommand = command;
                items.ItemsSource = itemsSource;
            }
            else if (newValue is IEnumerable enumerable)
            {
                items.ItemsSource = enumerable;
            }
            else
            {
                items.ItemsSource = new List<object> { newValue };
            }
        });

        public static object GetItemsSource(this ItemsView bindable) => bindable.GetValue(ItemsSourceProperty);
        public static void SetItemsSource(this ItemsView bindable, object value) => bindable.SetValue(ItemsSourceProperty, value);

        //public static readonly BindableProperty ValueIfAvailableProperty = BindableProperty.CreateAttached("ValueIfAvailable", typeof(Binding), typeof(VisualElement), null);

        public static readonly BindableProperty PaddingProperty = BindableProperty.CreateAttached("Padding", typeof(Thickness), typeof(ItemsView), default(Thickness));

        public static Thickness GetPadding(this ItemsView itemsView) => (Thickness)itemsView.GetValue(PaddingProperty);
        public static void SetPadding(this ItemsView itemsView, Thickness value) => itemsView.SetValue(PaddingProperty, value);

        private class DecoratorDataTemplate : DataTemplate
        {
            public DecoratorDataTemplate(DataTemplate baseTemplate, Action<object> decorator) : base(() =>
            {
                var content = baseTemplate.CreateContent();
                decorator(content);
                return content;
            })
            { }
        }
    }

    public class ToViewConverter : IValueConverter
    {
        public static readonly ToViewConverter Instance = new ToViewConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is View view)
            {
                return view;
            }
            else if (value is DataTemplate template && template.CreateContent() is View content)
            {
                return content;
            }
            else
            {
                throw new Exception("Cound not convert " + value.GetType() + " to a view");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty(nameof(PageTemplate))]
    public class PushPageCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public ElementTemplate PageTemplate { get; set; }
        public bool Modal { get; set; }

        public bool CanExecute(object parameter) => true;

        public async void Execute(object parameter)
        {
            object content = PageTemplate.CreateContent();
            Page page = content as Page ?? new ContentPage { Content = (View)content };

            if (parameter != null)
            {
                page.BindingContext = parameter;
            }

            if (Modal)
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(page);
            }
            else
            {
                await Application.Current.MainPage.Navigation.PushAsync(page);
            }
        }
    }

    public class PushPageEventArgs : EventArgs
    {
        public bool Modal { get; set; }
        public bool IsAnimated { get; set; }
        public object BindingContext { get; set; }
    }

    public class HideIfNoVisibleChildrenBehavior : Behavior<Layout<View>>
    {
        public static readonly Type ViewLayout = typeof(Layout<View>);

        protected override void OnAttachedTo(Layout<View> bindable)
        {
            base.OnAttachedTo(bindable);

            foreach (var child in bindable.Children)
            {
                ChildAdded(bindable, new ElementEventArgs(child));
            }
            bindable.ChildAdded += ChildAdded;
            bindable.ChildRemoved += ChildRemoved;

            UpdateIsVisible(bindable, new EventArgs());
        }

        private static void ChildAdded(object sender, ElementEventArgs e)
        {
            e.Element.PropertyChanged += IsVisibleChanged;
            IsVisibleChanged(e.Element, new PropertyChangedEventArgs(VisualElement.IsVisibleProperty.PropertyName));
        }

        private static void ChildRemoved(object sender, ElementEventArgs e)
        {
            e.Element.PropertyChanged -= IsVisibleChanged;
            if (e.Element.Parent is Layout<View> layout)
            {
                UpdateIsVisible(layout, new EventArgs());
            }
        }

        private static void IsVisibleChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName && sender is View view && view.Parent is Layout<View> layout)
            {
                if (view.IsVisible)
                {
                    layout.IsVisible = true;
                }
                else
                {
                    UpdateIsVisible(layout, new EventArgs());
                }
            }
        }

        private static void UpdateIsVisible(object sender, EventArgs e)
        {
            var layout = (Layout<View>)sender;

            foreach (View view in layout.Children)
            {
                if (view.IsVisible)
                {
                    layout.IsVisible = true;
                    return;
                }
            }

            layout.IsVisible = false;
        }
    }

    public class ChildViewModel : BindableObject
    {
        public static readonly BindableProperty LayoutProperty = BindableProperty.Create(nameof(Layout), typeof(Layout<View>), typeof(ChildViewModel), propertyChanged: (bindable, oldValue, newValue) =>
        {
            ChildViewModel model = (ChildViewModel)bindable;

            if (oldValue is Layout<View> oldLayout)
            {
                oldLayout.ChildAdded -= model.ChildrenChanged;
                oldLayout.ChildRemoved -= model.ChildrenChanged;
                oldLayout.ChildrenReordered -= model.ChildrenChanged;
            }

            if (newValue is Layout<View> layout)
            {
                model.ChildrenChanged(layout, new EventArgs());

                layout.ChildAdded += model.ChildrenChanged;
                layout.ChildRemoved += model.ChildrenChanged;
                layout.ChildrenReordered += model.ChildrenChanged;
            }
        });

        public Layout<View> Layout
        {
            get => (Layout<View>)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public View Child
        {
            get => _Child;
            private set
            {
                if (value != _Child)
                {
                    _Child = value;
                    OnPropertyChanged(nameof(Child));
                }
            }
        }

        public int Index { get; set; }

        private View _Child;

        private void ChildrenChanged(object sender, EventArgs e) => Child = Index < Layout.Children.Count ? Layout.Children[Index] : null;

        public override string ToString() => "Children[" + Index + "]";
    }

    public class MaxContentView : ContentView
    {
        public static readonly BindableProperty MaxWidthProperty = BindableProperty.Create(nameof(MaxWidth), typeof(double), typeof(MaxContentView));

        public static readonly BindableProperty MaxHeightProperty = BindableProperty.Create(nameof(MaxHeight), typeof(double), typeof(MaxContentView));

        public double MaxWidth
        {
            get => (double)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        public double MaxHeight
        {
            get => (double)GetValue(MaxHeightProperty);
            set => SetValue(MaxHeightProperty, value);
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint) => base.OnMeasure(Math.Min(MaxWidth, widthConstraint), Math.Min(MaxHeight, heightConstraint));
    }
}