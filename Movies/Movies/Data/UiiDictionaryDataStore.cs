using REpresentationalStateTransfer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class UiiDictionaryDataStore : IEventAsyncCache<ResourceRequestArgs<Uri>>, IAsyncDataStore<Uri, State>
    {
        public int Count => Datastore.Count;

        public DictionaryDataStore<Uri, State> Datastore { get; } = new DictionaryDataStore<Uri, State>();

        public async Task<bool> Read(IEnumerable<ResourceRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Read))).All(result => result);
        public async Task<bool> Read(ResourceRequestArgs<Uri> arg) => Datastore.TryGetValue(arg.Request.Key, out var value) ? arg.Handle(new RestResponse(await value, arg.Request.Expected)) : false;

        public async Task<bool> Write(IEnumerable<ResourceRequestArgs<Uri>> args) => (await Task.WhenAll(args.Where(arg => arg.IsHandled && arg.Request.Key is UniformItemIdentifier).Select(Write))).All(result => result);
        public Task<bool> Write(ResourceRequestArgs<Uri> arg) => UpdateAsync(arg.Request.Key, (arg.Response as RestResponse)?.Resource.Get() as State ?? State.Create(arg.Value));

        public Task<bool> CreateAsync(Uri key, State value) => UpdateAsync(key, value);
        public async Task<bool> CreateAsync(Uri key, Task<State> value) => await UpdateAsync(key, await value);

        public Task<State> DeleteAsync(Uri key) => Datastore.DeleteAsync(key);

        public Task<State> ReadAsync(Uri key) => Datastore.ReadAsync(key);

        public Task<bool> UpdateAsync(Uri key, State updatedValue) => key is UniformItemIdentifier ? Datastore.UpdateAsync(key, updatedValue) : Task.FromResult(false);

        public void Clear()
        {
            Datastore.Clear();
        }
    }

    public class DictionaryDataStore<TKey, TValue> : IAsyncDataStore<TKey, TValue>
    {
        public int Count => Cache.Count;

        private Dictionary<TKey, Task<TValue>> Cache { get; } = new Dictionary<TKey, Task<TValue>>();

        public bool TryGetValue(TKey key, out Task<TValue> value) => Cache.TryGetValue(key, out value);

        public Task<bool> CreateAsync(TKey key, TValue value) => UpdateAsync(key, value);

        public Task<TValue> ReadAsync(TKey key) => Cache.TryGetValue(key, out var value) ? value : null;

        public bool Update(TKey key, Task<TValue> updatedValue)
        {
            Cache[key] = updatedValue;
            return true;
        }

        public Task<bool> UpdateAsync(TKey key, TValue updatedValue)
        {
            Cache[key] = Task.FromResult(updatedValue);
            return Task.FromResult(true);
        }

        public Task<TValue> DeleteAsync(TKey key) => Cache.Remove(key, out var value) ? value : Task.FromResult<TValue>(default);

        public void Clear()
        {
            Cache.Clear();
        }
    }
}
