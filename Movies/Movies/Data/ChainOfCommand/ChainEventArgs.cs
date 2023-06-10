namespace Movies
{
    public class ChainEventArgs
    {
        public bool Handled { get; private set; }

        protected void Handle() => Handled = true;
    }
}