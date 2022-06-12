using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Movies.ViewModels
{
    public class MultiEditor<T> : Editor<T>, IEnumerable<Editor<T>>
    {
        public IList<Editor<T>> Editors { get; } = new ObservableCollection<Editor<T>>();
        //public IList<ObservableGroup<Property, PredicateEditor<T>>> Editors { get; } = new ObservableCollection<ObservableGroup<Property, PredicateEditor<T>>>();

        //private Dictionary<Property, ObservableGroup<Property, ExpressionBuilder<T>>> Lookup = new Dictionary<Property, ObservableGroup<Property, ExpressionBuilder<T>>>();

        public MultiEditor() //: base(null)//new PredicateTemplate<ExpressionBuilder<T>, T>())
        {
            PropertyChanged += SelectedChanged;

            var editors = new ObservableCollection<Editor<T>>();
            editors.CollectionChanged += EditorsChanged;
            Editors = editors;
        }

        public void Add(Editor<T> editor)
        {
            Editors.Add(editor);
        }

        private void EditorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Editor<T>>())
                {
                    item.PropertyChange -= SubEditorSelectedChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Editor<T>>())
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

            SubEditorSelectedChanged((Editor<T>)sender, e.OldValue as ObservableNode<object>, e.NewValue as ObservableNode<object>);
        }

        private void SubEditorSelectedChanged(Editor<T> editor, ObservableNode<object> oldValue, ObservableNode<object> newValue)
        {
            if (oldValue?.Value is IPredicateBuilder<T> oldBuilder)
            {
                oldBuilder.PredicateChanged -= SubPredicateChanged;
            }

            if (newValue?.Value is IPredicateBuilder<T> newBuilder)
            {
                SubPredicateChanged(newBuilder);
                newBuilder.PredicateChanged += SubPredicateChanged;
            }
        }

        private void SubPredicateChanged(object sender, EventArgs e) => SubPredicateChanged((IPredicateBuilder<T>)sender);

        private void SubPredicateChanged(IPredicateBuilder<T> builder)
        {
            if (Selected?.Value != builder)
            {
                PropertyChanged -= SelectedChanged;

                Selected = Editors.FirstOrDefault(editor => editor.Selected?.Value == builder)?.Selected;

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



            //PropertyChanged += SubPredicateChanged;
        }

        public IEnumerator<Editor<T>> GetEnumerator() => Editors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}