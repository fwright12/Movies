using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
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

    public class CacheAsideLink : ChainLinkAsync<MultiRestEventArgs>
    {
        public CacheAsideLink(IDataStore<Uri, State> datastore, ChainLink<MultiRestEventArgs> next = null) : this(new RestCache(datastore), next) { }
        //public CacheAsideLink(IDataStore<Uri, State> datastore, AsyncChainLinkEventHandler<MultiRestEventArgs> get = null, AsyncChainLinkEventHandler<MultiRestEventArgs> set = null, ChainLink<MultiRestEventArgs> next = null) : this(get ?? new DatastoreReadHandler(datastore).HandleAsync, set ?? new DatastoreCreateHandler(datastore).HandleAsync, next) { }
        //public CacheAsideLink(AsyncChainLinkEventHandler<MultiRestEventArgs> get, AsyncChainLinkEventHandler<MultiRestEventArgs> set, ChainLink<MultiRestEventArgs> next = null) : base(new CacheAside(new CacheAsideFunc<MultiRestEventArgs>(get, set)).GetAsync, next) { }
        public CacheAsideLink(DatastoreCache<Uri, State, MultiRestEventArgs> handlers, ChainLink<MultiRestEventArgs> next = null) : base(new CacheAside(handlers).GetAsync, next) { }

        private class CacheAside
        {
            public CacheAside<MultiRestEventArgs> Handlers { get; }

            private readonly object CacheLock = new object();
            private Dictionary<Uri, Task<State>> Cache = new Dictionary<Uri, Task<State>>();

            public CacheAside(CacheAside<MultiRestEventArgs> handlers)
            {
                Handlers = handlers;
            }

            public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                try
                {
                    await Handlers.HandleGet(e);
                }
                catch
                {
                    return;
                }

                if (next == null)
                {
                    return;
                }

                var unhandled = e.Unhandled.ToList();

                if (unhandled.Count == 0)
                {
                    return;
                }

                Task handling;
                List<Task> cached = new List<Task>();

                lock (CacheLock)
                {
                    for (int i = 0; i < unhandled.Count; i++)
                    {
                        var arg = unhandled[i];

                        if (Cache.TryGetValue(arg.Uri, out var task))
                        {
                            cached.Add(arg.Handle(task));
                            unhandled.RemoveAt(i--);
                        }
                    }

                    handling = next.InvokeAsync(e);

                    foreach (var arg in unhandled)
                    {
                        Cache.Add(arg.Uri, GetResponseAsync(handling, arg));
                    }
                    foreach (var kvp in e.GetAdditionalState())
                    {
                        Cache.TryAdd(kvp.Key, kvp.Value);
                    }
                }

                await Task.WhenAll(cached.Prepend(handling).Select(Safe));
                //e.HandleMany(eNext.GetAdditionalState());

                var handledByNext = unhandled.Where(arg => arg.Handled).ToArray();
                var put = handledByNext
                    .ToDictionary(arg => arg.Uri, arg => Task.FromResult(arg.Response));

                //var extra = new Dictionary<Uri, State>(handledByNext
                //    .Select(arg => arg.Response.TryGetRepresentation<IDictionary<Uri, State>>(out var extra) ? extra : null)
                //    .Where(values => values != null)
                //    .SelectMany());

                foreach (var kvp in e.GetAdditionalState())//.Where(kvp => kvp.Key is UniformItemIdentifier == false))
                {
                    //kvp.Value.Add(arg.Expected, this);
                    put.TryAdd(kvp.Key, kvp.Value);

                    lock (CacheLock)
                    {
                        //Cache.Add(kvp.Key, Task.FromResult(kvp.Value));
                        Cache.TryAdd(kvp.Key, kvp.Value);
                    }
                }

                //var set = Handlers.HandleSet(new MultiRestEventArgs(put));
                foreach (var kvp in put)
                {
                    UpdateCacheOnWriteComplete(kvp.Key, kvp.Value);
                }
            }

            private async void UpdateCacheOnWriteComplete(Uri uri, Task<State> state)
            {
                State body;

                try
                {
                    body = await state;
                }
                catch
                {
                    return;
                }

                if (body == null)
                {
                    return;
                }

                var request = new RestRequestArgs(uri, body);
                await Handlers.HandleSet(new MultiRestEventArgs(request));

                lock (CacheLock)
                {
                    Cache.Remove(uri);
                }
            }

            private static async Task<State> GetResponseAsync(Task task, RestRequestArgs e)
            {
                await task;
                return e.Response;
            }

            private static async Task Safe(Task task)
            {
                try
                {
                    await task;
                }
                catch { }
            }
        }
    }
}