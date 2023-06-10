using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using static Xamarin.Forms.UniversalSetter;

namespace Movies
{
    public class ResourceCache : IControllerLink, IDataStore<Uri, State>
    {
        public int Count => Cache.Count;

        private Dictionary<Uri, Task<State>> Cache { get; } = new Dictionary<Uri, Task<State>>();
        private readonly SemaphoreSlim CacheSemaphore = new SemaphoreSlim(1, 1);

        public Task<bool> CreateAsync(Uri key, State value) => UpdateAsync(key, value);

        public Task<State> ReadAsync(Uri key) => Cache.TryGetValue(key, out var value) ? value : Task.FromResult<State>(null);

        public Task<bool> UpdateAsync(Uri key, State updatedValue)
        {
            if (key is UniformItemIdentifier == false)
            {
                return Task.FromResult(false);
            }

            Cache[key] = Task.FromResult(updatedValue);
            return Task.FromResult(true);
        }

        Task<State> IDataStore<Uri, State>.DeleteAsync(Uri key)
        {
            if (Cache.TryGetValue(key, out var value))
            {
                Cache.Remove(key);
                return value;
            }
            else
            {
                return Task.FromResult<State>(null);
            }
        }

        public void Get(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs > next)
        {
            var cached = new List<Task<State>>();

            foreach (var arg in e.Unhandled)
            {
                //if (e.Uri is UniformItemIdentifier uii && Cache.TryGetValue(uii.Item, out var properties) && properties.TryGetValue(uii.Property, out var response))// && PropertyDictionary.TryCastTask<T>(response, out var resource) && resource.IsCompletedSuccessfully)
                if (Cache.TryGetValue(arg.Uri, out var state))
                {
                    
                    //arg.Handle(state);
                }
            }

            if (next == null)
            {
                return;
            }

            var unhandled = e.Unhandled.ToArray();
            var task = next.InvokeAsync(e);

            foreach (var arg in unhandled)
            {
                Put(arg.Uri, Unwrap(task, arg));
            }

            //_ = CacheAsideAsync(e, task, unhandled);
        }

        private static async Task<IEnumerable<KeyValuePair<Uri, State>>> GetResources(Task task, params RestRequestArgs[] args)
        {
            await task;
            return args
                .Where(arg => arg.Handled && arg.Response != null)
                .Select(arg => new KeyValuePair<Uri, State>(arg.Uri, arg.Response));
        }

        private static async Task<IEnumerable<KeyValuePair<Uri, State>>> GetAdditionalResources(Task task, MultiRestEventArgs e)
        {
            await task;
            return e.GetAdditionalState().Where(kvp => kvp.Key is UniformItemIdentifier);
        }

        public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            foreach (var arg in e.Unhandled)
            {
                //if (e.Uri is UniformItemIdentifier uii && Cache.TryGetValue(uii.Item, out var properties) && properties.TryGetValue(uii.Property, out var response))// && PropertyDictionary.TryCastTask<T>(response, out var resource) && resource.IsCompletedSuccessfully)
                await CacheSemaphore.WaitAsync();

                try
                {
                    if (Cache.TryGetValue(arg.Uri, out var state))
                    {
                        arg.Handle(await state);
                    }
                }
                finally
                {
                    CacheSemaphore.Release();
                }
            }

            if (next == null)
            {
                return;
            }

            var unhandled = e.Unhandled.ToArray();

            if (unhandled.Length == 0)
            {
                return;
            }

            //var nextE = new MultiRestEventArgs(unhandled);
            await next.InvokeAsync(e);
            var response = Task.WhenAll(e.Unhandled.Select(arg => arg.RequestedSuspension));

            for (int i = 0; i < unhandled.Length; i++)
            {
                var arg = unhandled[i];
                await PutAsync(arg.Uri, Unwrap(response, unhandled, i));
            }

            await response;
            //e.Handle(nextE);
            var put = Enumerable.Empty<KeyValuePair<Uri, State>>();

            foreach (var arg in e.AllArgs)
            {
                if (arg.Handled)
                {
                    if (arg.Response != null)
                    {
                        put = put.Append(new KeyValuePair<Uri, State>(arg.Uri, arg.Response));
                    }
                }
                else
                {
                    await CacheSemaphore.WaitAsync();

                    try
                    {
                        Cache.Remove(arg.Uri);
                    }
                    finally
                    {
                        CacheSemaphore.Release();
                    }
                }
            }

            put = put.Concat(e.GetAdditionalState());

            foreach (var kvp in put.Where(kvp => kvp.Key is UniformItemIdentifier))
            {
                await PutAsync(kvp.Key, Task.FromResult(kvp.Value));
            }
        }

        private async Task<State> Unwrap(Task response, RestRequestArgs args)
        {
            await response;
            return args.Response;
        }

        private async Task<State> Unwrap(Task response, RestRequestArgs[] args, int index)
        {
            await response;
            return args[index].Response;
        }

        public Task PutAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => Task.WhenAll(e.Unhandled.Select(arg => PutAsync(arg.Uri, arg.Body)));

        public async Task PutAsync<T>(Uri uri, T resource)
        {
            await CacheSemaphore.WaitAsync();

            try
            {
                Put(uri, resource);
            }
            finally
            {
                CacheSemaphore.Release();
            }
        }

        public void Put<T>(Uri uri, T resource)
        {
            if (uri is UniformItemIdentifier)
            Cache[uri] = Convert(resource);
        }

        private Task<State> Convert(object resource)
        {
            if (resource is Task<State> temp)
            {
                return temp;
            }

            if (resource is Task task && task.GetType() != typeof(Task))
            {
                try
                {
                    return ConvertTask(task);
                }
                catch { }
            }

            return Task.FromResult(new State(resource));
        }

        private async Task<State> ConvertTask(Task task)
        {
            var value = await (dynamic)task;

            if (value == null)
            {
                return State.Null(task.GetType().GetGenericArguments()[0]);
            }
            else
            {
                return value as State ?? new State(value);
            }
        }

        public async Task DeleteAsync(Uri uri)
        {
            await CacheSemaphore.WaitAsync();

            try
            {
                Cache.Remove(uri);
            }
            finally
            {
                CacheSemaphore.Release();
            }
        }

        public void Clear()
        {
            Cache.Clear();
        }
    }
}
