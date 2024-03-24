using System;

namespace Movies
{
    public class ResourceRequestArgs<TKey> : ResourceRequestArgs<KeyValueReadEventArgs<TKey>, TKey, ResourceResponse>
        where TKey : Uri
    {
        public ResourceRequestArgs(KeyValueReadEventArgs<TKey> request) : base(request) { }
        public ResourceRequestArgs(TKey key, Type expected = null) : this(new KeyValueReadEventArgs<TKey>(key, expected)) { }
    }

    public class ResourceRequestArgs<TRequest, TKey, TResponse> : KeyValueRequestArgs<TRequest, TKey, TResponse>
        where TKey : Uri
        where TRequest : KeyValueReadEventArgs<TKey>
        where TResponse : ResourceResponse
    {
        public ResourceRequestArgs(TRequest request) : base(request) { }

        protected override bool Accept(TResponse response) => response.Count > 0 && base.Accept(response);
    }

    public class ResourceRequestArgs<TKey, TValue> : ResourceRequestArgs<TKey>
        where TKey : Uri
    {
        public new TValue Value => IsHandled ? (TValue)base.Value : default;
        public ResourceRequestArgs(TKey key) : base(key, typeof(TValue)) { }
    }
}
