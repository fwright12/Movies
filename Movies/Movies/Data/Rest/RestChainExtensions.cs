using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public static class RestChainExtensions
    {
        public static async Task<RestRequestArgs> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> chain, string url) => (await Get(chain, new string[] { url }))[0];
        public static Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> chain, params string[] urls) => Get(chain, urls.Select(url => new Uri(url, UriKind.Relative)));
        public static async Task<RestRequestArgs[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> chain, IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(chain, args);
            return args;
        }

        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> chain, params ResourceRequestArgs<Uri>[] args) => Get(chain, (IEnumerable<ResourceRequestArgs<Uri>>)args);
        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> chain, IEnumerable<ResourceRequestArgs<Uri>> args)
        {
            var e = new BulkEventArgs<ResourceRequestArgs<Uri>>(args);
            return chain.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>(e));
        }

        public static async Task<ResourceRequestArgs<Uri, T>> TryGet<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>> processor, Uri uri)
        {
            var e = new ResourceRequestArgs<Uri, T>(uri);
            await processor.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<ResourceRequestArgs<Uri>>>(new BulkEventArgs<ResourceRequestArgs<Uri>>(e)));
            return e;
        }

        public static ResourceRequestArgs<Uri, T> Create<T>(this Property<T> property, Movies.Models.Item item) => new ResourceRequestArgs<Uri, T>(new UniformItemIdentifier(item, property));
    }
}