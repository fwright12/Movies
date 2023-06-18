using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbLocalHandlers : DatastoreReadHandler
    {
        new public LocalTMDbDatastore Datastore { get; }
        public TMDbResolver Resolver { get; }

        public TMDbLocalHandlers(LocalTMDbDatastore datastore, TMDbResolver resolver) : base(datastore)
        {
            Datastore = datastore;
            Resolver = resolver;
        }

        public override async Task HandleAsync(MultiRestEventArgs args)
        {
            bool parentCollectionWasRequested = args.Unhandled
                .Select(arg => arg.Uri)
                .OfType<UniformItemIdentifier>()
                .Select(uii => uii.Property)
                .Contains(Movie.PARENT_COLLECTION);

            foreach (var e in args.Unhandled)
            {
                if (!Datastore.IsCacheable(e.Uri))
                {
                    continue;
                }

                var url = Resolver.ResolveUrl(e.Uri);
                var uri = new TMDbResolver.DummyUri(url, (e.Uri as UniformItemIdentifier)?.Item, parentCollectionWasRequested);
                var response = await Datastore.ReadAsync(uri);

                if (response?.TryGetRepresentation<IEnumerable<KeyValuePair<Uri, object>>>(out var collection) == true)
                {
                    args.HandleMany(collection);

                    foreach (var arg in args.Unhandled)
                    {
                        if (MultiRestEventArgs.TryGetValue(collection, arg.Uri, out var obj))
                        {
                            arg.Handle(State.Create(obj));
                        }
                    }
                }

                if (false && response?.TryGetRepresentation<ArraySegment<byte>>(out var bytes) == true)
                {
                    var json = new AnnotatedJson(bytes.ToArray());

                    if (e.Uri is UniformItemIdentifier uii)
                    {
                        foreach (var annotation in Resolver.Annotate(url))
                        {
                            json.Add(new UniformItemIdentifier(uii.Item, annotation.Key), annotation.Value);
                        }
                    }

                    args.HandleMany(json);

                    if (!e.Handled && e.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(e.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                    {
                        e.Handle(converter);
                    }
                }
            }
        }
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
                var type = converter is HttpResourceCollectionConverter ? typeof(IEnumerable<KeyValuePair<Uri, object>>) : converted.GetType();
                state.Add(type, converted);
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
        private bool ContainsChangeKey(Property property) => Resolver.TryResolveJsonPropertyName(property, out string changeKey) && ChangeKeys.Contains(changeKey);
    }
}
