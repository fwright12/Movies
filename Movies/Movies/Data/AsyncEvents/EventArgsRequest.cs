using System;

namespace Movies
{
    public class EventArgsRequest : EventArgs
    {
        public EventArgs Request { get; }
        public object Response { get; private set; }

        public bool IsHandled => Response != null;

        public EventArgsRequest(EventArgs request)
        {
            Request = request;
        }

        public bool Handle(object response)
        {
            if (Accept(response))
            {
                Response = response;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual bool Accept(object response) => true;

        public override int GetHashCode() => Request.GetHashCode();
        public override bool Equals(object obj) => obj is EventArgsRequest other && Equals(Request, other.Request);

        public override string ToString() => Request.ToString();
    }

    public class EventArgsRequest<TRequest, TResponse> : EventArgsRequest
        where TRequest : EventArgs
    {
        public new TRequest Request => base.Request is TRequest request ? request : default;
        public new TResponse Response => base.Response is TResponse response ? response : default;

        public EventArgsRequest(TRequest request) : base(request) { }

        protected override bool Accept(object response) => response is TResponse t && Accept(t);

        protected virtual bool Accept(TResponse response) => true;
    }
}