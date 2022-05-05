using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Movies.Models;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class ItemTypeFilterViewModel : StringFilterViewModel
    {
        public ItemType Selected
        {
            get
            {
                var selected = (ItemType)0;

                foreach (var value in Enum.GetValues(typeof(ItemType)))
                {
                    var type = (ItemType)value;

                    if (SelectedOptions.Contains(ItemTypeDisplayName(type)))
                    {
                        selected |= type;
                    }
                }

                return selected;
            }
        }

        public ItemTypeFilterViewModel(ItemType allowedTypes)
        {
            Options = new List<string>(allowedTypes.ToString().Split(',').Select(type => ItemTypeDisplayName(Enum.Parse<ItemType>(type.Trim()))));

            SelectedOptions = new ObservableCollection<object>();
            SelectedOptions.CollectionChanged += CoerceSelection;
        }

        private string ItemTypeDisplayName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Movie:
                case ItemType.Collection: return type.ToString() + "s";
                case ItemType.TVShow: return "TV Shows";
                case ItemType.Person: return "People";
                case ItemType.Company: return "Companies";
                default: return type.ToString();
            }
        }

        public override Task<bool> Allowed(Item item) => Task.FromResult(Selected.HasFlag(item.ItemType));

        private bool IsExclusive(object item) => item is string str && (str == Options[3]);// || str == Options[4]);

        private void CoerceSelection(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedOptions.Count > 1)
            {
                ObservableCollection<object> oldSelectedOptions = SelectedOptions;

                if (e.NewItems != null)
                {
                    foreach (object item in e.NewItems)
                    {
                        if (IsExclusive(item))
                        {
                            SelectedOptions = new ObservableCollection<object> { (string)item };
                            break;
                        }
                    }
                }

                if (SelectedOptions.Count > 1 && Enumerable.Any(SelectedOptions, IsExclusive))
                {
                    SelectedOptions = new ObservableCollection<object>(Enumerable.Where(SelectedOptions, value => !IsExclusive(value)));
                }

                if (SelectedOptions != oldSelectedOptions)
                {
                    SelectedOptions.CollectionChanged += CoerceSelection;
                    OnPropertyChanged(nameof(SelectedOptions));
                }
            }

            OnValueChanged();
        }
    }

    public class StringFilterViewModel : FilterViewModel
    {
        public IList<string> Options { get; set; }
        public ObservableCollection<object> SelectedOptions { get; set; }
    }

    public class BooleanExpression<T> : IBooleanValued<T>
    {
        public IList<T> Parts { get; } = new List<T>();

        public bool Evaluate(T value) => Parts.All(part => Evaluate(part));
    }

    public abstract class SetConstraintViewModel : ConstraintViewModel
    {
        public BooleanExpressionViewModel Value { get; } = new BooleanExpressionViewModel();

        public SetConstraintViewModel(ItemPropertyViewModel property) : base(property)
        {

        }
    }

    public class SetConstraintViewModel<T> : SetConstraintViewModel
    {
        public IList<object> SelectedOptions { get; }

        public ICommand AddValueCommand { get; }

        public SetConstraintViewModel(ItemPropertyViewModel<T> property) : base(property)
        {
            AddValueCommand = new Command<T>(AddConstraint);

            var selected = new ObservableCollection<object>();
            selected.CollectionChanged += SelectedChanged;
            SelectedOptions = selected;

            if (Value.Expression is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += ConstraintsChanged;
            }
        }

        public override bool HasValues() => Value.Expression.Count > 0;

        private void RemoveConstraint(T value)
        {
            for (int i = 0; i < Value.Expression.Count; i++)
            {
                var constraint = Value.Expression[i];

                if (constraint is LiteralViewModel<T> literal && Equals(GetConstraint(value), literal.Constraint))
                {
                    Value.Expression.RemoveAt(i--);
                    break;
                }
            }
        }

        private void AddConstraint(T value) => Value.Expression.Add(new LiteralViewModel<T>((ItemPropertyViewModel<T>)Property, GetConstraint(value)));

        private Constraint<T> GetConstraint(T value) => new Constraint<T>(Property.Property)
        {
            Value = value,
            Comparison = Operators.Equal
        };

        private void SelectedChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var observable = Value.Expression as INotifyCollectionChanged;
            if (observable != null)
            {
                observable.CollectionChanged -= ConstraintsChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<T>())
                {
                    RemoveConstraint(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<T>())
                {
                    AddConstraint(item);
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += ConstraintsChanged;
            }
        }

        private void ConstraintsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var observable = SelectedOptions as INotifyCollectionChanged;
            if (observable != null)
            {
                observable.CollectionChanged -= SelectedChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<LiteralViewModel<T>>())
                {
                    SelectedOptions.Remove(item.Value);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<LiteralViewModel<T>>())
                {
                    SelectedOptions.Add(item.Value);
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += SelectedChanged;
            }
        }
    }

    public class BooleanExpressionViewModel
    {
        public IList<ConstraintViewModel> Expression { get; }

        public ICommand AddConstraintCommand { get; }
        public ICommand RemoveConstraintCommand { get; }

        public BooleanExpressionViewModel()
        {
            var expression = new ObservableCollection<ConstraintViewModel>();
            expression.CollectionChanged += (sender, e) =>
            {
                //Constraint = new BooleanExpression<Item>()
            };
            Expression = expression;

            RemoveConstraintCommand = new Command<ConstraintViewModel>(RemoveConstraint);
        }

        public void AddConstraint(ConstraintViewModel constraint) => Expression.Add(constraint);

        public void RemoveConstraint(ConstraintViewModel constraint) => Expression.Remove(constraint);
    }

    public abstract class ConstraintViewModel : BindableViewModel
    {
        public string Name => Property.DisplayName;
        public ItemPropertyViewModel Property { get; }

        public ConstraintViewModel(ItemPropertyViewModel property)
        {
            Property = property;
        }

        public abstract bool HasValues();
    }

    public abstract class LiteralViewModel : ConstraintViewModel
    {
        public Constraint Constraint
        {
            get => _Constraint;
            set => UpdateValue(ref _Constraint, value);
        }

        public Operators Comparison
        {
            get => _Comparison;
            set => UpdateValue(ref _Comparison, value);
        }

        public Constraint Default { get; }

        private Constraint _Constraint;
        private Operators _Comparison;

        public LiteralViewModel(ItemPropertyViewModel property, Constraint defaultConstraint) : base(property)
        {
            Constraint = Default = defaultConstraint;
        }
    }

    public class LiteralViewModel<T> : LiteralViewModel
    {
        public bool Comparable => Value is IComparable;

        public T Value
        {
            get => _Value;
            set => UpdateValue(ref _Value, value);
        }

        private T _Value;

        public LiteralViewModel(ItemPropertyViewModel<T> property) : this(property, new Constraint<T>(property.Property)
        {
            Value = default,
            Comparison = Operators.GreaterThan
        })
        { }

        public LiteralViewModel(ItemPropertyViewModel<T> property, Constraint<T> constraint) : base(property, constraint)
        {
            ConstraintChanged();

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Constraint))
                {
                    ConstraintChanged();
                }
                else if (e.PropertyName == nameof(Value) || e.PropertyName == nameof(Comparison))
                {
                    ComponentsChanged();
                }
            };
        }

        public override bool HasValues()
        {
            if (Property.Values is SteppedValueRange range)
            {
                bool isAbsoluteMin = Comparison != Operators.Equal && Equals(Value, range.LowerBound);
                bool isAbsoluteMax = Comparison != Operators.Equal && Equals(Value, range.UpperBound);

                return !(Equals(Constraint, Default) || isAbsoluteMin || isAbsoluteMax);
            }

            return true;
        }

        /*protected override bool TryGetConstraint(object value, out Constraint constraint)
        {
            if (value is T t)
            {
                constraint = new Constraint<T>(Name)
                {
                    Value = t,
                    Comparison = Operators.Equal
                };
                return true;
            }

            constraint = null;
            return false;
        }*/

        //public static implicit operator ConstraintViewModel<T>(Constraint<T> constraint) => new ConstraintViewModel<T>(constraint);

        /*public override void Clear()
        {
            base.Clear();

            Constraint = Default;
            OnPropertyChanged(nameof(Min));
            OnPropertyChanged(nameof(Max));
        }*/

        private void ConstraintChanged()
        {
            Value = (T)Constraint.Value;
            Comparison = Constraint.Comparison;
        }

        private void ComponentsChanged()
        {
            Constraint = new Constraint<T>(Constraint.Name)
            {
                Value = Value,
                Comparison = Comparison
            };
        }

        private async Task ComponentsChangedDelayed(CancellationToken token)
        {
            await Task.Delay(1000, token);
            ComponentsChanged();
        }
    }

    public abstract class CompareConstraintViewModel : FilterViewModel { }
    public class CompareConstraintViewModel<T> : CompareConstraintViewModel
    {
        public Constraint<T> Constraint
        {
            get => _Constraint;
            set => UpdateValue(ref _Constraint, value);
        }

        public T Value
        {
            get => _Value;
            set => UpdateValue(ref _Value, value);
        }

        public Operators Comparison
        {
            get => _Comparison;
            set => UpdateValue(ref _Comparison, value);
        }

        public Constraint<T> Default { get; }

        public T Min { get; }
        public T Max { get; }
        public int Step { get; set; } = 1;
        public object AbsoluteMin { get; set; }
        public object AbsoluteMax { get; set; }

        private Constraint<T> _Constraint;
        private T _Value;
        private Operators _Comparison;

        private CancellationTokenSource Cancel;

        public CompareConstraintViewModel(string name, T min = default, T max = default) : this(name, new Constraint<T>(name)
        {
            Value = default,
            Comparison = Operators.GreaterThan
        }, min, max)
        { }

        public CompareConstraintViewModel(string name, Constraint<T> defaultConstraint, T min = default, T max = default)
        {
            Name = name;
            Min = min;
            Max = max;
            Constraint = Default = defaultConstraint;
            ConstraintChanged();

            PropertyChanging += (sender, e) =>
            {
                if (e.PropertyName == nameof(Constraint))
                {
                    Constraints.Remove(Constraint);
                }
            };
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Constraint))
                {
                    bool isAbsoluteMin = Comparison != Operators.Equal && Equals(Value, AbsoluteMin);
                    bool isAbsoluteMax = (Comparison != Operators.Equal) && Equals(Value, AbsoluteMax);

                    if (!Equals(Constraint, Default) && !isAbsoluteMin && !isAbsoluteMax)
                    {
                        Constraints.Add(Constraint);
                    }
                }
            };

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Constraint))
                {
                    ConstraintChanged();
                }
                else if (e.PropertyName == nameof(Value) || e.PropertyName == nameof(Comparison))
                {
                    Cancel?.Cancel();
                    Cancel = new CancellationTokenSource();
                    _ = ComponentsChangedDelayed(Cancel.Token);
                }
            };
        }

        public override void Clear()
        {
            base.Clear();

            Constraint = Default;
            OnPropertyChanged(nameof(Min));
            OnPropertyChanged(nameof(Max));
        }

        private void ConstraintChanged()
        {
            Value = Constraint.Value;
            Comparison = Constraint.Comparison;
        }

        private void ComponentsChanged()
        {
            Constraint = new Constraint<T>(Name)
            {
                Value = Value,
                Comparison = Comparison
            };
        }

        private async Task ComponentsChangedDelayed(CancellationToken token)
        {
            await Task.Delay(1000, token);
            ComponentsChanged();
        }
    }

    public abstract class ItemPropertyViewModel : BindableViewModel
    {
        public string Property { get; set; }
        public string DisplayName { get; set; }
        public Type Type { get; protected set; }

        public ValueRange Values { get; }

        public ItemPropertyViewModel(string name, ValueRange values, string displayName = null)
        {
            Property = name;
            DisplayName = displayName ?? Property;
            Values = values;
        }

        public abstract ConstraintViewModel GetConstraintViewModel();
    }

    public class ItemPropertyViewModel<T> : ItemPropertyViewModel
    {
        public ItemPropertyViewModel(string name, ValueRange values, string displayName = null) : base(name, values, displayName)
        {
            Type = typeof(T);
        }

        public static implicit operator ConstraintViewModel(ItemPropertyViewModel<T> property) => new LiteralViewModel<T>(property);

        public override ConstraintViewModel GetConstraintViewModel() => default(T) is IComparable ? new LiteralViewModel<T>(this) : (ConstraintViewModel)new SetConstraintViewModel<T>(this);
    }

    public abstract class ValueRange { }
    public abstract class ValueRange<T> : ValueRange { }

    public class SteppedValueRange : ValueRange
    {
        public object LowerBound { get; set; }
        public object UpperBound { get; set; }
    }

    public class SteppedValueRange<TValue, TStep> : SteppedValueRange
    {
        public TValue High { get; set; }
        public TValue Low { get; set; }
        public TStep Step { get; set; }

        public SteppedValueRange() { }
        public SteppedValueRange(TValue lowerBound, TValue upperBound)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        //public abstract TValue Add(TValue value, TStep increment);
        //public abstract TValue Subtract(TValue value, TStep increment);
    }

    /*public class DoubleValueRange : SteppedValueRange<double, double>
    {
        public override double Add(double value, double increment) => value + increment;

        public override double Subtract(double value, double increment) => value - increment;
    }

    public class DateTimeValueRange : SteppedValueRange<DateTime, TimeSpan>
    {
        public override DateTime Add(DateTime value, TimeSpan increment) => value + increment;

        public override DateTime Subtract(DateTime value, TimeSpan increment) => value - increment;
    }*/

    public class DiscreteValueRange<T> : ValueRange<T>
    {
        public ObservableCollection<T> Values { get; } = new ObservableCollection<T>();
        public IList<FilterViewModel> Filters { get; } = new ObservableCollection<FilterViewModel>();
        public Collection<T> Items { get; }

        public ICommand AddValueCommand { get; set; }

        public DiscreteValueRange() : this(new Collection<T>()) { }
        public DiscreteValueRange(IEnumerable<T> items) : this(new Collection<T>(items)) { }
        public DiscreteValueRange(Collection<T> items)
        {
            Items = items;

            _ = ConstraintsChanged();
        }

        protected async Task ConstraintsChanged()
        {
            Values.Clear();

            await foreach (var result in Items.GetItems(new List<Constraint>(Filters.SelectMany(filter => filter.Constraints))))
            {
                Values.Add(result);
            }
        }
    }

    public class EnumFilterViewModel<T> : DiscreteFilterViewModel<string> where T : struct, Enum
    {
        public static IEnumerable<T> GetValues(T value) => value.ToString().Split(',').Select(type => Enum.Parse<T>(type.Trim()));

        public EnumFilterViewModel(string name) : base(name)
        {
            foreach (var value in Enum.GetNames(typeof(T)))
            {
                Results.Add(value);
            }
        }
    }

    public class ComparisonConverter : IValueConverter
    {
        public static readonly ComparisonConverter Instance = new ComparisonConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Operators comparison && comparison != Operators.NotEqual ? new List<int> { (int)comparison + 1 } : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (Operators)(((value as IList<int>)?.FirstOrDefault() ?? 0) - 1);
    }

    public class Collection<T>
    {
        private List<T> Items { get; }
        private Func<List<Constraint>, IAsyncEnumerable<T>> GetItemsFunc;

        public Collection() : this(null) { }
        public Collection(Func<List<Constraint>, IAsyncEnumerable<T>> getItems) : this(new List<T>(), getItems) { }

        public Collection(IEnumerable<T> items, Func<List<Constraint>, IAsyncEnumerable<T>> getItems = null)
        {
            Items = new List<T>(items);
            GetItemsFunc = getItems;
        }

        public async IAsyncEnumerable<T> GetItems(List<Constraint> constraints)
        {
            if (GetItemsFunc != null && constraints.Count > 0)
            {
                await foreach (var item in GetItemsFunc(constraints))
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in Items)
                {
                    yield return item;
                }
            }
        }
    }

    public abstract class DiscreteFilterViewModel : FilterViewModel
    {
        public IList<FilterViewModel> Filters { get; }
        public IList<object> SelectedOptions { get; set; }

        public DiscreteFilterViewModel(string name)
        {
            Name = name;

            if (Constraints is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += ConstraintsChanged;
            }

            var filters = new ObservableCollection<FilterViewModel>();
            filters.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<FilterViewModel>())
                    {
                        item.ValueChanged -= ConstraintValueChanged;
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<FilterViewModel>())
                    {
                        item.ValueChanged += ConstraintValueChanged;
                    }
                }
            };
            Filters = filters;

            var selected = new ObservableCollection<object>();
            selected.CollectionChanged += SelectedChanged;
            selected.CollectionChanged += (sender, e) =>
            {
                OnValueChanged();
            };

            SelectedOptions = selected;
        }

        protected abstract bool TryGetConstraint(object value, out Constraint constraint);
        protected abstract Task ConstraintsChanged();

        private void ConstraintValueChanged(object sender, EventArgs e) => ConstraintsChanged();

        private void ConstraintsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var observable = SelectedOptions as INotifyCollectionChanged;
            if (observable != null)
            {
                observable.CollectionChanged -= SelectedChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Constraint>())
                {
                    SelectedOptions.Remove(item.Value);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Constraint>())
                {
                    SelectedOptions.Add(item.Value);
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += SelectedChanged;
            }
        }

        private void SelectedChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var observable = Constraints as INotifyCollectionChanged;
            if (observable != null)
            {
                observable.CollectionChanged -= ConstraintsChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (TryGetConstraint(item, out var constraint))
                    {
                        Constraints.Remove(constraint);
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (TryGetConstraint(item, out var constraint))
                    {
                        Constraints.Add(constraint);
                    }
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += ConstraintsChanged;
            }
        }
    }

    public class DiscreteFilterViewModel<T> : DiscreteFilterViewModel
    {
        public ObservableCollection<T> Results { get; } = new ObservableCollection<T>();
        public Collection<T> Items { get; }

        public ICommand AddValueCommand { get; set; }

        public DiscreteFilterViewModel(string name) : this(name, new Collection<T>()) { }
        public DiscreteFilterViewModel(string name, Collection<T> items) : base(name)
        {
            Items = items;
            AddValueCommand = new Command<T>(AddConstraint);

            _ = ConstraintsChanged();
        }

        protected override bool TryGetConstraint(object value, out Constraint constraint)
        {
            if (value is T t)
            {
                constraint = new Constraint<T>(Name)
                {
                    Value = t,
                    Comparison = Operators.Equal
                };
                return true;
            }

            constraint = null;
            return false;
        }

        private void AddConstraint(T value)
        {
            if (TryGetConstraint(value, out var constraint))
            {
                AddConstraint(constraint);
            }
        }

        protected override async Task ConstraintsChanged()
        {
            Results.Clear();

            await foreach (var results in Items.GetItems(new List<Constraint>(Filters.SelectMany(filter => filter.Constraints))))
            {
                Results.Add(results);
            }
        }

        public override async Task<bool> Allowed(Item item)
        {
            T value = default;// await MovieService.GetValue<T>(item, Name);
            return value is IEnumerable<T> items && items.Contains(value);
        }
    }

    public class SearchFilterViewModel : FilterViewModel
    {
        public string Query
        {
            get => _Query;
            set
            {
                if (value != _Query)
                {
                    _Query = value;
                    OnPropertyChanged(nameof(Query));
                }
            }
        }

        public string Placeholder { get; set; }
        public ICommand SearchCommand { get; }
        public int SearchDelay { get; set; }

        private CancellationTokenSource Cancel;

        private string _Query;

        public SearchFilterViewModel()
        {
            Constraints.Add(new Constraint<string>(Name));

            SearchCommand = new Command(() => OnValueChanged());
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Query) && Query == null)
                {
                    OnValueChanged();
                }
            };

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

        private async Task SearchOnTextChanged(CancellationToken cancellationToken)
        {
            if (SearchDelay > 0)
            {
                await Task.Delay(SearchDelay, cancellationToken);
            }

            SearchCommand.Execute(Query);
        }
    }

    public abstract class BindableViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void UpdateValue<T>(ref T property, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (!Equals(property, value))
            {
                OnPropertyChanging(propertyName);
                property = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected virtual void OnPropertyChanging([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
            PropertyChanging?.Invoke(this, new System.ComponentModel.PropertyChangingEventArgs(property));
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public class Preset : BindableViewModel
    {
        public string Text { get; set; }
        public bool IsActive
        {
            get => _IsActive;
            set
            {
                if (value != _IsActive)
                {
                    _IsActive = value;
                    OnPropertyChanged();
                }
            }
        }
        public Constraint Value { get; set; }

        private bool _IsActive;
    }

    public abstract class Property
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }

    public class Property<T> : Property
    {
        public Property()
        {
            Type = typeof(T);
        }
    }

    public class FilterTemplateSelector : TypeTemplateSelector
    {
        protected override Type GetType(object item) => item.GetType().GenericTypeArguments.FirstOrDefault() ?? base.GetType(item);
        //protected override Type GetType(object item) => (item as ItemPropertyViewModel)?.Type ?? base.GetType(item);
    }

    public class FiltersViewModel : BindableViewModel
    {
        public BooleanExpressionViewModel Constraints { get; }
        public IList<ConstraintViewModel> SelectedConstraints { get; }
        public IList<Preset> Presets { get; } = new ObservableCollection<Preset>();
        public IList<ItemPropertyViewModel> Properties { get; } = new ObservableCollection<ItemPropertyViewModel>();

        public ICommand ClearCommand { get; }

        public FiltersViewModel()
        {
            ClearCommand = new Command(() => Clear());

            var constraints = new BooleanExpressionViewModel();
            if (constraints.Expression is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += (sender, e) =>
                {
                    var list = (IList<ConstraintViewModel>)sender;

                    foreach (var preset in Presets)
                    {
                        //preset.IsActive = list.Count == 1 && list[0].Constraint.Equals(preset.Value);
                    }
                };
            }
            Constraints = constraints;

            var selected = new ObservableCollection<ConstraintViewModel>();
            selected.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<LiteralViewModel>())
                    {
                        item.PropertyChanged -= CoerceLiteral;
                    }

                    foreach (var item in e.OldItems.OfType<SetConstraintViewModel>())
                    {
                        if (item.Value.Expression is INotifyPropertyChanged observable)
                        {

                        }
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<LiteralViewModel>())
                    {
                        item.PropertyChanged += CoerceLiteral;
                    }

                    foreach (var item in e.NewItems.OfType<SetConstraintViewModel>())
                    {
                        if (item.Value.Expression is INotifyPropertyChanged observable)
                        {

                        }
                    }
                }
            };
            SelectedConstraints = selected;

            ((INotifyCollectionChanged)Properties).CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<ItemPropertyViewModel>())
                    {
                        var constraint = item.GetConstraintViewModel();

                        SelectedConstraints.Add(constraint);
                        Constraints.Expression.Add(constraint);
                    }
                }

                //Constraints.Expression[0] = SelectedConstraints[0];
            };
        }

        private void CoerceLiteral(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LiteralViewModel.Constraint))
            {
                return;
            }

            
        }

        public virtual void Clear()
        {
            //Constraints.Clear();
        }
    }

    public class FilterViewModel : BindableViewModel
    {
        public event EventHandler ValueChanged;

        public string Name { get; set; }
        public IList<Constraint> Constraints { get; }
        public IList<Preset> Presets { get; } = new ObservableCollection<Preset>();
        public IList<ItemPropertyViewModel> Properties { get; } = new ObservableCollection<ItemPropertyViewModel>();

        public ICommand AddConstraintCommand { get; }
        public ICommand RemoveConstraintCommand { get; }
        public ICommand ItemTappedCommand { get; set; }
        public ICommand ClearCommand { get; }

        public FilterViewModel()
        {
            RemoveConstraintCommand = new Command<Constraint>(RemoveConstraint);
            ClearCommand = new Command(() => Clear());

            var constraints = new ObservableCollection<Constraint>();
            constraints.CollectionChanged += (sender, e) =>
            {
                var list = (IList<Constraint>)sender;

                foreach (var preset in Presets)
                {
                    preset.IsActive = list.Count == 1 && list[0].Equals(preset.Value);
                }
            };
            Constraints = constraints;
        }

        public static implicit operator FilterViewModel(Constraint filter)
        {
            var result = new FilterViewModel { Name = filter.Name };
            result.Constraints.Add(filter);
            return result;
        }

        public void AddConstraint(Constraint constraint) => Constraints.Add(constraint);

        public void RemoveConstraint(Constraint constraint) => Constraints.Remove(constraint);

        public virtual void Clear()
        {
            Constraints.Clear();
        }

        public virtual IEnumerable<Constraint> GetFilters() => Constraints;

        public virtual Task<bool> Allowed(Item item) => Task.FromResult(true);

        protected void OnValueChanged()
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }
    }

    public class AsyncList<T> : ObservableCollection<T>
    {
        public ICommand LoadCommand { get; }
        public bool Loading { get; private set; }

        private IAsyncEnumerator<T> Itr;

        public AsyncList(IAsyncEnumerable<T> items)
        {
            Itr = items.GetAsyncEnumerator();

            LoadCommand = new Command<int?>(count => _ = LoadLists(count));
        }

        private async Task LoadLists(int? count = null)
        {
            if (Loading)
            {
                return;
            }

            Loading = true;

            for (int i = 0; i < (count ?? 1) && await Itr.MoveNextAsync(); i++)
            {
                Add(Itr.Current);
            }

            Loading = false;
        }
    }

    public class ListSourceViewModel : ObservableCollection<CollectionViewModel>
    {
        public IListProvider Source { get; }
        public string Name { get; set; }

        public ListSourceViewModel(IListProvider source)
        {
            Source = source;
        }
    }

    public class NamedListViewModel : ListViewModel
    {
        public NamedListViewModel(DataManager dataManager, string name, IEnumerable<SyncOptions> sources, ItemType? allowedTypes = null) : base(dataManager, sources, allowedTypes, true)
        {
            if (Item is List list)
            {
                list.Name = name;
            }
        }
    }

    public class ListViewModel : CollectionViewModel
    {
        public event EventHandler SyncChanged;
        public event EventHandler<SavedEventArgs> Saved;

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand RemoveMultipleCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand RemoveSyncCommand { get; }

        public override bool Editable => true;
        public bool Editing
        {
            get => _Editing;
            set
            {
                if (_Editing != value)
                {
                    _Editing = value;
                    OnPropertyChanged();
                }
            }
        }
        public ICommand ToggleEditCommand { get; }

        public bool Syncing
        {
            get => _Syncing;
            set
            {
                if (value != _Syncing)
                {
                    _Syncing = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _Editing;
        private bool _Syncing;

        public ObservableCollection<SyncOptions> SyncWith { get; private set; }

        private Dictionary<object, bool?> ContainsCache = new Dictionary<object, bool?>();
        private IList<SyncOptions> SyncBackup;
        private ItemType? AllowedTypes;

        private bool IsAllowed(Item item) => (AllowedTypes ?? (Item as List)?.AllowedTypes)?.HasFlag(item.ItemType) == true;

        public class SyncOptions
        {
            public IListProvider Provider { get; set; }
            public List List { get; set; }
            public int Direction { get; set; }
        }

        public class SavedEventArgs
        {
            public IList<SyncOptions> OldSources { get; set; }
            public IList<SyncOptions> NewSources { get; set; }
        }

        public ListViewModel(DataManager manager, params SyncOptions[] sources) : this(manager, (IEnumerable<SyncOptions>)sources) { }
        public ListViewModel(DataManager manager, IEnumerable<SyncOptions> sources, ItemType? allowedTypes = null, bool reverse = false) : base(manager, new SyncList(sources.Select(sync => sync.List), reverse))
        {
            SyncWith = new ObservableCollection<SyncOptions>(sources);
            SyncBackup = new List<SyncOptions>(SyncWith);
            AllowedTypes = allowedTypes;

            SyncCommand = new Command<SyncOptions>(AddSync);
            RemoveSyncCommand = new Command<SyncOptions>(options => RemoveSync(options), options => SyncWith.Count > 1);
            SyncWith.CollectionChanged += (sender, e) => (RemoveSyncCommand as Command)?.ChangeCanExecute();

            AddCommand = new Command<Item>(item => _ = Add(item), item => item != null && IsAllowed(item) && Contains(item) == false);
            RemoveCommand = new Command<Item>(item => _ = Remove(item), item => item != null && IsAllowed(item) && Contains(item) == true);
            RemoveMultipleCommand = new Command<IEnumerable>(items => _ = Remove(items.OfType<ItemViewModel>().Select(item => item.Item)));

            DeleteListCommand = new Command(async () =>
            {
                //await Task.WhenAll(SyncWith.Select(source => source.List.Delete()));
                await (Item as List)?.Delete();
            });

            ToggleEditCommand = new Command(async () =>
            {
                Editing = !Editing;

                if (!Editing)
                {
                    await Save();
                }
            });

            if (Items is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += ListChanged;
            }
        }

        public void AddSync(SyncOptions options)
        {
            SyncWith.Add(options);
            SyncChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool RemoveSync(SyncOptions options)
        {
            if (SyncWith.Count > 1 && SyncWith.Remove(options))
            {
                SyncChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public async Task Save()
        {
            //await Task.WhenAll(SyncWith.Select(sync => sync.List.Update()));

            for (int i = 0; i < SyncBackup.Count; i++)
            {
                var backup = SyncBackup[i];

                for (int j = 0; j < SyncWith.Count; j++)
                {
                    var sync = SyncWith[j];

                    if (sync.Provider == backup.Provider)
                    {
                        if (sync.List != backup.List)
                        {
                            //SyncBackup[i] = same;
                            SyncWith[j] = backup;
                        }

                        break;
                    }
                }
            }

            var oldSources = SyncBackup.Except(SyncWith).ToList();
            var newSources = SyncWith.Except(SyncBackup).ToList();

            try
            {
                foreach (var source in oldSources)
                {
                    (Item as SyncList)?.RemoveSource(source.List);
                }

                if (Item is List list)
                {
                    await list.Update();
                }

                if (Item is SyncList syncList)
                {
                    var before = syncList.Count;

                    foreach (var source in newSources)
                    {
                        await syncList.AddSourceAsync(source.List);
                    }

                    if (syncList.Count != before)
                    {
                        syncList.Reset();
                        UpdateItems(syncList);
                    }
                }

                OnPropertyChanged(nameof(Item));
                OnSaved(oldSources, newSources);
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                ;
#endif
            }
        }

        protected virtual void OnSaved(IList<SyncOptions> oldSources, IList<SyncOptions> newSources)
        {
            Saved?.Invoke(this, new SavedEventArgs { OldSources = oldSources, NewSources = newSources });
            SyncBackup = new List<SyncOptions>(SyncWith);
        }

        public void UpdateCanExecute()
        {
            (AddCommand as Command)?.ChangeCanExecute();
            (RemoveCommand as Command)?.ChangeCanExecute();
        }

        public async Task Add(params Item[] items)
        {
            await ListChangedAsync(items, null);

            var observable = Items as INotifyCollectionChanged;

            if (observable != null)
            {
                observable.CollectionChanged -= ListChanged;
            }

            var order = this is NamedListViewModel ? items.Reverse() : items;
            //for (int i = items.Length - 1; i >= 0; i--)
            foreach (var item in order)
            {
                //ContainsCache[item] = true;
                if (Items is IList list)
                {
                    int index = -1;
                    if (this is NamedListViewModel)
                    {
                        index = 0;
                    }
                    else if (!(Item is Collection collection) || collection.IsFullyLoaded)
                    {
                        index = list.Count;
                    }

                    if (index != -1)
                    {
                        list.Insert(index, Map(item));
                    }
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += ListChanged;
            }

            //await Task.WhenAll(SyncWith.Select(source => source.List.AddAsync(items)));
            //await (Item as List)?.AddAsync(items);
        }

        public Task Remove(params Item[] items) => Remove((IEnumerable<Item>)items);
        public async Task Remove(IEnumerable<Item> items)
        {
            await ListChangedAsync(null, items);

            var observable = Items as INotifyCollectionChanged;

            if (observable != null)
            {
                observable.CollectionChanged -= ListChanged;
            }

            foreach (var item in items)
            {
                //ContainsCache[item] = false;

                if (Items is IList list)
                {
                    int index = 0;
                    foreach (var model in list)
                    {
                        if (model is ItemViewModel ivm && ivm.Item.Equals(item))
                        {
                            break;
                        }

                        index++;
                    }

                    if (index < list.Count)
                    {
                        list.RemoveAt(index);
                    }
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += ListChanged;
            }

            //await Task.WhenAll(SyncWith.Select(list => list.List.RemoveAsync(items)));
            //await (Item as List)?.RemoveAsync(items);
        }

        private async void ListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Loading)
            {
                await ListChangedAsync(e.NewItems?.OfType<ItemViewModel>().Select(ivm => ivm.Item), e.OldItems?.OfType<ItemViewModel>().Select(ivm => ivm.Item));
            }
        }

        private async Task ListChangedAsync(IEnumerable<Item> newItems, IEnumerable<Item> oldItems)
        {
            if (newItems != null)
            {
                foreach (var item in newItems)
                {
                    ContainsCache[item] = true;
                }

                foreach (var sync in SyncWith)
                {
                    //await sync.List.AddAsync(newItems);
                }
                await (Item as List)?.AddAsync(newItems);
            }

            if (oldItems != null)
            {
                foreach (var item in oldItems)
                {
                    ContainsCache[item] = false;
                }

                foreach (var sync in SyncWith)
                {
                    //await sync.List.RemoveAsync(oldItems);
                }
                await (Item as List)?.RemoveAsync(oldItems);
            }

            UpdateCanExecute();
        }

        private bool? Contains(Item item)
        {
            /*var check = (Item as List)?.Contains(item);
            if (check.Status == TaskStatus.RanToCompletion)
            {
                return check.Result;
            }
            else
            {
                _ = CanExecuteAsync(check);
                return null;
            }*/

            if (ContainsCache.TryGetValue(item, out var contains))
            {
                if (contains.HasValue)
                {
                    //ContainsCache.Remove(item);
                }

                return contains;
            }
            else
            {
                ContainsCache[item] = null;
                var check = CanExecuteAsync(item);
                return check.Status == TaskStatus.RanToCompletion ? (bool?)check.Result : null;
            }
        }

        private async Task<bool> CanExecuteAsync(Task<bool> task)
        {
            var result = await task;
            UpdateCanExecute();
            return result;
        }

        private async Task<bool?> CanExecuteAsync(Item item)
        {
            var result = await (Item as List)?.ContainsAsync(item);

            /*foreach (var source in SyncWith)
            {
                if (await source.List.ContainsAsync(item))
                {
                    result = true;
                    break;
                }
            }*/

            /*var checkAll = SyncWith.Select(sources => sources.List.ContainsAsync(item)).ToList();

            while (checkAll.Count > 0)
            {
                var task = await Task.WhenAny(checkAll);

                if (await task)
                {
                    result = true;
                    break;
                }

                checkAll.Remove(task);
            }*/

            if (ContainsCache.TryGetValue(item, out var contains) && !contains.HasValue)
            {
                ContainsCache[item] = result;
                UpdateCanExecute();
            }

            return result;
        }
    }
}
