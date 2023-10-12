using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class BatchDatastoreArgs<TArgs> : EventArgsRequest, IEnumerable<TArgs> //where TArgs : DatastoreArgs
    {
        public int Count => Requests.Count;

        public IEnumerable<TArgs> Unhandled => Requests.Where(request => (request as DatastoreReadArgs)?.IsHandled == false);// GetUnhandled();

        //public IEnumerable<TArgs> Args { get; }
        //private IEnumerable<KeyValuePair<Uri, Task<State>>> AdditionalState;// { get; private set; }
        private HashSet<TArgs> Requests;
        //public IEnumerable<TArgs> Unhandled { get; }

        private readonly LinkedList<TArgs> _Unhandled;
        //public readonly TArgs[] Requests;

        public BatchDatastoreArgs(params TArgs[] args) : this((IEnumerable<TArgs>)args) { }
        public BatchDatastoreArgs(IEnumerable<TArgs> args)
        {
            Requests = new HashSet<TArgs>(args);
            //_Unhandled = args as LinkedList<TArgs> ?? new LinkedList<TArgs>(args);
        }

        //private IEnumerable<TArgs> GetUnhandled()
        //{
        //    var node = _Unhandled.First;

        //    while (node != null)
        //    {
        //        var next = node.Next;
        //        var args = node.Value;

        //        if (args.IsHandled)
        //        {
        //            node.List.Remove(node);
        //        }
        //        else
        //        {
        //            yield return args;
        //        }

        //        node = next;
        //    }
        //}

        private IEnumerable<TArgs> GetAdditionalState()
        {
            return Requests ?? Enumerable.Empty<TArgs>();
            
            //var set = AllArgs.Select(arg => arg.Uri).ToHashSet();
            //return AdditionalState?.Where(kvp => !set.Contains(kvp.Key)) ?? Enumerable.Empty<KeyValuePair<Uri, Task<State>>>();
        }

        public void AddRequests(IEnumerable<TArgs> requests) => Requests.UnionWith(requests);

        public void AddRequest(TArgs request)
        {
            Requests.Add(request);
        }

#if false
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
                var request = new TArgs(kvp.Key);
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

            //Requests = data.Select(kvp => new TArgs(kvp.Key, State.Create(kvp.Value))).ToList();

            //AdditionalState = data as IEnumerable<KeyValuePair<Uri, Task<State>>> ?? data.Select(kvp => new KeyValuePair<Uri, Task<State>>(kvp.Key, Task.FromResult(State.Create(kvp.Value))));

            return success;
        }
#endif

        public IEnumerator<TArgs> GetEnumerator() => Requests.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
