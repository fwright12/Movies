using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class UiiDictionaryDatastore : IEventAsyncCache<ResourceRequestArgs<Uri>>, IDataStore<Uri, State>
    {
        public int Count => Datastore.Count;

        public DictionaryDatastore Datastore { get; } = new DictionaryDatastore();

        public Task<bool> Read(IEnumerable<ResourceRequestArgs<Uri>> args) => Datastore.Read(args);

        public Task<bool> Write(IEnumerable<ResourceRequestArgs<Uri>> args) => Datastore.Write(args.Where(arg => arg.Request.Key is UniformItemIdentifier));

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

    public class DictionaryDatastore : IEventAsyncCache<ResourceRequestArgs<Uri>>, IDataStore<Uri, State>
    {
        public int Count => Cache.Count;

        private Dictionary<Uri, Task<State>> Cache { get; } = new Dictionary<Uri, Task<State>>();

        public async Task<bool> Read(IEnumerable<ResourceRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Read))).All(result => result);

        public async Task<bool> Write(IEnumerable<ResourceRequestArgs<Uri>> args) => (await Task.WhenAll(args.Where(arg => arg.IsHandled).Select(Write))).All(result => result);

        public async Task<bool> Read(ResourceRequestArgs<Uri> arg) => Cache.TryGetValue(arg.Request.Key, out var value) ? arg.Handle(new RestResponse(await value)) : false;

        public Task<bool> Write(ResourceRequestArgs<Uri> arg) => UpdateAsync(arg.Request.Key, (arg.Response as RestResponse)?.Resource.Get() as State ?? State.Create(arg.Value));

        public Task<bool> CreateAsync(Uri key, State value) => UpdateAsync(key, value);

        public Task<State> ReadAsync(Uri key) => Cache.TryGetValue(key, out var value) ? value : null;

        public bool Update(Uri key, Task<State> updatedValue)
        {
            Cache[key] = updatedValue;
            return true;
        }

        public Task<bool> UpdateAsync(Uri key, State updatedValue)
        {
            Cache[key] = Task.FromResult(updatedValue);
            return Task.FromResult(true);
        }

        public Task<State> DeleteAsync(Uri key) => Cache.Remove(key, out var value) ? value : Task.FromResult<State>(null);

        public void Clear()
        {
            Cache.Clear();
        }
    }
}
