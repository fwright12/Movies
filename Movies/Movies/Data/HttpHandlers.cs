using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static Xamarin.Essentials.AppleSignInAuthenticator;

namespace Movies
{
    public class LazyJsonContent : HttpContent
    {
        public AnnotatedJson Json { get; }
        public Uri Uri { get; }

        public LazyJsonContent(AnnotatedJson json, Uri uri)
        {
            Json = json;
            Uri = uri;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (Json.TryGetValue(Uri, out var bytes))
            {
                return stream.WriteAsync(bytes).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (Json.TryGetValue(Uri, out var bytes))
            {
                length = bytes.Count;
                return true;
            }
            else
            {
                length = default;
                return false;
            }
        }
    }

    public class ObjectContent : HttpContent
    {
        public State Value { get; }

        public ObjectContent(object value) : this(new State(value)) { }

        public ObjectContent(State value)
        {
            Value = value;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            stream.Write(GetBytes());
            return Task.CompletedTask;
        }

        protected override bool TryComputeLength(out long length)
        {
            throw new NotImplementedException();
        }

        private byte[] GetBytes()
        {
            if (Value.TryGetRepresentation<byte[]>(out var bytes))
            {
                return bytes;
            }
            else
            {
                if (!Value.TryGetRepresentation<string>(out var str))
                {
                    str = JsonSerializer.Serialize(Value.OfType<object>().First());
                }

                return Encoding.UTF8.GetBytes(str);
            }
        }
    }

    public class ControllerHandler : DelegatingHandler
    {
        protected Task<HttpResponseMessage> NextAsync(HttpRequestMessage request, CancellationToken cancellationToken) => base.SendAsync(request, cancellationToken);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get) return GetAsync(request, cancellationToken);
            if (request.Method == HttpMethod.Put) return PutAsync(request, cancellationToken);
            else return NextAsync(request, cancellationToken);
        }

