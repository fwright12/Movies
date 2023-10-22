using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbLocalProcessor : TMDbProcessor, IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>>
    {
        public LocalTMDbDatastore Datastore { get; }

        private IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>> BulkProcessor { get; }

        public TMDbLocalProcessor(LocalTMDbDatastore datastore, TMDbResolver resolver) : base(datastore, resolver)
        {
            Datastore = datastore;
            BulkProcessor = new AsyncEventBulkProcessor<DatastoreWriteArgs, DatastoreKeyValueWriteArgs<Uri, Resource>>(Datastore);
        }

        public Task<bool> ProcessAsync(IEnumerable<DatastoreWriteArgs> e) => BulkProcessor.ProcessAsync(e);

        protected override IEnumerable<KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupUrls(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args) => args
            .Where(arg => Datastore.IsCacheable(arg.Key))
            .Select(arg => new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(Resolver.ResolveUrl(arg.Key), arg.AsEnumerable()))
            .GroupBy(kvp => kvp.Key, (key, group) => new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(key, group.SelectMany(pair => pair.Value)));
    }

    public class LocalTMDbDatastore : IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>>, IAsyncEventProcessor<DatastoreKeyValueWriteArgs<Uri, Resource>>
    {
        public TMDbResolver Resolver { get; }
        public IDataStore<Uri, State> Datastore { get; }
        public IAsyncCollection<string> ChangeKeys { get; set; }

        // There are no change keys for these properties but we're going to ignore that and cache them anyway
        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType CACHEABLE_TYPES = ItemType.Movie | ItemType.TVShow | ItemType.Person;

        public LocalTMDbDatastore(IDataStore<Uri, State> datastore, TMDbResolver resolver)
        {
            Datastore = datastore;
            Resolver = resolver;
        }

        public async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<Uri> e)
        {
            if (!IsCacheable(e.Key))
            {
                return false;
            }

            var url = Resolver.ResolveUrl(e.Key);
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var state = await Datastore.ReadAsync(uri);

            if (state?.TryGetRepresentation<IEnumerable<byte>>(out var bytes) == true && Resolver.TryGetConverter(e.Key, out var converter))
            {
                var converted = await converter.Convert(new ByteArrayContent(bytes as byte[] ?? bytes.ToArray()));
                //var type = converter is HttpResourceCollectionConverter ? typeof(IEnumerable<KeyValuePair<Uri, object>>) : converted.GetType();
                state.Add(converted.GetType(), converted);
            }

            return e.Handle(new RestResponse(state)
            {
                Expected = e.Expected
            });
        }

        public Task<bool> ProcessAsync(DatastoreKeyValueWriteArgs<Uri, Resource> e)
        {
            if (e.Key is UniformItemIdentifier || !IsCacheable(e.Key))
            {
                return Task.FromResult(false);
            }
            else
            {
                var url = Resolver.ResolveUrl(e.Key);
                var key = new Uri(url, UriKind.RelativeOrAbsolute);
                return Datastore.UpdateAsync(key, e.Value() as State);
            }
        }

        public bool IsCacheable(Uri uri)
        {
            if (uri is UniformItemIdentifier uii)
            {
                return CACHEABLE_TYPES.HasFlag(uii.Item.ItemType) && IsCacheable(uii.Property);
            }
            else
            {
                var url = Resolver.ResolveUrl(uri);

                if (!url.StartsWith("3/movie") && !url.StartsWith("3/tv") && !url.StartsWith("3/person"))
                {
                    return false;
                }

                if (Resolver.TryGetAnnotations(uri, out var annotations) && annotations.Value.All(parser => !IsCacheable(parser.Property)))
                {
                    return false;
                }

                return true;
            }
        }

        private bool IsCacheable(Property property) => CHANGES_IGNORED.Contains(property) || ContainsChangeKey(property);
        private bool ContainsChangeKey(Property property) => Resolver.TryGetChangeKey(property, out string changeKey) && ChangeKeys.Contains(changeKey);
    }
}
