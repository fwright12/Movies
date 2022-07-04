using System;
using System.ComponentModel;

namespace Movies.ViewModels
{
    public class OperatorPredicateBuilder : BindableViewModel, IPredicateBuilder
    {
        public event EventHandler PredicateChanged;
        public FilterPredicate Predicate { get; protected set; }

        public object LHS
        {
            get => _LHS;
            set => UpdateValue(ref _LHS, value);
        }

        public Operators Operator
        {
            get => _Operator;
            set => UpdateValue(ref _Operator, value);
        }

        public object RHS
        {
            get => _RHS;
            set => UpdateValue(ref _RHS, value);
        }

        private object _LHS;
        private Operators _Operator;
        private object _RHS;

        public OperatorPredicateBuilder()
        {
            PropertyChanged += BuildPredicate;
        }

        private void BuildPredicate(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LHS) || e.PropertyName == nameof(Operator) || e.PropertyName == nameof(RHS))
            {
                Predicate = BuildPredicate();

                if (LHS == null || RHS == null)
                {
                    Predicate = null;
                }

                OnPredicateChanged();
            }
        }

        protected virtual void OnPredicateChanged()
        {
            PredicateChanged?.Invoke(this, EventArgs.Empty);
        }

        public virtual FilterPredicate BuildPredicate() => new OperatorPredicate
        {
            LHS = LHS,
            Operator = Operator,
            RHS = RHS
        };
    }
}