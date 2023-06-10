using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class MultiRestEventArgs : AsyncChainEventArgs // where T : RestEventArgs
    {
        public IEnumerable<RestRequestArgs> Unhandled => GetUnhandled();

        //public IEnumerable<RestRequestArgs> Args { get; }
        private IEnumerable<KeyValuePair<Uri, State>> AdditionalState;// { get; private set; }
        //public IEnumerable<RestRequestArgs> Unhandled { get; }

        private readonly LinkedList<RestRequestArgs> _Unhandled;
        public readonly IEnumerable<RestRequestArgs> AllArgs;

        public MultiRestEventArgs(params RestRequestArgs[] args) : this((IEnumerable<RestRequestArgs>)args) { }
        public MultiRestEventArgs(IEnumerable<RestRequestArgs> args)
        {
            AllArgs = args.ToArray();
            _Unhandled = args as LinkedList<RestRequestArgs> ?? new LinkedList<RestRequestArgs>(args);
        }

        private IEnumerable<RestRequestArgs> GetUnhandled()
        {
            var node = _Unhandled.First;

            while (node != null)
            {
                var next = node.Next;
                var args = node.Value;

                if (args.Handled)
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

        public IEnumerable<KeyValuePair<Uri, State>> GetAdditionalState()
        {
            var set = AllArgs.Select(arg => arg.Uri).ToHashSet();
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

            foreach (var arg in AllArgs)
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

            foreach (var arg in AllArgs)
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
}
