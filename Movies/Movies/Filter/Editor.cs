using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class Editor<TPredicate> : Editor where TPredicate : IPredicateBuilder, new()
    {
        public override IPredicateBuilder CreateNew() => new TPredicate();
    }

    public class Editor : BindableViewModel
    {
        public ObservableNode<object> Selected
        {
            get => _Selected;
            set => UpdateValue(ref _Selected, value);
        }

        public ICommand AddNewCommand { get; }
        public ICommand SelectCommand { get; }
        public ICommand ResetCommand { get; }

        private ObservableNode<object> _Selected;
        
        public Editor()
        {
            AddNewCommand = new Command(AddNew);
            SelectCommand = new Command<ObservableNode<object>>(Select);
            ResetCommand = new Command(Reset);
        }

        public void AddNew() => Select(new ObservableNode<object>(CreateNew()));

        public void Select(ObservableNode<object> node)
        {
            Selected = node;
        }

        public void Reset()
        {
            Selected.Remove();
            AddNew();
        }

        public virtual IPredicateBuilder CreateNew() => new OperatorPredicateBuilder();
    }
}