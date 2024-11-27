using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class TMDbHttpProcessor : TMDbProcessor
    {
        private Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> Appendable { get; }
        private Dictionary<TMDbRequest, TMDbRequest> AppendsTo { get; }

        public TMDbHttpProcessor(HttpMessageHandler handler, TMDbResolver resolver, Dictionary<TMDbRequest, IEnumerable<TMDbRequest>> autoAppend = null) : this(new HttpMessageInvoker(handler), resolver, autoAppend) { }

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

        protected override IEnumerable<KeyValuePair<string, IEnumerable<KeyValueRequestArgs<Uri>>>> GroupUrls(IEnumerable<KeyValueRequestArgs<Uri>> args)
        {
            var trie = new Dictionary<string, List<(string Url, string Path, string Query, KeyValueRequestArgs<Uri> Arg)>>();
            if (args.All(arg => arg.IsHandled && (arg.Response as RestResponse)?.ControlData.TryGetValue(REpresentationalStateTransfer.Rest.ETAG, out _) != true))
            {
                yield break;
            }

            foreach (var request in args)
            {
                var values = GetUrlsGreedy(request.Request.Key).OfType<object>().Prepend(request);

                foreach (var value in values)
                {
                    var arg = value as KeyValueRequestArgs<Uri>;
                    var url = value as string ?? Resolver.ResolveUrl(arg.Request.Key);
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

                yield return new KeyValuePair<string, IEnumerable<KeyValueRequestArgs<Uri>>>(url, kvp.Value.Select(value => value.Arg).Where(arg => arg != null));
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
