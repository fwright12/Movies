using REpresentationalStateTransfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Movies
{
    public class LateBoundResource : IResource
    {
        private Task<IResource> BindingDelay { get; }

        public LateBoundResource(Task<IResource> bindingDelay)
        {
            BindingDelay = bindingDelay;
        }

        public IEnumerable<Entity> Get(DateTime time) => (BindingDelay.IsCompleted ? BindingDelay.Result : Rest.EmptyResource).Get(time);
    }

    public class HttpRequestArgs : RestRequestArgs
    {
        public HttpRequestArgs(Uri uri, Type expected = null) : base(uri, expected)
        {
        }
    }

    public class HttpResponse : RestResponse
    {
        public Task BindingDelay { get; }
        public HttpStatusCode StatusCode => Message.StatusCode;

        private HttpResponseMessage Message { get; }

        public HttpResponse(HttpResponseMessage message, Type expected = null) : this(message, message.IsSuccessStatusCode ? BindLate(message.Content) : Task.FromResult(Rest.EmptyResource), expected) { }
        private HttpResponse(HttpResponseMessage message, Task<IResource> resource, Type expected = null) : base(new LateBoundResource(resource), new HttpHeaderDictionary(message.Headers), null, expected)
        {
            Message = message;
            BindingDelay = resource;
        }

        private static async Task<IResource> BindLate(HttpContent content)
        {
            var bytes = await content.ReadAsByteArrayAsync();

            return State.Create(bytes);
            //Add(Entity.Create(bytes));
            //Add(Entity.Create(Encoding.UTF8.GetString(bytes)));
        }

        private class HttpHeaderDictionary : IReadOnlyDictionary<string, IEnumerable<string>>
        {
            public HttpHeaders Headers { get; }

            public IEnumerable<string> Keys => Headers.Select(header => header.Key);
            public IEnumerable<IEnumerable<string>> Values => Headers.Select(header => header.Value);

            public int Count => Headers.Count();

            public IEnumerable<string> this[string key] => TryGetValue(key, out var values) ? values : throw new KeyNotFoundException();

            public HttpHeaderDictionary(HttpHeaders headers)
            {
                Headers = headers;
            }

            public bool ContainsKey(string key) => Headers.Contains(key);

            public bool TryGetValue(string key, out IEnumerable<string> value) => Headers.TryGetValues(key, out value);

            public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator() => Headers.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
