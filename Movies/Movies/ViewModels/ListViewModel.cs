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

        public Task<bool> Allowed(Item item) => Task.FromResult(Selected.HasFlag(item.ItemType));

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

    public class FilterViewModel : BindableViewModel
    {
        public event EventHandler ValueChanged;

        public string Name { get; set; }
        public ICommand ItemTappedCommand { get; set; }

        public FilterViewModel()
        {

        }

        protected void OnValueChanged()
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }
    }

    public class BooleanExpressionViewModel : BindableViewModel
    {
        public IList<object> Expression { get; }

        public ItemPropertyViewModel SharedProperty
        {
            get => _SharedProperty;
            set => UpdateValue(ref _SharedProperty, value);
        }

        public Preset ActivePreset
        {
            get => _ActivePreset;
            set => UpdateValue(ref _ActivePreset, value);
        }

        public ICommand AddConstraintCommand { get; }
        public ICommand RemoveConstraintCommand { get; }

        private ItemPropertyViewModel _SharedProperty;
        private Preset _ActivePreset;

        public BooleanExpressionViewModel()
        {
            var expression = new ObservableCollection<object>();
            expression.CollectionChanged += (sender, e) =>
            {
                bool same = !Expression.Select(constraint => (constraint as ConstraintViewModel)?.Constraint.Name).Distinct().Skip(1).Any();

                if (same)
                {
                    var constraint = Expression.FirstOrDefault() as ConstraintViewModel;
                    /*var property = constraint?.Property;

                    if (property != SharedProperty)
                    {
                        if (SharedProperty?.Values != null)
                        {
                            foreach (var preset in SharedProperty.Values.Presets)
                            {
                                preset.PropertyChanged -= MonitorPresets;
                            }
                        }

                        SharedProperty = property;

                        if (SharedProperty?.Values != null)
                        {
                            foreach (var preset in SharedProperty.Values.Presets)
                            {
                                preset.PropertyChanged += MonitorPresets;
                            }
                        }
                    }

                    ActivePreset = constraint?.Property?.Values?.Presets.FirstOrDefault(preset => preset.IsActive);*/
                }

                if (e.NewItems != null)
                {

                }

                //Constraint = new BooleanExpression<Item>()
            };
            Expression = expression;

            RemoveConstraintCommand = new Command<ConstraintViewModel>(RemoveConstraint);
        }

        private void MonitorPresets(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Preset.IsActive))
            {
                return;
            }

            var preset = (Preset)sender;

            Expression.Clear();

            if (preset.IsActive)
            {
                Expression.Add(sender);
            }
        }

        public void AddConstraint(ConstraintViewModel constraint) => Expression.Add(constraint);

        public void RemoveConstraint(ConstraintViewModel constraint) => Expression.Remove(constraint);
    }

    public abstract class ConstraintViewModel : BindableViewModel
    {
        public virtual object ValueObj { get; }

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

        private Operators _Comparison;
        private Constraint _Constraint;

        public ConstraintViewModel(Constraint defaultConstraint)
        {
            Constraint = Default = defaultConstraint;
        }
    }

    public class LiteralViewModel<T> : ConstraintViewModel
    {
        public bool Comparable => Value is IComparable;
        public override object ValueObj => Value;

        public T Value
        {
            get => _Value;
            set => UpdateValue(ref _Value, value);
        }

        public IList<object> SelectedOptions { get; }

        public ICommand SetValueCommand { get; }

        private T _Value;

        public LiteralViewModel(string name) : this(new Constraint<T>(name)
        {
            Value = default,
            Comparison = default(T) is IComparable ? Operators.GreaterThan : Operators.Equal
        })
        { }

        public LiteralViewModel(Constraint<T> constraint) : base(constraint)
        {
            ConstraintChanged();
            SetValueCommand = new Command<T>(value => Value = value);

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

        /*public override void Clear()
        {
            base.Clear();

            Constraint = Default;
            OnPropertyChanged(nameof(Min));
            OnPropertyChanged(nameof(Max));
        }*/

        private void ConstraintChanged()
        {
            Value = ((Constraint<T>)Constraint).Value;
            Comparison = ((Constraint<T>)Constraint).Comparison;
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

    /*public abstract class SetConstraintViewModel : ConstraintViewModel
    {
        public BooleanExpressionViewModel Value { get; } = new BooleanExpressionViewModel();

        public SetConstraintViewModel(ItemPropertyViewModel property) : base(property) { }
    }

    public class SetConstraintViewModel<T> : SetConstraintViewModel
    {
        public IList<object> SelectedOptions { get; }

        public ICommand AddValueCommand { get; }

        public SetConstraintViewModel(ItemPropertyViewModel<T> property) : base(property)
        {
            Constraint = new BooleanExpression<Item>();
            AddValueCommand = new Command<T>(AddConstraint);

            var selected = new ObservableCollection<object>();
            selected.CollectionChanged += SelectedChanged;
            selected.CollectionChanged += (sender, e) =>
            {
                foreach (var preset in Property.Values.Presets)
                {
                    preset.IsActive = selected.Count == 1 && Equals(selected[0], preset.Value.Value);
                }
            };
            SelectedOptions = selected;

            if (Value.Expression is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(Constraint));
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
    }*/

    public class ChangedEventArgs<T>
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public ChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public abstract class SelectorViewModel : BindableViewModel
    {
        public event EventHandler<ChangedEventArgs<ConstraintViewModel>> ConstraintChanged;

        public string Name => DisplayName;
        public string DisplayName { get; set; }

        public ItemPropertyViewModel Property { get; }
        public ConstraintViewModel Constraint
        {
            get => _Constraint;
            set
            {
                if (value != _Constraint)
                {
                    OnPropertyChanging();
                    var e = new ChangedEventArgs<ConstraintViewModel>(_Constraint, _Constraint = value);
                    ConstraintChanged?.Invoke(this, e);
                    OnPropertyChanged();
                }
            }
        }

        private ConstraintViewModel _Constraint;

        public SelectorViewModel(ItemPropertyViewModel property, ConstraintViewModel constraint, string displayName = null)
        {
            Property = property;
            Constraint = constraint;

            DisplayName = displayName ?? property.Property;
        }

        public abstract void Reset();

        protected virtual void OnConstraintChanged()
        {
            
        }
    }

    public class SelectorViewModel<T> : SelectorViewModel
    {
        public bool Comparable => default(T) is IComparable;

        public SelectorViewModel(ItemPropertyViewModel<T> property, string displayName = null) : this(property, new LiteralViewModel<T>(property.Property)
        {
            Value = default,
            Comparison = default(T) is IComparable ? Operators.GreaterThan : Operators.Equal
        }, displayName)
        { }

        public SelectorViewModel(ItemPropertyViewModel<T> property, LiteralViewModel<T> constraint, string displayName = null) : base(property, constraint, displayName)
        {

        }

        public override void Reset() => Constraint = new LiteralViewModel<T>(Property.Property)
        {
            Value = default,
            Comparison = default(T) is IComparable ? Operators.GreaterThan : Operators.Equal
        };
    }

    public abstract class ItemPropertyViewModel : BindableViewModel
    {
        public string Property { get; set; }
        public Type Type { get; protected set; }

        public ValueRange Values { get; }

        public ItemPropertyViewModel(string name, ValueRange values) : this(name)
        {
            Values = values;
        }

        public ItemPropertyViewModel(string name)
        {
            Property = name;
        }

        //public abstract ConstraintViewModel GetConstraintViewModel();
    }

    public class ItemPropertyViewModel<T> : ItemPropertyViewModel
    {
        public ItemPropertyViewModel(string name, ValueRange values) : base(name, values) { }
        public ItemPropertyViewModel(string name) : base(name)
        {
            Type = typeof(T);
        }

        public static implicit operator SelectorViewModel<T>(ItemPropertyViewModel<T> property) => new SelectorViewModel<T>(property);
        //public static implicit operator ConstraintViewModel(ItemPropertyViewModel<T> property) => new LiteralViewModel<T>(property);

        //public override ConstraintViewModel GetConstraintViewModel() => new LiteralViewModel<T>(this);
        //public override ConstraintViewModel GetConstraintViewModel() => default(T) is IComparable ? new LiteralViewModel<T>(this) : (ConstraintViewModel)new SetConstraintViewModel<T>(this);
    }

    public abstract class ValueRange
    {
        public IList<FilterViewModel> Filters { get; } = new ObservableCollection<FilterViewModel>();
        public IList<Preset> Presets { get; } = new ObservableCollection<Preset>();
    }

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
    }

    public class DiscreteValueRange<T> : ValueRange<T>
    {
        public ObservableCollection<T> Values { get; } = new ObservableCollection<T>();
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

            await foreach (var result in Items.GetItems(new List<Constraint>()))
            {
                Values.Add(result);
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
            //Constraints.Add(new Constraint<string>(Name));

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

        private Dictionary<string, object> Values = new Dictionary<string, object>();

        protected T GetValue<T>([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) => Values.TryGetValue(propertyName, out var value) && value is T t ? t : default;

        protected void UpdateValue<T>(T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) => Values[propertyName] = value;

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
        public IList<SelectorViewModel> Selectors { get; } = new ObservableCollection<SelectorViewModel>();

        public ICommand ClearCommand { get; }

        public FiltersViewModel()
        {
            //ClearCommand = new Command(() => Clear());

            var constraints = new BooleanExpressionViewModel();
            if (constraints.Expression is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += (sender, e) =>
                {
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems.OfType<ConstraintViewModel>())
                        {
                            item.PropertyChanged -= ConstraintChanged;
                        }
                    }

                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems.OfType<ConstraintViewModel>())
                        {
                            item.PropertyChanged += ConstraintChanged;
                        }
                    }
                };
            }
            Constraints = constraints;

            var selectors = new ObservableCollection<SelectorViewModel>();
            selectors.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<SelectorViewModel>())
                    {
                        item.ConstraintChanged -= ConstraintChanged;
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<SelectorViewModel>())
                    {
                        ConstraintChanged(item, new ChangedEventArgs<ConstraintViewModel>(null, item.Constraint));
                        item.ConstraintChanged += ConstraintChanged;
                    }
                }
            };
            Selectors = selectors;
        }

        private void ConstraintChanged(object sender, ChangedEventArgs<ConstraintViewModel> e)
        {
            if (e.OldValue != null)
            {
                if (e.OldValue.ValueObj is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged -= SelectedChanged;
                }
                else
                {
                    e.NewValue.PropertyChanged -= AddConstraint;
                }
            }
            if (e.NewValue != null)
            {
                if (e.NewValue.ValueObj is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += SelectedChanged;
                }
                else
                {
                    e.NewValue.PropertyChanged += AddConstraint;
                }
            }
        }

        private void ConstraintChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConstraintViewModel.Constraint))
            {
                return;
            }

            var constraint = (ConstraintViewModel)sender;

            if (!HasValues(constraint))
            {
                Constraints.RemoveConstraint(constraint);
            }
        }

        private void AddConstraint(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConstraintViewModel.Constraint))
            {
                return;
            }

            var constraint = (ConstraintViewModel)sender;

            if (!Constraints.Expression.Contains(constraint) && HasValues(constraint))
            {
                Constraints.AddConstraint(constraint);
            }

            foreach (var selector in Selectors.Where(selector => selector.Constraint == constraint))
            {
                //selector.Reset();
            }
        }

        public bool HasValues(ConstraintViewModel constraint)
        {
            var selector = Selectors.FirstOrDefault(selector => selector.Property.Property == constraint.Constraint.Name);

            if (selector == null)
            {
                return false;
            }

            if (selector.Property.Values is SteppedValueRange range)
            {
                bool isAbsoluteMin = constraint.Comparison != Operators.Equal && Equals(constraint.ValueObj, range.LowerBound);
                bool isAbsoluteMax = constraint.Comparison != Operators.Equal && Equals(constraint.ValueObj, range.UpperBound);

                return !(Equals(constraint.Constraint, constraint.Default) || isAbsoluteMin || isAbsoluteMax);
            }
            else
            {
                return constraint.ValueObj != null;
            }

            return true;
        }

        private void SelectedChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var observable = Constraints.Expression as INotifyCollectionChanged;
            if (observable != null)
            {
                //observable.CollectionChanged -= ConstraintsChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    //RemoveConstraint(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    Constraints.AddConstraint(new LiteralViewModel<string>("Test")
                    {
                        Value = item.ToString(),
                        Comparison = Operators.Equal
                    });
                }
            }

            if (observable != null)
            {
                //observable.CollectionChanged += ConstraintsChanged;
            }
        }

        /*private void ConstraintsChanged(object sender, NotifyCollectionChangedEventArgs e)
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
        }*/
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
