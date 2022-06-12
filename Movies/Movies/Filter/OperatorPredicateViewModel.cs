using System;
using System.ComponentModel;

namespace Movies.ViewModels
{
    public class OperatorPredicateBuilder<T> : BindableViewModel, IPredicateBuilder<T>
    {
        public event EventHandler PredicateChanged;
        public FilterPredicate<T> Predicate { get; protected set; }

        public object RHS
        {
            get => _RHS;
            set => UpdateValue(ref _RHS, value);
        }

        public Operators Operator
        {
            get => _Operator;
            set => UpdateValue(ref _Operator, value);
        }

        public object LHS
        {
            get => _LHS;
            set => UpdateValue(ref _LHS, value);
        }

        private object _RHS;
        private Operators _Operator;
        private object _LHS;

        public OperatorPredicateBuilder()
        {
            PropertyChanged += BuildPredicate;
        }

        private void BuildPredicate(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LHS) || e.PropertyName == nameof(Operator) || e.PropertyName == nameof(RHS))
            {
                OnPredicateChanged();
                //Predicate = BuildPredicate();
            }
        }

        protected virtual void OnPredicateChanged()
        {
            Predicate = BuildPredicate();
            PredicateChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual FilterPredicate<T> BuildPredicate() => new OperatorPredicate<T>
        {
            LHS = LHS,
            Operator = Operator,
            RHS = RHS
        };
    }
}