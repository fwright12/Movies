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
        public static readonly DataService Instance = new DataService();

        public event EventHandler<ItemEventArgs> GetItemDetails;
        public event EventHandler BatchBegan;
        public event EventHandler BatchCommitted;

        public bool Batched { get; private set; }

        private Dictionary<Item, PropertyDictionary> Cache = new Dictionary<Item, PropertyDictionary>();

        private DataService() { }

        public void BatchBegin()
        {
            Batched = true;
            BatchBegan?.Invoke(this, EventArgs.Empty);
        }

        public void BatchEnd()
        {
            Batched = false;
            BatchCommitted?.Invoke(this, EventArgs.Empty);
        }

        //public Task<object> GetValue(Item item, Property property) => GetDetails(item).TryGetValue(property, out var value) ? value : Task.FromResult<object>(null);

        public PropertyDictionary GetDetails(Item item)
        {
            if (!Cache.TryGetValue(item, out var properties))
            {
                Cache.Add(item, properties = new PropertyDictionary());
                var e = new ItemEventArgs(properties);
                GetItemDetails?.Invoke(item, e);
            }

            return properties;
        }
    }
}
