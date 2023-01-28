using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

    public class TMDbCompressor : ControllerLink
    {
        public TMDbLocalResources LocalCache { get; }

        public TMDbCompressor(TMDbLocalResources localCache)
        {
            LocalCache = localCache;
        }

        public override Task GetAsync(MultiRestEventArgs<GetEventArgs> e, ChainLinkEventHandler<MultiRestEventArgs<GetEventArgs>> next) => base.GetAsync(new MultiRestEventArgs<GetEventArgs>(GetUrisGreedy(e.Args).ToArray()), next);

        private Dictionary<TMDbRequest, TMDbRequest> Appendable;

        private IEnumerable<GetEventArgs> GetUrisGreedy(IEnumerable<GetEventArgs> requests)
        {
            var uiis = requests.Select(request => request.Uri).OfType<UniformItemIdentifier>();

            foreach (var uii in uiis)
            {
                if (TMDbRemoteResources.TryResolve(uii, out var request, out var args))
                {
                    if (Appendable.TryGetValue(request, out var parent))
                    {
                        yield return new GetEventArgs<HttpContent>(new Uri(string.Format(parent.GetURL(), args), UriKind.Relative));
                        request = parent;
                    }

                    if (request.SupportsAppendToResponse)
                    {
                        // append all supported
                    }
                }
            }
        }
    }

    public class TMDbLocalResources : ControllerLink<HttpContent>, IHandleSingleRestEvent<GetEventArgs<HttpContent>>
    {
        public HashSet<string> ChangeKeys { get; set; }
        public Task ChangeKeysLoaded { get; set; }

        private static Dictionary<Property, string> CHANGE_KEY_PROPERTY_MAP = new Dictionary<Property, string>
        {
            [Media.KEYWORDS] = "plot_keywords",
            [Media.POSTER_PATH] = "images",
            [Media.BACKDROP_PATH] = "images",
            [Media.TRAILER_PATH] = "videos",
            [Movie.CONTENT_RATING] = "releases",
            [Movie.RELEASE_DATE] = "releases",
            [TVShow.CONTENT_RATING] = "releases",
            [TVShow.FIRST_AIR_DATE] = "episode",
            [TVShow.LAST_AIR_DATE] = "episode",
            //[TVShow.SEASONS] = "season",
            [Person.PROFILE_PATH] = "images",
        };

        // There are no change keys for these properties but we're going to ignore that and cache them anyway
        public static HashSet<Property> CHANGES_IGNORED = new HashSet<Property>
        {
            Media.RATING,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS
        };

        private const ItemType UNCACHED_TYPES = ~(ItemType.Movie | ItemType.TVShow | ItemType.Person);

        private ItemInfoCache Cache { get; }

        public TMDbLocalResources(ItemInfoCache cache) : base()
        {
            Cache = cache;
        }

        public async Task HandleAsync(GetEventArgs<HttpContent> arg)
        {
            if (arg.Uri is UniformItemIdentifier uii && TMDbResources.TryGetUrl(uii, out var url) && await Cache.TryGetValueAsync(url) is JsonResponse json && json != null)
            {
                arg.Handle(json.Content);
            }
        }

        public async Task PostAsync<T>(Uri uri, T resource)
        {
            if (uri is UniformItemIdentifier uii && !UNCACHED_TYPES.HasFlag(uii.Item.ItemType) && IsCacheValid(uii.Property) && uii.Item.TryGetID(TMDB.ID, out var id) && TMDbResources.TryGetUrl(uii, out var url) && Converter.TryConvert(resource, out var content))
            {
                await Cache.AddAsync(uii.Item.ItemType, id, url, new JsonResponse(content));
            }
        }

        public static async Task<string> GetContent(JsonResponse json) => await json.Content.ReadAsStringAsync();

        public bool IsCacheValid(Property property) => CHANGES_IGNORED.Contains(property);// || (Context.ChangeKeyLookup.TryGetValue(property, out var changeKey) && Context.ChangeKeys.Contains(changeKey));
    }

    public class TMDbConverter
    {
        public static TMDbConverter Instance = new TMDbConverter();

        private Dictionary<ItemType, Dictionary<Property, Parser>> Index;

        private TMDbConverter()
        {
            Index = new Dictionary<ItemType, Dictionary<Property, Parser>>();

            foreach (var kvp in TMDB.ITEM_PROPERTIES)
            {
                var index = new Dictionary<Property, Parser>();

                foreach (var parser in kvp.Value.Info.Values.SelectMany())
                {
                    index[parser.Property] = parser;
                }

                Index[kvp.Key] = index;
            }
        }

        public bool TryGetUrl(UniformItemIdentifier uii, out string url)
        {
            if (TMDB.ITEM_PROPERTIES.TryGetValue(uii.Item.ItemType, out var properties) && properties.PropertyLookup.TryGetValue(uii.Property, out var request) && TMDB.TryGetParameters(uii.Item, out var args))
            {
                url = string.Format(request.GetURL(), args.ToArray());
                return true;
            }
            else
            {
                url = null;
                return false;
            }
        }

        public async Task HandleAsync(IEnumerable<GetEventArgs> args, ChainEventHandler<MultiRestEventArgs<GetEventArgs<HttpContent>>> handler)
        {
            var typed = args.Select(arg => new GetEventArgs<HttpContent>(arg.Uri)).ToArray();
            await handler(new MultiRestEventArgs<GetEventArgs<HttpContent>>(typed), null);

            args.Zip(typed, Convert).ToArray();
        }

        private bool Convert(GetEventArgs untyped, GetEventArgs<HttpContent> typed)
        {
            if (typed.Handled)
            {
                if (untyped.Handle(typed.Resource, Converter))
                {
                    return true;
                }
                else if (untyped.Uri is UniformItemIdentifier uii && Index.TryGetValue(uii.Item.ItemType, out var properties) && properties.TryGetValue(uii.Property, out var parser))
                {
                    var pair = parser.GetPair(Convert(typed.Resource));
                    untyped.Handle(pair.Value, Converter);
                    return true;
                }
            }

            return false;
        }

        private static async Task<ArraySegment<byte>> Convert(HttpContent content) => await content.ReadAsByteArrayAsync();

        private readonly IConverter<object> Converter = new DefaultConverter<object>();

        private class DefaultConverter<T> : IConverter<T>
        {
            public bool TryConvert<TTarget>(T source, out TTarget target)
            {
                if (source is TTarget t)
                {
                    target = t;
                    return true;
                }
                else
                {
                    target = default;
                    return false;
                }
            }
        }
    }

    public class TMDbResources : ControllerLink
    {
        private ControllerLink<HttpContent> Controller { get; }
        private static Dictionary<ItemType, Dictionary<Property, Parser>> Index;

        static TMDbResources()
        {
            Index = new Dictionary<ItemType, Dictionary<Property, Parser>>();

            foreach (var kvp in TMDB.ITEM_PROPERTIES)
            {
                var index = new Dictionary<Property, Parser>();

                foreach (var parser in kvp.Value.Info.Values.SelectMany())
                {
                    index[parser.Property] = parser;
                }

                Index[kvp.Key] = index;
            }
        }

        public TMDbResources(ControllerLink<HttpContent> controller)
        {
            Controller = controller;
        }

        public static bool TryGetUrl(UniformItemIdentifier uii, out string url)
        {
            if (TMDB.ITEM_PROPERTIES.TryGetValue(uii.Item.ItemType, out var properties) && properties.PropertyLookup.TryGetValue(uii.Property, out var request) && TMDB.TryGetParameters(uii.Item, out var args))
            {
                url = string.Format(request.GetURL(), args.ToArray());
                return true;
            }
            else
            {
                url = null;
                return false;
            }
        }

        private async Task<(bool Success, TResource Resource)> Convert<TResource>(Uri uri, (bool Success, HttpContent Resource) response)
        {
            if (response.Resource is TResource resource)
            {
                return (response.Success, resource);
            }

            if (response.Success && uri is UniformItemIdentifier uii && Index.TryGetValue(uii.Item.ItemType, out var properties) && properties.TryGetValue(uii.Property, out var parser))
            {
                var pair = parser.GetPair(Convert(response.Resource));

                if (pair.Value is Task<TResource> task)
                {
                    return (true, await task);
                }
            }

            return (false, default);
        }

        private static async Task<ArraySegment<byte>> Convert(HttpContent content) => await content.ReadAsByteArrayAsync();
    }

    public class TMDbRemoteResources : ControllerLink<HttpContent>, IHandleMultiRestEvent<GetEventArgs>, IHandleMultiRestEvent<GetEventArgs<HttpContent>>
    {
        private Dictionary<Uri, HttpContent> Responses = new Dictionary<Uri, HttpContent>();
        private SemaphoreSlim ResponseCacheSemaphore = new SemaphoreSlim(1, 1);
        private TMDbConverter TMDbConverter = TMDbConverter.Instance;

        public Task HandleAsync(IEnumerable<GetEventArgs> args) => TMDbConverter.HandleAsync(args, GetAsync);

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

        public Task HandleAsync(IEnumerable<GetEventArgs<HttpContent>> e)
        {
            var trie = new Dictionary<string, List<(string Path, string Query, TMDbRequest Request, GetEventArgs<HttpContent> Arg)>>();

            foreach (var arg in e)
            {
                if (arg.Uri is UniformItemIdentifier uii && TryGetRequest(uii, out var request))
                {
                    var args = new object[0];

                    if (TMDB.TryGetParameters(uii.Item, out var temp))
                    {
                        args = temp.ToArray();
                    }

                    var url = string.Format(request.GetURL(), args);
                    var parts = url.Split('?');

                    AddToTrie(trie, parts[0], (parts[0], parts.Length > 1 ? parts[1] : string.Empty, request, arg));
                }
            }

            foreach (var kvp in trie)
            {
                var url = kvp.Key;
                var paths = kvp.Value.Select(value => value.Path.Substring(url.Length).Trim('/')).ToArray();
                var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
                var queries = kvp.Value.Select(value => value.Query).ToArray();

                var append = $"append_to_response={string.Join(',', validPaths)}";
                var query = CombineQueries(queries.Append(append));
                var response = Parse(TMDB.WebClient.TryGetAsync($"{url}?{query}"), validPaths);

                var itr = kvp.Value.GetEnumerator();

                for (int i = 0; i < paths.Length && itr.MoveNext(); i++)
                {
                    var value = itr.Current;
                    var path = paths[i];

                    value.Arg.Handle(new AppendedContent(response, path));
                }
            }

            return Task.CompletedTask;
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

        public async Task HandleAsync1(IEnumerable<GetEventArgs<HttpContent>> requests)
        {
            /*if (Responses.TryGetValue(uri, out var content))
            {
                return (true, content);
            }*/

            var request = new TMDbHttpRequest();
            var promises = requests.Select(uri => (request.TryAdd(uri.Uri, out var resource), resource)).ToArray();
            //var results = new (bool Success, HttpContent Resource)[promises.Length];

            var responses = await Task.WhenAll(promises.Select(promise => promise.Item1 ? promise.resource.Request.Send(TMDB.WebClient) : Task.FromResult((false, (LazyJson)null))));
            var itr = ((IEnumerable<(bool Success, TMDbHttpRequest.Resource resource)>)promises).GetEnumerator();

            for (int i = 0; i < responses.Length && itr.MoveNext(); i++)
            {
                var response = responses[i];
                var promise = itr.Current;

                if (response.Item1)
                {
                    var resource = promise.resource;
                    requests.ElementAt(i).Handle(new AppendedContent(Task.FromResult(response.Item2), resource.Path));
                    //results[i] = (true, new AppendedContent(Task.FromResult(response.Item2), resource.Path));
                }
                else
                {
                    //results[i] = (false, null);
                }
            }

            /*try
            {
                await ResponseCacheSemaphore.WaitAsync();

                Responses.TryAdd(uri, response.Content);
            }
            finally
            {
                ResponseCacheSemaphore.Release();
            }*/
        }

        public static bool TryGetRequest(UniformItemIdentifier uii, out TMDbRequest request)
        {
            request = null;
            return TMDB.ITEM_PROPERTIES.TryGetValue(uii.Item.ItemType, out var properties) && properties.PropertyLookup.TryGetValue(uii.Property, out request);
        }

        public static bool TryResolve(Uri uri, out TMDbRequest request, out object[] args)
        {
            args = new object[0];

            if (uri is UniformItemIdentifier uii && TryGetRequest(uii, out request))
            {
                if (TMDB.TryGetParameters(uii.Item, out var temp))
                {
                    args = temp.ToArray();
                }
                return true;
            }

            request = null;
            return false;
        }

        public class TMDbHttpRequest
        {
            //private Dictionary<string, string> SingleRequests { get; } = new Dictionary<string, string>();
            private Dictionary<string, Request> AppendedRequests { get; } = new Dictionary<string, Request>();
            private IEnumerable<TMDbRequest> Appendable { get; }

            public TMDbHttpRequest(params TMDbRequest[] appendable) : this((IEnumerable<TMDbRequest>)appendable) { }
            public TMDbHttpRequest(IEnumerable<TMDbRequest> appendable)
            {
                Appendable = appendable;
            }

            //public Resource[] AddMany(IEnumerable<Uri> uris) => uris.Select(uri => Add(uri)).ToArray();

            public bool TryAdd(Uri uri, out Resource resource)
            {
                if (!TryResolve(uri, out var request, out var args))
                {
                    resource = null;
                    return false;
                }

                TryAdd(request, args, out resource);
                return true;
            }

            private bool TryAdd(TMDbRequest request, object[] args, out Resource resource)
            {
                var url = string.Format(request.GetURL(), args);
                Request r;

                if (request.SupportsAppendToResponse)
                {
                    r = new Request(url);

                    if (AppendedRequests.TryGetValue(r.Url, out var atr))
                    {
                        resource = new Resource(atr);
                        return false;
                    }
                    else
                    {
                        AppendedRequests.Add(r.Url, r);
                    }
                }
                else
                {
                    var itr = Appendable.GetEnumerator();
                    while (itr.MoveNext() && TryAdd(itr.Current, args, out _)) { }

                    //var itr = Appendable.Select(appendable => string.Format(appendable.GetURL(), args)).GetEnumerator();
                    //while (itr.MoveNext() && TryAddAtr(itr.Current, out _)) { }

                    foreach (var appendable in AppendedRequests.Values)
                    {
                        if (appendable.TryAppend(url, out var path))
                        {
                            resource = new Resource(appendable, path);
                            return true;
                        }
                    }

                    r = new Request(url);
                }

                resource = new Resource(r);
                return true;
            }

            public class Resource
            {
                public Request Request { get; }
                public string Path { get; }

                public Resource(Request request, string path = "")
                {
                    Request = request;
                    Path = path;
                }
            }

            public class Request
            {
                public string Url { get; }

                private HashSet<string> Append { get; } = new HashSet<string>();
                private Dictionary<string, string> Queries { get; } = new Dictionary<string, string>();

                private Task<(bool, LazyJson)> Response;

                public Request(string url)
                {
                    Url = Split(url);
                }

                public bool TryAppend(string url, out string path)
                {
                    if (url.StartsWith(Url))
                    {
                        path = Split(url).Substring(Url.Length).Trim('/');

                        if (!string.IsNullOrEmpty(path))
                        {
                            Append.Add(path);
                        }
                        return true;
                    }

                    path = null;
                    return false;
                }

                private string Split(string url)
                {
                    var parts = url.Split('?');

                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        var query = parts[1].Split('&');

                        foreach (var arg in query)
                        {
                            var kvp = arg.Split('=');

                            if (kvp.Length == 2)
                            {
                                Queries.TryAdd(kvp[0], kvp[1]);
                            }
                        }
                    }

                    return parts.FirstOrDefault();
                }

                public string GetUrl()
                {
                    var args = new List<string>();

                    args.Add(string.Join('&', Queries.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                    if (Append.Count > 0)
                    {
                        args.Add($"append_to_response={string.Join(',', Append)}");
                    }

                    return TMDB.BuildApiCall(Url, args.ToArray());
                }

                private static async Task<(bool, LazyJson)> Parse(Task<HttpResponseMessage> responseTask, IEnumerable<string> properties)
                {
                    var response = await responseTask;

                    if (response != null && response.IsSuccessStatusCode)
                    {
                        return (response?.IsSuccessStatusCode == true, new LazyJson(await response.Content.ReadAsByteArrayAsync(), properties));
                    }
                    else
                    {
                        return (false, default);
                    }
                }

                public Task<(bool Success, LazyJson Content)> Send(HttpClient client)
                {
                    if (Response == null)
                    {
                        Response = Parse(client.TryGetAsync(GetUrl()), Append);
                    }

                    return Response;
                }
            }
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
        public ChainLink<MultiRestEventArgs<GetEventArgs>> GetChain { get; }

        public Controller(ControllerLink first)
        {
            GetChain = new ChainLink<MultiRestEventArgs<GetEventArgs>>(first.GetAsync);
        }

        public Controller SetNext(ControllerLink controller)
        {
            var handler = new ChainLink<MultiRestEventArgs<GetEventArgs>>(controller.GetAsync);
            GetChain.SetNext(handler);

            return this;
        }

        public Task Get(params GetEventArgs[] args) => Get((IEnumerable<GetEventArgs>)args);
        public Task Get(IEnumerable<GetEventArgs> args) => GetChain.HandleAsync(new MultiRestEventArgs<GetEventArgs>(args));

        public async Task<(bool Success, T Resource)> Get<T>(Uri uri)
        {
            var args = new GetEventArgs<T>(uri);
            await Get(args);

            return (args.Handled, args.Resource);
        }

        public Task PostAsync<T>(Uri uri, T resource)
        {
            throw new NotImplementedException();
        }
    }

    public interface IHandleMultiRestEvent<T> where T : RestEventArgs
    {
        Task HandleAsync(IEnumerable<T> args);
    }

    public interface IHandleSingleRestEventNext<T> where T : RestEventArgs
    {
        Task HandleAsync(T arg, ChainLinkEventHandler<T> next);
    }

    public interface IHandleSingleRestEvent<T> where T : RestEventArgs
    {
        Task HandleAsync(T arg);
    }

    public abstract class ControllerLink
    {
        public virtual Task GetAsync(MultiRestEventArgs<GetEventArgs> e, ChainLinkEventHandler<MultiRestEventArgs<GetEventArgs>> next) => HandleAsync(e, next);

        //public Task HandleAsync<T>(T e, ChainLinkEventHandler<MultiRestEventArgs<T>> next) where T : RestEventArgs => HandleAsync(e.AsEnumerable(), next);
        //public Task HandleAsync<T>(IEnumerable<T> e) where T : RestEventArgs => HandleAsync(e, null);
        //public Task HandleAsync<T>(T e) where T : RestEventArgs => HandleAsync(e.AsEnumerable(), null);

        protected Task HandleAsync<T>(MultiRestEventArgs<T> args, ChainLinkEventHandler<MultiRestEventArgs<T>> next) where T : RestEventArgs => HandleAsync(args.Args, next);
        protected async Task HandleAsync<T>(IEnumerable<T> args, ChainLinkEventHandler<MultiRestEventArgs<T>> next) where T : RestEventArgs
        {
            if (this is IHandleMultiRestEvent<T> multi)
            {
                await multi.HandleAsync(args);
            }
            if (this is IHandleSingleRestEvent<T> single)
            {
                await Task.WhenAll(WhereUnhandled(args).Select(single.HandleAsync));
            }
            if (this is IRestService restService)
            {
                await Task.WhenAll(WhereUnhandled(args).Select(arg => arg.HandleAsync(restService)));
            }

            if (next != null)
            {
                await next(new MultiRestEventArgs<T>(WhereUnhandled(args)));

                if (this is IHandleSingleRestEventNext<T> singleNext)
                {
                    Task sendNext(T e) => next(new MultiRestEventArgs<T>(e.AsEnumerable()));
                    await Task.WhenAll(WhereUnhandled(args).Select(arg => singleNext.HandleAsync(arg, sendNext)));
                }
            }
        }

        protected IEnumerable<T> WhereUnhandled<T>(IEnumerable<T> e) where T : RestEventArgs => e.Where(NotHandled);

        protected bool NotHandled<T>(T arg) where T : ChainEventArgs => !arg.Handled;
    }

    public abstract class ControllerLink<T> : ControllerLink
    {
        public ResourceConverter<T> Converter { get; }

        public virtual Task GetAsync(MultiRestEventArgs<GetEventArgs<T>> e, ChainLinkEventHandler<MultiRestEventArgs<GetEventArgs<T>>> next) => HandleAsync(e, next);

        public override async Task GetAsync(MultiRestEventArgs<GetEventArgs> e, ChainLinkEventHandler<MultiRestEventArgs<GetEventArgs>> next)
        {
            Task TypeSafeHandler(MultiRestEventArgs<GetEventArgs<T>> args) => next?.Invoke(new MultiRestEventArgs<GetEventArgs>(args.Args)) ?? Task.CompletedTask;

            var typedRequests = e.Args.Select(WrapGetEventArgs).ToArray();
            await GetAsync(new MultiRestEventArgs<GetEventArgs<T>>(typedRequests), TypeSafeHandler);

            e.Args.Zip(typedRequests, HandleAsync);

            await base.GetAsync(e, next);
        }

        private bool HandleAsync(GetEventArgs target, GetEventArgs<T> source)
        {
            target.Handle(source.Resource, Converter);
            return true;
        }

        private GetEventArgs<T> WrapGetEventArgs(GetEventArgs e) => new GetEventArgs<T>(e.Uri);

        protected async Task HandleAsyncTypeSafe<TArgs, TGenericArgs>(IEnumerable<TGenericArgs> e, ChainLinkEventHandler<MultiRestEventArgs<TArgs>> next)
            where TArgs : RestEventArgs
            where TGenericArgs : TArgs
        {
            //Task TypeSafeHandler(MultiRestEventArgs<TGenericArgs> args) => next(new MultiRestEventArgs<TArgs>(args.Args));
            Task TypeSafeHandler(MultiRestEventArgs<TGenericArgs> args) => next(new MultiRestEventArgs<TArgs>(args.Args));

            await HandleAsync(e, TypeSafeHandler);
            //await HandleAsync<TArgs>(new MultiRestEventArgs<TArgs>(e.Args), TypeSafeHandler);
        }

        protected Task HandleAsyncTypeSafe<TArgs, TGenericArgs>(MultiRestEventArgs<TArgs> e, ChainLinkEventHandler<MultiRestEventArgs<TArgs>> next)
            where TArgs : RestEventArgs
            where TGenericArgs : TArgs
        {
            Task TypeSafeHandler(MultiRestEventArgs<TGenericArgs> args) => next(new MultiRestEventArgs<TArgs>(args.Args));
            return HandleAsync(e.Args.OfType<TGenericArgs>(), TypeSafeHandler);
        }
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

    public class ControllerWrapper
    {
        public IRestService RestService { get; }

        public ControllerWrapper(IRestService restService)
        {
            RestService = restService;
        }

        //protected override Task HandleAsync(ChainEventArgs e) => e is RestEventArgs rest ? rest.HandleAsync(RestService) : Task.CompletedTask;
    }

    public interface IRestService
    {
        Task<(bool Success, T Resource)> Get<T>(Uri uri);
        Task PostAsync<T>(Uri uri, T resource);
        //void Delete(Uri uri);
    }

    public class ChainEventArgs
    {
        public bool Handled { get; protected set; }
    }

    public class MultiRestEventArgs<T> : ChainEventArgs where T : RestEventArgs
    {
        public IEnumerable<T> Args { get; }

        public MultiRestEventArgs(params T[] args) : this((IEnumerable<T>)args) { }
        public MultiRestEventArgs(IEnumerable<T> args)
        {
            Args = args;
        }
    }

    public abstract class RestEventArgs : ChainEventArgs
    {
        public HttpMethod Method { get; }
        public Uri Uri { get; }
        public object Response { get; protected set; }

        public RestEventArgs(HttpMethod method, Uri uri)
        {
            Method = method;
            Uri = uri;
        }

        public abstract Task HandleAsync(IRestService controller);
    }

    public abstract class GetEventArgs : RestEventArgs
    {
        public Type ResourceType { get; }

        public GetEventArgs(Uri uri, Type resourceType) : base(HttpMethod.Get, uri)
        {
            ResourceType = resourceType;
        }

        public abstract bool Handle<T>(T response, IConverter<T> converter);
    }

    public class GetEventArgs<T> : GetEventArgs
    {
        public T Resource { get; protected set; }

        public GetEventArgs(Uri uri) : base(uri, typeof(T)) { }

        public void Handle(T resource)
        {
            Resource = resource;
            Handled = true;
        }

        public override bool Handle<TResponse>(TResponse response, IConverter<TResponse> converter)
        {
            if (converter.TryConvert<T>(response, out var converted))
            {
                Resource = converted;
                return Handled = true;
            }
            else
            {
                return false;
            }
        }

        public override async Task HandleAsync(IRestService restService)
        {
            var response = await restService.Get<T>(Uri);

            if (response.Success)
            {
                Handle(Resource = response.Resource);
            }
        }
    }

    public class ResourceCache : ControllerLink, IRestService
    {
        private Dictionary<Item, Dictionary<Property, Task>> Cache = new Dictionary<Item, Dictionary<Property, Task>>();

        public async Task<(bool Success, T Resource)> Get<T>(Uri uri)
        {
            if (uri is UniformItemIdentifier uii && Cache.TryGetValue(uii.Item, out var properties) && properties.TryGetValue(uii.Property, out var response) && PropertyDictionary.TryCastTask<T>(response, out var resource) && resource.IsCompletedSuccessfully)
            {
                return (true, await resource);
            }

            return (false, default);
        }

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
                    Cache[uii.Item] = properties = new Dictionary<Property, Task>();
                }

                properties[uii.Property] = Task.FromResult(resource);
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