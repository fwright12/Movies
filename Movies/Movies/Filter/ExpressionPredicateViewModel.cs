using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Movies.ViewModels
{
    public class ExpressionBuilder<T> : BindableViewModel, IPredicateBuilder<T>
    {
        public event EventHandler PredicateChanged;

        public FilterPredicate<T> Predicate { get; private set; }
        public ObservableNode<object> Root { get; }
        public ObservableNode<object> Selected => Editor.Selected?.Parent ?? Root;
        public Editor<T> Editor
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

        private Editor<T> _Editor;

        public ExpressionBuilder()
        {
            Root = new ObservableNode<object>("AND");
            Root.SubtreeChanged += UpdatePredicate;
        }

        private void SelectedChanged(object sender, PropertyChangeEventArgs e)
        {
            if (e.PropertyName != nameof(Editor<T>.Selected))
            {
                return;
            }

            OnPropertyChanged(nameof(Selected));
            SelectedChanged((Editor<T>)sender, e.OldValue as ObservableNode<object>, e.NewValue as ObservableNode<object>);
        }

        private void SelectedChanged(Editor<T> sender, ObservableNode<object> oldValue, ObservableNode<object> newValue)
        {
            if (oldValue?.Value is IPredicateBuilder<T> oldBuilder)
            {
                oldBuilder.PredicateChanged -= SelectedPredicateChanged;
            }

            if (newValue?.Value is IPredicateBuilder<T> newBuilder)
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
            if (node.Value is IPredicateBuilder<T> builder && builder.Predicate != FilterPredicate<T>.TAUTOLOGY)
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
                    if (item.Value is IPredicateBuilder<T> builder)
                    {
                        builder.PredicateChanged -= ChildPredicateChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ObservableNode<object>>())
                {
                    if (item.Value is IPredicateBuilder<T> builder)
                    {
                        builder.PredicateChanged += ChildPredicateChanged;
                    }
                }
            }

            OnPredicateChanged();
        }

        private void ChildPredicateChanged(object sender, EventArgs e)
        {
            var builder = (IPredicateBuilder<T>)sender;

            if (builder.Predicate == FilterPredicate<T>.TAUTOLOGY)
            {
                Root.Remove(builder);
            }

            OnPredicateChanged();
        }

        protected virtual void OnPredicateChanged()
        {
            var exp = new BooleanExp<T>();
            foreach (var child in Root.Children)
            {
                if (child.Value is IPredicateBuilder<T> childBuilder)
                {
                    //exp.Predicates.Add(childBuilder.Predicate);
                }
            }

            Predicate = exp;
            PredicateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}