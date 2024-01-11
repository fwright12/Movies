using Movies;
using System.Collections.Concurrent;

namespace MoviesTests
{
    public class DummyDatastore<TKey> : ConcurrentDictionary<TKey, ResourceResponse>, IAsyncEventProcessor<ResourceReadArgs<TKey>>, IEventAsyncCache<ResourceReadArgs<TKey>>
    {
        public int SimulatedDelay { get; set; }

        public async Task<bool> Read(IEnumerable<ResourceReadArgs<TKey>> args) => (await Task.WhenAll(args.Select(ProcessAsync))).All(result => result);

        public async Task<bool> Write(IEnumerable<ResourceReadArgs<TKey>> args) => (await Task.WhenAll(args.Select(Write))).All(result => result);
        public async Task<bool> Write(ResourceReadArgs<TKey> args)
        {
            await Delay();
            return TryAdd(args.Key, new ResourceResponse<object>(args.Value));
        }

        public async Task<bool> ProcessAsync(ResourceReadArgs<TKey> e)
        {
            await Delay();
            if (TryGetValue(e.Key, out var response))
            {
                return e.Handle(response as RestResponse ?? new RestResponse(State.Create(response.TryGetRepresentation<object>(out var value) ? value : null)));
            }
            else
            {
                return false;
            }
        }

        public Task<bool> CreateAsync(TKey key, State value) => UpdateAsync(key, value);

        public async Task<State> DeleteAsync(TKey key)
        {
            await Delay();
            return TryRemove(key, out var value) ? State.Create(value) : null;
        }

        //public async Task<State> ReadAsync(TKey key)
        //{
        //    await Delay();
        //    return TryGetValue(key, out var value) ? value.Entities as State : null;
        //}

        public async Task<bool> UpdateAsync(TKey key, State updatedValue)
        {
            await Delay();
            TryAdd(key, new RestResponse(updatedValue));
            return true;
        }

        private Task Delay() => Task.Delay(SimulatedDelay);
    }

    public class DummyCache : Dictionary<string, JsonResponse>, IJsonCache, IDataStore<Uri, State>
    {
        public int SimulatedDelay { get; set; }

        public DummyCache() : base() { }

        public DummyCache(IEnumerable<KeyValuePair<string, JsonResponse>> collection) : base(collection) { }

        public Task AddAsync(string url, JsonResponse response)
        {
            TryAdd(url, response);
            return Delay();
        }

        public Task<bool> CreateAsync(Uri key, State value) => UpdateAsync(key, value);

        public async Task<State> DeleteAsync(Uri key)
        {
            await Expire(key.ToString());
            return null;
        }

        public async Task<bool> Expire(string url)
        {
            await Delay();
            return Remove(url);
        }

        public IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => AsyncEnumerable.ToAsyncEnumerable(Task.FromResult<IEnumerable<KeyValuePair<string, JsonResponse>>>(this)).GetAsyncEnumerator();

        public async Task<bool> IsCached(string url)
        {
            await Delay();
            return ContainsKey(url);
        }

        public async Task<State> ReadAsync(Uri key)
        {
            var response = await TryGetValueAsync(key.ToString());

            if (response == null)
            {
                return null;
            }

            return State.Create(new ArraySegment<byte>(await response.Content.ReadAsByteArrayAsync()));
        }

        public async Task<JsonResponse> TryGetValueAsync(string url)
        {
            await Delay();
            return TryGetValue(url, out var value) ? value : null;
        }

        public async Task<bool> UpdateAsync(Uri key, State updatedValue)
        {
            if (updatedValue.TryGetValue<IEnumerable<byte>>(out var bytes))
            {
                await AddAsync(key.ToString(), new JsonResponse(bytes.ToArray()));
                return true;
            }
            else
            {
                return false;
            }
        }

        Task IJsonCache.Clear()
        {
            Clear();
            return Delay();
        }

        private Task Delay() => Task.Delay(SimulatedDelay);
    }
}
