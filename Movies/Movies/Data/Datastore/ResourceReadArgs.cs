using System;

namespace Movies
{
    public class ResourceReadArgs<TKey> : KeyValueReadArgs<TKey>
    {
        public ResourceResponse Response { get; private set; }

        public ResourceReadArgs(TKey key, Type expected = null) : base(key, expected) { }

        public bool Handle(ResourceResponse response)
        {
            if (Accept(response) && base.Handle(response.RawValue))
            {
                Response = response;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual bool Accept(ResourceResponse response) => true;
    }

    public class ResourceReadArgs<TKey, TValue> : ResourceReadArgs<TKey>
    {
        new public TValue Value => IsHandled ? (TValue)base.Value : default;

        public ResourceReadArgs(TKey key) : base(key, typeof(TValue)) { }

        public bool Handle(TValue value) => Handle(new ResourceResponse<TValue>(value));
    }
}
