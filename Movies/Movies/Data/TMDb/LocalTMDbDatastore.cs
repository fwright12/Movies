using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbLocalHandlers : TMDbRestCache
    {
        new public LocalTMDbDatastore Datastore { get; }

        public TMDbLocalHandlers(LocalTMDbDatastore datastore, TMDbResolver resolver) : base(datastore, resolver)
        {
            Datastore = datastore;
        }

        protected override IEnumerable<KeyValuePair<string, IEnumerable<RestRequestArgs>>> GroupRequests(IEnumerable<RestRequestArgs> args) => args
            .Where(arg => Datastore.IsCacheable(arg.Uri))
            .Select(arg => new KeyValuePair<string, IEnumerable<RestRequestArgs>>(Resolver.ResolveUrl(arg.Uri), arg.AsEnumerable()))
            .GroupBy(kvp => kvp.Key, (key, group) => new KeyValuePair<string, IEnumerable<RestRequestArgs>>(key, group.SelectMany(pair => pair.Value)));
    }

    public class LocalTMDbDatastore : IDataStore<Uri, State>
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

        public Task<bool> CreateAsync(Uri key, State value) => UpdateAsync(key, value);

        public async Task<State> ReadAsync(Uri key)
        {
            if (!IsCacheable(key))
            {
                return await Task.FromResult<State>(null);
            }

            var url = Resolver.ResolveUrl(key);
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var state = await Datastore.ReadAsync(uri);

            if (state?.TryGetRepresentation<IEnumerable<byte>>(out var bytes) == true && Resolver.TryGetConverter(key, out var converter))
            {
                var converted = await converter.Convert(new ByteArrayContent(bytes as byte[] ?? bytes.ToArray()));
                //var type = converter is HttpResourceCollectionConverter ? typeof(IEnumerable<KeyValuePair<Uri, object>>) : converted.GetType();
                state.Add(converted.GetType(), converted);
            }

            return state;
        }

        public Task<bool> UpdateAsync(Uri key, State updatedValue)
        {
            if (key is UniformItemIdentifier || !IsCacheable(key))
            {
                return Task.FromResult(false);
            }
            else
            {
                var url = Resolver.ResolveUrl(key);
                key = new Uri(url, UriKind.RelativeOrAbsolute);
                return Datastore.UpdateAsync(key, updatedValue);
            }
        }

        public Task<State> DeleteAsync(Uri key) => Datastore.DeleteAsync(key);

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
