using Movies.Models;
using REpresentationalStateTransfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using static Movies.BatchHandler;

namespace Movies
{
    public class HttpProcessor : IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>>
    {
        public HttpMessageInvoker Invoker { get; }

        public HttpProcessor(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        public async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<Uri> e)
        {
            var response = await Invoker.SendAsync(ToMessage(e), default);

            e.Handle(new HttpResponse(response));
            return response.IsSuccessStatusCode;
        }

        public static HttpRequestMessage ToMessage(DatastoreKeyValueReadArgs<Uri> args)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, args.Key);

            if (args is RestRequestArgs restArgs)
            {
                foreach (var kvp in restArgs.RequestControlData)
                {
                    message.Headers.Add(kvp.Key, kvp.Value);
                }

                message.Content = new StringContent(restArgs.RequestRepresentation.Value);
            }

            return message;
        }
    }

    public abstract class TMDbProcessor : AsyncEventGroupProcessor<DatastoreKeyValueReadArgs<Uri>, DatastoreKeyValueReadArgs<Uri>>
    {
        public TMDbResolver Resolver { get; }

        public TMDbProcessor(IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> processor, TMDbResolver resolver) : base(new TrojanProcessor(processor))
        {
            Resolver = resolver;
        }

        protected override bool Handle(DatastoreKeyValueReadArgs<Uri> grouped, IEnumerable<DatastoreKeyValueReadArgs<Uri>> singles)
        {
            if (false == (grouped.Key as TMDbResolver.TrojanTMDbUri)?.Converter is HttpResourceCollectionConverter resources)
            {
                return false;
            }

            if (false == grouped.Response is RestResponse restResponse || !restResponse.TryGetRepresentation<IEnumerable<KeyValuePair<Uri, object>>>(out var collection))
            {
                collection = grouped.Response.RawValue as IEnumerable<KeyValuePair<Uri, object>>;
            }

            var index = collection?.ToReadOnlyDictionary() ?? new Dictionary<Uri, object>();

            foreach (var request in singles)
            {
                var response = grouped.Response;

                if (collection.TryGetValue(request.Key, out var value))
                {
                    if (value is IEnumerable<Entity> entities)
                    {
                        response = new RestResponse(entities)
                        {
                            Expected = request.Expected
                        };
                    }
                    else
                    {
                        response = new DatastoreResponse<object>(value);
                    }
                }

                request.Handle(response);
            }

            return true;
        }

        protected override IEnumerable<KeyValuePair<DatastoreKeyValueReadArgs<Uri>, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupRequests(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args)
        {
            var item = args.Select(arg => arg.Key).OfType<UniformItemIdentifier>().Select(uii => uii.Item).FirstOrDefault(item => item != null);

            foreach (var kvp in GroupUrls(args))
            {
                var requests = kvp.Value.ToArray();
                var uri = GetUri(item, kvp.Key, requests);
                IEnumerable<DatastoreKeyValueReadArgs<Uri>> grouped = requests;

                if (uri.Converter is HttpResourceCollectionConverter converter && args is BatchDatastoreArgs<DatastoreKeyValueReadArgs<Uri>> batch)
                {
                    var index = requests.ToDictionary(req => req.Key, req => req);

                    foreach (var resource in converter.Resources)
                    {
                        if (!index.ContainsKey(resource))
                        {
                            var request = new DatastoreKeyValueReadArgs<Uri>(resource);

                            index.Add(resource, request);
                            batch.AddRequest(request);
                        }
                    }

                    grouped = index.Values;
                }

                yield return new KeyValuePair<DatastoreKeyValueReadArgs<Uri>, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(new DatastoreKeyValueReadArgs<Uri>(uri), grouped);
            }
        }

        protected virtual IEnumerable<KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupUrls(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args) => args.Select(arg => new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(Resolver.ResolveUrl(arg.Key), arg.AsEnumerable()));

        private TMDbResolver.TrojanTMDbUri GetUri(Item item, string url, DatastoreKeyValueReadArgs<Uri>[] requests)
        {
            var properties = requests
                    .Select(arg => arg.Key)
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

            return uri;
        }

        private class TrojanProcessor : IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>>
        {
            public IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> Processor { get; }

            public TrojanProcessor(IAsyncEventProcessor<DatastoreKeyValueReadArgs<Uri>> processor)
            {
                Processor = processor;
            }

            public async Task<bool> ProcessAsync(DatastoreKeyValueReadArgs<Uri> e)
            {
                var response = await Processor.ProcessAsync(e);

                if (e.Response is HttpResponse httpResponse && e.Key is TMDbResolver.TrojanTMDbUri trojan)
                {
                    await Task.WhenAll(httpResponse.BindingDelay, httpResponse.Add(trojan.Converter));
                }

                return response;
            }
        }
    }

    public class TMDbHttpProcessor : TMDbProcessor
    {
        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbHttpProcessor(HttpMessageHandler handler, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new HttpMessageInvoker(handler), resolver, autoAppend) { }
        //public TMDbHttpProcessor(HttpMessageInvoker client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(new DatastoreProcessor(new TMDbRemoteDatastore(client, resolver)), resolver) //: this(new HttpDatastore(client), resolver, autoAppend) { }
        //public TMDbReadHandler(IDataStore<Uri, State> datastore, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(datastore)
        public TMDbHttpProcessor(HttpMessageInvoker client, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : base(new HttpProcessor(client), resolver)
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

        protected override IEnumerable<KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>> GroupUrls(IEnumerable<DatastoreKeyValueReadArgs<Uri>> args)
        {
            var trie = new Dictionary<string, List<(string Url, string Path, string Query, DatastoreKeyValueReadArgs<Uri> Arg)>>();

            foreach (var request in args)
            {
                var values = GetUrlsGreedy(request.Key).OfType<object>().Prepend(request);

                foreach (var value in values)
                {
                    var arg = value as DatastoreKeyValueReadArgs<Uri>;
                    var url = value as string ?? Resolver.ResolveUrl(arg.Key);
                    var parts = url.Split('?');
                    AddToTrie(trie, parts[0], (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg));
                }
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

                yield return new KeyValuePair<string, IEnumerable<DatastoreKeyValueReadArgs<Uri>>>(url, kvp.Value.Select(value => value.Arg).Where(arg => arg != null));
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
