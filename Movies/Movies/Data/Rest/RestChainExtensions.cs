using System;
using System.Collections.Generic;
using System.Linq;
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
            var e = new BulkEventArgs<DatastoreKeyValueReadArgs<Uri>>(args);
            return chain.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(e));
        }

        public static async Task<DatastoreKeyValueReadArgs<Uri, T>> TryGet<T>(this IEventProcessor<EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> processor, Uri uri)
        {
            var e = new DatastoreKeyValueReadArgs<Uri, T>(uri);
            await processor.ProcessAsync(new EventArgsAsyncWrapper<IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(new BulkEventArgs<DatastoreKeyValueReadArgs<Uri>>(e)));
            return e;
        }

        public static DatastoreKeyValueReadArgs<Uri, T> Create<T>(this Property<T> property, Movies.Models.Item item) => new DatastoreKeyValueReadArgs<Uri, T>(new UniformItemIdentifier(item, property));
    }
}