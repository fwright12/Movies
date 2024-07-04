using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class UiiDictionaryDataStore : IEventAsyncCache<KeyValueRequestArgs<Uri>>, IAsyncDataStore<Uri, State>
    {
        public int Count => Datastore.Count;

        public DictionaryDataStore Datastore { get; } = new DictionaryDataStore();

        public Task<bool> Read(IEnumerable<KeyValueRequestArgs<Uri>> args) => Datastore.Read(args);

        public Task<bool> Write(IEnumerable<KeyValueRequestArgs<Uri>> args) => Datastore.Write(args.Where(arg => arg.IsHandled && (arg.Request.Key is UniformItemIdentifier || arg.Request.Key.ToString().Contains("/external_ids"))));

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

    public class DictionaryDataStore : IEventAsyncCache<KeyValueRequestArgs<Uri>>, IAsyncDataStore<Uri, State>
    {
        public int Count => Cache.Count;

        private Dictionary<Uri, Task<State>> Cache { get; } = new Dictionary<Uri, Task<State>>();

        public async Task<bool> Read(IEnumerable<KeyValueRequestArgs<Uri>> args) => (await Task.WhenAll(args.Select(Read))).All(result => result);

        public async Task<bool> Write(IEnumerable<KeyValueRequestArgs<Uri>> args) => (await Task.WhenAll(args.Where(arg => arg.IsHandled).Select(Write))).All(result => result);

        public async Task<bool> Read(KeyValueRequestArgs<Uri> arg)
        {
            if (Cache.TryGetValue(arg.Request.Key, out var value))
            {
                var state = await value;
                var response = RestResponse.Create(arg.Request.Expected, state);

                if (response != null && arg.Handle(response))
                {
                    return true;
                }
                else if (arg.Request.Key is UniformItemIdentifier uii && uii.Property == Models.Movie.PARENT_COLLECTION)
                {    
                    if (state.TryGetValue<IEnumerable<byte>>(out var bytes) && TMDB.COLLECTION_PARSER.GetPair(bytes.ToArray())?.Value is int id && id != -1)
                    {
                        var collection = await TMDB.GetCollection(id);
                        state = new State(new ObjectRepresentation<IEnumerable<byte>>(bytes), new ObjectRepresentation<Collection>(collection));

                        response = RestResponse.Create(arg.Request.Expected, state);
                        if (response != null)
                        {
                            Cache[arg.Request.Key] = Task.FromResult(state);
                            return arg.Handle(response);
                        }
                    }
                }
            }

            return false;
        }

        public Task<bool> Write(KeyValueRequestArgs<Uri> arg) => UpdateAsync(arg.Request.Key, (arg.Response as RestResponse)?.Resource.Get() as State ?? State.Create(arg.Value));

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
