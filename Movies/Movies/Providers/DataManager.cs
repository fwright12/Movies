using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Movies.Models;

namespace Movies
{
    public interface IAssignID<T>
    {
        public bool TryGetID(Item item, out T id);
        public Task<Item> GetItem(ItemType type, T id);
    }

    public interface IService
    {
        string Name { get; }
    }

    public interface IDataProvider : IService
    {
        void HandleInfoRequests(DataManager manager);
    }

    public interface IListProvider : IService
    {
        Task<List> GetWatchlist();
        Task<List> GetFavorites();
        Task<List> GetHistory();

        IAsyncEnumerable<List> GetAllListsAsync();
        List CreateList();
        //Task UpdateListAsync(Collection list);
        //Task DeleteListAsync(int listID);
        Task<List> GetListAsync(string id);
    }

    public interface IAccount : IService
    {
        public Company Company { get; }
        public string Username { get; }

        Task<string> GetOAuthURL(Uri redirectUri);
        Task<object> Login(object credentials);
        Task<bool> Logout();
    }

    public sealed class ID<T>
    {
        private ID() { }

        public sealed class Key
        {
            public readonly ID<T> ID = new ID<T>();
        }
    }

    public static class ItemExtensions
    {
        public static TItem WithID<TItem, TID>(this TItem item, ID<TID>.Key key, TID value) where TItem : Item
        {
            item.SetID(key, value);
            return item;
        }
    }

    public class ItemInfoEventArgs<TItem, TValue> : EventArgs
    {
        public TItem Item { get; }
        public string Property { get; }
        public Task<TValue> Value { get; private set; }

        public ItemInfoEventArgs(TItem item, string property) : this(item)
        {
            Property = property;
        }

        public ItemInfoEventArgs(TItem item)
        {
            Item = item;
        }

        public void SetValue(Task<TValue> value) => Value = value;
        public void SetValue(TValue value) => SetValue(Task.FromResult(value));
    }

    public class InfoRequestHandler<TItem>
    {
        private EventHandler<ItemInfoEventArgs<TItem, object>> Handler;
        private Dictionary<TItem, Dictionary<string, Task<object>>> Cache = new Dictionary<TItem, Dictionary<string, Task<object>>>();

        public Task<object> GetSingle(TItem item, string property)
        {
            if (Cache.TryGetValue(item, out var properties) && properties.TryGetValue(property, out var value))
            {
                return value;

                if (value.IsCompleted && !value.IsCompletedSuccessfully)
                {
                    Cache.Remove(item);
                }
                else
                {

                }
            }

            var args = new ItemInfoEventArgs<TItem, object>(item);

            try
            {
                Handler?.Invoke(item, args);
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                //throw e;
#endif
                return default;
            }

            Task<object> result;

            if (args.Value == null)
            {
                result = Task.FromResult<object>(default);
            }
            else
            {
                result = args.Value;
                //Cache.Add(item, result = args.Value);
            }

            return result;
        }

        public void AddHandler(EventHandler<ItemInfoEventArgs<TItem, object>> handler)
        {
            Handler += handler;
        }
    }

    public class InfoRequestHandler<TItem, TValue>
    {
        private EventHandler<ItemInfoEventArgs<TItem, TValue>> Handler;
        private Dictionary<TItem, Task<TValue>> Cache = new Dictionary<TItem, Task<TValue>>();

        public Task<TValue> GetSingle(TItem item)
        {
            if (Cache.TryGetValue(item, out var value))
            {
                return value;

                if (value.IsCompleted && !value.IsCompletedSuccessfully)
                {
                    Cache.Remove(item);
                }
                else
                {
                    
                }
            }

            var args = new ItemInfoEventArgs<TItem, TValue>(item);

            try
            {
                Handler?.Invoke(item, args);
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                //throw e;
#endif
                return default;
            }

            Task<TValue> result;

            if (args.Value == null)
            {
                result = Task.FromResult(default(TValue));
            }
            else
            {
                Cache.Add(item, result = args.Value);
            }

            return result;
        }

        public void AddHandler(EventHandler<ItemInfoEventArgs<TItem, TValue>> handler)
        {
            Handler += handler;
        }
    }

    public enum Operators
    {
        Equal = 0,
        LessThan = -1,
        GreaterThan = 1,
        NotEqual = 2
    }

    public interface IBooleanValued<in T>
    {
        bool Evaluate(T value);
    }

