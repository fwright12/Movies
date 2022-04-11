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
        public Task<TValue> Value { get; private set; }

        public ItemInfoEventArgs(TItem item)
        {
            Item = item;
        }

        public void SetValue(Task<TValue> value) => Value = value;
        public void SetValue(TValue value) => SetValue(Task.FromResult(value));
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

    public abstract class MediaService<TItem> where TItem : Item
    {
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
        public readonly InfoRequestHandler<Movie, DateTime?> ReleaseDateRequested = new InfoRequestHandler<Movie, DateTime?>();

        public readonly InfoRequestHandler<Movie, long?> BudgetRequested = new InfoRequestHandler<Movie, long?>();
        public readonly InfoRequestHandler<Movie, long?> RevenueRequested = new InfoRequestHandler<Movie, long?>();
        public readonly InfoRequestHandler<Movie, Collection> ParentCollectionRequested = new InfoRequestHandler<Movie, Collection>();
    }

    public class TVShowService : MediaService<TVShow>
    {
        public readonly InfoRequestHandler<TVShow, DateTime?> FirstAirDateRequested = new InfoRequestHandler<TVShow, DateTime?>();
        public readonly InfoRequestHandler<TVShow, DateTime?> LastAirDateRequested = new InfoRequestHandler<TVShow, DateTime?>();
        public readonly InfoRequestHandler<TVShow, IEnumerable<Company>> NetworksRequested = new InfoRequestHandler<TVShow, IEnumerable<Company>>();
    }

    public class TVSeasonService
    {
        public readonly InfoRequestHandler<TVSeason, DateTime?> YearRequested = new InfoRequestHandler<TVSeason, DateTime?>();
        public readonly InfoRequestHandler<TVSeason, TimeSpan?> AvgRuntimeRequested = new InfoRequestHandler<TVSeason, TimeSpan?>();
        public readonly InfoRequestHandler<TVSeason, IEnumerable<Credit>> CastRequested = new InfoRequestHandler<TVSeason, IEnumerable<Credit>>();
        public readonly InfoRequestHandler<TVSeason, IEnumerable<Credit>> CrewRequested = new InfoRequestHandler<TVSeason, IEnumerable<Credit>>();
    }

    public class TVEpisodeService : MediaService<TVEpisode>
    {
        public readonly InfoRequestHandler<TVEpisode, DateTime?> AirDateRequested = new InfoRequestHandler<TVEpisode, DateTime?>();
    }

    public class PersonService
    {
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
