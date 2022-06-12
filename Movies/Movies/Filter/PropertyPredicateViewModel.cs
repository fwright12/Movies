using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class PropertyPredicateBuilder<T> : OperatorPredicateBuilder<T>
    {
        public Property Property => LHS as Property;
        public bool IsValid
        {
            get => _IsValid;
            private set => UpdateValue(ref _IsValid, value);
        }

        public ICommand PauseChangesCommand { get; }
        public ICommand ResumeChangesCommand { get; }

        private bool _IsValid;
        private bool DeferUpdate;

        public PropertyPredicateBuilder()//Property property)
        {
            //Property = property;

            PauseChangesCommand = new Command(PauseChanges);
            ResumeChangesCommand = new Command(ResumeChanges);

            PropertyChanged += Changed;
        }

        public void PauseChanges() => DeferUpdate = true;
        public void ResumeChanges()
        {
            DeferUpdate = false;
            OnPredicateChanged();
        }

        protected override void OnPredicateChanged()
        {
            if (!DeferUpdate)
            {
                base.OnPredicateChanged();
            }
        }

        private void Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LHS) || e.PropertyName == nameof(Operator) || e.PropertyName == nameof(RHS))
            {
                IsValid = Predicate != FilterPredicate<T>.TAUTOLOGY;// && Predicate != FilterPredicate<T>.CONTRADICTION;
            }
        }

        public override FilterPredicate<T> BuildPredicate()
        {
            if (Property?.Values is SteppedValueRange range)
            {
                if (RHS is IComparable comparable)
                {
                    if (Operator != Operators.Equal && (int)Operator == comparable.CompareTo(range.First))
                    {
                        return FilterPredicate<T>.CONTRADICTION;
                    }
                    else
                    {
                        bool min = Operator == Operators.GreaterThan && range.Last == null && Equals(RHS, range.First);
                        bool max = Operator == Operators.LessThan && range.First == null && Equals(RHS, range.Last);

                        if (min || max)
                        {
                            return FilterPredicate<T>.TAUTOLOGY;
                        }
                    }
                }
                else
                {
                    return FilterPredicate<T>.CONTRADICTION;
                }

                //bool isAbsoluteMin = Operator != Operators.Equal && Equals(RHS, range.First);
                //bool isAbsoluteMax = Operator != Operators.Equal && Equals(RHS, range.Last);

                //return RHS != null && !isAbsoluteMin && !isAbsoluteMax;
                //return !(Equals(constraint.Constraint, constraint.Default) || isAbsoluteMin || isAbsoluteMax);
            }
            else if (Property?.Values != null)
            {
                if (!Property.Values.OfType<object>().Contains(RHS))
                {
                    return FilterPredicate<T>.CONTRADICTION;
                }
            }

            return base.BuildPredicate();
        }
    }
}