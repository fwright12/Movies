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

        private static readonly string APPEND_TO_RESPONSE = "append_to_response";

        public TMDbRemoteDatastore(HttpMessageInvoker invoker, TMDbResolver resolver) : base(invoker)
        {
            Resolver = resolver;
        }

        protected override bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
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

            if (data.Count == 0 || uri is DummyUri dummyUri == false)
            {
                resource = default;
                return false;
            }
            else
            {
                resource = new Converter(dummyUri.Item, data);
                return true;
            }
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

        public class DummyUri : Uri
        {
            public Item Item { get; }

            public DummyUri(string url, Item item) : base(url, UriKind.Relative)
            {
                Item = item;
            }
        }
    }

    public class TMDbReadHandler : DatastoreReadHandler
    {
        private TMDbResolver Resolver { get; }

        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbReadHandler(HttpMessageHandler handler, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new HttpMessageInvoker(handler), resolver, autoAppend) { }
        public TMDbReadHandler(HttpMessageInvoker client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(new HttpDatastore(client)) //: this(new HttpDatastore(client), resolver, autoAppend) { }
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
                var item = e.AllArgs.Select(arg => arg.Uri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item as Item;
                var response = await Datastore.ReadAsync(new Uri(url, UriKind.Relative));

                

                if (response.TryGetRepresentation<byte[]>(out var content))
                {
                    //var json = Parse(response.Content, validPaths);
                    var json = new AnnotatedJson(content);

                    var values = Enumerable.Range(0, kvp.Value.Count).Select(i => (kvp.Value[i].Url, paths[i])).Distinct();

                    foreach (var value in values)
                    {
                        var uri = value.Url;
                        var path = value.Item2;
                        var temp = string.IsNullOrEmpty(path) ? new string[0] : new string[] { path };

                        json.Add(new Uri(uri, UriKind.Relative), temp);
                        //json.Add(kvp.Value[i].Arg.Uri, temp);

                        if (item != null)
                        {
                            foreach (var annotation in Resolver.Annotate(uri, temp))
                            {
                                var uii = new UniformItemIdentifier(item, annotation.Key);
                                json.Add(uii, annotation.Value);
                            }
                        }
                    }

                    //var itr = kvp.Value.GetEnumerator();

                    e.HandleMany(json);
                    var states = new Dictionary<Uri, State>();

                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        var value = kvp.Value[i];
                        var path = paths[i];

                        if (value.Arg.Response == null && json.TryGetValue(value.Arg.Uri, out var bytes))
                        {
                            value.Arg.Handle(bytes);
                        }

                        if (!value.Arg.Handled && value.Arg.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(value.Arg.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                        {
                            //value.Arg.Handle(new AppendedContent(json, path));
                            //value.Arg.Response.Add<string, byte[]>(ByteConverter);
                            value.Arg.Handle(converter);
                        }

                        if (value.Arg.Response != null)
                        {
                            states.Add(value.Arg.Uri, value.Arg.Response);
                        }
                    }

                    //e.HandleMany(states);
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

    public class TMDbClient : ControllerLink//<HttpContent>
    {
        public HttpMessageInvoker Client { get; }
        private TMDbResolver Resolver { get; }

        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbClient(HttpMessageHandler handler, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new HttpMessageInvoker(handler), resolver, autoAppend) { }
        public TMDbClient(HttpMessageInvoker client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null)
        {
            Client = client;
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

        public Task GetAsync(MultiRestEventArgs e)
        {
            throw new NotImplementedException();
        }

        public Task PutAsync(MultiRestEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override async Task GetInternalAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) //=> HandleAsync(e, next, async (IEnumerable<RestRequestArgs> e) =>
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
                var response = await Client.TrySendAsync(url);

                if (response?.IsSuccessStatusCode == true)
                {
                    //var json = Parse(response.Content, validPaths);
                    var json = new AnnotatedJson(await response.Content.ReadAsByteArrayAsync());
                    var item = e.AllArgs.Select(arg => arg.Uri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item as Item;

                    var values = Enumerable.Range(0, kvp.Value.Count).Select(i => (kvp.Value[i].Url, paths[i])).Distinct();

                    foreach (var value in values)
                    {
                        var uri = value.Url;
                        var path = value.Item2;
                        var temp = string.IsNullOrEmpty(path) ? new string[0] : new string[] { path };

                        json.Add(new Uri(uri, UriKind.Relative), temp);
                        //json.Add(kvp.Value[i].Arg.Uri, temp);

                        if (item != null)
                        {
                            foreach (var annotation in Resolver.Annotate(uri, temp))
                            {
                                var uii = new UniformItemIdentifier(item, annotation.Key);
                                json.Add(uii, annotation.Value);
                            }
                        }
                    }

                    //var itr = kvp.Value.GetEnumerator();

                    e.HandleMany(json);
                    var states = new Dictionary<Uri, State>();

                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        var value = kvp.Value[i];
                        var path = paths[i];

                        if (value.Arg.Response == null && json.TryGetValue(value.Arg.Uri, out var bytes))
                        {
                            value.Arg.Handle(bytes);
                        }

                        if (!value.Arg.Handled && value.Arg.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(value.Arg.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                        {
                            //value.Arg.Handle(new AppendedContent(json, path));
                            //value.Arg.Response.Add<string, byte[]>(ByteConverter);
                            value.Arg.Handle(converter);
                        }

                        if (value.Arg.Response != null)
                        {
                            states.Add(value.Arg.Uri, value.Arg.Response);
                        }
                    }

                    //e.HandleMany(states);
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
