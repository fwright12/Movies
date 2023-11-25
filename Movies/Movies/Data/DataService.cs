using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
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

    public partial class DataService
    {
        public static readonly DataService Instance = new DataService();

        public ChainLink<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> Controller { get; }
        public UiiDictionaryDatastore ResourceCache { get; }
        public const int BATCH_TIMEOUT = 5000;

        public event EventHandler BatchBegan;
        public event EventHandler BatchCommitted;

        public Task Batch => BatchSource?.Task ?? Task.CompletedTask;
        private TaskCompletionSource<bool> BatchSource;

        public bool Batched { get; private set; }

        private DataService()
        {
            ResourceCache = new UiiDictionaryDatastore();
            Controller = new AsyncCacheAsideProcessor<ResourceReadArgs<Uri>>(new ResourceBufferedCache<Uri>(ResourceCache)).ToChainLink();
        }

        public void BatchBegin()
        {
            Batched = true;
            BatchBegan?.Invoke(this, EventArgs.Empty);
            BatchSource = new TaskCompletionSource<bool>();
            TimeoutBatch();
        }

        public void BatchEnd()
        {
            Batched = false;
            BatchCommitted?.Invoke(this, EventArgs.Empty);
            BatchSource?.TrySetResult(true);
        }

        private async void TimeoutBatch()
        {
            await Task.Delay(BATCH_TIMEOUT);
            BatchEnd();
        }

        //public Task<object> GetValue(Item item, Property property) => GetDetails(item).TryGetValue(property, out var value) ? value : Task.FromResult<object>(null);
    }
}
