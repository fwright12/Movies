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
                await Handlers.HandleGet(e);

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
                MultiRestEventArgs eNext;

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

                    eNext = new MultiRestEventArgs(unhandled);
                    handling = next.InvokeAsync(eNext);

                    foreach (var arg in unhandled)
                    {
                        Cache.Add(arg.Uri, GetResponseAsync(handling, arg));
                    }
                }

                await Task.WhenAll(cached.Prepend(handling));
                e.HandleMany(eNext.GetAdditionalState());

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

                    lock (CacheLock)
                    {
                        Cache.Add(kvp.Key, Task.FromResult(kvp.Value));
                    }
                }

                var set = Handlers.HandleSet(new MultiRestEventArgs(put));
                UpdateCacheOnWriteComplete(set, put);
            }

            private async void UpdateCacheOnWriteComplete(Task writing, IEnumerable<RestRequestArgs> args)
            {
                await writing;

                lock (CacheLock)
                {
                    foreach (var arg in args)
                    {
                        Cache.Remove(arg.Uri);
                    }
                }
            }

            private async Task GetAsync1(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                await Handlers.HandleGet(e);

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
                MultiRestEventArgs eNext;

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

                    eNext = new MultiRestEventArgs(unhandled);
                    handling = next.InvokeAsync(eNext);

                    foreach (var arg in unhandled)
                    {
                        Cache.Add(arg.Uri, GetResponseAsync(handling, arg));
                    }
                    foreach (var kvp in eNext.GetAdditionalState())
                    {
                        //Cache.TryAdd(kvp.Key, kvp.Value);
                    }
                }

                await Task.WhenAll(cached.Prepend(handling));
                e.HandleMany(eNext.GetAdditionalState());

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
                    //put.Add(kvp.Key, kvp.Value);

                    lock (CacheLock)
                    {
                        //Cache.Add(kvp.Key, Task.FromResult(kvp.Value));
                        //Cache.TryAdd(kvp.Key, kvp.Value);
                    }
                }

                //var set = Handlers.HandleSet(new MultiRestEventArgs(put));
                //UpdateCacheOnWriteComplete(put);
            }

            private static async Task<State> GetResponseAsync(Task task, RestRequestArgs e)
            {
                await task;
                return e.Response;
            }
        }
    }
}