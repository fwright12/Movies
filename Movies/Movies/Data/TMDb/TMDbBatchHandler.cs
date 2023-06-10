using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TrieValue = System.Collections.Generic.List<(int Index, string Url, string Path, string Query, System.Net.Http.HttpRequestMessage Arg)>;

namespace Movies
{
    public class TMDbBatchHandler : BatchHandler
    {
        private TMDbResolver Resolver { get; }

        //private static readonly ByteConverter ByteConverter = new ByteConverter(Encoding.UTF8);

        public TMDbBatchHandler(TMDbResolver resolver) : this(null, resolver) { }

        public TMDbBatchHandler(HttpMessageHandler handler, TMDbResolver resolver) : base(handler)
        {
            Resolver = resolver;
        }

        protected internal override Task<HttpResponseMessage>[] SendAsync(IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken)
        {
            var trie = new Dictionary<string, TrieValue>();
            int i = 0;

            for (var itr = requests.GetEnumerator(); itr.MoveNext(); i++)
            {
                //if (arg.Uri is UniformItemIdentifier uii && Resolver.TryGetRequest(uii, out var request))
                var request = itr.Current;
                var url = Resolver.ResolveUrl(request.RequestUri);
                var parts = url.Split('?');
                AddToTrie(trie, parts[0], (i, url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, request));

                //IEnumerable<string> keys = parts[0].Split('/');

                //if (parts.Length > 1)
                //{
                //    keys = keys.Append(parts[1]);
                //}

                //var value = (url, parts[0], parts.Length > 1 ? parts[1] : string.Empty, arg);
                //trie1.Add(value, keys.ToArray());
            }

            //IEnumerable<KeyValuePair<IEnumerable<string>, (string, string, string, RestRequestArgs)>> asdfasd = trie1;

            var results = new Task<HttpResponseMessage>[i];
            var item = requests.Select(arg => arg.RequestUri).OfType<UniformItemIdentifier>().FirstOrDefault()?.Item as Item;

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

                var json = GetResponse(item, url, kvp, paths);

                foreach (var value in kvp.Value)
                {
                    results[value.Index] = ToHttpResponseMessage(json, value.Arg.RequestUri);
                }
                //e.Handle(kvp.Value.ToDictionary(info => info.Arg.Uri, info => Handle(json, info)));
            }

            return results;
        }

        private async Task<HttpResponseMessage> ToHttpResponseMessage(Task<AnnotatedJson> task, Uri uri)
        {
            var json = await task;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new LazyJsonContent(json, uri)
            };
        }

        private async Task<AnnotatedJson> GetResponse(Item item, string url, KeyValuePair<string, TrieValue> kvp, string[] paths)
        {
            var response = await base.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), default);

            if (response?.IsSuccessStatusCode == false)
            {
                return null;
            }

            var json = new AnnotatedJson(await response.Content.ReadAsByteArrayAsync());
            var values = Enumerable.Range(0, kvp.Value.Count).Select(i => (kvp.Value[i].Url, paths[i])).Distinct();

            foreach (var value in values)
            {
                var uri = value.Url;
                var path = value.Item2;
                var temp = string.IsNullOrEmpty(path) ? new string[0] : new string[] { path };

                json.Add(new Uri(uri, UriKind.Relative), temp);
                //json.Add(kvp.Value[i].Arg.Uri, temp);

                foreach (var annotation in Resolver.Annotate(uri, temp))
                {
                    var uii = new UniformItemIdentifier(item, annotation.Key);
                    json.Add(uii, annotation.Value);
                }
            }

            return json;
        }

        private async Task<ArraySegment<byte>> Handle(Task<AnnotatedJson> jsonTask, (string Url, string Path, string Query, Movies.RestRequestArgs Arg) value)
        {
            var json = await jsonTask;

            if (json.TryGetValue(value.Arg.Uri, out var bytes))
            {
                if (value.Arg.Response == null)
                {
                    value.Arg.Handle(bytes);
                }

                if (!value.Arg.Handled && value.Arg.Response?.TryGetRepresentation<ArraySegment<byte>>(out var temp) == true && await Resolver.GetConverter(value.Arg.Uri, temp) is IConverter<ArraySegment<byte>> converter)
                {
                    //value.Arg.Handle(new AppendedContent(json, path));
                    //value.Arg.Response.Add<string, byte[]>(ByteConverter);
                    value.Arg.Handle(converter);
                }

                return bytes;
            }

            return default;
        }

        private void AddToTrie<TValue>(Dictionary<string, List<TValue>> trie, string key, TValue value)
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
