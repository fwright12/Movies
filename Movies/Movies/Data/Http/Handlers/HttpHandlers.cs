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

    public sealed class ChainEndHandler : HttpMessageHandler
    {
        public static ChainEndHandler Instance => _Instance ??= new ChainEndHandler();
        private static ChainEndHandler _Instance;

        private ChainEndHandler() { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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