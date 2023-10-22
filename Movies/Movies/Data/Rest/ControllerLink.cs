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
        public static async Task<RestRequestArgs> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> chain, string url) => (await Get(chain, new string[] { url }))[0];
        public static Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> chain, params string[] urls) => Get(chain, urls.Select(url => new Uri(url, UriKind.Relative)));
        public static async Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> chain, IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(chain, args);
            return args;
        }

        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> chain, params DatastoreKeyValueReadArgs<Uri>[] args) => Get(chain, (IEnumerable<DatastoreKeyValueReadArgs<Uri>>)args);
        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> chain, IEnumerable<DatastoreKeyValueReadArgs<Uri>> args)
        {
            var e = new BatchDatastoreArgs<DatastoreKeyValueReadArgs<Uri>>(args);
            return chain.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(e));
        }

        public static async Task<DatastoreKeyValueReadArgs<Uri, T>> TryGet<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> processor, Uri uri)
        {
            var e = new DatastoreKeyValueReadArgs<Uri, T>(uri);
            await processor.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(new BatchDatastoreArgs<DatastoreKeyValueReadArgs<Uri>>(e)));
            return e;
        }

        public static DatastoreKeyValueReadArgs<Uri, T> Create<T>(this Property<T> property, Movies.Models.Item item) => new DatastoreKeyValueReadArgs<Uri, T>(new UniformItemIdentifier(item, property));
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

    public class DatastoreProcessor : IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>>, IAsyncEventProcessor<DatastoreWriteArgs>
    {
        public IDataStore<Uri, State> Datastore { get; }

        public DatastoreProcessor(IDataStore<Uri, State> datastore)
        {
            Datastore = datastore;
        }

        public virtual async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<Uri> e)
        {
            var state = Datastore.ReadAsync(e.Key);

            if (state != null)
            {
                return e.Handle(new RestResponse(await state)
                {
                    Expected = e.Expected
                });
            }
            else
            {
                return false;
            }
        }

        public virtual Task<bool> ProcessAsync(DatastoreWriteArgs e)
        {
            if (e is DatastoreKeyValueWriteArgs<Uri, Resource> keyValueArgs)
            {
                return Datastore.CreateAsync(keyValueArgs.Key, keyValueArgs.Value() as State);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}