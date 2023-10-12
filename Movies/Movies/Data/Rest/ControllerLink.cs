using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public static class RestChainExtensions
    {
        public static async Task<RestRequestArgs> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, string url) => (await Get(chain, new string[] { url }))[0];
        public static Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, params string[] urls) => Get(chain, urls.Select(url => new Uri(url, UriKind.Relative)));
        public static Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, params Uri[] uris) => Get(chain, (IEnumerable<Uri>)uris);
        public static async Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(chain, args);
            return args;
        }

        public static async Task<RestRequestArgs<T>> Get<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, Uri uri)
        {
            var args = new RestRequestArgs<T>(uri);
            await Get(chain, args);
            return args;
        }

        public static async Task<(bool Success, T Resource)> TryGet<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, Uri uri)
        {
            var args = await Get<T>(chain, uri);
            return args.IsHandled && args.Response.TryGetRepresentation<T>(out var value) ? (true, value) : (false, default);
        }

        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, params RestRequestArgs[] args) => Get(chain, (IEnumerable<RestRequestArgs>)args);
        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>> chain, IEnumerable<RestRequestArgs> args)
        {
            var e = new BatchDatastoreArgs<DatastoreKeyReadArgs<Uri>>(args);
            return chain.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<DatastoreKeyReadArgs<Uri>>>(e));
        }
    }

    public interface IDataStore<TKey, TValue>
    {
        Task<bool> CreateAsync(TKey key, TValue value);
        Task<TValue> ReadAsync(TKey key);
        Task<bool> UpdateAsync(TKey key, TValue updatedValue);
        Task<TValue> DeleteAsync(TKey key);
    }

    public class HttpDatastore : IDataStore<Uri, State>
    {
        public HttpMessageInvoker Invoker { get; }

        public HttpDatastore(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        public async Task<bool> CreateAsync(Uri key, State value) => await SendAsync(HttpMethod.Post, key, value) != null;

        public Task<State> DeleteAsync(Uri key) => SendAsync(HttpMethod.Delete, key);

        public Task<State> ReadAsync(Uri key) => SendAsync(HttpMethod.Get, key);

        public async Task<bool> UpdateAsync(Uri key, State updatedValue) => await SendAsync(HttpMethod.Put, key, updatedValue) != null;

        public virtual bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
        {
            resource = default;
            return false;
        }

        private async Task<State> SendAsync(HttpMethod method, Uri uri, State body = null)
        {
            var message = new HttpRequestMessage(method, uri);
            if (body != null && TryGetHttpContent(body, out var content))
            {
                message.Content = content;
            }

            var response = await Invoker.SendAsync(message, default);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var state = State.Create(await response.Content.ReadAsByteArrayAsync());

            if (TryGetConverter(uri, out var converter))
            {
                var converted = await converter.Convert(response.Content);
                //var type = converter is HttpResourceCollectionConverter ? typeof(IEnumerable<KeyValuePair<Uri, object>>) : converted.GetType();
                state.Add(converted.GetType(), converted);
            }

            return state;
        }

        public static bool TryGetHttpContent(State state, out HttpContent content)
        {
            if (state.TryGetRepresentation<byte[]>(out var bytes))
            {
                content = new ByteArrayContent(bytes);
            }
            else if (state.TryGetRepresentation<string>(out var str))
            {
                content = new StringContent(str);
            }
            else
            {
                content = default;
                return false;
            }

            return true;
        }
    }

    public class RestCache : IAsyncEventProcessor<IEnumerable<DatastoreKeyReadArgs<Uri>>>, IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>>
    {
        public IDataStore<Uri, State> Datastore { get; }

        public RestCache(IDataStore<Uri, State> datastore)
        {
            Datastore = datastore;
        }

        public virtual async Task<bool> ProcessAsync(IEnumerable<DatastoreKeyReadArgs<Uri>> e)
        {
            var tasks = new List<Task>();

            foreach (var request in e.Where(request => !request.IsHandled).OfType<RestRequestArgs>())
            {
                tasks.Add(HandleGet(request));
            }

            await Task.WhenAll(tasks);
            return e.All(request => request.IsHandled);
        }

        public async Task<bool> ProcessAsync(IEnumerable<DatastoreWriteArgs> e)
        {
            if (e is IEnumerable<DatastoreKeyValueWriteArgs<Uri, Resource>> keyValueArgs)
            {
                var result = await Task.WhenAll(keyValueArgs.Select(request => Datastore.CreateAsync(request.Key, request.Value() as State)));
                return result.All(value => value);
            }
            else
            {
                return false;
            }
        }

        private async Task HandleGet(RestRequestArgs e)
        {
            var state = Datastore.ReadAsync(e.Uri);

            if (state != null)
            {
                var value = await state;
                e.Handle(() => value);
            }
        }
    }
}