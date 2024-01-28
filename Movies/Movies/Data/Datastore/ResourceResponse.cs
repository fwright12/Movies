using REpresentationalStateTransfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public abstract class ResourceResponse : KeyValueResponse
    {
        protected ResourceResponse() : base(null) { }
        protected ResourceResponse(object value) : base(value) { }

        public abstract bool TryGetRepresentation(Type type, out object value);

        public bool TryGetRepresentation<T>(out T value)
        {
            if (TryGetRepresentation(typeof(T), out var valueObj) && valueObj is T t)
            {
                value = t;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }

    public class ResourceResponse<T> : ResourceResponse
    {
        public new T Value => (T)base.Value;

        public ResourceResponse(T value) : base(value) { }

        public override bool TryGetRepresentation(Type type, out object value)
        {
            if (type == null || type.IsAssignableFrom(typeof(T)))
            {
                value = Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