    public abstract class Constraint : IBooleanValued<Item>
    {
        public string Name { get; }
        public object Value { get; set; }
        public Operators Comparison { get; set; }

        public Constraint(string name)
        {
            Name = name;
        }

        public bool Evaluate(Item value) => IsAllowed((Item)value);
        public abstract bool IsAllowed(object value);

        public override bool Equals(object obj) => obj is Constraint constraint && constraint.Name == Name && Equals(constraint.Value, Value) && constraint.Comparison == Comparison;

        public override int GetHashCode() => Name?.GetHashCode() ?? base.GetHashCode();
    }

    public class Constraint<T> : Constraint
    {
        new public T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }

        public Constraint(string name) : base(name) { }

        public override bool IsAllowed(object value)
        {
            if (!(value is T t))
            {
                return false;
            }

            if (t is IComparable comparable)
            {
                int compare = comparable.CompareTo(Value);
                return Comparison == Operators.NotEqual ? compare != 0 : compare == (int)Comparison;
            }

            if (Comparison == Operators.LessThan || Comparison == Operators.GreaterThan)
            {
                return false;
            }

            var equals = Equals(value, Value);
            return Comparison == Operators.Equal ? equals : !equals;
        }
    }

    public abstract class DataService<T> where T : Item
    {
        public abstract Task<TResult> GetValue<TResult>(T item, string property);
    }

    public abstract class MediaService<TItem> : DataService<TItem> where TItem : Item
    {
        public static readonly string TITLE = "Title";
        public static readonly string TAGLINE = "Tagline";
        public static readonly string DESCRIPTION = "Description";
        public static readonly string CONTENT_RATING = "Content Rating";
        public static readonly string RUNTIME = "Runtime";
        public static readonly string OriginalTitle = "Original Title";
        public static readonly string OriginalLanguage = "Original Language";
        public static readonly string LANGUAGES = "Languages";
        public static readonly string GENRES = "Genres";

        public static readonly string POSTER_PATH = "Poster Path";
        public static readonly string BACKDROP_PATH = "Backdrop Path";
        public static readonly string TRAILER_PATH = "Trailer Path";

        public static readonly string RATING = "Rating";
        public static readonly string CREW = "Crew";
        public static readonly string CAST = "Cast";
        public static readonly string PRODUCTION_COMPANIES = "Production Companies";
        public static readonly string PRODUCTION_COUNTRIES = "Production Countries";
        public static readonly string WATCH_PROVIDERS = "Watch Providers";
        public static readonly string KEYWORDS = "Keywords";
        public static readonly string RECOMMENDED = "Recommended";

        public static readonly IReadOnlyCollection<Constraint> Properties = new HashSet<Constraint>
        {
            
        };

        public override Task<TResult> GetValue<TResult>(TItem item, string property)
        {
            throw new NotImplementedException();
        }

        public readonly InfoRequestHandler<TItem, string> TaglineRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, string> DescriptionRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, string> ContentRatingRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, TimeSpan?> RuntimeRequested = new InfoRequestHandler<TItem, TimeSpan?>();
        public readonly InfoRequestHandler<TItem, string> OriginalTitleRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, string> OriginalLanguageRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, IEnumerable<string>> LanguagesRequested = new InfoRequestHandler<TItem, IEnumerable<string>>();
        public readonly InfoRequestHandler<TItem, IEnumerable<string>> GenresRequested = new InfoRequestHandler<TItem, IEnumerable<string>>();

        public readonly InfoRequestHandler<TItem, string> PosterPathRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, string> BackdropPathRequested = new InfoRequestHandler<TItem, string>();
        public readonly InfoRequestHandler<TItem, string> TrailerPathRequested = new InfoRequestHandler<TItem, string>();

        public readonly InfoRequestHandler<TItem, Rating> RatingRequested = new InfoRequestHandler<TItem, Rating>();
        public readonly InfoRequestHandler<TItem, IEnumerable<Credit>> CrewRequested = new InfoRequestHandler<TItem, IEnumerable<Credit>>();
        public readonly InfoRequestHandler<TItem, IEnumerable<Credit>> CastRequested = new InfoRequestHandler<TItem, IEnumerable<Credit>>();
        public readonly InfoRequestHandler<TItem, IEnumerable<Company>> ProductionCompaniesRequested = new InfoRequestHandler<TItem, IEnumerable<Company>>();
        public readonly InfoRequestHandler<TItem, IEnumerable<string>> ProductionCountriesRequested = new InfoRequestHandler<TItem, IEnumerable<string>>();
        public readonly InfoRequestHandler<TItem, IEnumerable<WatchProvider>> WatchProvidersRequested = new InfoRequestHandler<TItem, IEnumerable<WatchProvider>>();
        public readonly InfoRequestHandler<TItem, IEnumerable<string>> KeywordsRequested = new InfoRequestHandler<TItem, IEnumerable<string>>();
        public readonly InfoRequestHandler<TItem, IAsyncEnumerable<Item>> RecommendedRequested = new InfoRequestHandler<TItem, IAsyncEnumerable<Item>>();
    }

    public class MovieService : MediaService<Movie>
    {
        public static readonly string RELEASE_DATE = "Release Date";
        public static readonly string BUDGET = "Budget";
        public static readonly string REVENUE = "Revenue";
        public static readonly string PARENT_COLLECTION = "Parent Collection";

        public readonly InfoRequestHandler<Movie, DateTime?> ReleaseDateRequested = new InfoRequestHandler<Movie, DateTime?>();

        public readonly InfoRequestHandler<Movie, long?> BudgetRequested = new InfoRequestHandler<Movie, long?>();
        public readonly InfoRequestHandler<Movie, long?> RevenueRequested = new InfoRequestHandler<Movie, long?>();
        public readonly InfoRequestHandler<Movie, Collection> ParentCollectionRequested = new InfoRequestHandler<Movie, Collection>();
    }

    public class TVShowService : MediaService<TVShow>
    {
        public static readonly string FIRST_AIR_DATE = "First Air Date";
        public static readonly string LAST_AIR_DATE = "Last Air Date";
        public static readonly string NETWORKS = "Networks";

        public readonly InfoRequestHandler<TVShow, DateTime?> FirstAirDateRequested = new InfoRequestHandler<TVShow, DateTime?>();
        public readonly InfoRequestHandler<TVShow, DateTime?> LastAirDateRequested = new InfoRequestHandler<TVShow, DateTime?>();
        public readonly InfoRequestHandler<TVShow, IEnumerable<Company>> NetworksRequested = new InfoRequestHandler<TVShow, IEnumerable<Company>>();
    }

    public class TVSeasonService
    {
        public static readonly string YEAR = "Year";
        public static readonly string AVERAGE_RUNTIME = "Average Runtime";
        public static readonly string CAST = "Cast";
        public static readonly string CREW = "Crew";

        public readonly InfoRequestHandler<TVSeason, DateTime?> YearRequested = new InfoRequestHandler<TVSeason, DateTime?>();
        public readonly InfoRequestHandler<TVSeason, TimeSpan?> AvgRuntimeRequested = new InfoRequestHandler<TVSeason, TimeSpan?>();
        public readonly InfoRequestHandler<TVSeason, IEnumerable<Credit>> CastRequested = new InfoRequestHandler<TVSeason, IEnumerable<Credit>>();
        public readonly InfoRequestHandler<TVSeason, IEnumerable<Credit>> CrewRequested = new InfoRequestHandler<TVSeason, IEnumerable<Credit>>();
    }

    public class TVEpisodeService : MediaService<TVEpisode>
    {
        public static readonly string AIR_DATE = "Air Date";

        public readonly InfoRequestHandler<TVEpisode, DateTime?> AirDateRequested = new InfoRequestHandler<TVEpisode, DateTime?>();
    }

    public class PersonService
    {
        public static readonly string BIRTHDAY = "Birthday";
        public static readonly string BIRTHPLACE = "Birthplace";
        public static readonly string DEATHDAY = "Deathday";
        public static readonly string ALSO_KNOWN_AS = "Also Known As";
        public static readonly string GENDER = "Gender";
        public static readonly string BIO = "Bio";
        public static readonly string PROFILE_PATH = "Profile Path";
        public static readonly string CREDITS = "Credits";

        public readonly InfoRequestHandler<Person, DateTime?> BirthdayRequested = new InfoRequestHandler<Person, DateTime?>();
        public readonly InfoRequestHandler<Person, string> BirthplaceRequested = new InfoRequestHandler<Person, string>();
        public readonly InfoRequestHandler<Person, DateTime?> DeathdayRequested = new InfoRequestHandler<Person, DateTime?>();
        public readonly InfoRequestHandler<Person, IEnumerable<string>> AlsoKnownAsRequested = new InfoRequestHandler<Person, IEnumerable<string>>();
        public readonly InfoRequestHandler<Person, string> GenderRequested = new InfoRequestHandler<Person, string>();
        public readonly InfoRequestHandler<Person, string> BioRequested = new InfoRequestHandler<Person, string>();
        public readonly InfoRequestHandler<Person, string> ProfilePathRequested = new InfoRequestHandler<Person, string>();
        public readonly InfoRequestHandler<Person, IEnumerable<Item>> CreditsRequested = new InfoRequestHandler<Person, IEnumerable<Item>>();
    }

    public class DataManager
    {
        public InfoRequestHandler<Movie> MovieInfoRequested = new InfoRequestHandler<Movie>();
        public InfoRequestHandler<TVShow> TVShowInfoRequested = new InfoRequestHandler<TVShow>();
        public InfoRequestHandler<TVSeason> TVSeasonInfoRequested = new InfoRequestHandler<TVSeason>();
        public InfoRequestHandler<TVEpisode> TVEpisodeInfoRequested = new InfoRequestHandler<TVEpisode>();
        public InfoRequestHandler<Person> PersonInfoRequested = new InfoRequestHandler<Person>();

        public event EventHandler<SearchEventArgs> Searched;
        public event EventHandler<RatingEventArgs> ItemRated;

        public readonly MovieService MovieService;
        public readonly TVShowService TVShowService;
        public readonly TVSeasonService TVSeasonService;
        public readonly TVEpisodeService TVEpisodeService;
        public readonly PersonService PersonService = new PersonService();

        public bool Batched { get; private set; }

        public void BatchBegin() => Batched = true;
        public void BatchEnd() => Batched = false;

        /*public bool IsBatched(Movie movie) => Batch.CurrentCount == 1;
        public async void BatchBegin(Movie movie)
        {
            Batch.Release();
            await Batch.WaitAsync();
        }

        public async Task BatchEnd(Movie movie)
        {
            await Batch.WaitAsync();
            Batch.Release();
        }*/

        private List<IDataProvider> DataSources;

        public DataManager(params IDataProvider[] dataSources)
        {
            DataSources = new List<IDataProvider>(dataSources);
            
            MovieService = new MovieService();
            TVShowService = new TVShowService();
            TVSeasonService = new TVSeasonService();
            TVEpisodeService = new TVEpisodeService();
        }

        public void AddDataSource(IDataProvider data)
        {
            data.HandleInfoRequests(this);
            DataSources.Add(data);
        }

        public Task<object> Request<T>(Item item, string property)
        {
            if (item is Movie movie)
            {
                return MovieInfoRequested.GetSingle(movie, property);
            }
            else if (item is TVShow show)
            {
                return TVShowInfoRequested.GetSingle(show, property);
            }
            else if (item is TVSeason season)
            {
                return TVSeasonInfoRequested.GetSingle(season, property);
            }
            else if (item is TVEpisode episode)
            {
                return TVEpisodeInfoRequested.GetSingle(episode, property);
            }
            else if (item is Person person)
            {
                return PersonInfoRequested.GetSingle(person, property);
            }
            else
            {
                return default;
            }
        }

        public IAsyncEnumerable<Item> Search(string query = null, Dictionary<string, object> filters = null, string sortBy = null, bool sortAscending = false)
        {
            var e = new SearchEventArgs(query, filters, sortBy, sortAscending);
            Searched?.Invoke(this, e);
            return e.Results;
        }

        public void Rate(Movie movie, double score) => Rate(movie, score);
        public void Rate(TVShow show, double score) => Rate(show, score);

        private void Rate(Item item, double score)
        {
            ItemRated?.Invoke(this, new RatingEventArgs(item, new Rating { Score = score }));
        }

        public class SearchEventArgs : EventArgs
        {
            public string Query { get; }
            public IDictionary<string, object> Filters { get; }
            public string SortBy { get; }
            public bool SortAscending { get; }
            public IAsyncEnumerable<Item> Results { get; set; }

            public SearchEventArgs(string query = null, IDictionary<string, object> filters = null, string sortBy = null, bool sortAscending = false)
            {
                Query = query;
                Filters = filters;
                SortBy = sortBy;
                SortAscending = sortAscending;
            }
        }

        public class RatingEventArgs : EventArgs
        {
            public object Item { get; }
            public Rating Rating { get; }

            public RatingEventArgs(object item, Rating rating)
            {
                Item = item;
                Rating = rating;
            }
        }
    }
}
