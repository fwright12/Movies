using Movies.Models;
using System;
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

        protected override bool TryGetConverter(Uri uri, out IHttpConverter<object> resource) => Resolver.TryGetConverter(uri, out resource);
    }

    public class TMDbReadHandler : DatastoreReadHandler
    {
        private TMDbResolver Resolver { get; }

        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbReadHandler(HttpMessageHandler handler, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new HttpMessageInvoker(handler), resolver, autoAppend) { }
        public TMDbReadHandler(HttpMessageInvoker client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(new TMDbRemoteDatastore(client, resolver)) //: this(new HttpDatastore(client), resolver, autoAppend) { }
        //public TMDbReadHandler(IDataStore<Uri, State> datastore, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(datastore)
        {
            Resolver = resolver;

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

        public override async Task HandleAsync(MultiRestEventArgs e)
        {
            //var parentCollectionWasRequested = new Lazy<bool>(() => e.Args.OfType<UniformItemIdentifier>().Any(uii => uii.Property == Movie.PARENT_COLLECTION));
            var greedyUrls = e.Unhandled.SelectMany(arg => GetUrlsGreedy(arg.Uri)).Distinct().ToArray();
            var greedy = greedyUrls.Select(url => new RestRequestArgs(new Uri(url, UriKind.Relative)));
            var args = e.Unhandled.Concat(greedy);

            var trie = new Dictionary<string, List<(string Url, string Path, string Query, RestRequestArgs Arg)>>();
            //var trie1 = new Trie<string, (string Url, string Path, string Query, RestRequestArgs Arg)>();

            foreach (var arg in args)
            {
                //if (arg.Uri is UniformItemIdentifier uii && Resolver.TryGetRequest(uii, out var request))
                var url = Resolver.ResolveUrl(arg.Uri);
                var parts = url.Split('?');
                AddToTrie(trie, parts[0], (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg));

                //IEnumerable<string> keys = parts[0].Split('/');

                //if (parts.Length > 1)
                //{
                //    keys = keys.Append(parts[1]);
                //}

                //var value = (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg);
                //trie1.Add(value, keys.ToArray());
            }

            //IEnumerable<KeyValuePair<IEnumerable<string>, (string, string, string, RestRequestArgs)>> asdfasd = trie1;

            var item = e.AllArgs.Select(arg => arg.Uri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item as Item;
            var properties = e.Unhandled
                .Select(arg => arg.Uri)
                .OfType<UniformItemIdentifier>()
                .Select(uii => uii.Property);

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
                bool parentCollectionWasRequested = e.Unhandled
                    .Select(arg => arg.Uri)
                    .OfType<UniformItemIdentifier>()
                    .Select(uii => uii.Property)
                    .Contains(Movie.PARENT_COLLECTION);
                var response = await Datastore.ReadAsync(new TMDbResolver.DummyUri(url, item, parentCollectionWasRequested)
                {
                    RequestedProperties = properties
                });

                if (response?.TryGetRepresentation<IEnumerable<KeyValuePair<Uri, object>>>(out var collection) == true)
                {
                    e.HandleMany(collection);

                    foreach (var arg in e.Unhandled)
                    {
                        if (MultiRestEventArgs.TryGetValue(collection, arg.Uri, out var obj))
                        {
                            arg.Handle(State.Create(obj));
                        }
                    }
                }
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
