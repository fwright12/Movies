using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class RestChainLink : ChainLink<MultiRestEventArgs>
    {
        public RestChainLink(ChainEventHandler<MultiRestEventArgs> handler, ChainLink<MultiRestEventArgs> next = null) : base(handler, next) { }
    }

    public static class RestChainExtensions
    {
        public static async Task<(bool Success, T Resource)> TryGet<T>(this ChainLink<MultiRestEventArgs> chain, string url)
        {
            var args = new RestRequestArgs(new Uri(url, UriKind.Relative));
            var e = new MultiRestEventArgs(args);
            chain.Handle(e);
            await e.RequestedSuspension;

            if (args.Handled && args.Response.TryGetRepresentation<T>(out var value))
            {
                return (true, value);
            }
            else
            {
                return (false, default);
            }
        }

        public static async Task<RestRequestArgs> Get(this ChainLink<MultiRestEventArgs> chain, string url) => (await Get(chain, new string[] { url }))[0];
        public static Task<RestRequestArgs[]> Get(this ChainLink<MultiRestEventArgs> chain, params string[] urls) => Get(chain, urls.Select(url => new Uri(url, UriKind.Relative)));
        public static Task<RestRequestArgs[]> Get(this ChainLink<MultiRestEventArgs> chain, params Uri[] uris) => Get(chain, (IEnumerable<Uri>)uris);
        public static async Task<RestRequestArgs[]> Get(this ChainLink<MultiRestEventArgs> chain, IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(chain, args);
            return args;
        }

        public static async Task<(bool Success, T Resource)> Get<T>(this ChainLink<MultiRestEventArgs> chain, Uri uri)
        {
            var args = new RestRequestArgs<T>(uri);
            await Get(chain, args);

            if (args.Handled && args.Response.TryGetRepresentation<T>(out var value))
            {
                return (true, value);
            }
            else
            {
                return (false, default);
            }
        }

        public static Task Get(this ChainLink<MultiRestEventArgs> chain, params RestRequestArgs[] args) => Get(chain, (IEnumerable<RestRequestArgs>)args);
        public static Task Get(this ChainLink<MultiRestEventArgs> chain, IEnumerable<RestRequestArgs> args1)
        {
            var args = args1.ToArray();
            var e = new MultiRestEventArgs(args);
            chain.Handle(e);
            return e.RequestedSuspension;
            //return Task.WhenAll(e.Args.Select(arg => arg.RequestedSuspension).Prepend(e.RequestedSuspension));
        }
    }

    public class DataArgs<TKey, TValue> : AsyncChainEventArgs
    {
        public TKey Key { get; set; }
        public AsyncData<TValue> Data { get; set; }
    }

    public class AsyncData<T>
    {
        public bool Success { get; set; }
        public T Value { get; set; }
    }

    public interface IDataStore<TKey, TValue>
    {
        Task<bool> CreateAsync(TKey key, TValue value);
        Task<TValue> ReadAsync(TKey key);
        Task<bool> UpdateAsync(TKey key, TValue updatedValue);
        Task<TValue> DeleteAsync(TKey key);
    }

    public class ReadThroughCache<TKey, TValue> : ReadThroughCache<IDataStore<TKey, TValue>, TKey, TValue> { }

    public abstract class ReadThroughCache<TDatastore, TKey, TValue> : IDataStore<TKey, TValue> where TDatastore : IDataStore<TKey, TValue>
    {
        public TDatastore Self { get; }
        public TDatastore Next { get; }

        public virtual Task<bool> CreateAsync(TKey key, TValue value) => Self.CreateAsync(key, value);

        public virtual Task<TValue> DeleteAsync(TKey key) => Self.DeleteAsync(key);

        public virtual async Task<TValue> ReadAsync(TKey key)
        {
            var value = await Self.ReadAsync(key);

            if (Equals(value, default(TValue)))
            {
                value = await Next.ReadAsync(key);
                await Self.CreateAsync(key, value);
            }

            return value;
        }

        public virtual Task<bool> UpdateAsync(TKey key, TValue updatedValue) => Self.UpdateAsync(key, updatedValue);
    }

    public class StateConverter<T>
    {
        public virtual bool TryConvert(T t, out State converted)
        {
            converted = State.Create(t);
            return true;
        }

        public virtual bool TryConvert(State state, out T converted) => state.TryGetRepresentation<T>(out converted);
    }

    public class StateDatastore<TKey, TValue> : IDataStore<TKey, State>
    {
        public IDataStore<TKey, TValue> Datastore { get; }
        public StateConverter<TValue> Converter { get; }

        public StateDatastore(IDataStore<TKey, TValue> datastore, StateConverter<TValue> converter)
        {
            Datastore = datastore;
            Converter = converter;
        }

        public Task<bool> CreateAsync(TKey key, State value) => Converter.TryConvert(value, out var converted) ? Datastore.CreateAsync(key, converted) : Task.FromResult(false);

        public async Task<State> DeleteAsync(TKey key)
        {
            var value = await Datastore.DeleteAsync(key);
            return value != null && Converter.TryConvert(value, out var converted) ? converted : null;
        }

        public async Task<State> ReadAsync(TKey key)
        {
            var value = await Datastore.ReadAsync(key);
            return value != null && Converter.TryConvert(value, out var converted) ? converted : null;
        }

        public Task<bool> UpdateAsync(TKey key, State updatedValue) => Converter.TryConvert(updatedValue, out var converted) ? Datastore.UpdateAsync(key, converted) : Task.FromResult(false);
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

        protected virtual bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
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
                var type = converter is HttpResourceCollectionConverter ? typeof(IEnumerable<KeyValuePair<Uri, object>>) : converted.GetType();
                state.Add(type, converted);
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

    public abstract class MultiRestHandler
    {
        public abstract Task HandleAsync(MultiRestEventArgs e);
    }

    public class MultiRestHandlerFunc : MultiRestHandler
    {
        public Func<MultiRestEventArgs, Task> Handler { get; }

        public MultiRestHandlerFunc(Func<MultiRestEventArgs, Task> handler)
        {
            Handler = handler;
        }

        public override Task HandleAsync(MultiRestEventArgs e) => Handler(e);
    }

    public abstract class RestHandler : MultiRestHandler
    {
        public override Task HandleAsync(MultiRestEventArgs e) => Task.WhenAll(e.Unhandled.Select(HandleAsync));

        public abstract Task HandleAsync(RestRequestArgs e);
    }

    public class RestHandlerFunc : RestHandler
    {
        public Func<RestRequestArgs, Task> Handler { get; }

        public RestHandlerFunc(Func<RestRequestArgs, Task> handler)
        {
            Handler = handler;
        }

        public override Task HandleAsync(RestRequestArgs e) => Handler(e);
    }

    public abstract class DatastoreHandler : RestHandler
    {
        public IDataStore<Uri, State> Datastore { get; }

        protected DatastoreHandler(IDataStore<Uri, State> datastore)
        {
            Datastore = datastore;
        }
    }

    public class DatastoreReadHandler : DatastoreHandler
    {
        public DatastoreReadHandler(IDataStore<Uri, State> datastore) : base(datastore) { }

        public override async Task HandleAsync(RestRequestArgs e)
        {
            var state = await Datastore.ReadAsync(e.Uri);

            if (state != null)
            {
                e.Handle(state);
            }
        }
    }

    public class DatastoreCreateHandler : DatastoreHandler
    {
        public DatastoreCreateHandler(IDataStore<Uri, State> datastore) : base(datastore) { }

        public override Task HandleAsync(RestRequestArgs e) => Datastore.CreateAsync(e.Uri, e.Body);
    }

    public delegate Task AsyncChainLinkEventHandler<TArgs>(TArgs args);

    public abstract class CacheAside<TArgs>
    {
        public abstract Task HandleGet(TArgs e);
        public abstract Task HandleSet(TArgs e);
    }

    public class CacheAsideLink : ChainLinkAsync<MultiRestEventArgs>
    {
        public CacheAsideLink(IDataStore<Uri, State> datastore, AsyncChainLinkEventHandler<MultiRestEventArgs> get = null, AsyncChainLinkEventHandler<MultiRestEventArgs> set = null, ChainLink<MultiRestEventArgs> next = null) : this(get ?? new DatastoreReadHandler(datastore).HandleAsync, set ?? new DatastoreCreateHandler(datastore).HandleAsync, next) { }
        public CacheAsideLink(AsyncChainLinkEventHandler<MultiRestEventArgs> get, AsyncChainLinkEventHandler<MultiRestEventArgs> set, ChainLink<MultiRestEventArgs> next = null) : base(new CacheAside(new CacheAsideFunc<MultiRestEventArgs>(get, set)).GetAsync, next) { }

        public class CacheAsideFunc<TArgs> : CacheAside<TArgs>
        {
            public AsyncChainLinkEventHandler<TArgs> Get { get; }
            public AsyncChainLinkEventHandler<TArgs> Set { get; }

            public CacheAsideFunc(AsyncChainLinkEventHandler<TArgs> get, AsyncChainLinkEventHandler<TArgs> set)
            {
                Get = get;
                Set = set;
            }

            public override Task HandleGet(TArgs e) => Get(e);

            public override Task HandleSet(TArgs e) => Set(e);
        }

        private class CacheAside
        {
            public CacheAside<MultiRestEventArgs> Handlers { get; }

            public CacheAside(CacheAside<MultiRestEventArgs> handlers)
            {
                Handlers = handlers;
            }

            public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                await Handlers.HandleGet(e);

                if (!e.Handled && next != null)
                {
                    var unhandled = e.Unhandled.ToList();
                    await next.InvokeAsync(e);

                    var handledByNext = unhandled.Where(arg => arg.Handled).ToArray();
                    var put = handledByNext
                        .Select(arg => new RestRequestArgs(arg.Uri, arg.Response))
                        .ToList();

                    //var extra = new Dictionary<Uri, State>(handledByNext
                    //    .Select(arg => arg.Response.TryGetRepresentation<IDictionary<Uri, State>>(out var extra) ? extra : null)
                    //    .Where(values => values != null)
                    //    .SelectMany());

                    foreach (var kvp in e.GetAdditionalState())//.Where(kvp => kvp.Key is UniformItemIdentifier == false))
                    {
                        //kvp.Value.Add(arg.Expected, this);
                        put.Add(new RestRequestArgs(kvp.Key, kvp.Value));
                    }

                    await Handlers.HandleSet(new MultiRestEventArgs(put));
                }
            }
        }
    }

    public class CacheAsideLink<TArgs> : ChainLinkAsync<TArgs> where TArgs : AsyncChainEventArgs
    {
        public CacheAsideLink(AsyncChainLinkEventHandler<TArgs> handler, ChainLink<TArgs> next = null) : base(new Wrapper(handler).GetAsync, next)
        {
        }

        private class Wrapper
        {
            public AsyncChainLinkEventHandler<TArgs> Handler { get; }

            public Wrapper(AsyncChainLinkEventHandler<TArgs> handler)
            {
                Handler = handler;
            }

            public Task GetAsync(TArgs args, ChainLinkEventHandler<TArgs> next)
            {
                return next.InvokeAsync(args);
            }
        }
    }
}