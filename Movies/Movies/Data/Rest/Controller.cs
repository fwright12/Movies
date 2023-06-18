using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies
{
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

        public Controller(ChainLink<MultiRestEventArgs> get) : this()
        {
            ChainFirst[(int)HttpMethod.GET] = get;
        }

        public ChainLink<MultiRestEventArgs> GetFirst(HttpMethod method) => ChainFirst[(int)method];
        private ChainLink<MultiRestEventArgs> GetLast(HttpMethod method) => ChainLast[(int)method];

        public ChainLink<MultiRestEventArgs> AddLast(HttpMethod method, ChainLink<MultiRestEventArgs> link)
        {
            ChainLast[(int)method] = link;
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
        public Task Get(IEnumerable<RestRequestArgs> args1)
        {
            var args = args1.ToArray();
            var e = new MultiRestEventArgs(args);
            GetChain.Handle(e);
            return e.RequestedSuspension;
            //return Task.WhenAll(e.Args.Select(arg => arg.RequestedSuspension).Prepend(e.RequestedSuspension));
        }

        public async Task Put<T>(Uri uri, T value)
        {
            var request = new RestRequestArgs(uri, State.Create(value));
            await Put(request);
        }
        public Task Put(params RestRequestArgs[] args) => Put((IEnumerable<RestRequestArgs>)args);
        public Task Put(IEnumerable<RestRequestArgs> args) => Task.CompletedTask;// PutChain.Handle(new MultiRestEventArgs(args));
    }
}