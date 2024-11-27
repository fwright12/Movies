using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbLocalCache : IEventAsyncCache<KeyValueRequestArgs<Uri>>
    {
        public IEventAsyncCache<KeyValueRequestArgs<Uri>> DAO { get; }
        public IAsyncEventProcessor<IEnumerable<KeyValueRequestArgs<Uri>>> Processor { get; }
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

        public TMDbLocalCache(IEventAsyncCache<KeyValueRequestArgs<Uri>> dao, TMDbResolver resolver)
        {
            DAO = dao;
            Processor = new TMDbProcessor(new TMDbSQLProcessor(dao), resolver)
            {
                Flag = false
            };
            Resolver = resolver;
        }

        public Task<bool> Read(IEnumerable<KeyValueRequestArgs<Uri>> args) => Processor.ProcessAsync(args);

        public Task<bool> Write(IEnumerable<KeyValueRequestArgs<Uri>> args) => DAO.Write(args.Where(ShouldWrite));

        private bool ShouldWrite(KeyValueRequestArgs<Uri> arg)
        {
            if (arg.Request.Key is UniformItemIdentifier)
            {
                return false;
            }

            return true;
        }

        private bool IsCacheable(Uri uri)
        {
            if (uri is UniformItemIdentifier uii)
            {
                return CACHEABLE_TYPES.HasFlag(uii.Item.ItemType);// && IsCacheable(uii.Property);
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
