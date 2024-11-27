using System;

namespace Movies
{
#if false
    public class KeyValueRequestArgs<TKey> : KeyValueRequestArgs<TKey>
        where TKey : Uri
    {
        new public ResourceResponse Response => (ResourceResponse)base.Response;

        public KeyValueRequestArgs(KeyValueReadEventArgs<TKey> request) : base(request) { }
        public KeyValueRequestArgs(TKey key, Type expected = null) : this(new KeyValueReadEventArgs<TKey>(key, expected)) { }

        protected override bool Accept(KeyValueResponse response) => response is ResourceResponse resourceResponse && resourceResponse.Count > 0 && base.Accept(response);
    }

    public class KeyValueRequestArgs<TKey, TValue> : KeyValueRequestArgs<TKey>
        where TKey : Uri
    {
        public new TValue Value => IsHandled ? (TValue)base.Value : default;
        public KeyValueRequestArgs(TKey key) : base(key, typeof(TValue)) { }

        //protected override bool Accept(ResourceResponse response) => base.Accept(response) && response.TryGetRepresentation<TValue>(out _);
    }
#endif
}
