using FFImageLoading.Work;
using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Movies
{
    public class UniformItemIdentifier : Uri
    {
        public Item Item { get; }
        public Property Property { get; }

        public UniformItemIdentifier(Item item, Property property) : base($"urn:{item.ItemType}:{(item.TryGetID(TMDB.ID, out var id) ? id.ToString() : item.Name)}:{property.Name}")
        {
            Item = item;
            Property = property;
        }
    }

    public delegate void ChainLinkEventHandler<in T>(T e) where T : ChainEventArgs;
    public delegate void ChainEventHandler<T>(T e, ChainLinkEventHandler<T> next) where T : ChainEventArgs;
    public delegate Task AsyncChainEventHandler<T>(T e, ChainLinkEventHandler<T> next) where T : ChainEventArgs;

    public class ChainLink<T> where T : ChainEventArgs
    {
        public ChainLink<T> Next { get; set; }
        public ChainEventHandler<T> Handler { get; }

        public ChainLink(ChainEventHandler<T> handler, ChainLink<T> next = null)
        {
            Next = next;
            Handler = handler;
        }

        //public static implicit operator ChainLink<T>(ILinkHandler<T> handler) => new ChainLink<T>(handler);

        public static ChainLink<T> Build(params ChainLink<T>[] links)
        {
            for (int i = 0; i < links.Length - 1; i++)
            {
                links[i].Next = links[i + 1];
            }

            return links.FirstOrDefault();
        }

        public ChainLink<T> SetNext(ChainLink<T> link) => Next = link;

        public void Handle(T e)
        {
            if (Handler == null)
            {
                return;
            }

            Handler.Invoke(e, Next == null ? (ChainLinkEventHandler<T>)null : Next.Handle);
        }
    }

    public class ChainLinkAsync<T> : ChainLink<T> where T : AsyncChainEventArgs
    {
        public ChainLinkAsync(AsyncChainEventHandler<T> handler, ChainLink<T> next = null) : base((e, next) => handler?.Invoke(e, next), next) { }
        //public ChainLinkAsync(AsyncChainEventHandler<T> handler, ChainLink<T> next = null) : base(new Wrapper(handler, next).Handle, next) { }

        private class Wrapper
        {
            private AsyncChainEventHandler<T> Handler { get; }
            private ChainLink<T> Next { get; }

            public Wrapper(AsyncChainEventHandler<T> handler, ChainLink<T> next = null)
            {
                Handler = handler;
                Next = next;
            }

            public void Handle(T e, ChainLinkEventHandler<T> next)
            {
                e.Handle(Handler);
                Handler?.Invoke(e, Next == null ? (ChainLinkEventHandler<T>)null : Next.Handle);
            }
        }

        public async Task HandleAsync(T e)
        {
            if (Handler == null)
            {
                return;
            }

            Handler.Invoke(e, Next == null ? (ChainLinkEventHandler<T>)null : Next.Handle);
            await e.IsHandled();
        }
    }

    public class Controller
    {
        public enum HttpMethod { GET, PUT }

        public ChainLink<MultiRestEventArgs> GetChain => GetFirst(HttpMethod.GET);
        public ChainLink<MultiRestEventArgs> PutChain => GetFirst(HttpMethod.PUT);

        private ChainLink<MultiRestEventArgs>[] ChainFirst { get; }
        private ChainLink<MultiRestEventArgs>[] ChainLast { get; }
        private int MethodCount { get; }

        public Controller()
        {
            MethodCount = Enum.GetValues(typeof(HttpMethod)).Length;

            ChainFirst = new ChainLink<MultiRestEventArgs>[MethodCount];
            ChainLast = new ChainLink<MultiRestEventArgs>[MethodCount];
        }

        public ChainLink<MultiRestEventArgs> GetFirst(HttpMethod method) => ChainFirst[(int)method];
        private ChainLink<MultiRestEventArgs> GetLast(HttpMethod method) => ChainLast[(int)method];

        public ChainLink<MultiRestEventArgs> AddLast(HttpMethod method, ChainLink<MultiRestEventArgs> link)
        {
            ChainLast[(int)method] = link;
            return link;
        }

        public Controller AddLast(IControllerLink controller)
        {
            var handlers = new Dictionary<HttpMethod, ChainLink<MultiRestEventArgs>>
            {
                [HttpMethod.GET] = new ChainLink<MultiRestEventArgs>(controller.Handle),
                //[HttpMethod.PUT] = new ChainLink<MultiRestEventArgs>(controller.PutAsync),
            };

            foreach (var kvp in handlers)
            {
                var method = kvp.Key;
                var handler = kvp.Value;
                var index = (int)method;

                ChainLast[index] = GetLast(method)?.SetNext(handler) ?? (ChainFirst[index] = handler);
            }

            return this;
        }

        public async Task<(bool Success, T Resource)> TryGet<T>(string url)
        {
            var args = new RestRequestArgs(new Uri(url, UriKind.Relative));
            await Get(args);

            if (args.Handled && args.Response.TryGetRepresentation<T>(out var value))
            {
                return (true, value);
            }
            else
            {
                return (false, default);
            }
        }

        public async Task<RestRequestArgs> Get(string url) => (await Get(new string[] { url }))[0];
        public Task<RestRequestArgs[]> Get(params string[] urls) => Get(urls.Select(url => new Uri(url, UriKind.Relative)));
        public Task<RestRequestArgs[]> Get(params Uri[] uris) => Get((IEnumerable<Uri>)uris);
        public async Task<RestRequestArgs[]> Get(IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(args);
            return args;
        }

        public async Task<(bool Success, T Resource)> Get<T>(Uri uri)
        {
            var args = new RestRequestArgs<T>(uri);
            await Get(args);

            if (args.Handled && args.Response.TryGetRepresentation<T>(out var value))
            {
                return (true, value);
            }
            else
            {
                return (false, default);
            }
        }

        public Task Get(params RestRequestArgs[] args) => Get((IEnumerable<RestRequestArgs>)args);
        public Task Get(IEnumerable<RestRequestArgs> args)
        {
            var e = new MultiRestEventArgs(args);
            GetChain.Handle(e);
            var adsf = args.First().Handling.Status;
            return Task.WhenAll(e.Args.Select(arg => arg.Handling));
        }

        public async Task Put<T>(Uri uri, T value)
        {
            var request = new RestRequestArgs(uri, State.Create(value));
            await Put(request);
        }
        public Task Put(params RestRequestArgs[] args) => Put((IEnumerable<RestRequestArgs>)args);
        public Task Put(IEnumerable<RestRequestArgs> args) => Task.CompletedTask;// PutChain.Handle(new MultiRestEventArgs(args));
    }

    public abstract class ControllerLink : ControllerLink<object>
    {
        public ControllerLink() : base() { }

        public ControllerLink(params DelegatingHandler[] handlers) : base(handlers) { }

        public ControllerLink(HttpClient client) : base(client) { }

        public override bool TryConvert(object value, Type type, out object converted)
        {
            converted = value;
            return type.IsAssignableFrom(value.GetType());
        }
    }

    public interface IControllerLink
    {
        void Handle(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);

        void Get(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);
        Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);
        Task PutAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next);
    }

    public class RestService
    {
        public Task Get(RestRequestArgs e)
        {
            return Task.CompletedTask;
        }

        public Task Put(RestRequestArgs e)
        {
            return Task.CompletedTask;
        }
    }

    public class ControllerHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get) return Get(request.RequestUri);
            else return base.SendAsync(request, cancellationToken);
        }

        protected virtual Task<HttpResponseMessage> Get(Uri requestUri) => base.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), default);
    }

    public class CacheAsideHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = base.SendAsync(request, cancellationToken);
            
            if (request.Method == HttpMethod.Get)
            {
                SendAsync(new HttpRequestMessage(HttpMethod.Put, request.RequestUri)
                {
                    Content = new HttpContentWrapper(response)
                }, default);
            }

            return response;
        }

        private class HttpContentWrapper : HttpContent
        {
            private Task<HttpResponseMessage> Response { get; }

            public HttpContentWrapper(Task<HttpResponseMessage> response)
            {
                Response = response;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) => await (await Response).Content.CopyToAsync(stream);

            protected override bool TryComputeLength(out long length)
            {
                length = default;
                return false;
            }
        }
    }

    public class BufferedHandler : DelegatingHandler
    {
        private Dictionary<Uri, Task<HttpResponseMessage>> Buffer = new Dictionary<Uri, Task<HttpResponseMessage>>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Buffer.TryGetValue(request.RequestUri, out var buffered))
            {
                return RespondBuffered(buffered);
            }

            var response = base.SendAsync(request, cancellationToken);
            Buffer.Add(request.RequestUri, response);
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

    public abstract class ControllerLink<T> : IControllerLink, IConverter<T>
    {
        public bool CacheAside { get; set; } = true;
        public HttpClient Client { get; }

        public ControllerLink() { }

        public ControllerLink(params DelegatingHandler[] handlers)
        {
            for (int i = 0; i < handlers.Length - 1; i++)
            {
                handlers[i].InnerHandler = handlers[i + 1];
            }

            Client = new HttpClient(handlers[0]);
            Client.BaseAddress = new Uri("https://mocktmdb");
        }

        public ControllerLink(HttpClient client)
        {
            Client = client;
        }

        public virtual void Handle(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            var task = HandleAsync(e, next);

            foreach (var arg in e.Args)
            {
                arg.BeginHandle(task);
            }
        }

        public virtual Task HandleAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e.Args, next);

        private Dictionary<HttpMethod, Dictionary<Uri, RestRequestArgs>> Pending { get; } = new Dictionary<HttpMethod, Dictionary<Uri, RestRequestArgs>>();

        public virtual void Get(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            var response = GetInternalAsync(e, next);

            foreach (var arg in e.Args)
            {
                arg.Handle(Handle(arg, response));
            }
        }

        private async Task<bool> Handle(RestRequestArgs e, Task task)
        {
            await task;
            return e.Handled;
        }

        public async Task GetAsync1(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            Get(e, next);

            var tasks = e.Args.Select(arg => arg.Handling).ToList();
            var unhandled = new List<RestRequestArgs>();

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);
                var index = tasks.IndexOf(task);
                tasks.RemoveAt(index);

                var result = await task;
                var arg = e.Args.ElementAt(index);

                if (result)
                {
                    AddRepresentations(arg.AsEnumerable());
                }
                else
                {
                    unhandled.Add(arg);
                }
            }
        }

        public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            await GetInternalAsync(e, null);
            AddRepresentations(e.Args.Where(Unhandled).ToArray());

            if (next != null)
            {
                var unhandled = e.Args.Where(Unhandled).ToArray();
                var nextE = new MultiRestEventArgs(unhandled);
                next(nextE);
                await Task.WhenAll(nextE.Args.Select(arg => arg.Handling));

                e.Handle(nextE);
            }

            if (CacheAside && false)
            {
                var handled = e.Args.Where(arg => arg.Handled).ToArray();

                foreach (var arg in handled)
                {
                    if (arg.Response != null)
                    {
                        await PutAsync(new RestRequestArgs(arg.Uri, arg.Response).AsEnumerable(), null);
                    }
                }

                foreach (var kvp in e.GetAdditionalState().Where(kvp => kvp.Key is UniformItemIdentifier == false))
                {
                    //kvp.Value.Add(arg.Expected, this);
                    await PutAsync(new RestRequestArgs(kvp.Key, kvp.Value).AsEnumerable(), null);

                    var get = Pending[HttpMethod.Get];
                    if (get.Remove(kvp.Key, out var args))
                    {
                        args.Handle(kvp.Value);
                    }
                }

                //await HandleAsync(posts, null);
            }
        }

        public Task PutAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => PutAsync(e.Args, next);

        protected virtual Task GetInternalAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e.Args, next);

        protected virtual Task PutAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        public abstract bool TryConvert(T value, Type targetType, out object converted);

        private void AddRepresentations(IEnumerable<RestRequestArgs> args)
        {
            foreach (var arg in args.Where(arg => arg.Expected != null))
            {
                arg.Handle((IConverter<T>)this);
            }
        }

        protected Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            next?.Invoke(new MultiRestEventArgs(e.Where(Unhandled).ToArray()));// ?? Task.CompletedTask;
            return Task.WhenAll(e.Select(arg => arg.Handling));
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<IEnumerable<RestRequestArgs>, Task> handler)
        {
            await handler(e);
            await HandleAsync(e.Where(Unhandled).ToArray(), next);
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, ChainLinkEventHandler<RestRequestArgs>, Task> handler)
        {
            //Task sendNext(RestRequestArgs e) => next(new MultiRestEventArgs(e));
            void sendNext(RestRequestArgs e) { }
            await Task.WhenAll(e.Select(arg => handler(arg, sendNext)));
            await HandleAsync(e.Where(Unhandled).ToArray(), next);
        }
        protected void Handle(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, Task<T>> handler)
        {
            foreach (var arg in e)
            {
                arg.Handle(handler?.Invoke(arg));
            }
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, Task> handler)
        {
            await Task.WhenAll(e.Select(handler));
            await HandleAsync(e.Where(Unhandled).ToArray(), next);
        }

        //protected IEnumerable<RestRequestArgs> WhereUnhandled(IEnumerable<RestRequestArgs> e) => e.Where(NotHandled);

        protected bool Unhandled(RestRequestArgs arg) => !arg.Handled;
    }

    public interface IRestService
    {
        Task<(bool Success, T Resource)> Get<T>(Uri uri);
        Task PostAsync<T>(Uri uri, T resource);
        //void Delete(Uri uri);
    }

    public class AsyncChainEventArgs : ChainEventArgs
    {
        public Task<bool> Handling => IsHandled();
        private Task Wait;

        public void Handle<T>(AsyncChainEventHandler<T> result) where T : AsyncChainEventArgs
        {
            if (this is T t)
            {
                Wait = result?.Invoke(t, null);
            }
        }

        public void BeginHandle(Task task)
        {
            Wait = task;
        }

        public async Task<bool> IsHandled()
        {
            await Wait;
            return Handled;
        }
    }

    public class ChainEventArgs
    {
        public bool Handled { get; private set; }

        protected void Handle() => Handled = true;
    }

    public interface IConverter
    {
        bool CanConvert(Type fromType, Type toType);
    }

    public interface IConverter<T>// : IConverter1
    {
        bool TryConvert(T original, Type targetType, out object converted);
    }

    public interface IConverter<TSource, TTarget>
    {
        TTarget Convert(TSource source);
    }

    public class ConverterCollection<T> : IConverter<T>, IEnumerable
    {
        private Dictionary<Type, object> Converters { get; } = new Dictionary<Type, object>();

        public bool TryConvert(T original, Type targetType, out object converted)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator() => Converters.Values.GetEnumerator();
    }

    public class JsonIndex : IReadOnlyDictionary<string, JsonIndex>
    {
        public JsonIndex this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

        public ArraySegment<byte> Bytes
        {
            get
            {
                if (!IsByteCountComputed)
                {
                    foreach (var kvp in Read()) { }
                }

                return _Bytes;
            }
        }

        private ArraySegment<byte> _Bytes;
        private bool IsByteCountComputed;

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, JsonIndex>)Index).Keys;

        public IEnumerable<JsonIndex> Values => ((IReadOnlyDictionary<string, JsonIndex>)Index).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<string, JsonIndex>>)Index).Count;

        private Dictionary<string, JsonIndex> Index { get; } = new Dictionary<string, JsonIndex>();
        private Enumerator Itr { get; }

        //public JsonIndex(string json) : this(JsonDocument.Parse(json).RootElement) { }

        public JsonIndex(byte[] json) : this(new ArraySegment<byte>(json))
        {
            IsByteCountComputed = true;
        }

        public JsonIndex(ArraySegment<byte> bytes)
        {
            _Bytes = bytes;
            Itr = new Enumerator(this);
        }

        private JsonIndex(ArraySegment<byte> bytes, JsonReaderState state) : this(bytes)
        {
            Itr = new Enumerator(this, state);
        }

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, JsonIndex>)Index).ContainsKey(key);
        }

        public bool TryGetValue(string key, out JsonIndex value)
        {
            if (Index.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var kvp in Read())
            {
                if (kvp.Key == key)
                {
                    value = kvp.Value;
                    return true;
                }
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<string, JsonIndex>> GetEnumerator() => Index.Concat(Read()).GetEnumerator();

        private IEnumerable<KeyValuePair<string, JsonIndex>> Read()
        {
            while (Itr.MoveNext())
            {
                //ICollection<KeyValuePair<string, JsonIndex>> cache = Index;
                //cache.Add(Itr.Current);

                yield return Itr.Current;
            }
        }

        private bool Read(ref Utf8JsonReader reader)
        {
            if (Itr.MoveNext(ref reader))
            {
                ICollection<KeyValuePair<string, JsonIndex>> cache = Index;
                cache.Add(Itr.Current);

                return true;
            }
            else
            {
                _Bytes = new ArraySegment<byte>(_Bytes.Array, _Bytes.Offset, (int)Itr.Consumed);
                IsByteCountComputed = true;

                return false;
            }
        }

        private class Enumerator : IEnumerator<KeyValuePair<string, JsonIndex>>
        {
            public KeyValuePair<string, JsonIndex> Current { get; private set; }
            object IEnumerator.Current => Current;

            public long Consumed { get; private set; }
            private JsonIndex Index { get; }
            private ArraySegment<byte> Bytes { get; }
            private JsonReaderState State;

            public Enumerator(JsonIndex index)
            {
                Index = index;
                Bytes = new ArraySegment<byte>(index._Bytes.Array).Slice(index._Bytes.Offset);

                Reset();
            }

            public Enumerator(JsonIndex index, JsonReaderState state) : this(index)
            {
                State = state;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var start = Consumed;
                if (Current.Value != null)
                {
                    State = Current.Value.Itr.State;
                    start += Current.Value.Itr.Consumed;
                }

                var reader = new Utf8JsonReader(Bytes.Slice((int)start), true, State);
                return Index.Read(ref reader);
                //return MoveNext(ref reader);
            }

            public bool MoveNext(ref Utf8JsonReader reader)
            {
                if (Index.IsByteCountComputed && Consumed >= Index.Bytes.Count)
                {
                    return false;
                }

                if (Current.Value != null)
                {
                    //long before = Current.Value.Itr.Consumed;
                    while (Current.Value.Read(ref reader)) { }
                    Consumed += Current.Value.Bytes.Count;
                    //State = Current.Value.Itr.State;
                }

                var start = reader.BytesConsumed;

                if (reader.Read())
                {
                    //if (reader.TokenType != JsonTokenType.PropertyName && reader.TokenType != JsonTokenType.StartObject)
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        reader.Skip();
                    }

                    Consumed += reader.BytesConsumed - start;
                    State = reader.CurrentState;

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        return MoveNext(ref reader);
                    }
                    else if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        var index = new JsonIndex(Bytes.Slice((int)Consumed), reader.CurrentState);

                        Current = new KeyValuePair<string, JsonIndex>(propertyName, index);
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                Current = default;
                State = default;
                Consumed = 0;
            }
        }
    }

    public class StringConverter : JsonConverter<string>
    {
        public static readonly StringConverter Instance = new StringConverter();

        private StringConverter() { }

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var json = JsonDocument.ParseValue(ref reader))
            {
                var root = json.RootElement;
                return root.ValueKind == JsonValueKind.String ? root.GetString() : root.GetRawText();
            }

            var start = reader.BytesConsumed;
            if (reader.TokenType == JsonTokenType.String) return reader.GetString();
            reader.Skip();
            return Encoding.UTF8.GetString(reader.ValueSpan);
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class JsonConverterWrapper<T> : JsonConverter<T>
    {
        public string[] Path { get; }
        public JsonConverter<T> Converter { get; }

        public JsonConverterWrapper(string[] path) : this(path, null) { }

        public JsonConverterWrapper(JsonConverter<T> converter, params string[] path) : this(path, converter) { }

        public JsonConverterWrapper(string[] path, JsonConverter<T> converter)
        {
            Path = path;
            Converter = converter;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class AnnotatedJson : IReadOnlyDictionary<Uri, ArraySegment<byte>>
    {
        public ArraySegment<byte> this[Uri key] => Paths.TryGetValue(key, out var paths) && TryGetJson(paths, out var value) ? value : throw new KeyNotFoundException();

        public IEnumerable<Uri> Keys => this.Select(kvp => kvp.Key);

        public IEnumerable<ArraySegment<byte>> Values => this.Select(kvp => kvp.Value);

        public int Count => this.Count();

        private Dictionary<Uri, JsonConverterWrapper<string>> Paths { get; } = new Dictionary<Uri, JsonConverterWrapper<string>>();
        private JsonIndex Index { get; }
        private byte[] Bytes { get; }
        private string StringValue => _StringValue ??= Encoding.UTF8.GetString(Bytes);
        private string _StringValue;

        public AnnotatedJson(string json)
        {
            Bytes = Encoding.UTF8.GetBytes(json);
            Index = new JsonIndex(Bytes);
        }

        public AnnotatedJson(byte[] bytes)
        {
            Bytes = bytes;
            Index = new JsonIndex(bytes);
        }

        //public bool Add(Uri uri, string value) => Cache.TryAdd(uri, value);
        //public void Add(Uri uri, string path) => Add(uri, path.Split("/").ToArray());
        /*public void Add(Uri uri, params string[] paths)
        {
            Paths.TryAdd(uri, new Location(paths.ToList<string>()));
            return;

            if (!Paths.TryGetValue(uri, out var value))
            {
                //Paths.Add(uri, value = new List<string[]>());
            }

            //value.AddRange(paths);
        }*/

        public void Add(Uri uri, JsonConverterWrapper<string> wrapper) => Paths.TryAdd(uri, wrapper);
        public void Add(Uri uri, params string[] path) => Paths.TryAdd(uri, new JsonConverterWrapper<string>(path, StringConverter.Instance));
        public void Add(Uri uri, string[] path, JsonConverter<string> converter) => Paths.TryAdd(uri, new JsonConverterWrapper<string>(path, converter));

        public bool ContainsKey(Uri key) => Paths.ContainsKey(key);

        public bool TryGetValue(Uri key, out ArraySegment<byte> value)
        {
            if (Paths.TryGetValue(key, out var wrapper))
            {
                return TryGetJson(wrapper, out value);
            }
            else
            {
                value = default;
                return false;
            }
        }

        private bool TryGetJson(JsonConverterWrapper<string> wrapper, out ArraySegment<byte> json)
        {
            //if (paths.Count == 0)
            //{
            //    json = Json.RootElement.GetRawText();
            //    return true;
            //}

            if (TryGetSubProperty(wrapper.Path, out json))
            {
                return true;

                if (wrapper.Converter == StringConverter.Instance)
                {
                    //return true;
                }

                //json = elem.ValueKind == JsonValueKind.String ? elem.GetString() : elem.GetRawText();
                var reader = new Utf8JsonReader(json);
                json = Encoding.UTF8.GetBytes(wrapper.Converter.Read(ref reader, typeof(string), null));
                return true;
            }

            json = default;
            return false;
        }

        private bool TryGetSubProperty(IEnumerable<string> path, out ArraySegment<byte> value)
        {
            var index = Index;

            foreach (var property in path)
            {
                if (!index.TryGetValue(property, out index))
                {
                    value = default;
                    return false;
                }
            }

            value = index.Bytes;
            return true;
        }

        public IEnumerator<KeyValuePair<Uri, ArraySegment<byte>>> GetEnumerator()
        {
            foreach (var kvp in Paths)
            {
                if (TryGetJson(kvp.Value, out var json))
                {
                    yield return new KeyValuePair<Uri, ArraySegment<byte>>(kvp.Key, json);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class State : IEnumerable
    {
        private Dictionary<Type, object> Representations { get; }

        public State(object initial) : this(initial.GetType(), initial) { }
        private State(Type type, object initial)
        {
            Representations = new Dictionary<Type, object>
            {
                { type, initial }
            };
        }

        public static State Create<T>(T value) => value as State ?? new State(typeof(T), value);
        public static State Null(Type type) => new State(type, null);

        public bool Add<TExisting, TNew>(IConverter<TExisting> converter) => Add<TExisting>(typeof(TNew), converter);
        public bool Add<T>(Type type, IConverter<T> converter)
        {
            foreach (var value in this.OfType<T>())
            {
                if (converter.TryConvert(value, type, out var converted))
                {
                    Representations.Add(type, converted);
                    return true;
                }
            }

            return false;
        }

        public bool HasRepresentation<T>() => HasRepresentation(typeof(T));
        public bool HasRepresentation(Type type) => TryGetRepresentation(type, out _);

        public bool TryGetRepresentation(Type type, out object value)
        {
            if (Representations.TryGetValue(type, out value))
            {
                return true;
            }

            foreach (var kvp in Representations)
            {
                if (type.IsAssignableFrom(kvp.Key))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool TryGetRepresentation<T>(out T value)
        {
            if (TryGetRepresentation(typeof(T), out var raw) && raw is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public IEnumerator GetEnumerator() => Representations.Values.GetEnumerator();
    }

    public class MultiRestEventArgs : ChainEventArgs // where T : RestEventArgs
    {
        public IEnumerable<RestRequestArgs> Args { get; }
        private IEnumerable<KeyValuePair<Uri, State>> AdditionalState;// { get; private set; }
        public IEnumerable<RestRequestArgs> Unhandled { get; }

        public MultiRestEventArgs(params RestRequestArgs[] args) : this((IEnumerable<RestRequestArgs>)args) { }
        public MultiRestEventArgs(IEnumerable<RestRequestArgs> args)
        {
            Args = args;
        }

        public IEnumerable<KeyValuePair<Uri, State>> GetAdditionalState()
        {
            var set = Args.Select(arg => arg.Uri).ToHashSet();
            return AdditionalState?.Where(kvp => !set.Contains(kvp.Key)) ?? Enumerable.Empty<KeyValuePair<Uri, State>>();
        }

        public void Handle(MultiRestEventArgs e)
        {
            if (e.AdditionalState != null)
            {
                AdditionalState = AdditionalState?.Concat(e.AdditionalState) ?? e.AdditionalState;
            }
        }

        public void Handle<T>(IEnumerable<KeyValuePair<Uri, Task<T>>> data)
        {
            //var success = true;

            foreach (var arg in Args)
            {
                //success &= TryGetValue(data, arg.Uri, out var value) && arg.Handle(value);
                if (TryGetValue(data, arg.Uri, out var value))
                {
                    arg.Handle(value);
                }
            }

            //AdditionalState = data as IEnumerable<KeyValuePair<Uri, State>> ?? data.Select(kvp => new KeyValuePair<Uri, State>(kvp.Key, new State(kvp.Value)));

            //return success;
        }

        public bool HandleMany<T>(IEnumerable<KeyValuePair<Uri, T>> data)
        {
            var success = true;

            foreach (var arg in Args)
            {
                success &= TryGetValue(data, arg.Uri, out var value) && arg.Handle(value);
            }

            AdditionalState = data as IEnumerable<KeyValuePair<Uri, State>> ?? data.Select(kvp => new KeyValuePair<Uri, State>(kvp.Key, new State(kvp.Value)));

            return success;
        }

        private static bool TryGetValue<T>(IEnumerable<KeyValuePair<Uri, T>> data, Uri uri, out T value)
        {
            if (data is IReadOnlyDictionary<Uri, T> dict)
            {
                return dict.TryGetValue(uri, out value);
            }

            foreach (var kvp in data)
            {
                if (Equals(uri, kvp.Key))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }

    public class RestRequest
    {
        public Uri Uri { get; }
        public HttpMethod Method { get; }
        public State Body { get; }
        public Type Expected { get; }
    }

    public class RestResponse
    {
        public State State { get; }
    }

    public class RestRequestArgs : AsyncChainEventArgs
    {
        public RestRequest Request { get; }
        public RestResponse RestResponse { get; private set; }

        public void Handle(RestResponse response)
        {
            if (Handle(response.State))
            {
                RestResponse = response;
            }
        }

        public async Task<RestResponse> GetResponseAsync()
        {
            await Handling;
            return RestResponse;
        }

        public Uri Uri { get; }
        public State Body { get; }
        public Task<State> ResponseAsync { get; private set; }
        public State Response { get; private set; }
        public Type Expected { get; }

        public RestRequestArgs(Uri uri, Type expected = null)
        {
            Uri = uri;
            Expected = expected;
        }

        public RestRequestArgs(Uri uri, State body)
        {
            Uri = uri;
            Body = body;
        }

        public void Handle<T>(Task<T> response)
        {
            BeginHandle(HandleAsync(response));
        }

        private async Task<bool> HandleAsync<T>(Task<T> value) => Handle(await value);

        public bool Handle(object response) => Handle(new State(response));

        public bool Handle<T>(T response) => Handle(response as State ?? State.Create<T>(response));

        public bool Handle<T>(IConverter<T> converter)
        {
            if (Expected != null && Expected != typeof(T) && Response?.Add(Expected, converter) == true)
            {
                Handle();
                return true;
            }

            return false;
        }

        public bool Handle(State response)
        {
            Response = response;

            if (Expected == null || response.HasRepresentation(Expected))
            {
                Handle();
                return true;
            }

            return false;
        }
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }

    public class ResourceCache : IControllerLink
    {
        public int Count => Cache.Count;

        private Dictionary<Uri, Task<State>> Cache { get; } = new Dictionary<Uri, Task<State>>();
        private readonly SemaphoreSlim CacheSemaphore = new SemaphoreSlim(1, 1);

        public void Get(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {

        }

        public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            foreach (var arg in e.Args)
            {
                //if (e.Uri is UniformItemIdentifier uii && Cache.TryGetValue(uii.Item, out var properties) && properties.TryGetValue(uii.Property, out var response))// && PropertyDictionary.TryCastTask<T>(response, out var resource) && resource.IsCompletedSuccessfully)
                await CacheSemaphore.WaitAsync();

                try
                {
                    if (Cache.TryGetValue(arg.Uri, out var state))
                    {
                        arg.Handle(await state);
                    }
                }
                finally
                {
                    CacheSemaphore.Release();
                }
            }

            if (next == null)
            {
                return;
            }

            var unhandled = e.Args.Where(arg => !arg.Handled).ToArray();

            if (unhandled.Length == 0)
            {
                return;
            }

            var nextE = new MultiRestEventArgs(unhandled);
            next(nextE);
            var response = Task.WhenAll(nextE.Args.Select(arg => arg.Handling));

            for (int i = 0; i < unhandled.Length; i++)
            {
                var arg = unhandled[i];
                await PutAsync(arg.Uri, Unwrap(response, unhandled, i));
            }

            await response;
            e.Handle(nextE);
            var put = Enumerable.Empty<KeyValuePair<Uri, State>>();

            foreach (var arg in e.Args)
            {
                if (arg.Handled)
                {
                    if (arg.Response != null)
                    {
                        put = put.Append(new KeyValuePair<Uri, State>(arg.Uri, arg.Response));
                    }
                }
                else
                {
                    await CacheSemaphore.WaitAsync();

                    try
                    {
                        Cache.Remove(arg.Uri);
                    }
                    finally
                    {
                        CacheSemaphore.Release();
                    }
                }
            }

            put = put.Concat(e.GetAdditionalState());

            foreach (var kvp in put)
            {
                await PutAsync(kvp.Key, Task.FromResult(kvp.Value));
            }
        }

        private async Task<State> Unwrap(Task response, RestRequestArgs[] args, int index)
        {
            await response;
            return args[index].Response;
        }

        public Task PutAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => Task.WhenAll(e.Args.Select(arg => PutAsync(arg.Uri, arg.Body)));

        public async Task PutAsync<T>(Uri uri, T resource)
        {
            await CacheSemaphore.WaitAsync();

            try
            {
                Put(uri, resource);
            }
            finally
            {
                CacheSemaphore.Release();
            }
        }

        public void Put<T>(Uri uri, T resource)
        {
            Cache[uri] = Convert(resource);
        }

        private Task<State> Convert(object resource)
        {
            if (resource is Task<State> temp)
            {
                return temp;
            }

            if (resource is Task task && task.GetType() != typeof(Task))
            {
                try
                {
                    return ConvertTask(task);
                }
                catch { }
            }

            return Task.FromResult(State.Create(resource));
        }

        private async Task<State> ConvertTask(Task task)
        {
            var value = await (dynamic)task;

            if (value == null)
            {
                return State.Null(task.GetType().GetGenericArguments()[0]);
            }
            else
            {
                return value as State ?? new State(value);
            }
        }

        public async Task DeleteAsync(Uri uri)
        {
            await CacheSemaphore.WaitAsync();

            try
            {
                Cache.Remove(uri);
            }
            finally
            {
                CacheSemaphore.Release();
            }
        }

        public void Handle(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            throw new NotImplementedException();
        }
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