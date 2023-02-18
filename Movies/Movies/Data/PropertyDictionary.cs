using FFImageLoading.Work;
using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

    public interface IConverter<TSource>
    {
        bool TryConvert<TTarget>(TSource source, out TTarget target);
    }

    public abstract class ResourceConverter<TResource> : IConverter<TResource>
    {
        public abstract bool TryConvert<T>(TResource resource, out T converted);
        public abstract bool TryConvert<T>(T resource, out TResource converted);
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

    public interface ILinkHandler<T> where T : ChainEventArgs
    {
        Task HandleAsync(T e, ChainLinkEventHandler<T> next);
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

        public Task<RestRequestArgs[]> Get(params string[] urls) => Get(urls.Select(url => new Uri(url, UriKind.Relative)));
        public Task<RestRequestArgs[]> Get(params Uri[] uris) => Get((IEnumerable<Uri>)uris);
        public async Task<RestRequestArgs[]> Get(IEnumerable<Uri> uris)
        {
            var args = uris.Select(uri => new RestRequestArgs(uri)).ToArray();
            await Get(args);
            return args;
        }

        public Task<(bool Success, HttpContent Resource)> Get(string url) => GetInternal<HttpContent>(new Uri(url, UriKind.Relative));
        public Task<(bool Success, HttpContent Resource)> Get(Uri uri) => GetInternal<HttpContent>(uri);

        public async Task<(bool Success, T Resource)> Get<T>(Uri uri)
        {
            var response = await GetInternal<Task<T>>(uri);
            return (response.Success, response.Success ? await response.Resource : default);
        }

        private async Task<(bool Success, T Resource)> GetInternal<T>(Uri uri)
        {
            var args = new RestRequestArgs(uri);
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

    public interface IMultiRestEventHandler<T> where T : RestRequestArgs
    {
        Task HandleAsync(IEnumerable<T> args);
    }

    public interface ISingleRestEventNextHandler<T> where T : RestRequestArgs
    {
        Task HandleAsync(T arg, ChainLinkEventHandler<T> next);
    }

    public interface ISingleRestEventHandler<T> where T : RestRequestArgs
    {
        Task HandleAsync(T arg);
    }

    public class RestLink : ChainLink<MultiRestEventArgs>
    {
        public HttpMethod Method { get; }

        public RestLink(ChainEventHandler<MultiRestEventArgs> handler, ChainLink<MultiRestEventArgs> next = null) : base(handler, next)
        {
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

    public abstract class ControllerLink<T> : IConverter1<T>
    {
        public delegate Task SingleHandler(RestRequestArgs e);
        public delegate Task MultiHandler(IEnumerable<RestRequestArgs> e);
        public delegate Task SingleNextHandler(RestRequestArgs e, ChainLinkEventHandler<RestRequestArgs> next);
        public delegate Task MultiNextHandler(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next);

        public bool CacheAside { get; set; } = true;

        //public Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        //public Task PostAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        public async Task GetAsync(MultiRestEventArgs e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            await GetAsync(e.Args, null);
            AddRepresentations(e.Args);

            var unhandled = WhereUnhandled(e.Args).ToArray();
            if (next != null)
            {
                await next(e);
            }

            if (CacheAside)
            {
                var handled = unhandled.Where(arg => arg.Handled).ToArray();
                var posts = handled.Select(arg => new RestRequestArgs(arg.Uri)).ToArray();
                await HandleAsync(posts, null);
            }
        }

        protected virtual Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next) => HandleAsync(e, next);

        public abstract bool TryConvert(T value, Type targetType, out object converted);

        private void AddRepresentations(IEnumerable<RestRequestArgs> args)
        {
            foreach (var arg in args)
            {
                arg.Response?.Add(arg.Expected, this);
            }
        }

        protected Task HandleAsync<T>(IEnumerable<T> e, ChainLinkEventHandler<MultiRestEventArgs> next) where T : RestRequestArgs => next?.Invoke(new MultiRestEventArgs(WhereUnhandled(e).ToArray())) ?? Task.CompletedTask;
        protected async Task HandleAsync<T>(IEnumerable<T> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<IEnumerable<T>, Task> handler) where T : RestRequestArgs
        {
            await handler(e);
            await HandleAsync(e, next);
        }
        protected async Task HandleAsync<T>(IEnumerable<T> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<T, ChainLinkEventHandler<T>, Task> handler) where T : RestRequestArgs
        {
            Task sendNext(RestRequestArgs e) => next(new MultiRestEventArgs(e));
            await Task.WhenAll(e.Select(arg => handler(arg, sendNext)));
            await HandleAsync(e, next);
        }
        protected async Task HandleAsync<T>(IEnumerable<T> e, ChainLinkEventHandler<MultiRestEventArgs> next, Func<T, Task> handler) where T : RestRequestArgs
        {
            await Task.WhenAll(e.Select(handler));
            await HandleAsync(e, next);
        }

        private class RestRequestHandlers
        {
            private SingleHandler Single;
            private MultiHandler Multi;
            private SingleNextHandler SingleNext;
            private MultiNextHandler MultiNext;

            public RestRequestHandlers(SingleHandler single, MultiHandler multi, SingleNextHandler singleNext, MultiNextHandler multiNext)
            {
                Single = single;
                Multi = multi;
                SingleNext = singleNext;
                MultiNext = multiNext;
            }

            public Task HandleAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                MultiNext = null;
                return HandleAnyAsync(e, next);
            }

            public Task HandleAsync(RestRequestArgs e, ChainLinkEventHandler<RestRequestArgs> next)
            {
                SingleNext = null;
                return HandleAnyAsync(e.AsEnumerable(), null);
            }

            public Task HandleAsync(IEnumerable<RestRequestArgs> e)
            {
                Multi = null;
                return HandleAnyAsync(e, null);
            }

            public Task HandleAsync(RestRequestArgs e)
            {
                Single = null;
                return HandleAnyAsync(e.AsEnumerable(), null);
            }

            private Task HandleAnyAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next)
            {
                if (MultiNext != null)
                {
                    return MultiNext(e, next);
                }
                else if (SingleNext != null)
                {
                    Task sendNext(RestRequestArgs e) => next(new MultiRestEventArgs(e));
                    return Task.WhenAll(e.Select(arg => SingleNext(arg, sendNext)));
                }
                else if (Multi != null)
                {
                    return Multi(e);
                }
                else if (Single != null)
                {
                    return Task.WhenAll(e.Select(arg => Single(arg)));
                }

                return Task.CompletedTask;
            }
        }

        protected IEnumerable<RestRequestArgs> WhereUnhandled(IEnumerable<RestRequestArgs> e) => e.Where(NotHandled);

        protected bool NotHandled(RestRequestArgs arg) => !arg.Handled;
    }

    public abstract class ControllerLink1<T> : ControllerLink
    {
        public ResourceConverter<T> Converter { get; }

        public ControllerLink1(ResourceConverter<T> converter = null)
        {
            Converter = converter;
        }



        /*protected virtual Task GetAsync(IEnumerable<RestRequestArgs<T>> e, ChainLinkEventHandler<MultiRestEventArgs> next) => base.HandleAsync(e, next);

        protected override async Task GetAsync(IEnumerable<RestRequestArgs> e, ChainLinkEventHandler<MultiRestEventArgs> next)
        {
            //Task TypeSafeHandler(MultiRestEventArgs<GetEventArgs<T>> args) => next?.Invoke(new MultiRestEventArgs<GetEventArgs>(args.Args)) ?? Task.CompletedTask;

            IEnumerable<RestRequestArgs<T>> typedRequests = e.Select(arg => new RestRequestArgs<T>(arg.Request)).ToArray();
            await GetAsync(typedRequests, null);

            var typedItr = typedRequests.GetEnumerator();
            var untypedItr = e.GetEnumerator();

            while (typedItr.MoveNext() && untypedItr.MoveNext())
            {
                var typed = typedItr.Current;
                var untyped = untypedItr.Current;

                if (typed.Handled && !untyped.Handle(typed.Response) && CacheAside)
                {
                    //await PostAsync(new MultiRestEventArgs<PostEventArgs>(new PostEventArgs<T>(typed.Uri, typed.Resource)), null);
                }
            }

            await base.HandleAsync(WhereUnhandled(e).ToArray(), next);
        }*/
    }

    //public abstract class ControllerLinkHandler : LinkHandler
    //{
    //    public override async Task HandleAsync(ChainEventArgs e, ChainEventHandler next)
    //    {
    //        if (e is MultiRestEventArgs multi)
    //        {
    //            await HandleAsync(multi.Args, next);
    //        }
    //        else if (e is GetEventArgs get)
    //        {
    //            await HandleGetAsync(get, next);
    //        }
    //        else
    //        {
    //            await base.HandleAsync(e, next);
    //        }
    //    }

    //    protected override Task HandleAsync(ChainEventArgs e) => e is MultiRestEventArgs multi ? HandleAsync(multi.Args) : base.HandleAsync(e);

    //    public virtual Task HandleGetAsync(GetEventArgs e, ChainEventHandler next) => base.HandleAsync(e, next);
    //    public virtual Task HandleGetAsync(GetEventArgs e) => base.HandleAsync(e);
    //    public virtual Task HandleGetAsync(IEnumerable<GetEventArgs> args, ChainEventHandler next) => Task.WhenAll(args.Select(base.HandleAsync));
    //    public virtual Task HandleGetAsync(IEnumerable<GetEventArgs> args) => Task.WhenAll(args.Select(base.HandleAsync));

    //    public virtual Task HandleAsync(IEnumerable<RestEventArgs> args)
    //    {

    //    }

    //    public Task HandleAsync(params RestEventArgs[] args) => HandleAsync((IEnumerable<RestEventArgs>)args);
    //    public virtual Task HandleAsync(IEnumerable<RestEventArgs> args, ChainEventHandler next)
    //    {
    //        var unhandled = args.ToList();
    //        var gets = new List<GetEventArgs>();

    //        foreach (var arg in args)
    //        {
    //            if (arg is GetEventArgs get)
    //            {
    //                unhandled.Remove(arg);
    //                gets.Add(get);
    //            }
    //        }

    //        return Task.WhenAll(args.Select(base.HandleAsync).Prepend(HandleGetAsync(gets, next)));
    //    }
    //}

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

    public interface IConverter1
    {
        bool CanConvert(Type fromType, Type toType);
    }

    public interface IConverter1<T> : IConverter1
    {
        bool TryConvert(T original, Type targetType, out object converted);
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

        public void Add<T>(T value)
        {
            Representations.Add(typeof(T), value);
        }

        public void Add(object value)
        {
            Representations.Add(value.GetType(), value);
        }

        public bool Add<T>(Type type, IConverter1<T> converter)
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

        public bool CanRepresentAs(Type type)
        {
            if (Representations.ContainsKey(type))
            {
                return true;
            }

            return false;
        }

        public bool TryGetRepresentation(Type type, out object value) => Representations.TryGetValue(type, out value);
        public bool TryGetRepresentation<T>(out T value)
        {
            if (TryGetRepresentation(typeof(T), out var rawValue) && rawValue is T t)
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

        public RestRequestArgs(Uri uri, Type expected = null)
        {
            Uri = uri;
            Expected = expected;
        }

        public bool Handle(object response) => Handle(new State(response));

        public bool Handle<TResponse>(TResponse response) => Handle(new State(response));

        public bool Handle<T>(IConverter1<T> converter)
        {
            if (response.Add(Expected, converter))
            {
                Response = response;
                Handle();
                return true;
            }

            return false;
        }

        public bool Handle(State response)
        {
            Response = response;

            if (Expected != null && response.CanRepresentAs(Expected))
            {
                Handle();
                return true;
            }

            return false;
        }
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