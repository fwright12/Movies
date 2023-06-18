using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Movies
{
    public class TMDbLocalHandler : ControllerHandler
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

        public TMDbLocalHandler(IJsonCache cache, TMDbResolver resolver) : base()
        {
            Cache = cache;
            Resolver = resolver;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => IsCacheable(request.RequestUri) ? base.SendAsync(request, cancellationToken) : NextAsync(request, cancellationToken);

        protected override async Task<HttpResponseMessage> GetAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = Resolver.ResolveUrl(request.RequestUri);
            var response = await Cache.TryGetValueAsync(url);

            if (response == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = response.Content
                };
            }
        }

        protected override async Task<HttpResponseMessage> PutAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is UniformItemIdentifier)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var url = Resolver.ResolveUrl(request.RequestUri);
            await Cache.AddAsync(url, new JsonResponse(request.Content));

            return new HttpResponseMessage(HttpStatusCode.OK);
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
        private bool ContainsChangeKey(Property property) => Resolver.TryResolveJsonPropertyName(property, out string changeKey) && ChangeKeys.Contains(changeKey);
    }

    public class TMDbController : HttpController
    {
        public TMDbResolver Resolver { get; }

        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbController(HttpMessageInvoker invoker, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(invoker)
        {
            Resolver = resolver;

            var kvps = autoAppend;// Enumerable.Empty<KeyValuePair<TMDbRequest, IEnumerable<TMDbRequest>>>();
            Appendable = new Dictionary<TMDbRequest, IEnumerable<TMDbRequest>>(kvps.Where(kvp => kvp.Key.SupportsAppendToResponse));
            AppendsTo = new Dictionary<TMDbRequest, TMDbRequest>();

            foreach (var kvp in kvps)
            {
                foreach (var request in kvp.Value)
                {
                    AppendsTo.Add(request, kvp.Key);
                }
            }
        }

        private static readonly string APPEND_TO_RESPONSE = "append_to_response";

        public override Task Send(IEnumerable<RestArgs> args1)
        {
            var result = new List<string>();
            var args = args1.ToArray();
            var uris = args
                .Select(arg => arg.Request.Uri)
                .ToArray();
            var urls = uris
                .SelectMany(GetUrlsGreedy)
                .Distinct()
                .ToArray();
            var pairs = args
                .Select(arg => (Resolver.ResolveUrl(arg.Request.Uri), arg))
                .Concat(urls.Select(url => (url, (RestArgs)null)))
                .ToArray();
            var trie = new Dictionary<string, List<(string Url, string Path, string Query, RestArgs Arg)>>();

            foreach (var pair in pairs)
            {
                //if (arg.Uri is UniformItemIdentifier uii && Resolver.TryGetRequest(uii, out var request))
                //var url = Resolver.ResolveUrl(arg.Request.Uri);
                var url = pair.Item1;
                var arg = pair.Item2;

                var parts = url.Split('?');
                //TMDbClient.AddToTrie(trie, parts[0], (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg));
            }

            foreach (var kvp in trie)
            {
                var url = kvp.Key;
                var paths = kvp.Value.Select(value => value.Path.Substring(url.Length).Trim('/')).ToArray();
                var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
                var queries = kvp.Value.Select(value => value.Query).ToArray();
                var query = CombineQueries(queries);

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var pair = kvp.Value[i];
                    pair.Url = pair.Path + (string.IsNullOrEmpty(query) ? "" : ("?" + query));
                    kvp.Value[i] = pair;
                }

                if (validPaths.Length > 0)
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        query += "&";
                    }

                    query += $"append_to_response={string.Join(',', validPaths)}";
                }

                if (!string.IsNullOrEmpty(query))
                {
                    url += "?" + query;
                }

                result.Add(url);
            }

            var item = uris.OfType<UniformItemIdentifier>().FirstOrDefault()?.Item;
            var requests = result
                .Select(url => new RestArgs(new RestRequest(HttpMethod.Get, new DummyUri(url, item))))
                .ToArray();

            var response = base.Send((IEnumerable<RestArgs>)requests);
            var itr = trie.GetEnumerator();

            for (int i = 0; itr.MoveNext(); i++)
            {
                var kvp = itr.Current;
                var request = requests[i];

                foreach (var value in kvp.Value)
                {
                    var url = value.Url;
                    var uri = new Uri(url, UriKind.Relative);

                    if (request.Resource is HttpResourceCollection collection)
                    {
                        value.Arg?.Handle(new HttpResourceCollection(GetValue(collection, uri), collection.Uris));
                    }
                }
            }

            return response;
        }

        private static async Task<IReadOnlyDictionary<Uri, object>> GetValue(HttpResourceCollection task, Uri key)
        {
            var dict = await task;

            if (dict.TryGetValue(key, out var value))
            {
                if (value is State state && state.TryGetRepresentation<IReadOnlyDictionary<Uri, object>>(out var inner))
                {
                    return inner;
                }
                else
                {
                    return (IReadOnlyDictionary<Uri, object>)value;
                }
            }
            else
            {
                return null;
            }
        }

        private static async Task<T> CastAsync<T>(Task<object> value) where T : class => (await value) as T;

        private async Task Handle(Task task)
        {
            await task;
        }

        private class DummyUri : Uri
        {
            public Item Item { get; }

            public DummyUri(string url, Item item) : base(url, UriKind.Relative)
            {
                Item = item;
            }
        }

        public override bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
        {
            var url = Resolver.ResolveUrl(uri);
            var rawQuery = url.Split('?').LastOrDefault();
            //var rawQuery = new Uri(url, UriKind.Relative).Query;
            var query = HttpUtility.ParseQueryString(rawQuery);
            var append = query.GetValues(APPEND_TO_RESPONSE);

            var data = new List<(Uri Uri, string Path, List<Parser> Parsers)>();

            if (append == null)
            {
                append = new string[0];
            }
            else
            {
                append = append[0].Split(',');
            }

            var basePath = uri.ToString().Split('?')[0];
            query.Remove(APPEND_TO_RESPONSE);
            var queryString = query.ToString();

            foreach (var appended in append.Prepend(string.Empty))
            {
                var fullUrl = basePath;
                if (!string.IsNullOrEmpty(appended))
                {
                    fullUrl += "/" + appended;
                }
                fullUrl += "?" + queryString;

                var fullUri = new Uri(fullUrl, UriKind.Relative);

                if (Resolver.TryGetAnnotations(fullUri, out var annotation))
                {
                    data.Add((fullUri, appended, annotation.Value));
                }
            }

            if (data.Count == 0)
            {
                resource = default;
                return false;
            }
            else
            {
                resource = new Converter((uri as DummyUri)?.Item, data);
                return true;
            }
        }

        private IEnumerable<string> GetUrlsGreedy(Uri uri)
        {
            if (Resolver.TryGetRequest(uri, out var request) && Resolver.TryResolve(uri, out _, out var args))
            {
                var original = request;

                if (AppendsTo.TryGetValue(request, out var temp))
                {
                    yield return Resolver.Resolve(request = temp, args);
                }

                if (Appendable.TryGetValue(request, out var appendable))
                {
                    foreach (var url in appendable.Where(req => req != original).Select(req => Resolver.Resolve(req, args)))
                    {
                        yield return url;
                    }
                }
            }
        }

        private static string CombineQueries(IEnumerable<string> queries)
        {
            var result = new Dictionary<string, string>();

            foreach (var query in queries)
            {
                var args = query.Split('&');

                foreach (var arg in args)
                {
                    var kvp = arg.Split('=');

                    if (kvp.Length == 2)
                    {
                        result.TryAdd(kvp[0], kvp[1]);
                    }
                }
            }

            return string.Join('&', result.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        private class Converter : HttpResourceCollectionConverter
        {
            public Item Item { get; }
            public IEnumerable<(Uri Uri, string Path, List<Parser> Parsers)> Annotations { get; }
            public override IEnumerable<Uri> Resources { get; }

            public Converter(Item item, IEnumerable<(Uri, string, List<Parser>)> annotations)
            {
                Item = item;
                Resources = annotations.Select(temp => temp.Item1).ToArray();
                Annotations = annotations;
            }

            public override async Task<IReadOnlyDictionary<Uri, object>> Convert(HttpContent content)
            {
                var result = new Dictionary<Uri, object>();
                var json = new JsonIndex(await content.ReadAsByteArrayAsync());

                foreach (var annotation in Annotations)
                {
                    JsonIndex index;

                    if (string.IsNullOrEmpty(annotation.Path))
                    {
                        index = json;
                    }
                    else if (!json.TryGetValue(annotation.Path, out index))
                    {
                        continue;
                    }

                    var inner = new Dictionary<Uri, object>();
                    result.Add(annotation.Uri, new State(index.Bytes)
                    {
                        inner,
                    });

                    foreach (var parser in annotation.Parsers)
                    {
                        var value = await parser.TryGetValue(parser.GetPair(Task.FromResult(index.Bytes)));

                        if (value.Success)
                        {
                            var uii = new UniformItemIdentifier(Item, parser.Property);
                            var state = new State(value);
                            state.Add(index.Bytes);

                            inner.Add(uii, value.Result);
                        }
                    }

                    foreach (var kvp in inner)
                    {
                        result.Add(kvp.Key, kvp.Value);
                    }
                }

                return result;
            }
        }
    }
}
