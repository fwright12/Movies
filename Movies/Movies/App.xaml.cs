using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
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
     * Compiled bindings
     * Clean up user database
     * Language and region settings
     * Fix autosizing
     * Character/job for tv show/season credits
     * Throw error when failing to parse json
     * Try api calls again
     * Removed AspectContentView, added aspect request to ImageView
     * Better template for tv episode grid view
     * Allow more release types for certifications
     * Order collection parts by release date
     * 
     */

    public partial class App : Application
    {
        public static readonly string[] AdKeywords = "movie,tv,shows,tv show,series,streaming,entertainment,watch,list,film,actor,guide,library,theater".Split(',').ToArray();

        public static readonly ImageSource TMDbAttribution = ImageSource.FromResource("Movies.Logos.TMDbAttribution.png");
        public static readonly ImageSource TraktAttributionLight = ImageSource.FromResource("Movies.Logos.TraktAttributionLight.png");
        public static readonly ImageSource TraktAttributionDark = ImageSource.FromResource("Movies.Logos.TraktAttributionDark.png");
        public static readonly ImageSource JustWatchAttribution = ImageSource.FromResource("Movies.Logos.JustWatchAttribution.png");

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
        public CollectionViewModel Popular => _Popular ??= GetPopular();
        //public List<WatchProvider> MyServices { get; } = null;// new List<WatchProvider>(MockData.Interstellar.WatchProviders);

        public IEnumerable<AccountViewModel> Accounts { get; }

        public static ICommand FocusCommand { get; } = new Command<VisualElement>(visualElement => visualElement.Focus());

        private Dictionary<ServiceName, IService> Services;
        private Database LocalDatabase;

        private CollectionViewModel _Popular;

        public App()
        {
#if DEBUG
            bool update = false;

            var cachedWebRequests = new Dictionary<TMDbRequest, string>
            {
                [API.GENRES.GET_MOVIE_LIST] = HttpClient.MOVIE_GENRE_VALUES,
                [API.GENRES.GET_TV_LIST] = HttpClient.TV_GENRE_VALUES,
                [API.CERTIFICATIONS.GET_MOVIE_CERTIFICATIONS] = HttpClient.MOVIE_CERTIFICATION_VALUES,
                [API.CERTIFICATIONS.GET_TV_CERTIFICATIONS] = HttpClient.TV_CERTIFICATION_VALUES,
                [API.WATCH_PROVIDERS.GET_MOVIE_PROVIDERS] = HttpClient.MOVIE_WATCH_PROVIDER_VALUES,
                [API.WATCH_PROVIDERS.GET_TV_PROVIDERS] = HttpClient.TV_WATCH_PROVIDER_VALUES,
                [API.CONFIGURATION.GET_COUNTRIES] = HttpClient.COUNTRIES_VALUES,
            }.Select(kvp => new KeyValuePair<string, object>(kvp.Key.GetURL(), System.Text.Json.JsonSerializer.Serialize(new JsonResponse(kvp.Value))));

            foreach (var kvp in new Dictionary<string, object>(cachedWebRequests)
            {
                [GetLoginCredentialsKey(ServiceName.TMDb)] = TMDB_LOGIN_INFO,
                [GetLoginCredentialsKey(ServiceName.Trakt)] = TRAKT_LOGIN_INFO,
            })
            {
                if (!Properties.ContainsKey(kvp.Key))
                {
                    Properties[kvp.Key] = kvp.Value;
                    update = true;
                }
            }

            if (update)
            {
                _ = SavePropertiesAsync();
            }

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

            _ = tmdb.SetValues(Prefs.Regions, API.CONFIGURATION.GET_COUNTRIES, new JsonArrayParser<Region>(new JsonPropertyParser<Region>("iso_3166_1", new JsonNodeParser<Region>((JsonNode json, out Region region) =>
            {
                if (json.TryGetValue(out string iso_3166))
                {
                    region = new Region(iso_3166);
                    return true;
                }

                region = null;
                return false;
            }))));

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
            //DataManager.AddDataSource(tmdb);

#if DEBUG
            MovieExplore = new List<object>
            {
                new CollectionViewModel("Trending Movies", tmdb.GetTrendingMoviesAsync()),
                new CollectionViewModel("Trending TV", tmdb.GetTrendingTVShowsAsync()),
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
            public override async IAsyncEnumerable<PersonViewModel> GetItems(FilterPredicate predicate, CancellationToken cancellationToken = default)
            {
                if (!(predicate is SearchPredicate search) || string.IsNullOrEmpty(search.Query))
                {
                    yield break;
                }

#if DEBUG && true
                await System.Threading.Tasks.Task.CompletedTask;

                foreach (var item in await System.Threading.Tasks.Task.FromResult(Enumerable.Repeat(new PersonViewModel(MockData.Instance.MatthewM.WithID(TMDB.IDKey, 0)), 5)))
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
            public override async IAsyncEnumerable<Keyword> GetItems(FilterPredicate predicate, CancellationToken cancellationToken = default)
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

        private static readonly string POPULAR_FILTERS = "PopularFilters";
        private readonly System.Text.Json.JsonSerializerOptions FilterSerializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            Converters =
            {
                new ItemTypeConverter(),
                new PredicateConverter(),
                new PropertyConverter(),
                new PersonConverter()
            }
        };

        private CollectionViewModel GetPopular()
        {
            var collection = new CollectionViewModel(null, TMDB.Database.Instance, ItemType.Movie | ItemType.TVShow | ItemType.Person | ItemType.Collection, TMDB.Database.Instance)// | ItemType.Company)
            {
                ListLayout = ListLayouts.Grid,
                SortOptions = new List<Property> { TMDB.POPULARITY, Movie.RELEASE_DATE, Movie.REVENUE, Media.ORIGINAL_TITLE, TMDB.SCORE, TMDB.VOTE_COUNT },
            };

            if (collection.Source.Predicate is ExpressionBuilder builder && builder.Editor is MultiEditor editor)
            {
                var search = new SimpleEditor(() => new SearchPredicateBuilder
                {
                    Placeholder = "Search movies, TV, people, and more..."
                });
                search.AddNew();
                editor.Editors.Insert(0, search);

                if (Properties.TryGetValue(POPULAR_FILTERS, out var value) && value is string filters)
                {
#if !DEBUG || true
                    _ = DeserializeFilters(builder, filters);
#endif
                }
            }

            collection.Source.Predicate.PredicateChanged += (sender, e) =>
            {
                _ = SerializeFilters((IPredicateBuilder)sender);
            };

            return collection;
        }

        private Task TMDbGetPropertyValues { get; }

        private Task SerializeFilters(IPredicateBuilder builder)
        {
            try
            {
                //var temp = SerializeFilters(builder.Predicate);
                var predicates = (builder.Predicate as BooleanExpression)?.Predicates.OfType<OperatorPredicate>() ?? Enumerable.Empty<OperatorPredicate>();
                var json = System.Text.Json.JsonSerializer.Serialize(predicates, FilterSerializerOptions);
#if DEBUG && false
                var temp = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<OperatorPredicate>>(json, FilterSerializerOptions);

                Print.Log(json);
#endif

                Properties[POPULAR_FILTERS] = json;
                return SavePropertiesAsync();
            }
            catch (Exception e1)
            {
                Print.Log(e1);
            }

            return Task.CompletedTask;
        }

        private async Task DeserializeFilters(ExpressionBuilder builder, string filters)
        {
            await TMDbGetPropertyValues;

            try
            {
                var expression = new BooleanExpression();
                var predicates = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<OperatorPredicate>>(filters, FilterSerializerOptions);

                foreach (var predicate in predicates)
                {
                    expression.Predicates.Add(predicate);
                }

                builder.Add(expression);

                foreach (var child in builder.Root.Children)
                {
                    builder.Editor.Select(child);
                }
            }
            catch (Exception e)
            {
                Print.Log(e);
            }
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