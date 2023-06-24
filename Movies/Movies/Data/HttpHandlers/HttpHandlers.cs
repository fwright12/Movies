using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
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

    public abstract class ReroutingHandler : DelegatingHandler
    {
        public ReroutingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        public Dictionary<Uri, HttpResource> Types { get; }



        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri = Reroute(request.RequestUri);
            return base.SendAsync(request, cancellationToken);
        }

        protected virtual Uri Reroute(Uri uri) => uri;
    }

    public class ResourceMap
    {
        public Dictionary<Uri, HttpResource> Types { get; }

        public virtual bool TryGetResource(Uri uri, out HttpResource resource) => Types.TryGetValue(uri, out resource);
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