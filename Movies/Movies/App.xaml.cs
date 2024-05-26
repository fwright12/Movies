using Movies.Data;
using Movies.Models;
using Movies.ViewModels;
using Movies.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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
     */

    public static class JavaScriptEvaluationService
    {
        public static IJavaScriptEvaluatorFactory Factory { get; private set; }

        public static void Register(IJavaScriptEvaluatorFactory factory)
        {
            Factory = factory;
        }
    }

    public partial class App : Application
    {
        public static readonly string[] AdKeywords = "movie,tv,shows,tv show,series,streaming,entertainment,watch,list,film,actor,guide,library,theater".Split(',').ToArray();

        public static readonly ImageSource TMDbAttribution = ImageSource.FromResource("Movies.Logos.TMDbAttribution.png");
        public static readonly ImageSource TraktAttributionLight = ImageSource.FromResource("Movies.Logos.TraktAttributionLight.png");
        public static readonly ImageSource TraktAttributionDark = ImageSource.FromResource("Movies.Logos.TraktAttributionDark.png");
        public static readonly ImageSource JustWatchAttribution = ImageSource.FromResource("Movies.Logos.JustWatchAttribution.png");

        public static readonly BiMap<ItemType, Type> TypeMap = new BiMap<ItemType, Type>
        {
            [ItemType.Movie] = typeof(Movie),
            [ItemType.TVShow] = typeof(TVShow),
            [ItemType.TVSeason] = typeof(TVSeason),
            [ItemType.TVEpisode] = typeof(TVEpisode),
            [ItemType.Person] = typeof(Person),
            [ItemType.Collection] = typeof(Collection),
            [ItemType.List] = typeof(List),
            [ItemType.Company] = typeof(Company),
            [ItemType.Network] = typeof(Company),
            [ItemType.WatchProvider] = typeof(WatchProvider),
        };

        public UserPrefs Prefs { get; }

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
        public CollectionViewModel Popular => GetPopular();
        //public List<WatchProvider> MyServices { get; } = null;// new List<WatchProvider>(MockData.Interstellar.WatchProviders);

        public IEnumerable<AccountViewModel> Accounts { get; }

        new public static App Current => Application.Current as App;

        public static ICommand FocusCommand { get; } = new Command<VisualElement>(visualElement => visualElement.Focus());
        public ICommand SaveRatingTemplatesCommand { get; }

        private Dictionary<ServiceName, IService> Services;
        private Database LocalDatabase;
        private Session Session { get; }

        private CollectionViewModel _Popular;

#if DEBUG
        public static Task Message(string message) => Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Console", message, "OK");

        private static System.IO.StringWriter iOSConsole;

        private void App_PageAppearing(object sender, Page e)
        {
            PageAppearing -= App_PageAppearing;

            e.ToolbarItems.Add(new ToolbarItem
            {
                Text = "Console",
                Command = new Command(async () =>
                {
                    await MainPage.Navigation.PushAsync(new ContentPage
                    {
                        Content = new ScrollView
                        {
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
                            Content = new Label
                            {
                                Text = iOSConsole.ToString()
                            }
                        }
                    });
                })
            });
        }

        private class FilterWriter : System.IO.StringWriter
        {
        }
#endif

        public App()
        {
#if DEBUG
            if (Device.RuntimePlatform == Device.iOS)
            {
                iOSConsole = new System.IO.StringWriter();
                System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(iOSConsole));
                PageAppearing += App_PageAppearing;
            }

            foreach (var kvp in new Dictionary<string, string>()
            {
                [GetLoginCredentialsKey(ServiceName.TMDb)] = TMDB_DEV_LOGIN_INFO,
                [GetLoginCredentialsKey(ServiceName.Trakt)] = TRAKT_LOGIN_INFO,

            })
            {
                if (!Properties.ContainsKey(kvp.Key))
                {
                    Properties[kvp.Key] = kvp.Value;
                }
            }


            //Properties.Remove(GetLoginCredentialsKey(ServiceName.TMDb));
            //Properties.Remove(GetLoginCredentialsKey(ServiceName.Trakt));

            _ = SavePropertiesAsync();

            //UserAppTheme = OSAppTheme.Dark;
#endif

            Prefs = new UserPrefs(Properties);
            Prefs.PropertyChanged += async (sender, e) => await SavePropertiesAsync();

            TMDB.LANGUAGE = Prefs.Language;
            TMDB.REGION = Prefs.Region;

            TMDB tmdb = new TMDB(TMDB_API_KEY, TMDB_V4_BEARER, new AppPropertiesCache(this));
            Trakt trakt = new Trakt(tmdb, TRAKT_CLIENT_ID, TRAKT_CLIENT_SECRET);

            TMDbGetPropertyValues = tmdb.GetPropertyValues;
            tmdb.Company.LogoPath ??= "file://Movies.Logos.TMDbLogo.png";
            trakt.Company.LogoPath ??= "file://Movies.Logos.TraktLogo.png";

            async Task SetRegions()
            {
                var regions = new List<Region>();
                await tmdb.SetValues(regions, API.CONFIGURATION.GET_COUNTRIES, new JsonArrayParser<Region>(new JsonNodeParser<Region>((JsonNode json, out Region region) =>
                {
                    if (json.TryGetValue("iso_3166_1", out string iso_3166))
                    {
                        region = new Region(iso_3166, json["english_name"]?.TryGetValue<string>());
                        return true;
                    }

                    region = null;
                    return false;
                })));

                foreach (var region in regions.OrderBy(region => region.ToString()))
                {
                    Prefs.Regions.Add(region);
                }
            }

            _ = SetRegions();

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

            Session = new Session(Properties);
            Session.PropertyChanged += async (sender, e) => await SavePropertiesAsync();

            LocalDatabase = new Database(tmdb, TMDB.IDKey, Session.DBLastCleaned);
            if (LocalDatabase.NeedsCleaning)
            {
                Session.DBLastCleaned = DateTime.Now;
            }
            //DataManager.AddDataSource(tmdb);

            ItemHelpers.PersistentCache = LocalDatabase.ItemCache;
            //_ = tmdb.SetItemCache(LocalDatabase.ItemCache, Session.LastAccessed);
            Session.LastAccessed = DateTime.Now;

            var resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);
            IAsyncEventProcessor<IEnumerable<ResourceRequestArgs<Uri>>> tmdbHandlers = new TMDbHttpProcessor(TMDB.WebClient, resolver, TMDbApi.AutoAppend);
            var tmdbLocalCache = new TMDbLocalCache(LocalDatabase.ItemCache, resolver);

            DataService.Instance.Controller
                .SetNext(new AsyncCacheAsideProcessor<ResourceRequestArgs<Uri>>(new UriBufferedCache(tmdbLocalCache)))
                .SetNext(tmdbHandlers);

