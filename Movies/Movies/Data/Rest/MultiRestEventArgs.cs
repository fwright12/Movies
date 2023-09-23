using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class MultiRestEventArgs : EventArgsRequest, IEnumerable<RestRequestArgs>
    {
        public int Count => Requests.Count;

        public IEnumerable<RestRequestArgs> Unhandled => Requests.Where(request => !request.IsHandled);// GetUnhandled();

        //public IEnumerable<RestRequestArgs> Args { get; }
        //private IEnumerable<KeyValuePair<Uri, Task<State>>> AdditionalState;// { get; private set; }
        private HashSet<RestRequestArgs> Requests;
        //public IEnumerable<RestRequestArgs> Unhandled { get; }

        private readonly LinkedList<RestRequestArgs> _Unhandled;
        //public readonly RestRequestArgs[] Requests;

        public MultiRestEventArgs(params RestRequestArgs[] args) : this((IEnumerable<RestRequestArgs>)args) { }
        public MultiRestEventArgs(IEnumerable<RestRequestArgs> args)
        {
            Requests = new HashSet<RestRequestArgs>(args);
            //_Unhandled = args as LinkedList<RestRequestArgs> ?? new LinkedList<RestRequestArgs>(args);
        }

        private IEnumerable<RestRequestArgs> GetUnhandled()
        {
            var node = _Unhandled.First;

            while (node != null)
            {
                var next = node.Next;
                var args = node.Value;

                if (args.IsHandled)
                {
                    node.List.Remove(node);
                }
                else
                {
                    yield return args;
                }

                node = next;
            }
        }

        private IEnumerable<RestRequestArgs> GetAdditionalState()
        {
            return Requests ?? Enumerable.Empty<RestRequestArgs>();
            
            //var set = AllArgs.Select(arg => arg.Uri).ToHashSet();
            //return AdditionalState?.Where(kvp => !set.Contains(kvp.Key)) ?? Enumerable.Empty<KeyValuePair<Uri, Task<State>>>();
        }

        public void AddRequests(IEnumerable<RestRequestArgs> requests) => Requests.UnionWith(requests);

        public void AddRequest(RestRequestArgs request)
        {
            Requests.Add(request);
        }

        public async Task<bool> HandleMany<T>(IReadOnlyDictionary<Uri, Task<T>> data)
        {
            //AdditionalState = data as IEnumerable<KeyValuePair<Uri, Task<State>>> ?? data.Select(kvp => new KeyValuePair<Uri, Task<State>>(kvp.Key, Task.FromResult(State.Create(kvp.Value))));

            var tasks = new List<Task<bool>>();

            foreach (var request in Requests)
            {
                if (data.TryGetValue(request.Uri, out var value))
                {

                }
            }

            //return (await Task.WhenAll(tasks)).All(value => value);

            var set = Requests.Select(request => request.Uri).ToHashSet();
            Requests.AddRange(data.Where(kvp => !set.Contains(kvp.Key)).Select(kvp =>
            {
                var request = new RestRequestArgs(kvp.Key);
                _ = request.Handle(kvp.Value);
                return request;
            }));

            var result = await Task.WhenAll(Unhandled.Select(arg => data.TryGetValue(arg.Uri, out var value) ? arg.Handle(value) : Task.FromResult(false)));

            return result.All(value => value);
        }

        private bool HandleMany<T>(IReadOnlyDictionary<Uri, T> data)
        {
            var success = true;

            if (Requests.Count > 1 && data is IDictionary<Uri, T> == false && data is IReadOnlyDictionary<Uri, T> == false)
            {
                data = new Dictionary<Uri, T>(data);
            }
            
            foreach (var arg in Requests)
            {
                //success &= TryGetValue(data, arg.Uri, out var value) && arg.Handle(value);
            }

            //Requests = data.Select(kvp => new RestRequestArgs(kvp.Key, State.Create(kvp.Value))).ToList();

            //AdditionalState = data as IEnumerable<KeyValuePair<Uri, Task<State>>> ?? data.Select(kvp => new KeyValuePair<Uri, Task<State>>(kvp.Key, Task.FromResult(State.Create(kvp.Value))));

            return success;
        }

        public IEnumerator<RestRequestArgs> GetEnumerator() => Requests.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
