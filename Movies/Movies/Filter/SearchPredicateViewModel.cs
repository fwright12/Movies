using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class SearchPredicateBuilder : BindableViewModel, IPredicateBuilder
    {
        public event EventHandler PredicateChanged;

        public FilterPredicate Predicate { get; private set; }

        public string Query
        {
            get => _Query;
            set => UpdateValue(ref _Query, value);
        }

        private string _Query;

        public string Placeholder { get; set; }
        public ICommand SearchCommand { get; }
        public int SearchDelay { get; set; } = 1000;

        private CancellationTokenSource Cancel;

        public SearchPredicateBuilder()
        {
            SearchCommand = new Command(BuildPredicate);

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

        private string LastQuery;

        private static string CoerceQuery(string query) => string.IsNullOrEmpty(query) ? string.Empty : query;

        public void BuildPredicate()
        {
            if (CoerceQuery(LastQuery) == CoerceQuery(Query))
            {
                return;
            }

            LastQuery = Query;
            Cancel?.Cancel();

            if (string.IsNullOrEmpty(Query))
            {
                Predicate = FilterPredicate.TAUTOLOGY;
            }
            else
            {
                Predicate = new SearchPredicate
                {
                    Query = Query
                };
            }

            PredicateChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task SearchOnTextChanged(CancellationToken cancellationToken)
        {
            await Task.Delay(SearchDelay, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                BuildPredicate();
            }
        }
    }
}