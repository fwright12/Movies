using System;

namespace Movies
{
    public class ResourceRequestArgs<TKey> : KeyValueRequestArgs<TKey>
        where TKey : Uri
    {
        new public ResourceResponse Response => (ResourceResponse)base.Response;

        public ResourceRequestArgs(KeyValueReadEventArgs<TKey> request) : base(request) { }
        public ResourceRequestArgs(TKey key, Type expected = null) : this(new KeyValueReadEventArgs<TKey>(key, expected)) { }

        protected override bool Accept(KeyValueResponse response) => response is ResourceResponse resourceResponse && resourceResponse.Count > 0 && base.Accept(response);
    }

    public class ResourceRequestArgs<TKey, TValue> : ResourceRequestArgs<TKey>
        where TKey : Uri
    {
        public new TValue Value => IsHandled ? (TValue)base.Value : default;
        public ResourceRequestArgs(TKey key) : base(key, typeof(TValue)) { }

        //protected override bool Accept(ResourceResponse response) => base.Accept(response) && response.TryGetRepresentation<TValue>(out _);
    }
}
