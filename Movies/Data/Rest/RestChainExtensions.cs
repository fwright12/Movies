using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public static class RestChainExtensions
    {
        public static async Task<KeyValueRequestArgs<Uri>> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> chain, string url) => (await Get(chain, new string[] { url }))[0];
        public static Task<KeyValueRequestArgs<Uri>[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> chain, params string[] urls) => Get(chain, urls.Select(url => new Uri(url, UriKind.Relative)));
        public static async Task<KeyValueRequestArgs<Uri>[]> Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> chain, IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new KeyValueRequestArgs<Uri>(uri)).ToArray();
            await Get(chain, args);
            return args;
        }

        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> chain, params KeyValueRequestArgs<Uri>[] args) => Get(chain, (IEnumerable<KeyValueRequestArgs<Uri>>)args);
        public static Task Get(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> chain, IEnumerable<KeyValueRequestArgs<Uri>> args)
        {
            var e = new BulkEventArgs<KeyValueRequestArgs<Uri>>(args);
            return chain.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>(e));
        }

        public static async Task<KeyValueRequestArgs<Uri, T>> TryGet<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> processor, Uri uri)
        {
            var e = new KeyValueRequestArgs<Uri, T>(uri);
            await processor.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>(new BulkEventArgs<KeyValueRequestArgs<Uri>>(e)));
            return e;
        }

        public static async Task<KeyValueRequestArgs<Uri, T>> TryGet<T>(this IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> processor, Uri uri)
        {
            var e = new KeyValueRequestArgs<Uri, T>(uri);
            await processor.ProcessAsync(new BulkEventArgs<KeyValueRequestArgs<Uri>>(e));
            return e;
        }

        public static KeyValueRequestArgs<Uri, T> Create<T>(this Property<T> property, Movies.Models.Item item) => new KeyValueRequestArgs<Uri, T>(new UniformItemIdentifier(item, property));
    }
}