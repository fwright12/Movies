using Movies;
using System.Collections.Concurrent;

namespace MoviesTests
{
    public class DummyDatastore<TKey> : ConcurrentDictionary<TKey, ResourceResponse>, IAsyncEventProcessor<KeyValueRequestArgs<TKey>>, IEventAsyncCache<KeyValueRequestArgs<TKey>>
        where TKey : Uri
    {
        public int ReadLatency { get; set; }
        public int WriteLatency { get; set; }

        public async Task<bool> Read(IEnumerable<KeyValueRequestArgs<TKey>> args) => (await Task.WhenAll(args.Select(ProcessAsync))).All(result => result);

        public async Task<bool> Write(IEnumerable<KeyValueRequestArgs<TKey>> args) => (await Task.WhenAll(args.Select(Write))).All(result => result);
        public async Task<bool> Write(KeyValueRequestArgs<TKey> args)
        {
            if (!args.IsHandled)// && (args.Response as HttpResponse)?.StatusCode != System.Net.HttpStatusCode.NotModified)
            {
                return false;
            }

            await WriteDelay();
            return TryAdd(args.Request.Key, args.Response as ResourceResponse);
        }

        public async Task<bool> ProcessAsync(KeyValueRequestArgs<TKey> e)
        {
            await ReadDelay();
            
            if (TryGetValue(e.Request.Key, out var resourceResponse))
            {
                var response = resourceResponse as RestResponse ?? RestResponse.Create(e.Request.Expected, State.Create(resourceResponse.TryGetRepresentation<object>(out var value1) ? value1 : null));
                //response = RestResponse.Create(e.Request.Expected, response.Resource, response.ControlData, response.Metadata);
                return response == null ? false : e.Handle(response);
                //return e.Handle(resourceResponse as RestResponse ?? new RestResponse(State.Create(resourceResponse.TryGetRepresentation<object>(out var value1) ? value1 : null), e.Request.Expected));

                var restResponse = resourceResponse as RestResponse;

                if (restResponse == null)
                {
                    object value;

                    if (e.Request.Expected == null)
                    {
                        value = resourceResponse.Value;

                        if (value == null)
                        {
                            return false;
                        }
                    }
                    else if (!resourceResponse.TryGetRepresentation(e.Request.Expected, out value))
                    {
                        value = null;
                    }

                    var resource = value == null ? State.Null(e.Request.Expected) : State.Create(value);
                    restResponse = RestResponse.Create(e.Request.Expected, resource);
                    //restResponse = new RestResponse(resource, e.Request.Expected);
                }

                return e.Handle(restResponse);
            }
            else
            {
                return false;
            }
        }

        public Task<bool> CreateAsync(TKey key, State value) => UpdateAsync(key, value);

        public async Task<State> DeleteAsync(TKey key)
        {
            await WriteDelay();
            return TryRemove(key, out var value) ? State.Create(value) : null;
        }

        //public async Task<State> ReadAsync(TKey key)
        //{
        //    await Delay();
        //    return TryGetValue(key, out var value) ? value.Entities as State : null;
        //}

        public async Task<bool> UpdateAsync(TKey key, State updatedValue)
        {
            await WriteDelay();

            var response = RestResponse.Create(null, updatedValue);
            if (response == null)
            {
                return false;
            }

            TryAdd(key, response);
            return true;
        }

        private Task ReadDelay() => Task.Delay(ReadLatency);
        private Task WriteDelay() => Task.Delay(WriteLatency);
    }

    public class DummyCache : Dictionary<string, JsonResponse>, IJsonCache, IAsyncDataStore<Uri, State>
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
