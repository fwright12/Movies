using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public static class RestChainExtensions
    {
        public static async Task<RestRequestArgs> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> chain, string url) => (await Get(chain, new string[] { url }))[0];
        public static Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> chain, params string[] urls) => Get(chain, urls.Select(url => new Uri(url, UriKind.Relative)));
        public static async Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> chain, IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(chain, args);
            return args;
        }

        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> chain, params ResourceReadArgs<Uri>[] args) => Get(chain, (IEnumerable<ResourceReadArgs<Uri>>)args);
        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> chain, IEnumerable<ResourceReadArgs<Uri>> args)
        {
            var e = new BulkEventArgs<ResourceReadArgs<Uri>>(args);
            return chain.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>(e));
        }

        public static async Task<ResourceReadArgs<Uri, T>> TryGet<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> processor, Uri uri)
        {
            var e = new ResourceReadArgs<Uri, T>(uri);
            await processor.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>(new BulkEventArgs<ResourceReadArgs<Uri>>(e)));
            return e;
        }

        public static ResourceReadArgs<Uri, T> Create<T>(this Property<T> property, Movies.Models.Item item) => new ResourceReadArgs<Uri, T>(new UniformItemIdentifier(item, property));
    }
}