using FFImageLoading.Work;
using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        public UniformItemIdentifier(Item item, Property property) : base($"urn:{item.Name}:{property.Name}")
        {
            Item = item;
            Property = property;
        }
    }

    public delegate Task ChainLinkEventHandler<in T>(T e) where T : ChainEventArgs;
    public delegate Task ChainEventHandler<T>(T e, ChainLinkEventHandler<T> next) where T : ChainEventArgs;

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

        public async Task HandleAsync(T e)
        {
            if (Handler == null)
            {
                return;
            }

            await Handler.Invoke(e, Next == null ? (ChainLinkEventHandler<T>)null : Next.HandleAsync);
        }
    }

    public class Controller : IRestService
    {
        public ChainLink<MultiRestEventArgs> GetChain { get; private set; }
        public ChainLink<MultiRestEventArgs> PostChain { get; private set; }

        public Controller SetNext<T>(ControllerLink<T> controller)
        {
            var handler = new ChainLink<MultiRestEventArgs>(controller.GetAsync);

            if (GetChain == null)
            {
                GetChain = handler;
            }
            else
            {
                GetChain.SetNext(handler);
            }

            return this;
        }

        public ChainLink<MultiRestEventArgs> SetNext(ChainEventHandler<MultiRestEventArgs> handler)
        {
            var link = new ChainLink<MultiRestEventArgs>(handler);

            if (PostChain == null)
            {
                PostChain = link;
            }
            else
            {
                PostChain.SetNext(link);
            }

            return link;
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

        public Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => GetChain.HandleAsync(e);

        public Task Get(params RestRequestArgs[] args) => Get((IEnumerable<RestRequestArgs>)args);
        public Task Get(IEnumerable<RestRequestArgs> args) => GetChain.HandleAsync(new MultiRestEventArgs(args));

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

        public Task PostAsync<T>(Uri uri, T resource)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ControllerLink : ControllerLink<object>
    {
        public override bool TryConvert(object value, Type type, out object converted)
        {
            converted = value;
            return type.IsAssignableFrom(value.GetType());
        }
    }

    public abstract class ControllerLink<T> : IConverter<T>
    {
        public bool CacheAside { get; set; } = true;

        //public Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        //public Task PostAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            await GetAsync(e.Args, null);
            AddRepresentations(WhereUnhandled(e.Args));

            var unhandled = WhereUnhandled(e.Args).ToArray();
            if (next != null)
            {
                await next(new MultiRestEventArgs(unhandled));
            }

            if (CacheAside)
            {
                var handled = unhandled.Where(arg => arg.Handled).ToArray();
                var posts = handled.Select(arg => new RestRequestArgs(arg.Uri)).ToArray();

                foreach (var arg in e.Args)
                {
                    if (arg.Response != null)
                    {
                        await PutAsync(new RestRequestArgs(arg.Uri, arg.Response).AsEnumerable(), null);
                    }

                    if (arg.AdditionalState != null)
                    {
                        foreach (var kvp in arg.AdditionalState.Where(kvp => kvp.Key is UniformItemIdentifier == false))
                        {
                            //kvp.Value.Add(arg.Expected, this);
                            await PutAsync(new RestRequestArgs(kvp.Key, kvp.Value).AsEnumerable(), null);
                        }
                    }
                }

                //await HandleAsync(posts, null);
            }
        }

        protected virtual Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        protected virtual Task PutAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        public abstract bool TryConvert(T value, Type targetType, out object converted);

        private void AddRepresentations(IEnumerable<RestRequestArgs> args)
        {
            foreach (var arg in args.Where(arg => arg.Expected != null))
            {
                arg.Handle((IConverter<T>)this);
            }
        }

        protected Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => next?.Invoke(new MultiRestEventArgs(WhereUnhandled(e).ToArray())) ?? Task.CompletedTask;
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<IEnumerable<RestRequestArgs>, Task> handler)
        {
            await handler(e);
            await HandleAsync(e, next);
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, ChainLinkEventHandler<RestRequestArgs>, Task> handler)
        {
            Task sendNext(RestRequestArgs e) => next(new MultiRestEventArgs(e));
            await Task.WhenAll(e.Select(arg => handler(arg, sendNext)));
            await HandleAsync(e, next);
        }
        protected async Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<RestRequestArgs, Task> handler)
        {
            await Task.WhenAll(e.Select(handler));
            await HandleAsync(e, next);
        }

        protected IEnumerable<RestRequestArgs> WhereUnhandled(IEnumerable<RestRequestArgs> e) => e.Where(NotHandled);

        protected bool NotHandled(RestRequestArgs arg) => !arg.Handled;
    }

    public interface IRestService
    {
        Task<(bool Success, T Resource)> Get<T>(Uri uri);
        Task PostAsync<T>(Uri uri, T resource);
        //void Delete(Uri uri);
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

    public class AnnotatedJson : IReadOnlyDictionary<Uri, string>
    {
        public string this[Uri key] => Cache.TryGetValue(key, out var value) || (Paths.TryGetValue(key, out var paths) && TryGetJson(paths, out value)) ? value : throw new KeyNotFoundException();

        public IEnumerable<Uri> Keys => this.Select(kvp => kvp.Key);

        public IEnumerable<string> Values => this.Select(kvp => kvp.Value);

        public int Count => this.Count();

        private Dictionary<Uri, List<string[]>> Paths { get; } = new Dictionary<Uri, List<string[]>>();
        private Dictionary<Uri, string> Cache { get; } = new Dictionary<Uri, string>();
        private JsonDocument Json { get; }

        public AnnotatedJson(string json)
        {
            Json = JsonDocument.Parse(json);
        }

        public AnnotatedJson(byte[] bytes)
        {
            Json = JsonDocument.Parse(bytes);
        }

        public void Add(Uri uri, string path) => Add(uri, path.Split("/").ToArray());
        public void Add(Uri uri, params string[][] paths)
        {
            if (!Paths.TryGetValue(uri, out var value))
            {
                Paths.Add(uri, value = new List<string[]>());
            }

            value.AddRange(paths);
        }

        public bool ContainsKey(Uri key) => Cache.ContainsKey(key) || Paths.ContainsKey(key);

        public bool TryGetValue(Uri key, out string value)
        {
            if (Cache.TryGetValue(key, out value))
            {
                return true;
            }
            else if (Paths.TryGetValue(key, out var paths))
            {
                return TryGetJson(paths, out value);
            }
            else
            {
                return false;
            }
        }

        private bool TryGetJson(List<string[]> paths, out string json)
        {
            if (paths.Count == 0)
            {
                json = Json.RootElement.GetRawText();
                return true;
            }
            if (paths.Count == 1)
            {
                if (TryGetSubProperty(paths[0], out var elem))
                {
                    json = elem.ValueKind == JsonValueKind.String ? elem.GetString() : elem.GetRawText();
                    return true;
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            json = default;
            return false;
        }

        private bool TryGetSubProperty(string[] path, out JsonElement value)
        {
            value = Json.RootElement;

            foreach (var property in path)
            {
                if (!value.TryGetProperty(property, out value))
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerator<KeyValuePair<Uri, string>> GetEnumerator()
        {
            foreach (var kvp in Cache)
            {
                yield return kvp;
            }

            foreach (var kvp in Paths)
            {
                if (TryGetJson(kvp.Value, out var json))
                {
                    yield return new KeyValuePair<Uri, string>(kvp.Key, json);
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

        public static State Create<T>(T value) => new State(typeof(T), value);

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

        public MultiRestEventArgs(params RestRequestArgs[] args) : this((IEnumerable<RestRequestArgs>)args) { }
        public MultiRestEventArgs(IEnumerable<RestRequestArgs> args)
        {
            Args = args;
        }
    }

    public class RestRequestArgs : ChainEventArgs
    {
        public Uri Uri { get; }
        public State Body { get; }
        public State Response { get; private set; }
        public Type Expected { get; }
        public IEnumerable<KeyValuePair<Uri, State>> AdditionalState { get; private set; }

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

        public bool HandleMany<T>(IEnumerable<KeyValuePair<Uri, T>> data)
        {
            if (TryGetValue(data, Uri, out var value))
            {
                var handled = Handle(value);

                AdditionalState = data as IEnumerable<KeyValuePair<Uri, State>> ?? data.Select(kvp => new KeyValuePair<Uri, State>(kvp.Key, new State(kvp.Value)));
                AdditionalState = AdditionalState.Where(kvp => kvp.Key != Uri);

                return handled;
            }

            return false;
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

        private bool TryLocateState(IDictionary<Uri, State> substates, out State state)
        {
            if (substates.TryGetValue(Uri, out state))
            {
                return true;
            }

            foreach (var kvp in substates)
            {
                if (kvp.Value.TryGetRepresentation<IDictionary<Uri, State>>(out var temp) && TryLocateState(temp, out state))
                {
                    return true;
                }
            }

            state = default;
            return false;
        }
    }

    public class RestRequestArgs<T> : RestRequestArgs
    {
        public RestRequestArgs(Uri uri) : base(uri, typeof(T)) { }
    }

    public class ResourceCache : ControllerLink
    {
        private Dictionary<Item, Dictionary<Property, object>> Cache = new Dictionary<Item, Dictionary<Property, object>>();

        protected override Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next, (RestRequestArgs e) =>
        {
            if (e.Uri is UniformItemIdentifier uii && Cache.TryGetValue(uii.Item, out var properties) && properties.TryGetValue(uii.Property, out var response))// && PropertyDictionary.TryCastTask<T>(response, out var resource) && resource.IsCompletedSuccessfully)
            {
                e.Handle(response);
            }

            return Task.CompletedTask;
        });

        public Task PostAsync<T>(Uri uri, T resource)
        {
            Post(uri, resource);
            return Task.CompletedTask;
        }

        public void Post<T>(Uri uri, T resource)
        {
            if (uri is UniformItemIdentifier uii)
            {
                if (!Cache.TryGetValue(uii.Item, out var properties))
                {
                    Cache[uii.Item] = properties = new Dictionary<Property, object>();
                }

                properties[uii.Property] = resource;
            }
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