#if DEBUG
            MovieExplore = new List<object>
            {
                new CollectionViewModel("Trending Movies", tmdb.GetTrendingMoviesAsync()),
                new CollectionViewModel("Trending TV", tmdb.GetTrendingTVShowsAsync()),
                new CollectionViewModel("Trending People", tmdb.GetTrendingPeopleAsync()),
            };

            TVExplore = new List<object>
            {
                new CollectionViewModel("Trending TV", tmdb.GetTrendingTVShowsAsync()),
            };

            PeopleExplore = new List<object>
            {
                new CollectionViewModel("Trending People", tmdb.GetTrendingPeopleAsync()),
            };
#else
            MovieExplore = new ObservableCollection<object>
            {
                new CollectionViewModel("Trending", tmdb.GetTrendingMoviesAsync()),
                new CollectionViewModel("Most Anticipated", trakt.GetAnticpatedMoviesAsync()),
                new CollectionViewModel("In Theaters", tmdb.GetNowPlayingMoviesAsync()),
                new CollectionViewModel("Upcoming", tmdb.GetUpcomingMoviesAsync()),
                new CollectionViewModel("Top Rated", tmdb.GetTopRatedMoviesAsync()),
            };

            TVExplore = new ObservableCollection<object>
            {
                new CollectionViewModel("Trending", tmdb.GetTrendingTVShowsAsync()),
                new CollectionViewModel("Most Anticipated", trakt.GetAnticipatedTVAsync()),
                new CollectionViewModel("Currently Airing", tmdb.GetTVOnAirAsync()),
                new CollectionViewModel("Airing Today", tmdb.GetTVAiringTodayAsync()),
                new CollectionViewModel("Top Rated", tmdb.GetTopRatedTVShowsAsync()),
            };

            PeopleExplore = new ObservableCollection<object>
            {
                new CollectionViewModel("Trending", tmdb.GetTrendingPeopleAsync()),
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

            //CustomLists = new List<ListViewModel>();
            //LazyWatchlist = new Lazy<Task<ListViewModel>>(() => Task.FromResult<ListViewModel>(null));
            //LazyFavorites = new Lazy<Task<ListViewModel>>(() => Task.FromResult<ListViewModel>(null));
            //LazyHistory = new Lazy<Task<ListViewModel>>(() => Task.FromResult<ListViewModel>(null));

            CreateListCommand = new Command<ElementTemplate>(template => _ = CreateList(template));
            AddSyncSourceCommand = new Command<ListViewModel>(model => _ = AddSyncSource(model), list => list != null && list.SyncWith.Count != LoggedInListProviders().Count());
            DeleteListCommand = new Command<ListViewModel>(list => _ = DeleteList(list));
            OpenFiltersCommand = new Command<DrawerView>(async drawer =>
            {
                if (drawer?.BindingContext is ListViewModel list && !list.SyncWith.Any(sync => sync.Provider == LocalDatabase))
                {
                    await Shell.Current.CurrentPage.DisplayAlert("Feature not available", "You must sync this list locally to access this feature. To do this go to Edit -> Add Source and select \"Local\"", "Ok");
                    return;
                }

                drawer.NextSnapPointCommand.Execute(null);
            });
            SaveRatingTemplatesCommand = new Command(async () => await SaveRatingsTemplates(RatingTemplateManager.Items));

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

            if (item is Movie movie) context = new MovieViewModel(movie);
            else if (item is TVShow show) context = new TVShowViewModel(show);
            else if (item is TVSeason season) context = new TVSeasonViewModel(season);
            else if (item is TVEpisode episode) context = new TVEpisodeViewModel(episode);
            else if (item is Collection collection) context = new CollectionViewModel(collection);
            else if (item is Person person) context = new PersonViewModel(person);

            if (context != null)
            {
                bindable.BindingContext = context;
            }
        });

        public static Item GetAutoWireFromItem(BindableObject bindable) => (Item)bindable.GetValue(AutoWireFromItemProperty);
        public static void SetAutoWireFromItem(BindableObject bindable, Item value) => bindable.SetValue(AutoWireFromItemProperty, value);

        public class PeopleSearch : AsyncFilterable<PersonViewModel>
        {
            public override async IAsyncEnumerator<PersonViewModel> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                if (!(predicate is SearchPredicate search) || string.IsNullOrEmpty(search.Query))
                {
                    yield break;
                }

#if DEBUG && true
                await System.Threading.Tasks.Task.CompletedTask;
                var MatthewM = new Person("Matthew M").WithID(TMDB.IDKey, 0);

                foreach (var item in await System.Threading.Tasks.Task.FromResult(Enumerable.Repeat(new PersonViewModel(MatthewM.WithID(TMDB.IDKey, 0)), 5)))
                {
                    yield return item;
                }
#else
                await foreach (var person in TMDB.Database.SearchPeople(search.Query))
                {
                    yield return new PersonViewModel(person);
                }
#endif
            }
        }

        public class KeywordsSearch : AsyncFilterable<Keyword>
        {
            public override async IAsyncEnumerator<Keyword> GetAsyncEnumerator(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                if (!(predicate is SearchPredicate search) || string.IsNullOrEmpty(search.Query))
                {
                    yield break;
                }

#if DEBUG && true
                await System.Threading.Tasks.Task.CompletedTask;

                var keywords = await System.Threading.Tasks.Task.FromResult(App.AdKeywords);
                for (int i = 0; i < keywords.Length; i++)
                {
                    yield return new Keyword
                    {
                        Id = i,
                        Name = keywords[i],
                    };
                }
#else
                await foreach (var keyword in TMDB.Database.SearchKeywords(search.Query))
                {
                    yield return keyword;
                }
#endif
            }
        }

