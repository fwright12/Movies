using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Movies.ViewModels
{
    public class MultiEditor : Editor, IEnumerable<Editor>
    {
        public IList<Editor> Editors { get; } = new ObservableCollection<Editor>();
        //public IList<ObservableGroup<Property, PredicateEditor<T>>> Editors { get; } = new ObservableCollection<ObservableGroup<Property, PredicateEditor<T>>>();

        //private Dictionary<Property, ObservableGroup<Property, ExpressionBuilder<T>>> Lookup = new Dictionary<Property, ObservableGroup<Property, ExpressionBuilder<T>>>();

        public MultiEditor() //: base(null)//new PredicateTemplate<ExpressionBuilder<T>, T>())
        {
            PropertyChanged += SelectedChanged;

            var editors = new ObservableCollection<Editor>();
            editors.CollectionChanged += EditorsChanged;
            Editors = editors;
        }

        public void Add(Editor editor)
        {
            Editors.Add(editor);
        }

        private void EditorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Editor>())
                {
                    item.PropertyChange -= SubEditorSelectedChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Editor>())
                {
                    SubEditorSelectedChanged(item, null, item.Selected);
                    item.PropertyChange += SubEditorSelectedChanged;
                }
            }
        }

        private void SubEditorSelectedChanged(object sender, PropertyChangeEventArgs e)
        {
            if (e.PropertyName != nameof(Selected))
            {
                return;
            }

            var newNode = e.NewValue as ObservableNode<object>;
            SubPredicateChanged(newNode?.Value as IPredicateBuilder);
            SubEditorSelectedChanged((Editor)sender, e.OldValue as ObservableNode<object>, newNode);
        }

        private void SubEditorSelectedChanged(Editor editor, ObservableNode<object> oldValue, ObservableNode<object> newValue)
        {
            if (oldValue?.Value is IPredicateBuilder oldBuilder)
            {
                oldBuilder.PredicateChanged -= SubPredicateChanged;
            }

            if (newValue?.Value is IPredicateBuilder newBuilder)
            {
                newBuilder.PredicateChanged += SubPredicateChanged;
            }
        }

        private void SubPredicateChanged(object sender, EventArgs e) => SubPredicateChanged((IPredicateBuilder)sender);

        private void SubPredicateChanged(IPredicateBuilder builder)
        {
            if (Selected?.Value != builder)
            {
                PropertyChanged -= SelectedChanged;

                Select(Editors.FirstOrDefault(editor => editor.Selected?.Value == builder)?.Selected);

                PropertyChanged += SelectedChanged;
            }
        }

        private void SelectedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Selected))
            {
                return;
            }

            //PropertyChanged -= SubPredicateChanged;

            SelectedChanged(Selected);

            //PropertyChanged += SubPredicateChanged;
        }

        private bool SelectedChanged(ObservableNode<object> selected)
        {
            foreach (var editor in Editors)
            {
                if (editor is MultiEditor multiEditor)
                {
                    if (multiEditor.SelectedChanged(selected))
                    {
                        return true;
                    }
                }
                else if (editor is OperatorEditor op && editor.Selected?.Value.GetType() == selected.Value.GetType() && selected.Value is OperatorPredicateBuilder builder && op.LHSOptions.OfType<object>().Contains(builder.LHS))// && op.RHSOptions.OfType<object>().Contains(builder.RHS))
                {
                    editor.Select(selected);
                    return true;
                }
            }

            return false;
        }

        public IEnumerator<Editor> GetEnumerator() => Editors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}