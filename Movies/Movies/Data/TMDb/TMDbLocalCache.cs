using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
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

        private Dictionary<Uri, ResourceResponse> ResponseCache { get; } = new Dictionary<Uri, ResourceResponse>();

        public TMDbLocalCache(IEventAsyncCache<ResourceReadArgs<Uri>> dao, TMDbResolver resolver)
        {
            DAO = dao;
            Processor = new TMDbProcessor(new TMDbSQLProcessor(dao), resolver);
            Resolver = resolver;
        }

        public async Task<bool> Read(IEnumerable<ResourceReadArgs<Uri>> args)
        {
            var result = await Processor.ProcessAsync(args);

            foreach (var arg in args)
            {
                if (false == arg.Key is UniformItemIdentifier && arg.IsHandled && arg.Response is RestResponse restResponse && restResponse.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out var values))
                {
                    if (ResponseCache.ContainsKey(arg.Key))
                    {
                        (restResponse.ControlData as IDictionary<string, IEnumerable<string>>)?.Remove(REpresentationalStateTransfer.Rest.ETAG);
                    }
                    else
                    {
                        var itr = values.GetEnumerator();

                        if (itr.MoveNext())
                        {
                            var value = itr.Current;

                            if (!itr.MoveNext())
                            {
                                ResponseCache[arg.Key] = arg.Response;
                            }
                        }
                    }
                }
            }

            return result && args.All(arg => false == arg.Response is RestResponse restResponse || !restResponse.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out _));
        }

        private async Task<bool> Read1(IEnumerable<ResourceReadArgs<Uri>> args)
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

            foreach (var arg in applicable)
            {
                if (arg.IsHandled && arg.Response is RestResponse restResponse && restResponse.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out var values))
                {
                    var itr = values.GetEnumerator();

                    if (itr.MoveNext())
                    {
                        var value = itr.Current;

                        if (!itr.MoveNext())
                        {
                            ResponseCache[arg.Key] = arg.Response;
                        }
                    }
                }
            }

            return result && isAllApplicable;
        }

        //private Task<bool> Write1(IEnumerable<ResourceReadArgs<Uri>> args) => DAO.Write(args.Where(arg => (!ResponseCache.Remove(arg.Key, out var response) || response != arg.Response) && (false == arg.Key is UniformItemIdentifier)));

        public Task<bool> Write(IEnumerable<ResourceReadArgs<Uri>> args) => DAO.Write(args.Where(ShouldWrite));

        private bool ShouldWrite(ResourceReadArgs<Uri> arg)
        {
            if (arg.Key is UniformItemIdentifier)
            {
                return false;
            }
            if (!ResponseCache.TryGetValue(arg.Key, out var response))
            {
                ResponseCache[arg.Key] = arg.Response;
                return false == arg.Response is HttpResponse httpResponse || httpResponse.StatusCode != System.Net.HttpStatusCode.NotModified;
            }

            return response != arg.Response;
        }

        public bool IsCacheable(Uri uri)
        {
            return true;
            if (uri is UniformItemIdentifier uii)
            {
                return CACHEABLE_TYPES.HasFlag(uii.Item.ItemType);// && IsCacheable(uii.Property);
            }
            else
            {
                return true;
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
