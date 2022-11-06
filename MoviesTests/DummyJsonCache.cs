namespace MoviesTests
{
    public class DummyJsonCache : Dictionary<string, JsonResponse>, IJsonCache
    {
        public DummyJsonCache() : base() { }

        public DummyJsonCache(IEnumerable<KeyValuePair<string, JsonResponse>> collection) : base(collection) { }

        public Task AddAsync(string url, JsonResponse response)
        {
            Add(url, response);
            return Task.CompletedTask;
        }

        public Task<bool> Expire(string url) => Task.FromResult(Remove(url));

        public IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => AsyncEnumerable.ToAsyncEnumerable(Task.FromResult<IEnumerable<KeyValuePair<string, JsonResponse>>>(this)).GetAsyncEnumerator();

        public Task<bool> IsCached(string url) => Task.FromResult(ContainsKey(url));

        public Task<JsonResponse> TryGetValueAsync(string url) => TryGetValue(url, out var value) ? Task.FromResult(value) : null;

        Task IJsonCache.Clear()
        {
            Clear();
            return Task.CompletedTask;
        }
    }
}
