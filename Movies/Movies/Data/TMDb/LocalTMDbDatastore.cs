using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbLocalHandlers : DatastoreReadHandler
    {
        public TMDbResolver Resolver { get; }

        public TMDbLocalHandlers(IDataStore<Uri, State> datastore, TMDbResolver resolver) : base(datastore)
        {
            Resolver = resolver;
        }

        public override async Task HandleAsync(MultiRestEventArgs args)
        {
            foreach (var e in args.Unhandled)
            {
                var url = Resolver.ResolveUrl(e.Uri);
                var response = await Datastore.ReadAsync(e.Uri);

                if (response?.TryGetRepresentation<byte[]>(out var bytes) == true)
                {
                    var json = new AnnotatedJson(bytes);

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

        public Task<State> ReadAsync(Uri key)
        {
            if (!IsCacheable(key))
            {
                return Task.FromResult<State>(null);
            }

            var url = Resolver.ResolveUrl(key);
            key = new Uri(url, UriKind.RelativeOrAbsolute);
            return Datastore.ReadAsync(key);
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

    public class TMDbLocalResources : ControllerLink//<HttpContent>
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

        private IJsonCache Cache { get; }
        private TMDbResolver Resolver { get; }

        public TMDbLocalResources(IJsonCache cache, TMDbResolver resolver) : base()
        {
            Cache = cache;
            Resolver = resolver;
        }

        protected override async Task GetInternalAsync(MultiRestEventArgs args, ChainLinkEventHandler<MultiRestEventArgs> next) //=> HandleAsync(e, next, async e =>
        {
            foreach (var e in args.Unhandled)
            {
                if (!IsCacheable(e.Uri))
                {
                    return;
                }

                var url = Resolver.ResolveUrl(e.Uri);
                var response = await Cache.TryGetValueAsync(url);

                if (response != null)
                {
                    var json = new AnnotatedJson(await response.Content.ReadAsByteArrayAsync());

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
        }//);

        protected override Task PutAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next, async e =>
        {
            //if (e.Uri is UniformItemIdentifier uii && !IsCacheable(uii)) // && uii.Item.TryGetID(TMDB.ID, out var id) && Resolver.TryResolve(uii, out var url) && arg.Response is HttpContent content)// && Converter.TryConvert(resource, out var content))
            if (e.Uri is UniformItemIdentifier || !IsCacheable(e.Uri))
            {
                return;
            }

            var url = Resolver.ResolveUrl(e.Uri);

            if (e.Body?.TryGetRepresentation<ArraySegment<byte>>(out var content) == true)
            {
                await Cache.AddAsync(url, new JsonResponse(content.ToArray()));
            }
        });

        /*public async Task HandleAsync1(GetEventArgs<HttpContent> arg)
        {
            var url = DeconstructUrl(arg.Uri.ToString(), out var query, out var paths);
            var urls = ConstructUrls(url, query, paths);
            var responses = (await Task.WhenAll(urls.Select(url => Cache.TryGetValueAsync(url))))
                .OfType<JsonResponse>()
                .ToArray();

            if (responses.All(response => response != null))
            {
                var json = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
                var properties = paths.Zip(json, JsonProperty);
                arg.Handle(new StringContent(string.Join(',', properties)));
            }
        }

        public async Task HandleAsync(PostEventArgs<HttpContent> arg)
        {
            var url = DeconstructUrl(arg.Uri.ToString(), out var query, out var paths);
            //var urls = ConstructUrls(url, query, paths);
            var json = new LazyJson(await arg.Resource.ReadAsByteArrayAsync(), paths);

            foreach (var path in paths.Prepend(""))
            {
                if (json.TryGetValue(path, out var bytes))
                {
                    var temp = url;

                    if (!string.IsNullOrEmpty(path))
                    {
                        temp += "/" + path;
                    }

                    //await Cache.AddAsync(ItemType.Movie, 0, $"{temp}?{query}", new JsonResponse(bytes.ToArray()));
                }
            }
        }

        private const string APPEND_TO_RESPONSE = "append_to_response";

        private string JsonProperty(string propertyName, string json) => $"[{propertyName}] = {json}";

        private IEnumerable<string> SeparateAppendedRequests(string url) => ConstructUrls(DeconstructUrl(url, out var query, out var appended), query, appended);

        private IEnumerable<string> ConstructUrls(string baseUrl, string query, IEnumerable<string> appended) => appended.Select(path => $"{baseUrl}/{path}?{query}");

        private string DeconstructUrl(string url, out string query, out IEnumerable<string> appended)
        {
            var parts = url.Split('?');
            appended = new string[] { "" };

            if (parts.Length > 1)
            {
                query = parts[1];
                var args = parts[1]
                    .Split('&')
                    .Select(arg => arg.Split('='))
                    .ToList();
                var i = args.FindIndex(arg => arg.FirstOrDefault() == APPEND_TO_RESPONSE);

                if (args[i].Length > 1)
                {
                    var atr = args[i];
                    args.RemoveAt(i);

                    var temp = query = string.Join('&', args.Select(arg => string.Join('=', arg)));
                    appended = atr[1].Split(',').ToArray();
                }
            }
            else
            {
                query = string.Empty;
            }

            return parts[0];
        }*/

        public static async Task<string> GetContent(JsonResponse json) => await json.Content.ReadAsStringAsync();

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
