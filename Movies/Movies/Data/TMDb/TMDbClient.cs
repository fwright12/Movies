using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Movies
{
    public class TMDbRemoteDatastore : HttpDatastore
    {
        public TMDbResolver Resolver { get; }

        public TMDbRemoteDatastore(HttpMessageInvoker invoker, TMDbResolver resolver) : base(invoker)
        {
            Resolver = resolver;
        }

        //protected override bool TryGetConverter(Uri uri, out IHttpConverter<object> resource) => Resolver.TryGetConverter(uri, out resource);
        protected override bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
        {
            if (uri is TMDbResolver.TrojanTMDbUri trojan)
            {
                resource = trojan.Converter;
                return resource != null;
            }

            return Resolver.TryGetConverter(uri, out resource);
        }
    }

    public abstract class TMDbRestCache : RestCache
    {
        public TMDbResolver Resolver { get; }

        public TMDbRestCache(IDataStore<Uri, State> datastore, TMDbResolver resolver) : base(datastore)
        {
            Resolver = resolver;
        }

        public override async Task HandleGet(MultiRestEventArgs e)
        {
            var item = e.Select(arg => arg.Uri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item as Item;
            var responses = new List<Task>();

            foreach (var kvp in GroupRequests(e.Unhandled))
            {
                var url = kvp.Key;
                var group = kvp.Value.ToArray();

                var properties = group
                    .Select(arg => arg.Uri)
                    .OfType<UniformItemIdentifier>()
                    .Select(uii => uii.Property)
                    .ToHashSet();
                bool parentCollectionWasRequested = properties.Contains(Movie.PARENT_COLLECTION);
                var uri = new TMDbResolver.TrojanTMDbUri(url, item, parentCollectionWasRequested)
                {
                    RequestedProperties = properties
                };

                if (Resolver.TryGetConverter(uri, out var converter))
                {
                    uri.Converter = converter;
                }

                var response = Datastore.ReadAsync(uri);

                if (uri.Converter is HttpResourceCollectionConverter resources)
                {
                    var index = response.TransformAsync(Convert).TransformAsync(kvps => new Dictionary<Uri, object>(kvps));
                    var values = resources.Resources.ToDictionary(uri => uri, uri => index.GetAsync(uri).TransformAsync(obj => State.Create(obj)));

                    foreach (var request in kvp.Value)
                    {
                        if (values.Remove(request.Uri, out var value))
                        {
                            responses.Add(request.Handle(value));
                        }
                    }

                    e.AddRequests(values.Select(kvp =>
                    {
                        var request = new RestRequestArgs(kvp.Key);
                        _ = request.Handle(kvp.Value);
                        return request;
                    }));
                }
            }

            await Task.WhenAll(responses);
        }

        private static IEnumerable<KeyValuePair<Uri, object>> Convert(State response) => response.TryGetRepresentation<IEnumerable<KeyValuePair<Uri, object>>>(out var collection) == true ? collection : Enumerable.Empty<KeyValuePair<Uri, object>>();

        protected virtual IEnumerable<KeyValuePair<string, IEnumerable<RestRequestArgs>>> GroupRequests(IEnumerable<RestRequestArgs> args) => args.Select(arg => new KeyValuePair<string, IEnumerable<RestRequestArgs>>(Resolver.ResolveUrl(arg.Uri), arg.AsEnumerable()));
    }

    public class TMDbReadHandler : TMDbRestCache
    {
        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbReadHandler(HttpMessageHandler handler, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new HttpMessageInvoker(handler), resolver, autoAppend) { }
        public TMDbReadHandler(HttpMessageInvoker client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(new TMDbRemoteDatastore(client, resolver), resolver) //: this(new HttpDatastore(client), resolver, autoAppend) { }
        //public TMDbReadHandler(IDataStore<Uri, State> datastore, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(datastore)
        {
            var kvps = autoAppend ?? Enumerable.Empty<KeyValuePair<TMDbRequest, IEnumerable<TMDbRequest>>>();
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

        protected override IEnumerable<KeyValuePair<string, IEnumerable<RestRequestArgs>>> GroupRequests(IEnumerable<RestRequestArgs> args)
        {
            //var parentCollectionWasRequested = new Lazy<bool>(() => e.Args.OfType<UniformItemIdentifier>().Any(uii => uii.Property == Movie.PARENT_COLLECTION));
            var greedyUrls = args.SelectMany(arg => GetUrlsGreedy(arg.Uri)).Distinct().ToArray();
            var greedy = greedyUrls.Select(url => new RestRequestArgs(new Uri(url, UriKind.Relative)));
            var urls = args.Select(arg => (Resolver.ResolveUrl(arg.Uri), arg));
            urls = urls.Concat(greedyUrls.Select(url => (url, (RestRequestArgs)null)));
            //args = args.Concat(greedy);

            var trie = new Dictionary<string, List<(string Url, string Path, string Query, RestRequestArgs Arg)>>();
            //var trie1 = new Trie<string, (string Url, string Path, string Query, RestRequestArgs Arg)>();

            foreach (var url in urls)
            {
                //var url = Resolver.ResolveUrl(arg.Uri);
                var parts = url.Item1.Split('?');
                AddToTrie(trie, parts[0], (url.Item1, parts[0], parts.Length > 1 ? parts[1] : string.Empty, url.arg));
            }

            foreach (var kvp in trie)
            {
                var url = kvp.Key;
                var paths = kvp.Value.Select(value => value.Path.Substring(url.Length).Trim('/')).ToArray();
                var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
                var queries = kvp.Value.Select(value => value.Query).ToArray();

                var query = CombineQueries(queries);
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

                yield return new KeyValuePair<string, IEnumerable<RestRequestArgs>>(url, kvp.Value.Select(value => value.Arg).Where(arg => arg != null));
            }
        }

        private IEnumerable<string> GetUrlsGreedy(Uri uri)
        {
            if (Resolver.TryResolve(uri, out var request, out var args))
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

        public static void AddToTrie<TValue>(Dictionary<string, List<TValue>> trie, string key, TValue value)
        {
            foreach (var kvp in trie)
            {
                if (key.StartsWith(kvp.Key))
                {
                    kvp.Value.Add(value);
                    return;
                }
            }

            var list = new List<TValue> { value };
            var kvps = trie.Where(kvp => kvp.Key.StartsWith(key)).ToArray();

            trie[key] = list;

            foreach (var kvp in kvps)
            {
                trie.Remove(kvp.Key);
                list.AddRange(kvp.Value);
            }
        }

        private static async Task<LazyJson> Parse(HttpContent response, IEnumerable<string> properties)
        {
            //var response = await responseTask;
            return new LazyJson(await response.ReadAsByteArrayAsync(), properties);
        }

        private static async Task<LazyJson> Parse(Task<HttpResponseMessage> responseTask, IEnumerable<string> properties)
        {
            var response = await responseTask;
            return response?.IsSuccessStatusCode == true ? new LazyJson(await response.Content.ReadAsByteArrayAsync(), properties) : null;
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
    }
}
