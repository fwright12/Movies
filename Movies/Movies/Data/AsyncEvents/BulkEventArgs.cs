using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public class BulkEventArgs<TArgs> : IEnumerable<TArgs>
    {
        public int Count => Requests.Count;

        public IEnumerable<TArgs> Unhandled => Requests.Where(request => (request as DatastoreReadArgs)?.IsHandled == false);// GetUnhandled();

        //public IEnumerable<TArgs> Args { get; }
        //private IEnumerable<KeyValuePair<Uri, Task<State>>> AdditionalState;// { get; private set; }
        private ISet<TArgs> Requests;
        //public IEnumerable<TArgs> Unhandled { get; }

        private readonly LinkedList<TArgs> _Unhandled;
        //public readonly TArgs[] Requests;

        public BulkEventArgs(params TArgs[] args) : this((IEnumerable<TArgs>)args) { }
        public BulkEventArgs(IEnumerable<TArgs> args)
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

        public void Add(IEnumerable<TArgs> requests) => Requests.UnionWith(requests);

        public void Add(TArgs request)
        {
            Requests.Add(request);
        }

        public IEnumerator<TArgs> GetEnumerator() => Requests.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
