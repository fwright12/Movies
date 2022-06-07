using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class SelectorViewModel : BindableViewModel
    {
        public string Name { get; set; }

        public FiltersViewModel Filter { get; }
        public IList<Preset> Presets { get; } = new ObservableCollection<Preset>();
        public IList Options => LazyOptions.Value;

        public ICommand CreateNewCommand { get; }
        public ICommand ResetCommand { get; }

        public bool IsImmutable { get; set; }

        public Property Property { get; }
        public ConstraintViewModel Constraint
        {
            get => _Constraint;
            set
            {
                if (Property != value.Constraint.Property)
                {
                    value.Constraint = new Constraint(Property)
                    {
                        Value = value.Value,
                        Comparison = value.Comparison
                    };
                }

                UpdateValue(ref _Constraint, value);
            }
        }
        public Constraint DefaultConstraint { get; }

        public IList<object> Values
        {
            get => _Values;
            set => UpdateValue(ref _Values, value);
        }

        protected IList<ConstraintViewModel> Constraints;
        private IList<object> _Values;

        private ConstraintViewModel _Constraint;
        private Lazy<IList> LazyOptions;

        public SelectorViewModel(Constraint constraint) : this(constraint.Property, constraint) { }

        public SelectorViewModel(Property property) : this(property, new Constraint(property)) { }
        public SelectorViewModel(Property property, Constraint defaultConstraint)
        {
            LazyOptions = new Lazy<IList>(() => new ObservableCollection<object>(property.Values.OfType<object>().Select<object, object>(value => new ConstraintViewModel{
                Constraint = new Constraint(property)
                {
                    Value = value,
                }
            })));

            if (property == null)
            {
                ;
            }

            Filter = (property.Values as CollectionViewModel.FilterCollection<object>)?.Filters;
            Property = property;
            Name = Property?.Name;
            DefaultConstraint = defaultConstraint;

            //Value = new ValueSelectorViewModel(defaultConstraint);

            if (property.Values is CanFilter filterable)
            {
                //FiltersChanged(Filter, EventArgs.Empty);
                //Filter.ValueChanged += FiltersChanged;
            }

            CreateNewCommand = new Command(CreateNew);

            ResetCommand = new Command(Reset);

            PropertyChanging += (sender, e) =>
            {
                if (e.PropertyName == nameof(Values))
                {
                    if (Values is INotifyCollectionChanged observable)
                    {
                        observable.CollectionChanged -= SelectedChanged;
                    }

                    Constraints = new List<ConstraintViewModel>();
                }
            };

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Values))
                {
                    if (Values is INotifyCollectionChanged observable)
                    {
                        //observable.CollectionChanged += SelectedChanged;
                    }

                    //SelectedChanged(Values, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Values));
                }
            };

            PropertyChanged += ConstraintChanged;

            Values = new ObservableCollection<object>();
        }

        private List<SelectorViewModel> Children = new List<SelectorViewModel>();

        public void AddChild(SelectorViewModel selector)
        {
            Children.Add(selector);

            PropertyChanging += ConstraintWillChange;
            PropertyChanged += ConstraintDidChange;

            CopyConstraint();
        }

        private void ConstraintWillChange(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            if (e.PropertyName != nameof(Constraint))
            {
                Constraint.PropertyChanged -= CopyConstraint;
            }
        }

        private void ConstraintDidChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Constraint))
            {
                Constraint.PropertyChanged += CopyConstraint;
            }
        }

        private void CopyConstraint(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConstraintViewModel.Constraint))
            {
                return;
            }

            CopyConstraint();
        }

        private void CopyConstraint()
        {
            foreach (var child in Children)
            {
                if (child.Constraint != null && Constraint != null)
                {
                    child.Constraint.Constraint = Constraint.Constraint;
                }
            }
        }

        public void CreateNew()
        {
            
        }

        private void ConstraintChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Constraint))
            {
                return;
            }

            if (!Values.Contains(Constraint.Value))
            {
                Values.Add(Constraint.Value);
            }
        }

        private void SelectedChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    for (int i = 0; i < Constraints.Count; i++)
                    {
                        var constraint = Constraints[i];

                        //if (constraint.Constraint.Value == item)
                        if (Equals(constraint.Constraint.Value, item))
                        {
                            constraint.Constraint = DefaultConstraint;

                            constraint.PropertyChanging -= ConstraintChanging;
                            Constraints.RemoveAt(i--);
                        }
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    var value = item is Constraint constraint ? constraint.Value : item;

                    if (value?.GetType() == Property?.Type)
                    {
                        Constraints.Add(Constraint);
                        Constraint.Value = value;

                        //Constraint.PropertyChanging += ConstraintChanging;
                    }
                }
            }
        }

        private void ConstraintChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            var constraint = (ConstraintViewModel)sender;

            Values.Remove(constraint.Constraint.Value);
        }

        private void FiltersChanged(object sender, EventArgs e)
        {
            //(Property.Values as CanFilter)?.ApplyFilters(Filter?.GetValue().ToList() ?? new List<Constraint>());
        }

        public virtual void Reset()
        {

        }
    }

    public class SelectorViewModel<T> : SelectorViewModel
    {
        public T Low { get; }
        public T High { get; }
        public object Step { get; set; }

        public SelectorViewModel(Property<T> property, Operators defaultComparison = Operators.Equal, T defaultValue = default, T low = default, T high = default) : base(property, new Constraint(property)
        {
            Value = defaultValue,
            Comparison = defaultComparison
        })
        {
            Low = low;
            High = high;
            //Step = (property.Values as SteppedValueRange)?.Step;
        }

        public override void Reset()
        {
            base.Reset();

            OnPropertyChanged(nameof(Low));
            OnPropertyChanged(nameof(High));
        }
    }
}