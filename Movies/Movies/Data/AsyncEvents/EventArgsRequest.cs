using System;

namespace Movies
{
    public class EventArgsRequest : EventArgs
    {
        public bool IsHandled { get; private set; }

        protected void Handle()
        {
            IsHandled = true;
        }
    }
}