namespace Movies.ViewModels
{
    public class Editor<T> : BindableViewModel
    {
        public ObservableNode<object> Selected
        {
            get => _Selected;
            set => UpdateValue(ref _Selected, value);
        }

        private ObservableNode<object> _Selected;

        public void AddNew()
        {
            Selected = new ObservableNode<object>(CreateNew());
        }

        public virtual IPredicateBuilder<T> CreateNew() => new OperatorPredicateBuilder<T>();
    }
}