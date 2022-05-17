using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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

    public class ItemInfoEventArgs : EventArgs
    {
        public object Value;
        public string Property { get; }

        public void SetValue(Property property, Task<object> value) => Value = value;
        public void SetValue(Property property, object value) => SetValue(property, Task.FromResult(value));
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

    public class BooleanExpression<T> : IBooleanValued<T>
    {
        public IList<IBooleanValued<T>> Parts { get; }

        public BooleanExpression() : this(System.Linq.Enumerable.Empty<IBooleanValued<T>>()) { }
        public BooleanExpression(IEnumerable<IBooleanValued<T>> parts)
        {
            Parts = new List<IBooleanValued<T>>(parts);
        }

        public bool Evaluate(T value)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BooleanExpression<T> expression))
            {
                return false;
            }

            return System.Linq.Enumerable.SequenceEqual(Parts, expression.Parts);
        }
    }

    public struct Constraint
    {
        public Property Property { get; }
        public object Value { get; set; }
        public Operators Comparison { get; set; }

        public Constraint(Property property)
        {
            Property = property;
            Value = null;
            Comparison = Operators.Equal;
        }
    }


    public abstract class Constraint1 : IBooleanValued<Item>
    {
        public Property Property { get; }
        public object Value { get; set; }
        public Operators Comparison { get; set; }

        public Constraint1(Property property)
        {
            Property = property;
        }

        public bool Evaluate(Item value) => IsAllowed((Item)value);
        public abstract bool IsAllowed(object value);

        public override bool Equals(object obj) => obj is Constraint1 constraint && constraint.Property == Property && Equals(constraint.Value, Value) && constraint.Comparison == Comparison;

        public override int GetHashCode() => Property?.GetHashCode() ?? base.GetHashCode();
    }

    public class Constraint1<T> : Constraint1
    {
        new public T Value
        {
            get => base.Value == null ? default : (T)base.Value;
            set => base.Value = value;
        }

        public Constraint1(Property property) : base(property) { }

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

    public abstract class Property
    {
        public string Name { get; set; }
        public IEnumerable Values { get; }
        public Property Parent { get; }

        public Property(string name, IEnumerable values) : this(name)
        {
            Values = values;
        }

        public Property(string name)
        {
            Name = name;
        }

        //public static Property<T> FromEnum<T>(string name) where T : struct, Enum => new Property<T>(name, new DiscreteValueRange<T>();

        public override int GetHashCode() => Name?.GetHashCode() ?? base.GetHashCode();

        public override bool Equals(object obj) => obj is Property property && property.Name == Name;

        public override string ToString() => Name ?? base.ToString();
    }

    public class Property<T> : Property
    {
        public Property(string name) : base(name) { }
        public Property(string name, IEnumerable values) : base(name, values) { }
    }

    public class ReflectedProperty : Property
    {
        public PropertyInfo Info { get; }

        public ReflectedProperty(PropertyInfo info, IEnumerable values) : this(info.Name, info, values) { }
        public ReflectedProperty(string name, PropertyInfo info, IEnumerable values) : base(name, values)
        {
            Info = info;
        }
    }

    public abstract class SteppedValueRange : IEnumerable
    {
        public object LowerBound { get; set; }
        public object UpperBound { get; set; }
        public object Step { get; set; }

        public abstract IEnumerator GetEnumerator();
    }

    public class SteppedValueRange<T> : SteppedValueRange<T, T>
    {
        public SteppedValueRange(T lowerBound, T upperBound, T step) : base(lowerBound, upperBound, step) { }
    }

    public class SteppedValueRange<TValue, TStep> : SteppedValueRange
    {
        public SteppedValueRange(TValue lowerBound, TValue upperBound, TStep step)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Step = step;
        }

        public override IEnumerator GetEnumerator()
        {
            yield break;
            for (TValue i = (TValue)LowerBound; !Equals(i, UpperBound); )
            {
                yield return i;
            }
        }
    }

    public class DiscreteValueRange<T> : IEnumerable<T>
    {
        public IEnumerable<T> Values { get; }

        public DiscreteValueRange(IEnumerable<T> values)
        {
            Values = values;
        }

        public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class AsyncDiscreteValueRange<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerable<T> Values { get; }

        public AsyncDiscreteValueRange(IAsyncEnumerable<T> values)
        {
            Values = values;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return Values.GetAsyncEnumerator(cancellationToken);
        }
    }

    public abstract class PropertyValuePair
    {
        public Property Property { get; }
        public object Value { get; }

        public PropertyValuePair(Property property, object value)
        {
            Property = property;
            Value = value;
        }
    }

    public class PropertyValuePair<T> : PropertyValuePair
    {
        public PropertyValuePair(Property<T> property, Task<T> value) : base(property, value) { }
        public PropertyValuePair(Property<T> property, Task<IEnumerable<T>> value) : base(property, value)
        {

        }
    }

    public class PropertyEventArgs : EventArgs
    {
        public Property Property { get; }

        public PropertyEventArgs(Property property)
        {
            Property = property;
        }
    }

    public class PropertyDictionary : IReadOnlyCollection<PropertyValuePair>
    {
        public event EventHandler<PropertyEventArgs> PropertyAdded;

        public int Count => Properties.Count;
        public Task<object> this[Property property] => GetSingle<object>(property);

        private Dictionary<Property, IList<object>> Properties = new Dictionary<Property, IList<object>>();

        public bool TryGetValue(Property key, out Task<object> value)
        {
            value = GetSingle<object>(key);
            return true;
        }

        public Task<IEnumerable<T>> GetMultiple<T>(Property<T> key, string source = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetSingle<T>(Property<T> key, string source = null) => GetSingle<T>((Property)key, source);
        private Task<T> GetSingle<T>(Property key, string source = null)
        {
            if (!Properties.TryGetValue(key, out var values))
            {
                AddProperty(key);
                return GetSingle<T>(key);
            }

            foreach (var value in values)
            {
                if (value is Task<T> task)
                {
                    return task;
                }
                else if (value is Task task1)
                {
                    try
                    {
                        return CastTask<T>(task1);
                    }
                    catch { }
                }
            }

            return Task.FromResult<T>(default);
        }

        private async Task<T> CastTask<T>(Task task) => (T)(await (dynamic)task);

        public bool AddProperty<T>(Property<T> property) => AddProperty((Property)property);
        private bool AddProperty(Property property)
        {
            if (Properties.ContainsKey(property))
            {
                return false;
            }

            Properties.Add(property, new List<object>());
            PropertyAdded?.Invoke(this, new PropertyEventArgs(property));
            return true;
        }

        public void Add<T>(Property<T> key, Task<T> value)
        {
            Add(key, value);
        }

        public void Add(PropertyValuePair pair)
        {
            Add(pair.Property, pair.Value);
        }

        private void Add(Property property, object value)
        {
            if (!Properties.TryGetValue(property, out var values))
            {
                Properties.Add(property, values = new List<object>());
            }

            values.Add(value);
        }

        public IEnumerator<PropertyValuePair> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ItemEventArgs : EventArgs
    {
        public PropertyDictionary Properties { get; }

        public ItemEventArgs(PropertyDictionary properties)
        {
            Properties = properties;
        }
    }

    public class DataService
    {
        public event EventHandler<ItemEventArgs> GetItemDetails;

        private Dictionary<ItemType, Dictionary<Item, PropertyDictionary>> Cache = new Dictionary<ItemType, Dictionary<Item, PropertyDictionary>>();

        //public Task<object> GetValue(Item item, Property property) => GetDetails(item).TryGetValue(property, out var value) ? value : Task.FromResult<object>(null);

        public Task<T> GetValue<T>(Item item, Property<T> property) => GetDetails(item).GetSingle(property);

        public PropertyDictionary GetDetails(Item item)
        {
            if (!Cache.TryGetValue(item.ItemType, out var items))
            {
                Cache.Add(item.ItemType, items = new Dictionary<Item, PropertyDictionary>());
            }

            if (!items.TryGetValue(item, out var properties))
            {
                items.Add(item, properties = new PropertyDictionary());
                var e = new ItemEventArgs(properties);
                GetItemDetails?.Invoke(item, e);
            }

            return properties;
        }
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
