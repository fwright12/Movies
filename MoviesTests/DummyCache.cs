namespace MoviesTests
{
    public class DummyCache : Dictionary<string, JsonResponse>, IJsonCache, IDataStore<Uri, State>
    {
        public DummyCache() : base() { }

        public DummyCache(IEnumerable<KeyValuePair<string, JsonResponse>> collection) : base(collection) { }

        public Task AddAsync(string url, JsonResponse response)
        {
            TryAdd(url, response);
            return Task.CompletedTask;
        }

        public Task<bool> CreateAsync(Uri key, State value) => UpdateAsync(key, value);

        public async Task<State> DeleteAsync(Uri key)
        {
            await Expire(key.ToString());
            return null;
        }

        public Task<bool> Expire(string url) => Task.FromResult(Remove(url));

        public IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => AsyncEnumerable.ToAsyncEnumerable(Task.FromResult<IEnumerable<KeyValuePair<string, JsonResponse>>>(this)).GetAsyncEnumerator();

        public Task<bool> IsCached(string url) => Task.FromResult(ContainsKey(url));

        public async Task<State> ReadAsync(Uri key)
        {
            var response = await TryGetValueAsync(key.ToString());

            if (response == null)
            {
                return null;
            }

            return State.Create(await response.Content.ReadAsByteArrayAsync());
        }

        public Task<JsonResponse> TryGetValueAsync(string url) => Task.FromResult(TryGetValue(url, out var value) ? value : null);

        public async Task<bool> UpdateAsync(Uri key, State updatedValue)
        {
            if (updatedValue.TryGetRepresentation<IEnumerable<byte>>(out var bytes))
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
            return Task.CompletedTask;
        }
    }
}
