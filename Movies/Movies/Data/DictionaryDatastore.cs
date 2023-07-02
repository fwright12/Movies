using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class UiiDictionaryDatastore : IDataStore<Uri, State>
    {
        public int Count => Datastore.Count;

        public DictionaryDatastore Datastore { get; } = new DictionaryDatastore();

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

    public class DictionaryDatastore : IDataStore<Uri, State>
    {
        public int Count => Cache.Count;

        private Dictionary<Uri, Task<State>> Cache { get; } = new Dictionary<Uri, Task<State>>();

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
