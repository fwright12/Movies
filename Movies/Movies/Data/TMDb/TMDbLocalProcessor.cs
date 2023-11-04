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
        public IAsyncCollection<string> ChangeKeys { get; set; }

        // There are no change keys for these properties but we're going to ignore that and cache them anyway
        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType CACHEABLE_TYPES = ItemType.Movie | ItemType.TVShow | ItemType.Person;

        private IAsyncEventProcessor<IEnumerable<DatastoreWriteArgs>> BulkProcessor { get; }

        public TMDbLocalProcessor(IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> readProcessor, IAsyncEventProcessor<DatastoreKeyValueWriteArgs<Uri, State>> writeProcessor, TMDbResolver resolver) : base(readProcessor, resolver)
        {
            BulkProcessor = new AsyncBulkEventProcessor<DatastoreWriteArgs, DatastoreKeyValueWriteArgs<Uri, State>>(writeProcessor);
        }

        public TMDbLocalProcessor(IDataStore<Uri, State> datastore, TMDbResolver resolver) : this(new DatastoreProcessor<Uri, State>(datastore), resolver) { }
        private TMDbLocalProcessor(DatastoreProcessor<Uri, State> processor, TMDbResolver resolver) : this(processor, processor, resolver) { }

        public override async Task<bool> ProcessAsync(IEnumerable<DatastoreKeyValueReadArgs<Uri>> e) => await base.ProcessAsync(e) && e.All(IsApplicable);

        public Task<bool> ProcessAsync(IEnumerable<DatastoreWriteArgs> e) => BulkProcessor.ProcessAsync(e.Where(arg => arg is DatastoreKeyValueWriteArgs<Uri, State> args && IsCacheable(args.Key) && false == args.Key is UniformItemIdentifier));

        protected override bool Handle(DatastoreKeyValueReadArgs<Uri> grouped, IEnumerable<DatastoreKeyValueReadArgs<Uri>> singles) => base.Handle(grouped, singles) && (grouped.Response as RestResponse)?.ControlData.TryGetValue(Rest.ETAG, out _) != true;

        protected override IEnumerable<KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupUrls(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args) => args
            .Where(IsApplicable)
            .Select(arg => new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(Resolver.ResolveUrl(arg.Key), arg.AsEnumerable()))
            .GroupBy(kvp => kvp.Key, (key, group) => new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(key, group.SelectMany(pair => pair.Value)));

        private bool IsApplicable(DatastoreKeyValueReadArgs<Uri> e) => !e.IsHandled && IsCacheable(e.Key);

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

    public class LocalTMDbDatastore1 : IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>>, IAsyncEventProcessor<DatastoreKeyValueWriteArgs<Uri, State>>
    {
        public TMDbResolver Resolver { get; }
        public IDataStore<Uri, State> Datastore { get; }

        public LocalTMDbDatastore1(IDataStore<Uri, State> datastore, TMDbResolver resolver)
        {
            Datastore = datastore;
            Resolver = resolver;
        }

        public async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<Uri> e)
        {
            var url = Resolver.ResolveUrl(e.Key);
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var state = await Datastore.ReadAsync(uri);

            return e.Handle(new RestResponse(state)
            {
                Expected = e.Expected
            });
        }

        public Task<bool> ProcessAsync(DatastoreKeyValueWriteArgs<Uri, State> e)
        {
            if (e.Key is UniformItemIdentifier)
            {
                return Task.FromResult(false);
            }
            else
            {
                var url = Resolver.ResolveUrl(e.Key);
                var key = new Uri(url, UriKind.RelativeOrAbsolute);
                return Datastore.UpdateAsync(key, e.Value);
            }
        }
    }
}
