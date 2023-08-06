using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class ExpressionBuilder : BindableViewModel, IPredicateBuilder
    {
        public event EventHandler PredicateChanged;

        public FilterPredicate Predicate { get; private set; }
        public ObservableNode<object> Root { get; }
        public ObservableNode<object> Selected => Editor.Selected?.Parent ?? Root;
        public Editor Editor
        {
            get => _Editor;
            set
            {
                if (_Editor != null)
                {
                    _Editor.PropertyChange -= SelectedChanged;
                }

                UpdateValue(ref _Editor, value);

                if (_Editor != null)
                {
                    _Editor.PropertyChange += SelectedChanged;
                }
            }
        }

        public ICommand PauseChangesCommand { get; }
        public ICommand ResumeChangesCommand { get; }

        private Editor _Editor;
        private bool DeferUpdate;

        public ExpressionBuilder()
        {
            Root = new ObservableNode<object>("AND");
            Root.SubtreeChanged += UpdatePredicate;

            PauseChangesCommand = new Command(PauseChanges);
            ResumeChangesCommand = new Command(ResumeChanges);

            OnPredicateChanged();
        }

        public void PauseChanges() => DeferUpdate = true;
        public void ResumeChanges()
        {
            DeferUpdate = false;
            OnPredicateChanged();
        }

        public void Add(FilterPredicate predicate)
        {
            if (predicate is OperatorPredicate op)
            {
                //var vm = op.LHS is Property ? new PropertyPredicateBuilder() : new OperatorPredicateBuilder();
                var vm = new PropertyPredicateBuilder();
                vm.LHS = op.LHS;
                vm.Operator = op.Operator;
                vm.RHS = op.RHS;

                var node = new ObservableNode<object>(vm);
                
                Add(node);
            }
            else if (predicate is BooleanExpression expression)
            {
                PauseChanges();

                foreach (var part in expression.Predicates)
                {
                    Add(part);
                }

                ResumeChanges();
            }
        }

        private void SelectedChanged(object sender, PropertyChangeEventArgs e)
        {
            if (e.PropertyName != nameof(Editor.Selected))
            {
                return;
            }

            SelectedChanged((Editor)sender, e.OldValue as ObservableNode<object>, e.NewValue as ObservableNode<object>);
            OnPropertyChanged(nameof(Selected));
        }

        private void SelectedChanged(Editor sender, ObservableNode<object> oldValue, ObservableNode<object> newValue)
        {
            if (oldValue?.Value is IPredicateBuilder oldBuilder)
            {
                oldBuilder.PredicateChanged -= SelectedPredicateChanged;
            }

            if (newValue?.Value is IPredicateBuilder newBuilder)
            {
                SelectedPredicateChanged();
                newBuilder.PredicateChanged += SelectedPredicateChanged;
            }
        }

        private void SelectedPredicateChanged(object sender, EventArgs e) => SelectedPredicateChanged();
        private void SelectedPredicateChanged()
        {
            if (Editor.Selected.Parent == null)
            {
                Add(Editor.Selected);
            }
        }

        private void Add(ObservableNode<object> node)
        {
            if (node.Value is IPredicateBuilder builder && builder.Predicate != null && builder.Predicate != FilterPredicate.TAUTOLOGY)
            {
                Root.Add(node);
            }
        }

        private void UpdatePredicate(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<ObservableNode<object>>())
                {
                    if (item.Value is IPredicateBuilder builder)
                    {
                        builder.PredicateChanged -= ChildPredicateChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ObservableNode<object>>())
                {
                    if (item.Value is IPredicateBuilder builder)
                    {
                        builder.PredicateChanged += ChildPredicateChanged;
                    }
                }
            }

            OnPredicateChanged();
        }

        private void ChildPredicateChanged(object sender, EventArgs e)
        {
            var builder = (IPredicateBuilder)sender;

            if (builder.Predicate == null || builder.Predicate == FilterPredicate.TAUTOLOGY)
            {
                Root.SubtreeChanged -= UpdatePredicate;
                Root.Remove(builder);
                Root.SubtreeChanged += UpdatePredicate;
            }

            OnPredicateChanged();
        }

        protected virtual void OnPredicateChanged()
        {
            if (DeferUpdate)
            {
                return;
            }

            if (Root.Children.Count == 0)
            {
                Predicate = FilterPredicate.TAUTOLOGY;
            }
            else
            {
                //Predicate = FormatFilters(Root.Children.Select<ObservableNode<object>, FilterPredicate>(child => (child.Value as IPredicateBuilder)?.Predicate).Where(child => child != null));

                var exp = new BooleanExpression();

                foreach (var child in Root.Children)
                {
                    if (child.Value is IPredicateBuilder childBuilder)
                    {
                        exp.Predicates.Add(childBuilder.Predicate);
                    }
                }

                Predicate = exp;
            }

            PredicateChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(Predicate));
        }

        public static FilterPredicate FormatFilters(IEnumerable<FilterPredicate> predicates)
        {
            var sorted = new Dictionary<object, List<OperatorPredicate>>();
            var expression = new BooleanExpression();

            foreach (var predicate in predicates)
            {
                if (predicate is OperatorPredicate op)
                {
                    if (!sorted.TryGetValue(op.LHS, out var values))
                    {
                        sorted[op.LHS] = values = new List<OperatorPredicate>();
                    }

                    values.Add(op);
                }
                else
                {
                    expression.Predicates.Add(predicate);
                }
            }

            foreach (var kvp in sorted)
            {
                if (kvp.Value.Count == 1)
                {
                    expression.Predicates.Add(kvp.Value[0]);
                }
                else
                {
                    var inner = new BooleanExpression
                    {
                        IsAnd = false
                    };

                    foreach (var value in kvp.Value)
                    {
                        inner.Predicates.Add(value);
                    }

                    expression.Predicates.Add(inner);
                }
            }

            return expression;
        }
    }
}