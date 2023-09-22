using System;

namespace Movies
{
    public class ChainEventArgs : EventArgs
    {
        public bool Handled { get; private set; }

        protected void Handle() => Handled = true;
    }
}