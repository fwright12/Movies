using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class UiiDictionaryDatastore : IEventAsyncCache<ResourceReadArgs<Uri>>, IDataStore<Uri, State>
    {
        public int Count => Datastore.Count;

        public DictionaryDatastore Datastore { get; } = new DictionaryDatastore();

        public Task<bool> Read(IEnumerable<ResourceReadArgs<Uri>> args) => Datastore.Read(args);

        public Task<bool> Write(IEnumerable<ResourceReadArgs<Uri>> args) => Datastore.Write(args.Where(arg => arg.Key is UniformItemIdentifier));

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

    public class DictionaryDatastore : IEventAsyncCache<ResourceReadArgs<Uri>>, IDataStore<Uri, State>
    {
        public int Count => Cache.Count;

        private Dictionary<Uri, Task<State>> Cache { get; } = new Dictionary<Uri, Task<State>>();

        public async Task<bool> Read(IEnumerable<ResourceReadArgs<Uri>> args) => (await Task.WhenAll(args.Select(Read))).All(result => result);

        public async Task<bool> Write(IEnumerable<ResourceReadArgs<Uri>> args) => (await Task.WhenAll(args.Where(arg => arg.IsHandled).Select(Write))).All(result => result);

        public async Task<bool> Read(ResourceReadArgs<Uri> arg) => Cache.TryGetValue(arg.Key, out var value) ? arg.Handle(new RestResponse(await value) { Expected = arg.Expected }) : false;

        public Task<bool> Write(ResourceReadArgs<Uri> arg) => UpdateAsync(arg.Key, (arg.Response as RestResponse)?.Entities as State ?? State.Create(arg.Response.RawValue));

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
