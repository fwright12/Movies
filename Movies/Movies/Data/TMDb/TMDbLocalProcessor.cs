using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbSQLProcessor : IAsyncEventProcessor<ResourceReadArgs<Uri>>
    {
        public IEventAsyncCache<ResourceReadArgs<Uri>> DAO { get; }

        public TMDbSQLProcessor(IEventAsyncCache<ResourceReadArgs<Uri>> dao)
        {
            DAO = dao;
        }

        public Task<bool> ProcessAsync(ResourceReadArgs<Uri> e) => DAO.Read(e.AsEnumerable());
    }

    public class TMDbLocalCache : IEventAsyncCache<ResourceReadArgs<Uri>>
    {
        public IEventAsyncCache<ResourceReadArgs<Uri>> DAO { get; }
        public IAsyncEventProcessor<IEnumerable<ResourceReadArgs<Uri>>> Processor { get; }
        public TMDbResolver Resolver { get; }

        public IAsyncCollection<string> ChangeKeys { get; set; }

        // There are no change keys for these properties but we're going to ignore that and cache them anyway
        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType CACHEABLE_TYPES = ItemType.Movie | ItemType.TVShow | ItemType.Person;

        public TMDbLocalCache(IEventAsyncCache<ResourceReadArgs<Uri>> dao, TMDbResolver resolver)
        {
            DAO = dao;
            Processor = new TMDbLocalProcessor(new TMDbSQLProcessor(dao), resolver);
            Resolver = resolver;
        }

        public async Task<bool> Read(IEnumerable<ResourceReadArgs<Uri>> args)
        {
            bool isAllApplicable = true;
            var applicable = new BulkEventArgs<ResourceReadArgs<Uri>>();

            foreach (var arg in args)
            {
                if (!arg.IsHandled && IsCacheable(arg.Key))
                {
                    applicable.Add(arg);
                }
                else
                {
                    isAllApplicable = false;
                }
            }

            var result = await Processor.ProcessAsync(applicable);
            (args as BulkEventArgs<ResourceReadArgs<Uri>>)?.Add(applicable);

            return result && isAllApplicable;
        }

        public Task<bool> Write(IEnumerable<ResourceReadArgs<Uri>> args) => DAO.Write(args.Where(arg => IsCacheable(arg.Key) && false == arg.Key is UniformItemIdentifier));

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

    public class TMDbLocalProcessor : TMDbProcessor
    {
        public TMDbLocalProcessor(IAsyncEventProcessor<ResourceReadArgs<Uri>> processor, TMDbResolver resolver) : base(processor, resolver) { }

        protected override bool Handle(ResourceReadArgs<Uri> grouped, IEnumerable<ResourceReadArgs<Uri>> singles) => base.Handle(grouped, singles) && (grouped.Response as RestResponse)?.ControlData.TryGetValue(Rest.ETAG, out _) != true;
    }

    public class LocalTMDbDatastore1 : IAsyncEventProcessor<ResourceReadArgs<Uri>>, IAsyncEventProcessor<DatastoreKeyValueWriteArgs<Uri, State>>
    {
        public TMDbResolver Resolver { get; }
        public IDataStore<Uri, State> Datastore { get; }

        public LocalTMDbDatastore1(IDataStore<Uri, State> datastore, TMDbResolver resolver)
        {
            Datastore = datastore;
            Resolver = resolver;
        }

        public async Task<bool> ProcessAsync(ResourceReadArgs<Uri> e)
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
