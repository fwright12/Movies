using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class MultiRestArgs : AsyncChainEventArgs
    {
        public IEnumerable<RestArgs> Requests { get; }

        public MultiRestArgs(IEnumerable<RestArgs> requests)
        {
            Requests = requests;
        }
    }

    public class RestArgs : AsyncChainEventArgs
    {
        public RestRequest Request { get; }

        public Task<RestResponse> Response { get; private set; }
        public HttpResource Resource { get; private set; }

        public RestArgs(RestRequest request)
        {
            Request = request;
        }

        public void Handle(State state)
        {
            Handle();
        }

        public bool Handle<T>(Task<T> value) => Handle(new HttpResource<T>(value));

        public bool Handle(HttpResource resource)
        {
            Resource = resource;
            Handle();
            return true;
        }
    }

    public class RestRequest
    {
        public HttpMethod Method { get; }
        public Uri Uri { get; }

        public Task<State> Body { get; set; }

        public RestRequest(HttpMethod method, Uri uri)
        {
            Method = method;
            Uri = uri;
        }
    }

    public class RestResponse
    {
        public bool IsSuccess { get; }
        public State Body { get; set; }
    }

    public class Controller1
    {
        public Controller1 Next { get; set; }

        public Task Send(HttpMethod method, Uri uri) => Send(new RestArgs(new RestRequest(method, uri)));

        public Task Send(params RestArgs[] args) => Send((IEnumerable<RestArgs>)args);
        public virtual Task Send(IEnumerable<RestArgs> args) => Task.WhenAll(args.Select(Send));

        public virtual void Send(MultiRestArgs e, Controller1 next)
        {
            //_ = SendAsync(e, next);
            //e.Handle(e.Requests.Select(request => request.HandledAsync));
        }

        private async Task Send(RestArgs e)
        {
            if (Next == null)
            {
                return;
            }

            var response = Next.Send(e);

            _ = Send(new RestArgs(new RestRequest(HttpMethod.Put, e.Request.Uri)
            {
                Body = null
            }));

            if (e.Resource is HttpResourceCollection collection)
            {
                foreach (var uri in collection.Uris)
                {
                    _ = Send(new RestArgs(new RestRequest(HttpMethod.Put, uri)
                    {
                        //Body = e.Resources.GetAsync(uri)
                    }));
                }
            }

            await response;
        }
    }

    public class HttpContentWrapper : HttpContent
    {
        public HttpContent Content { get; }

        public HttpContentWrapper(HttpContent content)
        {
            Content = content;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) => stream.Write(await Content.ReadAsByteArrayAsync());

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }

    public abstract class HttpController : Controller1
    {
        public HttpMessageInvoker Invoker { get; }
        public ResourceMap Resources { get; }

        public HttpController(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        private class StateContent : HttpContent
        {
            public Task<State> State { get; }

            public StateContent(Task<State> state)
            {
                State = state;
            }


            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                var state = await State;

                if (state.TryGetRepresentation<string>(out var content))
                {
                    stream.Write(Encoding.UTF8.GetBytes(content));
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = default;
                return false;
            }
        }

        private static async Task<T> Convert<T>(Task<HttpResponseMessage> response, IHttpConverter<T> converter) => await converter.Convert((await response).Content);
        public override Task Send(IEnumerable<RestArgs> args1)
        {
            var args = args1.ToArray();
            var requests = new List<HttpRequestMessage>();

            foreach (var e in args)
            {
                var request = GetHttpMessage(e);

                if (e.Request.Body != null)
                {
                    request.Content = new StateContent(e.Request.Body);
                }

                requests.Add(request);
            }

            var responses = Invoker.SendAsync(requests, default);
            var results = new List<Task>();

            for (int i = 0; i < args.Length; i++)
            {
                var e = args[i];
                var response = responses[i];

                if (TryGetConverter(e.Request.Uri, out var converter))
                {
                    if (converter is HttpResourceCollectionConverter collection)
                    {
                        //e.Handle(new HttpResourceCollection(Convert<IReadOnlyDictionary<Uri, object>>(response, collection), collection.Resources));
                    }
                    else
                    {
                        e.Handle(new HttpResource(typeof(object), Convert(response, converter)));
                    }

                    async Task Convert1(HttpResource resource) => await resource;
                    results.Add(Convert1(e.Resource));
                }
                else
                {
                    results.Add(base.Send(e));
                }
            }

            return Task.WhenAll(results);
        }

        public virtual bool TryGetConverter(Uri uri, out IHttpConverter<object> converter)
        {
            converter = default;
            return false;
        }

        public virtual HttpRequestMessage GetHttpMessage(RestArgs e) => new HttpRequestMessage(e.Request.Method, e.Request.Uri);
    }

    public static class ChainExtensions
    {
        public static Task InvokeAsync<T>(this ChainLinkEventHandler<T> handler, T e) where T : AsyncChainEventArgs
        {
            handler(e);
            return e.RequestedSuspension;
        }
    }

    public interface IConverter<T>// : IConverter1
    {
        bool TryConvert(T original, Type targetType, out object converted);
    }

    public interface IConverter<TSource, TTarget>
    {
        TTarget Convert(TSource source);
    }

    public class PropertyEventArgs : EventArgs
    {
        public IEnumerable<Property> Properties { get; }

        public PropertyEventArgs(params Property[] properties) : this((IEnumerable<Property>)properties) { }
        public PropertyEventArgs(IEnumerable<Property> properties)
        {
            Properties = properties;
        }
    }

    public abstract class PropertyValuePair
    {
        public Property Property { get; }
        public object Value { get; }

        public PropertyValuePair(Property property, object value)
        {
            Property = property;
            Value = value;
        }
    }

    public class PropertyValuePair<T> : PropertyValuePair
    {
        public PropertyValuePair(Property<T> property, Task<T> value) : base(property, value) { }
        public PropertyValuePair(MultiProperty<T> property, Task<IEnumerable<T>> value) : base(property, value) { }
    }

    public class PropertyDictionary : IReadOnlyCollection<PropertyValuePair>
    {
        public event EventHandler<PropertyEventArgs> PropertyAdded;

        public int Count => Properties.Count;
        public ICollection<Property> Keys => Properties.Keys;
        public Task<object> this[Property property] => TryGetSingle(property, out Task<object> result) ? result : throw new KeyNotFoundException();

        public Dictionary<Property, IList<object>> Properties = new Dictionary<Property, IList<object>>();

        public Task[] RequestValues(params Property[] properties) => RequestValues((IEnumerable<Property>)properties);
        public Task[] RequestValues(IEnumerable<Property> properties)
        {
            var added = new List<Property>();
            var values = new List<IList<object>>();

            foreach (var property in properties)
            {
                var list = new List<object>();

                if (Properties.TryAdd(property, list))
                {
                    added.Add(property);
                    values.Add(list);
                }
            }

            PropertyAdded?.Invoke(this, new PropertyEventArgs(added));

            var result = new Task[added.Count];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i].FirstOrDefault() as Task;
            }

            return result;
        }

        private IList<object> GetValues(Property property)
        {
            if (!Properties.TryGetValue(property, out var values))
            {
                Properties.TryAdd(property, values = new List<object>());
            }
            if (values.Count == 0)
            {
                PropertyAdded?.Invoke(this, new PropertyEventArgs(property));
            }

            return values;
        }

        public void Add<T>(Property<T> key, Task<T> value) => Add((Property)key, value);
        public void Add<T>(MultiProperty<T> key, Task<IEnumerable<T>> value) => Add((Property)key, value);

        public void Add(PropertyValuePair pair)
        {
            Add(pair.Property, pair.Value);
        }

        private void Add(Property property, object value)
        {
            if (!Properties.TryGetValue(property, out var values))
            {
                Properties.Add(property, values = new List<object>());
            }
            // Clear for now to discard old values - there should only ever be one value per source
            values.Clear();

            values.Add(value);
        }

        public int ValueCount(Property property) => Properties.TryGetValue(property, out var values) ? values.Count : 0;

        public bool Invalidate(Property property, Task task) => Properties.TryGetValue(property, out var values) && values.Remove(task);

        public bool TryGetValue(Property key, out Task<object> value) => TryGetSingle(key, out value);
        public bool TryGetValue<T>(Property<T> key, out Task<T> value) => TryGetSingle(key, out value);
        public bool TryGetValues<T>(MultiProperty<T> key, out Task<IEnumerable<T>> value) => TryGetMultiple(key, out value);

        //public Task<IEnumerable<T>> GetMultiple<T>(Property<T> key, string source = null) => TryGetMultiple(key, out Task<IEnumerable<T>> result, source) ? result : Task.FromResult(Enumerable.Empty<T>());

        //public Task<T> GetSingle<T>(Property<T> key, string source = null) => TryGetSingle(key, out Task<T> result, source) ? result : Task.FromResult<T>(default);

        private bool TryGetSingle<T>(Property key, out Task<T> result, string source = null)
        {
            var values = GetValues(key);

            foreach (var value in values)
            {
                if (TryCastTask<T>(value, out var temp))
                {
                    result = temp;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool TryGetMultiple<T>(Property key, out Task<IEnumerable<T>> result, string source = null)
        {
            var values = GetValues(key);

            foreach (var value in values)
            {
                if (TryCastTask<IEnumerable<T>>(value, out var multiple))
                {
                    result = multiple;
                }
                else if (TryCastTask<T>(value, out var single))
                {
                    result = FlattenTasks(single);
                }
                else
                {
                    continue;
                }

                return true;
            }

            result = null;
            return false;
        }

        private Task<IEnumerable<T>> FlattenTasks<T>(params Task<T>[] tasks) => FlattenTasks((IEnumerable<Task<T>>)tasks);
        private async Task<IEnumerable<T>> FlattenTasks<T>(IEnumerable<Task<T>> tasks)
        {
            var values = new List<T>();

            foreach (var task in tasks)
            {
                values.Add(await task);
            }

            return values;
        }

        public static bool TryCastTask<T>(object untyped, out Task<T> typed)
        {
            if (untyped is Task<T> task)
            {
                typed = task;
                return true;
            }
            else if (untyped is Task task1)
            {
                try
                {
                    typed = CastTask<T>(task1);
                    return true;
                }
                catch { }
            }

            typed = null;
            return false;
        }

        public static async Task<T> CastTask<T>(Task task) => (T)await (dynamic)task;

        public IEnumerator<PropertyValuePair> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}