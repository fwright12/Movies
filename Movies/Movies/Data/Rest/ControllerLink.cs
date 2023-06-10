using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public abstract class ControllerLink : ControllerLink<object>
    {
        public override bool TryConvert(object value, Type type, out object converted)
        {
            converted = value;
            return type.IsAssignableFrom(value.GetType());
        }
    }

    public interface IControllerLink
    {
        //void Handle(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);

        //void Get(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);
        Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);
        Task PutAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);
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
                var type = converter is HttpResourceCollectionConverter ? typeof(IDictionary<Uri, State>) : converted.GetType();
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

    public class CacheAsideLink : ChainLinkAsync<MultiRestEventArgs>
    {
        public CacheAsideLink(IDataStore<Uri, State> datastore, AsyncChainLinkEventHandler<MultiRestEventArgs> get = null, AsyncChainLinkEventHandler<MultiRestEventArgs> set = null, ChainLink<MultiRestEventArgs> next = null) : this(get ?? new DatastoreReadHandler(datastore).HandleAsync, set ?? new DatastoreCreateHandler(datastore).HandleAsync, next) { }
        public CacheAsideLink(AsyncChainLinkEventHandler<MultiRestEventArgs> get, AsyncChainLinkEventHandler<MultiRestEventArgs> set, ChainLink<MultiRestEventArgs> next = null) : base(new CacheAside(get, set).GetAsync, next) { }

        private class CacheAside
        {
            public AsyncChainLinkEventHandler<MultiRestEventArgs> Get { get; }
            public AsyncChainLinkEventHandler<MultiRestEventArgs> Set { get; }

            public CacheAside(AsyncChainLinkEventHandler<MultiRestEventArgs> get, AsyncChainLinkEventHandler<MultiRestEventArgs> set)
            {
                Get = get;
                Set = set;
            }

            public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                await Get(e);

                if (!e.Handled && next != null)
                {
                    var unhandled = e.Unhandled.ToList();
                    await next.InvokeAsync(e);

                    var handledByNext = unhandled.Where(arg => arg.Handled).ToArray();
                    var put = handledByNext
                        .Select(arg => new RestRequestArgs(arg.Uri, arg.Response))
                        .ToList();

                    var extra = new Dictionary<Uri, State>(handledByNext
                        .Select(arg => arg.Response.TryGetRepresentation<IDictionary<Uri, State>>(out var extra) ? extra : null)
                        .Where(values => values != null)
                        .SelectMany());

                    foreach (var kvp in e.GetAdditionalState())//.Where(kvp => kvp.Key is UniformItemIdentifier == false))
                    {
                        //kvp.Value.Add(arg.Expected, this);
                        put.Add(new RestRequestArgs(kvp.Key, kvp.Value));
                    }

                    await Set(new MultiRestEventArgs(put));
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

    public abstract class ControllerLink<T> : IControllerLink, IConverter<T>
    {
        public bool CacheAside { get; set; } = true;

        public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            await GetInternalAsync(e, null);
            AddRepresentations(e.Unhandled.ToArray());

            if (next != null)
            {
                //var unhandled = e.Args.Where(Unhandled).ToArray();
                //var nextE = new MultiRestEventArgs(unhandled);
                //Task.WhenAll(e.Args.Select(arg => arg.HandledAsync).Prepend(e.HandledAsync));
                await next.InvokeAsync(e);

                //e.Handle(w);
            }
            //await Task.WhenAll(e.Args.Select(arg => arg.HandledAsync).Prepend(e.HandledAsync));

            if (CacheAside)
            {
                //var handled = e.Args.Where(arg => arg.Handled).ToArray();

                foreach (var arg in e.AllArgs)
                {
                    if (arg.Response != null)
                    {
                        await PutAsync(new RestRequestArgs(arg.Uri, arg.Response).AsEnumerable(), null);
                    }
                }

                foreach (var kvp in e.GetAdditionalState().Where(kvp => kvp.Key is UniformItemIdentifier == false))
                {
                    //kvp.Value.Add(arg.Expected, this);
                    await PutAsync(new RestRequestArgs(kvp.Key, kvp.Value).AsEnumerable(), null);
                }

                //await HandleAsync(posts, null);
            }
        }

        public Task PutAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => PutAsync(e.Unhandled, next);

        protected virtual Task GetInternalAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e.Unhandled, next);

        protected virtual Task PutAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        public abstract bool TryConvert(T value, Type targetType, out object converted);

        private void AddRepresentations(IEnumerable<RestRequestArgs> args)
        {
            foreach (var arg in args.Where(arg => arg.Expected != null))
            {
                arg.Handle((IConverter<T>)this);
            }
        }

        protected Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            next?.Invoke(new MultiRestEventArgs(e.Where(Unhandled).ToArray()));// ?? Task.CompletedTask;
            return Task.WhenAll(e.Select(arg => arg.RequestedSuspension));
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<IEnumerable<RestRequestArgs>, Task> handler)
        {
            await handler(e);
            await HandleAsync(e.Where(Unhandled).ToArray(), next);
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, ChainLinkEventHandler<RestRequestArgs>, Task> handler)
        {
            //Task sendNext(RestRequestArgs e) => next(new MultiRestEventArgs(e));
            void sendNext(RestRequestArgs e) { }
            await Task.WhenAll(e.Select(arg => handler(arg, sendNext)));
            await HandleAsync(e.Where(Unhandled).ToArray(), next);
        }
        protected void Handle(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, Task<T>> handler)
        {
            foreach (var arg in e)
            {
                arg.Handle(handler?.Invoke(arg));
            }
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, Task> handler)
        {
            await Task.WhenAll(e.Select(handler));
            await HandleAsync(e.Where(Unhandled).ToArray(), next);
        }

        //protected IEnumerable<RestRequestArgs> WhereUnhandled(IEnumerable<RestRequestArgs> e) => e.Where(NotHandled);

        protected bool Unhandled(RestRequestArgs arg) => !arg.Handled;
    }
}