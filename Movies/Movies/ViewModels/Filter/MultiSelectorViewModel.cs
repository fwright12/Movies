using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Movies.ViewModels
{
    public abstract class MultiSelectorViewModel : SelectorViewModel
    {
        

        public MultiSelectorViewModel(Property property, Constraint defaultConstraint) : base(property, defaultConstraint)
        {
            IsImmutable = true;

            
        }

        public override void Reset()
        {
            base.Reset();

            //SelectedChanged(Values, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)Values));
            Values.Clear();
        }

        //protected abstract void SelectedChanged(object sender, NotifyCollectionChangedEventArgs e);
    }

    public class MultiSelectorViewModel<T> : MultiSelectorViewModel
    {
        public MultiSelectorViewModel(Property<T> property) : base(property, new Constraint(property)
        {
            Value = default(T),
            Comparison = Operators.Equal
        })
        {
            
        }
    }
}