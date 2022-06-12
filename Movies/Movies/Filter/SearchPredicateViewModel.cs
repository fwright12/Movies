using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class SearchPredicateBuilder<T> : BindableViewModel, IPredicateBuilder<T>
    {
        public event EventHandler PredicateChanged;

        public FilterPredicate<T> Predicate { get; private set; }

        public string Query
        {
            get => _Query;
            set
            {
                if (UpdateValue(ref _Query, value))
                {
                    Predicate = new OperatorPredicate<T>
                    {
                        LHS = CollectionViewModel.SearchProperty,
                        Operator = Operators.Equal,
                        RHS = Query
                    };

                    PredicateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _Query;

        public string Placeholder { get; set; }
        public ICommand SearchCommand { get; }
        public int SearchDelay { get; set; }

        private CancellationTokenSource Cancel;

        public SearchPredicateBuilder()
        {
            //Constraints.Add(new Constraint<string>(Name));

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Query))
                {
                    Cancel?.Cancel();
                    Cancel = new CancellationTokenSource();
                    _ = SearchOnTextChanged(Cancel.Token);
                }
            };
        }

        private async Task SearchOnTextChanged(CancellationToken cancellationToken)
        {
            await Task.Delay(SearchDelay, cancellationToken);
            SearchCommand.Execute(Query);
        }
    }
}