#if DEBUG
        private void TMDBSetup(AccountViewModel tmdb) { }
        private void TraktSetup(AccountViewModel tmdb) { }
#else
        private void AddFirst(IList<object> list, CollectionViewModel model)
        {
            list.Insert(0, model);
            return;

            if (model.Source.Items is INotifyCollectionChanged observable)
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
                //_ = model.LoadMoreCommand;
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
                    AddFirst(MovieExplore, new CollectionViewModel(recommended, tmdb.GetRecommendedMoviesAsync()));
                    AddFirst(TVExplore, new CollectionViewModel(recommended, tmdb.GetRecommendedTVShowsAsync()));
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
                    AddFirst(MovieExplore, new CollectionViewModel(recommended, trakt.GetRecommendedMoviesAsync()));
                    AddFirst(TVExplore, new CollectionViewModel(recommended, trakt.GetRecommendedTVAsync()));
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

        private CollectionViewModel GetPopular()
        {
            if (_Popular != null)
            {
                return _Popular;
            }
            
            _Popular = new CollectionViewModel(null, TMDB.Database.Instance, ItemType.Movie | ItemType.TVShow | ItemType.Person | ItemType.Collection, TMDB.Database.Instance)// | ItemType.Company)
            {
                //Name = "Popular",
                ListLayout = ListLayouts.Grid,
                SortOptions = new List<Property> { TMDB.POPULARITY, Movie.RELEASE_DATE, Movie.REVENUE, Media.ORIGINAL_TITLE, TMDB.SCORE, TMDB.VOTE_COUNT },
            };

            if (Popular.Source.Predicate is ExpressionBuilder builder && builder.Editor is MultiEditor editor)
            {
                var search = new SimpleEditor(() => new SearchPredicateBuilder
                {
                    Placeholder = "Search movies, TV, people, and more..."
                });
                search.AddNew();
                editor.Editors.Insert(0, search);

                var exclude = new List<Property> { Movie.BUDGET, Movie.REVENUE };
                Popular.AddFilters(FilterHelpers.PreferredFilterOrder.Except(exclude));

                _ = RestoreFilters(Popular);
            }

            return Popular;
        }

        private Task TMDbGetPropertyValues { get; }

        private string GetFiltersKey(CollectionViewModel collection) => collection == _Popular ? "Popular" : (collection.Item as List)?.ID ?? collection.Name;

        private async Task RestoreFilters(CollectionViewModel collection)
        {
            var key = GetFiltersKey(collection);
            await TMDbGetPropertyValues;

            try
            {
                if (key != null && collection.Source.Predicate is ExpressionBuilder builder && Session.TryGetFilters(key, out var predicates))
                {
#if !DEBUG || true
                    var expression = new BooleanExpression();

                    foreach (var predicate in predicates)
                    {
                        expression.Predicates.Add(predicate);
                    }

                    builder.Add(expression);

                    foreach (var child in builder.Root.Children)
                    {
                        builder.Editor.Select(child);
                    }
#endif
                }
            }
            catch (Exception e)
            {
                Print.Log(e);
            }

            collection.Source.Predicate.PredicateChanged += async (sender, e) =>
            {
                var builder = (IPredicateBuilder)sender;

                try
                {
                    await SaveFilters(collection);
                }
                catch (Exception e1)
                {
                    Print.Log(e1);
                }
            };

            //SavedFilters[key] = collection.Source
        }

        private Task SaveFilters(CollectionViewModel collection)
        {
            var filters = collection.Source.Predicate.Predicate;
            //var temp = SerializeFilters(builder.Predicate);
            var predicates = (filters as BooleanExpression)?.Predicates.OfType<OperatorPredicate>() ?? Enumerable.Empty<OperatorPredicate>();
#if DEBUG && false
            var temp = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<OperatorPredicate>>(json, FilterSerializerOptions);

            Print.Log(json);
#endif

            if (GetFiltersKey(collection) is string key)
            {
                Session.SaveFilters(key, predicates);
                return SavePropertiesAsync();
            }

            return Task.CompletedTask;
        }

        public IEnumerable<IListProvider> LoggedInListProviders() => Services.Values.OfType<IListProvider>().Where(provider => !(provider is IAccount account) || Accounts?.FirstOrDefault(temp => temp.Account == account)?.IsLoggedIn == true);

        private static string GetLoginCredentialsKey(ServiceName name) => name.ToString() + " login credentials";

        public async Task Login(IAccount account, object credentials = null)
        {
#if !DEBUG || true
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

        public static TimeSpan LogoCacheValidity = new TimeSpan(7, 0, 0, 0);

        public RatingTemplateCollectionViewModel RatingTemplateManager { get; } = new RatingTemplateCollectionViewModel();

        protected override void OnStart()
        {
#if DEBUG
            //if (!Properties.ContainsKey(RATING_TEMPLATES_KEY))
            _ = SaveRatingsTemplates(MockData.RATING_TEMPLATES);
#endif

            if (Properties.TryGetValue(RATING_TEMPLATES_KEY, out var templatesObj))
            {
                var templates = JsonSerializer.Deserialize<IEnumerable<RatingTemplate>>(templatesObj.ToString());
#if DEBUG
                //templates.Last().ScoreJavaScript = "9";
                //RatingTemplateManager.Items.Add(templates.First());
                //RatingTemplateManager.Items.AddRange(templates.Take(2));
#endif
                RatingTemplateManager.Items.AddRange(templates);
            }
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

        private static readonly string RATING_TEMPLATES_KEY = "ratings templates";

        private static async Task SaveRatingsTemplates(IEnumerable<RatingTemplate> templates)
        {
            Application.Current.Properties[RATING_TEMPLATES_KEY] = JsonSerializer.Serialize(templates);
            await Application.Current.SavePropertiesAsync();
        }
    }
}