        protected virtual Task<HttpResponseMessage> GetAsync(HttpRequestMessage request, CancellationToken cancellationToken) => NextAsync(request, cancellationToken);
        protected virtual Task<HttpResponseMessage> PutAsync(HttpRequestMessage request, CancellationToken cancellationToken) => NextAsync(request, cancellationToken);
    }

    public static class HttpExtensions
    {
        public static HttpMessageHandler BuildHandlerChain(HttpMessageHandler final, params DelegatingHandler[] handlers)
        {
            for (int i = 1; i < handlers.Length; i++)
            {
                handlers[i - 1].InnerHandler = handlers[i];
            }

            HttpMessageHandler start;

            if (handlers.Length == 0)
            {
                start = final;
            }
            else
            {
                start = handlers[0];
                handlers[handlers.Length - 1].InnerHandler = final;
            }

            return start;
        }

        public static Task<HttpResponseMessage>[] SendAsync(this HttpMessageInvoker invoker, IEnumerable<HttpRequestMessage> requests) => SendAsync(invoker, requests, default);
        public static Task<HttpResponseMessage>[] SendAsync(this HttpMessageInvoker invoker, IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken) => BatchHandler.SendAsync(invoker, requests, cancellationToken);

        public static Task<HttpResponseMessage[]> SendAsync(BatchHandler invoker, IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken, out Guid batchId)
        {
            batchId = Guid.NewGuid();
            var tasks = new List<Task<HttpResponseMessage>>();
            var itr = requests.GetEnumerator();

            if (itr.MoveNext())
            {
                while (true)
                {
                    var request = itr.Current;
                    var value = batchId.ToString();

                    if (!itr.MoveNext())
                    {
                        value += " final";
                    }

                    request.Headers.Add(BatchHandler.BATCH_HEADER, value);
                    //tasks.Add(invoker.SendAsync(request, cancellationToken));
                }
            }

            return Task.WhenAll(tasks);
        }
    }

    public class BatchEventArgs : EventArgs
    {
        public string Id { get; }

        public BatchEventArgs(string id)
        {
            Id = id;
        }
    }

    public class BatchHandler : DelegatingHandler
    {
        public static event EventHandler<BatchEventArgs> Received;
        private static event EventHandler<BatchEventArgs> BatchEnded;

        public const string BATCH_HEADER = "Batch";

        private Dictionary<string, (TaskCompletionSource<HttpResponseMessage[]> Source, List<HttpRequestMessage> Requests)> Batches = new Dictionary<string, (TaskCompletionSource<HttpResponseMessage[]> Source, List<HttpRequestMessage> Requests)>();

        protected BatchHandler()
        {
            Init();
        }

        protected BatchHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            Init();
        }

        private void Init()
        {
            BatchEnded += HandleBatchEnded;
        }

        private void HandleBatchEnded(object sender, BatchEventArgs e)
        {
            BatchEnd(e.Id);
        }

        public async void BatchEnd(string batchId)
        {
            if (!Batches.Remove(batchId, out var value))
            {
                return;
            }

            var responses = await Task.WhenAll(SendAsync(value.Requests, default));
            value.Source.SetResult(responses);

            Received?.Invoke(this, new BatchEventArgs(batchId));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues(BATCH_HEADER, out var values))
            {
                var batchId = values.FirstOrDefault();

                if (batchId != null)
                {
                    if (!Batches.TryGetValue(batchId, out var batch))
                    {
                        Batches.Add(batchId, batch = (new TaskCompletionSource<HttpResponseMessage[]>(), new List<HttpRequestMessage>()));
                    }

                    batch.Requests.Add(request);
                    Received?.Invoke(this, new BatchEventArgs(batchId));
                    return Get(batch.Source.Task, batch.Requests.Count - 1);
                }

                request.Headers.Remove(BATCH_HEADER);
            }

            return base.SendAsync(request, cancellationToken);
        }

        private static void OnBatchEnded(Guid batchId)
        {
            BatchEnded?.Invoke(null, new BatchEventArgs(batchId.ToString()));
        }

        public static Task<HttpResponseMessage>[] SendAsync(HttpMessageInvoker invoker, IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken)
        {
            var batch = new Batch(requests);
            var responses = requests.Select(request => invoker.SendAsync(request, cancellationToken)).ToArray();

            return batch.SignalWhenReady(responses);
        }

        public class Batch
        {
            public Guid BatchId { get; }
            public IEnumerable<Task<HttpResponseMessage>> Responses { get; private set; }

            private int Remaining;

            public Batch(IEnumerable<HttpRequestMessage> requests)
            {
                BatchId = Guid.NewGuid();
                var value = BatchId.ToString();

                foreach (var request in requests)
                {
                    request.Headers.Add(BatchHandler.BATCH_HEADER, value);
                }

                Received += HandleReceived;
            }

            public Task<HttpResponseMessage>[] SignalWhenReady(IEnumerable<Task<HttpResponseMessage>> responses)
            {
                Responses = responses;
                HandleReceived(responses.Count());

                Await();
                return responses.ToArray();
            }

            private void HandleReceived(object sender, BatchEventArgs e)
            {
                if (e.Id == BatchId.ToString())
                {
                    HandleReceived(-1);
                }
            }

            private void HandleReceived(int count)
            {
                Remaining += count;

                if (Remaining == 0)
                {
                    OnBatchEnded(BatchId);
                }
            }

            private async void Await()
            {
                while (Remaining > 0)
                {
                    await Task.WhenAny(Responses);
                    HandleReceived(-1);
                }
            }
        }

        protected internal virtual Task<HttpResponseMessage>[] SendAsync(IEnumerable<HttpRequestMessage> requests, CancellationToken cancellationToken)
        {
            var batch = new Batch(requests);
            var responses = requests.Select(request => base.SendAsync(request, cancellationToken));

            return batch.SignalWhenReady(responses);
        }

        private async Task<T> Get<T>(Task<T[]> list, int index) => (await list)[index];
    }

    public class BufferedHandler : DelegatingHandler
    {
        private Dictionary<Uri, Task<HttpResponseMessage>> Buffer = new Dictionary<Uri, Task<HttpResponseMessage>>();

        public BufferedHandler() : base() { }
        public BufferedHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (Buffer.TryGetValue(uri, out var buffered))
            {
                return RespondBuffered(buffered);
            }

            var response = base.SendAsync(request, cancellationToken);
            Buffer.Add(uri, response);
            return response;
        }

        private async Task<HttpResponseMessage> RespondBuffered(Task<HttpResponseMessage> buffered)
        {
            var response = await buffered;
            var request = response.RequestMessage;
            HttpContent content;

            if (request.Method == HttpMethod.Get)
            {
                content = response.Content;
            }
            else
            {
                content = request.Content;
            }

            return new HttpResponseMessage(response.StatusCode)
            {
                Content = content,
            };
        }
    }

    public interface IConvertTo<out T>
    {
        T Convert(object value);
    }

    public interface IConvertFrom<in T>
    {
        object Convert(T value);
    }

    public interface IHttpConverter<T>
    {
        Task<T> Convert(HttpContent content);
    }

    public class Resource
    {
        public Type Type { get; }
        public Task Task { get; }
        //public IHttpConverter<object> Converter { get; }

        public Resource(Type type, IHttpConverter<object> converter)
        {
            Type = type;
        }

        public Resource(Type type, Task task, IHttpConverter<object> converter)
        {
            Type = type;
            Task = task;
        }

        public static Resource Create<T>(IHttpConverter<T> converter) => null;

        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
    }

    public class Resource<T> : Resource
    {
        new public Task<T> Task { get; }

        public Resource(Type type, IHttpConverter<object> converter) : base(type, converter)
        {
            
        }

        public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();
    }

    public abstract class HttpResourceCollectionConverter : IHttpConverter<IReadOnlyDictionary<Uri, object>>, IHttpConverter<object>
    {
        public abstract IEnumerable<Uri> Resources { get; }

        public abstract Task<IReadOnlyDictionary<Uri, object>> Convert(HttpContent content);

        async Task<object> IHttpConverter<object>.Convert(HttpContent content) => await Convert(content);
    }

    public class HttpJsonCollectionConverter<TDom> : HttpResourceCollectionConverter
    {
        public JsonCollectionConverter<TDom> Converter { get; }
        public JsonSerializerOptions Options { get; }

        public override IEnumerable<Uri> Resources => Converter.Parsers.Keys;

        public HttpJsonCollectionConverter(JsonCollectionConverter<TDom> converter, JsonSerializerOptions options = null)
        {
            Converter = converter;
            Options = options ?? new JsonSerializerOptions();
        }

        public override async Task<IReadOnlyDictionary<Uri, object>> Convert(HttpContent content) => Converter.Read(await content.ReadAsByteArrayAsync(), typeof(IReadOnlyDictionary<Uri, object>), Options);
    }

    public interface IJsonDomExtractor<TDom, out TValue>
    {
        TValue Extract(TDom dom);
    }

    public class JsonDomExtractorFunc<TDom, TValue> : IJsonDomExtractor<TDom, TValue>
    {
        public Func<TDom, TValue> Func { get; }

        public JsonDomExtractorFunc(Func<TDom, TValue> func)
        {
            Func = func;
        }

        public TValue Extract(TDom dom) => Func.Invoke(dom);
    }

    public class JsonDomConverterExtractor<T> : IJsonDomExtractor<JsonIndex, T>
    {
        public JsonConverter<T> Converter { get; }
        public JsonSerializerOptions Options { get; }

        public JsonDomConverterExtractor(JsonConverter<T> converter, JsonSerializerOptions options = null)
        {
            Converter = converter;
            Options = options;
        }

        public T Extract(JsonIndex dom)
        {
            var reader = new Utf8JsonReader(dom.Bytes);
            return Converter.Read(ref reader, typeof(T), Options);
        }
    }

    public class JsonDomPropertyExtractor<TDom, TValue> : IJsonDomExtractor<TDom, TValue>
    {
        public JsonDomType<TDom> DomType { get; }
        public IJsonDomExtractor<TDom, TValue> Inner { get; }
        public string PropertyName { get; }

        public TValue Extract(TDom dom)
        {
            if (DomType.TryGetValue(dom, PropertyName, out var inner))
            {
                return Inner.Extract(inner);
            }
            else
            {
                return default;
            }
        }
    }

    public class JsonCollectionConverter<TDom> : JsonConverter<IReadOnlyDictionary<Uri, object>>
    {
        public JsonDomType<TDom> DomType { get; }

        public Dictionary<Uri, IJsonDomExtractor<TDom, object>> Parsers { get; }

        public JsonCollectionConverter(JsonDomType<TDom> domType)
        {
            DomType = domType;
        }

        public virtual IReadOnlyDictionary<Uri, object> Read(ReadOnlySpan<byte> json, Type typeToConvert, JsonSerializerOptions options) => Convert(DomType.Parse(json));

        public override IReadOnlyDictionary<Uri, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Convert(DomType.Parse(ref reader));

        protected virtual IReadOnlyDictionary<Uri, object> Convert(TDom dom) => Parsers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Extract(dom));

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<Uri, object> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class LazyJsonCollectionConverter<TDom> : JsonCollectionConverter<TDom>
    {
        public LazyJsonCollectionConverter(JsonDomType<TDom> domType) : base(domType)
        {
        }

        protected override IReadOnlyDictionary<Uri, object> Convert(TDom dom) => new Wrapper(dom, Parsers);

        private class Wrapper : ReadOnlyDictionaryTransform<KeyValuePair<Uri, IJsonDomExtractor<TDom, object>>, Uri, object>
        {
            public TDom Dom { get; }

            public Wrapper(TDom dom, IEnumerable<KeyValuePair<Uri, IJsonDomExtractor<TDom, object>>> source) : base(source)
            {
                Dom = dom;
            }

            public override Uri KeySelector(KeyValuePair<Uri, IJsonDomExtractor<TDom, object>> source) => source.Key;

            public override object ValueSelector(KeyValuePair<Uri, IJsonDomExtractor<TDom, object>> source) => source.Value.Extract(Dom);
        }
    }

    public abstract class ReadOnlyDictionaryTransform<TSource, TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        public TValue this[TKey key]
        {
            get
            {
                if (Cache.TryGetValue(key, out var value))
                {
                    return value;
                }

                TSource source;

                if (Source is IReadOnlyDictionary<TKey, TSource> dict)
                {
                    source = dict[key];
                }
                else
                {
                    foreach (var a in Source)
                    {
                        if (KeySelector(a).Equals(key))
                        {
                            source = a;
                        }
                    }

                    throw new KeyNotFoundException();
                }

                value = ValueSelector(source);
                Cache.TryAdd(key, value);

                return value;
            }
        }

        public IEnumerable<TSource> Source { get; }

        public IEnumerable<TKey> Keys => throw new NotImplementedException();

        public IEnumerable<TValue> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        private Dictionary<TKey, TValue> Cache { get; }

        protected ReadOnlyDictionaryTransform(IEnumerable<TSource> source)
        {
            Cache = new Dictionary<TKey, TValue>();
            Source = source;
        }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public abstract TKey KeySelector(TSource source);

        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        public abstract TValue ValueSelector(TSource source);

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public static class JsonDom
    {
        public static readonly JsonDomType<JsonIndex> JsonIndex = new JsonIndexDom();

        private class JsonIndexDom : JsonDomType<JsonIndex>
        {
            public override JsonIndex Parse(ReadOnlySpan<byte> json) => new JsonIndex(json.ToArray());

            public override JsonIndex Parse(Stream json)
            {
                throw new NotImplementedException();
            }

            public override JsonIndex Parse(string json) => new JsonIndex(Encoding.UTF8.GetBytes(json));

            public override JsonIndex Parse(ref Utf8JsonReader reader)
            {
                reader.Read();
                reader.Skip();
                return new JsonIndex(reader.ValueSpan.ToArray());
            }

            public override bool TryGetValue(JsonIndex dom, string propertyName, out JsonIndex value) => dom.TryGetValue(propertyName, out value);
        }
    }

    public abstract class JsonDomType<T>
    {
        public abstract T Parse(ReadOnlySpan<byte> json);
        public abstract T Parse(Stream json);
        public abstract T Parse(string json);
        public abstract T Parse(ref Utf8JsonReader reader);
        public abstract bool TryGetValue(T dom, string propertyName, out T value);
    }

    public abstract class ReroutingHandler : DelegatingHandler
    {
        public ReroutingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        public Dictionary<Uri, Resource> Types { get; }



        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri = Reroute(request.RequestUri);
            return base.SendAsync(request, cancellationToken);
        }

        protected virtual Uri Reroute(Uri uri) => uri;
    }

    public class ResourceMap
    {
        public Dictionary<Uri, Resource> Types { get; }

        public virtual bool TryGetResource(Uri uri, out Resource resource) => Types.TryGetValue(uri, out resource);
    }

    public class CacheHandler : DelegatingHandler
    {
        public HttpMessageInvoker Invoker { get; }
        public ResourceMap Resources { get; }

        public CacheHandler(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        public CacheHandler(HttpMessageHandler handler) : this(new HttpMessageInvoker(handler)) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await Invoker.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var uri = request.RequestUri;
            var next = base.SendAsync(request, cancellationToken);

            //if (Invoker is HttpMessageConverter converter && converter.Types.TryGetValue(request.RequestUri, out var type) && type.Converter is HttpResourceCollectionConverter collection)
            if (Resources?.TryGetResource(request.RequestUri, out var type) == true && (object)type is HttpResourceCollectionConverter collection)
            {
                var resources = GetCollectionAsync(collection, next);

                foreach (var resource in collection.Resources)
                {
                    _ = Invoker.SendAsync(new HttpRequestMessage(HttpMethod.Put, resource)
                    {
                        Content = new HttpContentWrapper(ChildResource(resources, resource))
                    }, default);
                }

                if (!uri.Equals(request.RequestUri))
                {
                    return response;
                }
            }
            else
            {
                _ = Invoker.SendAsync(new HttpRequestMessage(HttpMethod.Put, request.RequestUri)
                {
                    Content = new HttpContentWrapper(next)
                }, default);
            }

            return await next;
        }

        private static async Task<IReadOnlyDictionary<Uri, object>> GetCollectionAsync(IHttpConverter<IReadOnlyDictionary<Uri, object>> collection, Task<HttpResponseMessage> response) => await collection.Convert((await response).Content);

        private static async Task<HttpContent> ChildResource(Task<IReadOnlyDictionary<Uri, object>> parent, Uri uri) => (await parent).TryGetValue(uri, out var resource) ? new ObjectContent(resource) : null;

        private static async Task<HttpContent> Wrapper(Task<HttpResponseMessage> response) => (await response).Content;

        private static async Task<HttpContent> Wrapper(Uri uri, Task<HttpContent> content, IConverter<HttpContent, Dictionary<Uri, HttpContent>> converter)
        {
            var temp = await content;
            var resources = converter.Convert(temp);

            return resources[uri];
        }

        private class HttpContentWrapper : HttpContent
        {
            private Task<HttpContent> Content { get; }

            public HttpContentWrapper(Task<HttpResponseMessage> response) : this(Wrapper(response)) { }
            public HttpContentWrapper(Task<HttpContent> response)
            {
                Content = response;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) => await (await Content).CopyToAsync(stream);

            protected override bool TryComputeLength(out long length)
            {
                length = default;
                return false;
            }
        }
    }

    public sealed class ChainEndHandler : HttpMessageHandler
    {
        public static ChainEndHandler Instance => _Instance ??= new ChainEndHandler();
        private static ChainEndHandler _Instance;

        private ChainEndHandler() { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    public class MessageChainHandler : HttpMessageInvoker
    {
        public MessageChainHandler Next { get; set; }

        private BatchHandler BatchHandler { get; }

        public MessageChainHandler(HttpMessageHandler first, params HttpMessageHandler[] rest) : this(first)
        {

        }

        private static HttpMessageHandler Chain(params HttpMessageHandler[] handlers)
        {
            var first = new CacheHandler(handlers[0]);

            for (int i = 1; i < handlers.Length; i++)
            {
                first.InnerHandler = new CacheHandler(handlers[i]);
            }

            return first;
        }

        public MessageChainHandler(HttpMessageHandler final, params DelegatingHandler[] handlers) : this(handlers.Length == 0 ? final : handlers[0])
        {
            for (int i = 1; i < handlers.Length; i++)
            {
                handlers[i - 1].InnerHandler = handlers[i];
            }

            HttpMessageHandler start;

            if (handlers.Length == 0)
            {
                start = final;
            }
            else
            {
                start = handlers[0];
                handlers[handlers.Length - 1].InnerHandler = final;
            }
        }

        public MessageChainHandler(HttpMessageHandler handler) : base(handler)
        {
            var next = handler;

            do
            {
                BatchHandler = next as BatchHandler;
                next = (next as DelegatingHandler)?.InnerHandler;
            }
            while (BatchHandler == null && next != null);
        }
    }

    public class InvokerHandlerWrapper : DelegatingHandler
    {
        public HttpMessageInvoker Invoker { get; }

        public InvokerHandlerWrapper(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Invoker.SendAsync(request, cancellationToken);
    }
}