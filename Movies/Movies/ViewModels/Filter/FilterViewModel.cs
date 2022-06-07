using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class PropertyFilter
    {
        public IReadOnlyDictionary<Type, List<Property>> Properties => _Properties;

        private Dictionary<Type, List<Property>> _Properties = new Dictionary<Type, List<Property>>();
        private List<Property> Values = new List<Property>();

        public PropertyFilter(params Type[] types)
        {
            foreach (var type in types)
            {
                Add(type);
            }
        }

        public void Add(Property property)
        {

        }

        public void Add(Type type)
        {
            var list = new List<Property>();

            foreach (var field in type.GetFields())//System.Reflection.BindingFlags.Static))
            {
                var value = field.GetValue(null);

                if (value is Property property)
                {
                    list.Add(property);
                }
            }

            /*foreach (var property in type.GetProperties())
            {
                Print.Log(property);
                list.Add(new ReflectedProperty(property));
            }*/

            _Properties.Add(type, list);
        }

        public IEnumerable<Property> GetItems(List<Constraint> filters)
        {
            var types = new HashSet<Type>();

            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];

                if (filter.Property is Property<Type> && filter.Value is Type type && filter.Comparison == Operators.Equal)
                {
                    filters.RemoveAt(i--);
                    types.Add(type);
                }
            }

            return Properties.Where(type => types.Any(temp => temp == type.Key)).SelectMany(kvp => kvp.Value);
        }
    }

    public interface ITest<out TItem, TConstraint>
    {
        IEnumerable<TItem> Filter(List<TConstraint> constraints);
    }

    public class SelectorList : IFilterable<SelectorViewModel>, ITest<SelectorViewModel, Type>
    {
        public IList<SelectorViewModel> Selectors { get; } = new List<SelectorViewModel>();
        private Dictionary<Property, SelectorViewModel> SelectorLookup = new Dictionary<Property, SelectorViewModel>();

        public void Add<T>(Property<T> property, T defaultValue = default, Operators defaultComparison = Operators.Equal) => Add(property, defaultValue, defaultComparison);
        private void Add(Property property, object defaultValue = null, Operators defaultComparison = Operators.Equal)
        {
            //Properties.Add(property);
            var constraint = new Constraint(property)
            {
                Value = defaultValue,
                Comparison = defaultComparison
            };

            if (!SelectorLookup.TryGetValue(property, out var selector))
            {
                selector = null;

                foreach (var kvp in SelectorLookup)
                {
                    if (kvp.Key.Name == property.Name)
                    {
                        selector = kvp.Value;

                        foreach (var option in property.Values)
                        {
                            int i;
                            for (i = 0; i < selector.Options.Count; i++)
                            {
                                if (selector.Options[i] is Constraint other && Equals(option, other.Value))
                                {
                                    break;
                                }
                            }

                            var temp = new Constraint(property)
                            {
                                Value = option
                            };

                            if (i == selector.Options.Count)
                            {
                                selector.Options.Add(temp);
                            }
                            else
                            {
                                selector.Options[i] = new BooleanExpression
                                {
                                    Parts =
                                    {
                                        (Constraint)selector.Options[i],
                                        temp
                                    }
                                };
                            }
                        }

                        break;
                    }
                }

                selector ??= new SelectorViewModel(constraint)
                {
                    Name = property.Name,
                };
                SelectorLookup.Add(property, selector);
            }
        }

        public IEnumerable<SelectorViewModel> GetItems(List<Constraint> filters)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<SelectorViewModel> GetEnumerator() => GetItems(new List<Constraint>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<SelectorViewModel> Filter(List<Type> constraints)
        {
            foreach (var type in constraints)
            {
                yield return null;
            }
        }
    }

    public class AsyncListViewModel<T> : BindableViewModel
    {
        public IList<T> Items { get; }

        public ICommand LoadMoreCommand { get; }

        private IAsyncEnumerable<T> Source;
        private IAsyncEnumerator<T> Itr;

        public AsyncListViewModel(IAsyncEnumerable<T> source)
        {
            Items = new ObservableCollection<T>();
            Source = source;
            LoadMoreCommand = new Command<int?>(count => _ = LoadMore(count ?? 1));


            Refresh();
        }

        public async Task LoadMore(int count = 1)
        {
            if (Itr == null)
            {
                return;
            }

            for (int i = 0; i < count && await Itr.MoveNextAsync(); i++)
            {
                Items.Add(Itr.Current);
            }
        }

        public void Refresh()
        {
            Items.Clear();
            Itr = Source.GetAsyncEnumerator();
            _ = LoadMore(10);
        }

        public static async IAsyncEnumerable<T> Where(IAsyncEnumerable<T> items, Func<T, bool> predicate)
        {
            await foreach (var item in items)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        //private void Filter(object sender, EventArgs<IEnumerable<Constraint>> e) => UpdateValues(e.Value);
    }

    public abstract class FilterPredicate<T> : IPredicate<T>
    {
        public static readonly FilterPredicate<T> TAUTOLOGY = new Tautology();
        public static readonly FilterPredicate<T> CONTRADICTION = new Tautology();

        private class Tautology : FilterPredicate<T>
        {
            public override bool Evaluate(T item) => true;
        }

        public IPredicate<T> Predicate => this;

        //public static bool operator true(FilterPredicate<T> predicate) => predicate.Evaluate
        //public static bool operator false(FilterPredicate<T> predicate) => x == Green || x == Yellow;

        public static BooleanExp<T> operator &(FilterPredicate<T> first, FilterPredicate<T> second) => new BooleanExp<T>
        {
            Predicates =
            {
                //first,
                //second
            }
        };

        public abstract bool Evaluate(T item);
    }

    public class ExpressionPredicate<T> : FilterPredicate<T>
    {
        private IEnumerable<object> Predicates { get; }

        public ExpressionPredicate() : this(Enumerable.Empty<object>()) { }
        public ExpressionPredicate(params object[] predicates) : this((IEnumerable<object>)predicates) { }
        public ExpressionPredicate(IEnumerable<object> predicates)
        {
            Predicates = predicates;
        }

        public override bool Evaluate(T item) => Predicates.OfType<FilterPredicate<T>>().All(predicate => predicate.Evaluate(item));
    }

    public class OperatorPredicate<T> : FilterPredicate<T>
    {
        public object LHS { get; set; }
        public Operators Operator { get; set; }
        public object RHS { get; set; }

        public override bool Evaluate(T item)
        {
            if (Operator == Operators.LessThan || Operator == Operators.GreaterThan)
            {
                int compare;

                if (LHS is IComparable lhs)
                {
                    compare = lhs.CompareTo(RHS);
                }
                else if (RHS is IComparable rhs)
                {
                    compare = rhs.CompareTo(LHS) * -1;
                }
                else
                {
                    return true;
                }

                return compare == (int)Operator;
            }
            else
            {
                var equal = Equals(LHS, RHS);

                if (Operator == Operators.NotEqual)
                {
                    equal = !equal;
                }

                return equal;
            }
        }
    }

    public class BooleanExp<T> : FilterPredicate<T>
    {
        public IList<IPredicate<T>> Predicates { get; }

        public override bool Evaluate(T item) => false;// Predicates.All(predicate => predicate.Evaluate(item));

        public static BooleanExp<T> operator &(BooleanExp<T> expression, IPredicate<T> predicate)
        {
            expression.Predicates.Add(predicate);
            return expression;
        }
    }

    public class ItemFilter : IAsyncFilterable<Item>
    {
        private IAsyncEnumerable<Item> Items;

        public IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Item> GetItems(List<Constraint> filters, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    public class TreeToListConverter<T> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new FlatTreeViewModel<T>((ObservableNode<T>)value).Items;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FlatTreeViewModel<T>
    {
        public ObservableNode<T> Root { get; }

        public IList<T> Items { get; }

        public FlatTreeViewModel(ObservableNode<T> root)
        {
            Root = root;
            Items = new ObservableCollection<T>(Flatten(root));

            Root.SubtreeChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<ObservableNode<T>>())
                    {
                        foreach (var value in Flatten(item))
                        {
                            Items.Remove(value);
                        }
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<ObservableNode<T>>())
                    {
                        foreach (var value in Flatten(item))
                        {
                            Items.Add(value);
                        }
                    }
                }
            };
        }

        public static IEnumerable<T> Flatten(ObservableNode<T> root)
        {
            if (root.Children.Count == 0)
            {
                yield return root.Value;
            }
            else
            {
                foreach (var child in root.Children)
                {
                    if (child is ObservableNode<T> tree)
                    {
                        foreach (var node in Flatten(tree))
                        {
                            yield return node;
                        }
                    }
                }
            }
        }
    }

    public class TreeEditor<T> : BindableViewModel
    {
        public ObservableNode<T> Selected
        {
            get => _Selected;
            set => UpdateValue(ref _Selected, value);
        }

        //private ObservableNode<T> Root = new ObservableNode<T>();
        private ObservableNode<T> _Selected;

        public TreeEditor()
        {
            //Selected = Root;
        }
    }

    public class ObservableList<T> : ObservableCollection<T>, IList<object>
    {
        object IList<object>.this[int index]
        {
            get => ((IList)this)[index];
            set => ((IList)this)[index] = value;
        }

        bool ICollection<object>.IsReadOnly => ((IList)this).IsReadOnly;

        void ICollection<object>.Add(object item) => ((IList)this).Add(item);

        bool ICollection<object>.Contains(object item) => ((IList)this).Contains(item);

        void ICollection<object>.CopyTo(object[] array, int arrayIndex) => ((IList)this).CopyTo(array, arrayIndex);

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => this.OfType<object>().GetEnumerator();

        int IList<object>.IndexOf(object item) => ((IList)this).IndexOf(item);

        void IList<object>.Insert(int index, object item) => ((IList)this).Insert(index, item);

        bool ICollection<object>.Remove(object item) => item is T t ? Remove(t) : false;
    }

    public class ObservableNode<T>
    {
        public event NotifyCollectionChangedEventHandler SubtreeChanged;

        public T Value { get; }

        public ObservableNode<T> Parent { get; private set; }
        public ObservableList<ObservableNode<T>> Children { get; }

        public ObservableNode(T value)
        {
            Children = new ObservableList<ObservableNode<T>>();
            Children.CollectionChanged += AssignParent;

            Value = value;
        }

        public void Add(ObservableNode<T> subtree)
        {
            Children.Add(subtree);
        }

        public void Add(T value)
        {
            Children.Add(new ObservableNode<T>(value));
        }

        public bool Remove(T value)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Equals(Children[i].Value, value))
                {
                    Children.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        protected virtual void OnSubtreeChanged(ObservableNode<T> changed)
        {
            //SubtreeChanged?.Invoke(this, new EventArgs<ObservableTree<T>>(changed));
        }

        private void AssignParent(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var node in e.OldItems.OfType<ObservableNode<T>>())
                {
                    //node.SubtreeChanged -= SubtreeChanged;
                    node.Parent = null;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var node in e.NewItems.OfType<ObservableNode<T>>())
                {
                    node.Parent = this;
                    //node.SubtreeChanged += SubtreeChanged;
                }
            }

            var parent = this;
            do
            {
                parent.SubtreeChanged?.Invoke(this, e);
                parent = parent.Parent;
            }
            while (parent != null);
        }

        //private void ChildSubtreeChanged(object sender, EventArgs<ObservableTree<T>> e) => OnSubtreeChanged(e.Value);
    }

    //public delegate void PredicateChangedEventHandler<in T>(object sender, EventArgs<T> e);

    public class ExpressionPredicateBuilder<T> : ObservableNode<IPredicateBuilder<T>>, IPredicateBuilder<T>
    {
        public event EventHandler<EventArgs<FilterPredicate<T>>> PredicateChanged;

        public FilterPredicate<T> Predicate { get; private set; }

        public ExpressionPredicateBuilder() : this("AND") { }
        public ExpressionPredicateBuilder(string value) : this((IPredicateBuilder<T>)null) { }
        public ExpressionPredicateBuilder(IPredicateBuilder<T> value) : base(value)
        {
            Children.CollectionChanged += UpdatePredicate;
            //SubtreeChanged += UpdatePredicate;
        }

        private void UpdatePredicate(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<ObservableNode<IPredicateBuilder<T>>>())
                {
                    if (item.Value != null)
                    {
                        item.Value.PredicateChanged -= ChildPredicateChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ObservableNode<IPredicateBuilder<T>>>())
                {
                    if (item.Value != null)
                    {
                        item.Value.PredicateChanged += ChildPredicateChanged;
                    }
                }
            }
        }

        private void ChildPredicateChanged(object sender, EventArgs<FilterPredicate<T>> e)
        {
            var builder = (IPredicateBuilder<T>)sender;

            if (builder.Predicate == FilterPredicate<T>.TAUTOLOGY)
            {
                Remove(builder);
            }
            else
            {
                var exp = new BooleanExp<T>();
                foreach (var child in Children)
                {
                    //exp.Predicates.Add(child.Value.Predicate);
                }

                Predicate = exp;
                PredicateChanged?.Invoke(this, new EventArgs<FilterPredicate<T>>(Predicate));
            }
        }
    }

    public class OperatorPredicateBuilder<T> : BindableViewModel, IPredicateBuilder<T>
    {
        public event EventHandler<EventArgs<FilterPredicate<T>>> PredicateChanged;
        public FilterPredicate<T> Predicate { get; protected set; }

        public object RHS
        {
            get => _RHS;
            set => UpdateValue(ref _RHS, value);
        }

        public Operators Operator
        {
            get => _Operator;
            set => UpdateValue(ref _Operator, value);
        }

        public object LHS
        {
            get => _LHS;
            set => UpdateValue(ref _LHS, value);
        }

        private object _RHS;
        private Operators _Operator;
        private object _LHS;

        public OperatorPredicateBuilder()
        {
            PropertyChanged += BuildPredicate;
        }

        private void BuildPredicate(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LHS) || e.PropertyName == nameof(Operator) || e.PropertyName == nameof(RHS))
            {
                OnPredicateChanged();
                //Predicate = BuildPredicate();
            }
        }

        protected virtual void OnPredicateChanged()
        {
            Predicate = BuildPredicate();
            PredicateChanged?.Invoke(this, new EventArgs<FilterPredicate<T>>(Predicate));
        }

        public virtual FilterPredicate<T> BuildPredicate() => new OperatorPredicate<T>
        {
            LHS = LHS,
            Operator = Operator,
            RHS = RHS
        };
    }

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

    public class PredicateChangedEventArgs<T>: EventArgs
    {
        public IPredicate<T> OldValue { get; }
        public IPredicate<T> NewValue { get; }
    }

    public class ObservableGroup<TKey, TElement> : ObservableCollection<TElement>, IGrouping<TKey, TElement>
    {
        public TKey Key { get; }

        public ObservableGroup(TKey key, IEnumerable<TElement> elements) : base(elements)
        {
            Key = key;
        }

        public ObservableGroup(TKey key)
        {
            Key = key;
        }
    }

    public class ObservableGroupedList<TKey, TElement> : ObservableCollection<ObservableGroup<TKey, TElement>>
    {
        private Func<TElement, TKey> KeySelector;

        public ObservableGroupedList(Func<TElement, TKey> keySelector)
        {
            KeySelector = keySelector;
        }

        public ObservableGroupedList(Func<TElement, TKey> keySelector, IEnumerable<TElement> elements) : base(elements.GroupBy(keySelector, (key, elements) => new ObservableGroup<TKey, TElement>(key, elements)))
        {
            KeySelector = keySelector;
        }

        public void Add(TElement element)
        {
            var key = KeySelector(element);
            var grouping = this.FirstOrDefault(grouping => Equals(key, grouping.Key));

            if (grouping == null)
            {
                Add(grouping = new ObservableGroup<TKey, TElement>(key));
            }

            grouping.Add(element);
        }
    }

    public abstract class PredicateTemplate<T>
    {
        public abstract IPredicateBuilder<T> CreateNew();
    }

    public class PredicateTemplate<TPredicateBuilder, TValue> : PredicateTemplate<TValue> where TPredicateBuilder : IPredicateBuilder<TValue>, new()
    {
        public override IPredicateBuilder<TValue> CreateNew() => new TPredicateBuilder();
    }

    public class PropertyTemplate<T> : PredicateTemplate<T>
    {
        public Property Property { get; }
        public Operators DefaultOperator { get; }
        public object DefaultValue { get; }

        public PropertyTemplate(Property property, Operators defaultOperator = Operators.Equal, object defaultValue = null)
        {
            Property = property;
            DefaultOperator = defaultOperator;
            DefaultValue = defaultValue;
        }

        public override IPredicateBuilder<T> CreateNew() => new PropertyPredicateBuilder<T>
        {
            LHS = Property,
            Operator = DefaultOperator,
            RHS = DefaultValue
        };
    }

    public class PropertyEditor<T> : PredicateEditor<T>, IEnumerable
    {
        public IList<PredicateEditor<T>> Editors { get; } = new ObservableCollection<PredicateEditor<T>>();
        //public IList<ObservableGroup<Property, PredicateEditor<T>>> Editors { get; } = new ObservableCollection<ObservableGroup<Property, PredicateEditor<T>>>();

        private Dictionary<Property, ObservableGroup<Property, PredicateEditor<T>>> Lookup = new Dictionary<Property, ObservableGroup<Property, PredicateEditor<T>>>();

        public PropertyEditor(ObservableNode<IPredicateBuilder<T>> root) : base(root, null)
        {
            //PropertyChanged += SelectedChanged;

            //var editors = new ObservableCollection<PredicateEditor<T>>();
            //editors.CollectionChanged += EditorsChanged;
        }

        public void Add(Property property)
        {
            foreach (var value in property.Values)
            {
                Add(new PropertyTemplate<T>(property, Operators.Equal, value));
            }
        }

        public void Add(PredicateTemplate<T> template)
        {
            /*if (!Lookup.TryGetValue(template.Property, out var group))
            {
                Lookup.Add(template.Property, group = new ObservableGroup<Property, PredicateEditor<T>>(template.Property));
                Editors.Add(group);
            }*/

            Editors.Add(new PredicateEditor<T>(Parent, template)
            {
                Name = (template as PropertyTemplate<T>)?.Property.Name
            });
        }

        private void EditorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<PredicateEditor<T>>())
                {
                    //item.PropertyChanged += SubEditorSelectedChanged;
                }
            }
        }

        private void SelectedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Selected))
            {
                return;
            }

            
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)Editors).GetEnumerator();
        }
    }

    public class TemplateToOptionsConverter : IValueConverter
    {
        public static readonly TemplateToOptionsConverter Instance = new TemplateToOptionsConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var template = (PropertyTemplate<Item>)value;
            return template.Property.Values.OfType<object>().Select(value => new ObservableNode<IPredicateBuilder<Item>>(new PropertyPredicateBuilder<Item>
            {
                LHS = template.Property,
                Operator = template.DefaultOperator,
                RHS = value
            }));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PredicateEditor<T> : BindableViewModel
    {
        public string Name { get; set; }
        public PredicateTemplate<T> Template { get; }

        public ObservableNode<IPredicateBuilder<T>> Parent { get; set; }

        public ObservableNode<IPredicateBuilder<T>> Selected
        {
            get => _SelectedNode;
            set
            {
                if (_SelectedNode != null)
                {
                    _SelectedNode.Value.PredicateChanged -= SelectedPredicateChanged;
                }
                
                UpdateValue(ref _SelectedNode, value);

                if (_SelectedNode != null)
                {
                    _SelectedNode.Value.PredicateChanged += SelectedPredicateChanged;
                }
            }
        }

        private ObservableNode<IPredicateBuilder<T>> _SelectedNode;

        public PredicateEditor(ObservableNode<IPredicateBuilder<T>> root, PredicateTemplate<T> template)
        {
            Parent = root;
            Template = template;

            CreateNew();
        }

        public void CreateNew()
        {
            Selected = new ObservableNode<IPredicateBuilder<T>>(Template?.CreateNew() ?? new OperatorPredicateBuilder<T>());
            //OnCurrentChanged(Selected, _Selected = GetNew());
        }

        private void SelectedPredicateChanged(object sender, EventArgs<FilterPredicate<T>> e)
        {
            if (Selected.Parent == null)
            {
                Parent.Add(Selected);
            }
        }

        //protected abstract IPredicateBuilder<T> GetNew();

        protected virtual void OnCurrentChanged(IPredicate<T> oldValue, IPredicate<T> newValue)
        {
            //PredicateBuilt?.Invoke(this, new PredicateChangedEventArgs<T>(oldValue, newValue));
        }
    }

    public interface IPredicate<T>
    {
        IPredicate<T> Predicate { get; }
    }

    public interface IPredicateBuilder<T>
    {
        public event EventHandler<EventArgs<FilterPredicate<T>>> PredicateChanged;
        public FilterPredicate<T> Predicate { get; }
    }

    public class FilterListViewModel<T> : AsyncListViewModel<T>
    {
        //public ExpressionPredicateBuilder<object> Constraints { get; } = new ExpressionPredicateBuilder<object>();
        public IPredicateBuilder<T> Builder { get; set; }
        public IList<SelectorViewModel> Selectors { get; } = new List<SelectorViewModel>();
        public IList<PredicateEditor<T>> Editors { get; } = new ObservableCollection<PredicateEditor<T>>();
        public PredicateEditor<T> Editor { get; set; }


        private FilterList<T> Source;

        public FilterListViewModel(IAsyncEnumerable<T> source) : this(new FilterList<T>(source)) { }

        private FilterListViewModel(FilterList<T> source) : base(source)
        {
            Source = source;
            
            //Builder.PredicateChanged += UpdateItems;

            var builders = new ObservableCollection<PredicateEditor<T>>();
            builders.CollectionChanged += BuildersChanged;
            Editors = builders;
        }

        private void UpdateItems(object sender, EventArgs<T> e)
        {
            throw new NotImplementedException();
        }

        private void BuildersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<PredicateEditor<T>>())
                {
                    //item.PredicateBuilt += PredicateBuilt;
                }
            }
        }

        private void PredicateBuilt(object sender, PredicateChangedEventArgs<T> e)
        {
            
        }

        //private void ReplacePredicate(IPredicate<T> oldValue, IPredicate<T> newValue) => ReplacePredicate(Constraints, oldValue, newValue);
        private void ReplacePredicate(ExpressionPredicateBuilder<object> tree, IPredicate<T> oldValue, IPredicate<T> newValue)
        {
            for (int i = 0; i < tree.Children.Count; i++)
            {
                var child = tree.Children[i];

                if (child is IPredicate<T> predicate && predicate == oldValue)
                {
                    //tree.Children[i] = newValue;
                    break;
                }
                else if (child is ExpressionPredicateBuilder<object> subtree)
                {
                    ReplacePredicate(subtree, oldValue, newValue);
                }
            }
        }

        private class FilterList<T> : IAsyncEnumerable<T>
        {
            public IPredicate<T> Filter { get; set; }

            public IAsyncEnumerable<T> Items { get; }

            public FilterList(IAsyncEnumerable<T> items)
            {
                Items = items;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ((Items as IAsyncFilterable<T>)?.GetItems(new List<Constraint>(), cancellationToken) ?? Items).GetAsyncEnumerator(cancellationToken);
        }

        public void Filter(IPredicate<T> filter)
        {
            Source.Filter = filter;
            Refresh();
        }
    }

    public class FiltersViewModel : BindableViewModel, IEnumerable<Constraint>
    {
        public event EventHandler<EventArgs<IEnumerable<Constraint>>> ValueChanged;

        public BooleanExpressionViewModel Constraints { get; }
        public IList<Preset> Presets { get; }
        public IList<SelectorViewModel> Selectors { get; } = new ObservableCollection<SelectorViewModel>();
        public SelectorList SelectorList { get; } = new SelectorList();

        public IDictionary<Property, SelectorViewModel> SelectorLookup = new Dictionary<Property, SelectorViewModel>();

        public ICommand CreateNewCommand { get; }
        public ICommand ClearCommand { get; }

        private PropertyFilter Properties;

        public FiltersViewModel(params Type[] forTypes)
        {
            Properties = new PropertyFilter(forTypes);

            ClearCommand = new Command(Clear);
            CreateNewCommand = new Command<SelectorViewModel>(CreateNew);

            var constraints = new BooleanExpressionViewModel();
            if (constraints.Expression is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += ConstraintsChanged;
                observable.CollectionChanged += ExpressionChanged;
            }
            Constraints = constraints;

            var presets = new ObservableCollection<Preset>();
            presets.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<Preset>())
                    {

                    }
                }
            };
            Presets = presets;

            var selectors = new ObservableCollection<SelectorViewModel>();
            selectors.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<SelectorViewModel>())
                    {
                        //SelectorLookup[item.Property] = item;

                        foreach (var preset in item.Presets)
                        {
                            Presets.Add(preset);
                        }

                        /*var property = item.Property;
                        if (property?.Values is INotifyCollectionChanged observable)
                        {
                            observable.CollectionChanged += ValuesChanged;
                        }*/

                        CreateNewConstraint(item);
                    }
                }
            };
            Selectors = new ObservableCollection<SelectorViewModel>();

            if (forTypes.Length > 1)
            {
                var typeSelector = new SelectorViewModel(new Property<Type>(string.Empty, forTypes));
                Selectors.Add(typeSelector);
            }

            ValueChanged += CheckPresets;
            //ValueChanged += TypeChanged;
        }

        private void TypeChanged(object sender, EventArgs<IEnumerable<Constraint>> e)
        {
            Selectors.Clear();

            foreach (var property in Properties.GetItems(e.Value.ToList()))
            {
                if (SelectorLookup.TryGetValue(property, out var selector))
                {
                    Selectors.Add(selector);
                }
            }
        }

        public void CreateNew(SelectorViewModel selector)
        {
            return;
            Add(new Constraint(selector.Property), out var model, out _);
            Select1(selector, model);
        }

        public void Add<T>(Property<T> property, T defaultValue = default, Operators defaultComparison = Operators.Equal) => Add((Property)property, defaultValue, defaultComparison);
        private void Add(Property property, object defaultValue = null, Operators defaultComparison = Operators.Equal)
        {
            Properties.Add(property);
            var constraint = new Constraint(property)
            {
                Value = defaultValue,
                Comparison = defaultComparison
            };

            if (!SelectorLookup.TryGetValue(property, out var selector))
            {
                selector = null;

                foreach (var kvp in SelectorLookup)
                {
                    if (kvp.Key.Name == property.Name)
                    {
                        selector = kvp.Value;

                        foreach (var option in property.Values)
                        {
                            int i;
                            for (i = 0; i < selector.Options.Count; i++)
                            {
                                if (selector.Options[i] is Constraint other && Equals(option, other.Value))
                                {
                                    break;
                                }
                            }

                            var temp = new Constraint(property)
                            {
                                Value = option
                            };

                            if (i == selector.Options.Count)
                            {
                                selector.Options.Add(temp);
                            }
                            else
                            {
                                selector.Options[i] = new BooleanExpression
                                {
                                    Parts =
                                    {
                                        (Constraint)selector.Options[i],
                                        temp
                                    }
                                };
                            }
                        }

                        break;
                    }
                }

                selector ??= new SelectorViewModel(constraint)
                {
                    Name = property.Name,
                };
                SelectorLookup.Add(property, selector);

                for (int i = 0; i < Presets.Count; i++)
                {
                    var preset = Presets[i];

                    if (GetPresetSelector(preset) is SelectorViewModel temp)
                    {
                        Presets.RemoveAt(i--);
                        temp.Presets.Add(preset);
                    }
                }
            }
        }

        //public void Add(Constraint constraint) => AddConstraints(constraint);
        public void AddConstraints(params Constraint[] constraints) => AddConstraints((IEnumerable<Constraint>)constraints);
        public void AddConstraints(IEnumerable<Constraint> constraints)
        {
            foreach (var constraint in constraints)
            {
                Add(constraint, out var model, out var selector);
                Select1(selector, model);
            }

            OnValueChanged();
        }

        private void Add(Constraint constraint) => Add(constraint, out _, out _);
        private void Add(Constraint constraint, out ConstraintViewModel cvm, out SelectorViewModel selector)
        {
            var property = constraint.Property;
            cvm = new ConstraintViewModel
            {
                Constraint = constraint,
            };

            if (!SelectorLookup.TryGetValue(property, out selector))
            {
                Add(property);
            }

            Constraints.AddConstraint(cvm);
            ConstraintChanged(cvm, new PropertyChangedEventArgs(nameof(ConstraintViewModel.Constraint)));
        }

        public void Select1(ConstraintViewModel constraint)
        {
            if (SelectorLookup.TryGetValue(constraint.Constraint.Property, out var selector))
            {
                Select1(selector, constraint);
            }
        }

        private void Select1(SelectorViewModel selector, ConstraintViewModel constraint)
        {
            selector.Constraint = constraint;

            //if (!Selectors.Any(temp => temp.Name == selector.Name))
            if (!Selectors.Contains(selector))
            {
                Selectors.Add(selector);
            }
        }

        private void PropertiesUpdated(params Property[] properties)
        {
            var set = new HashSet<Property>(properties);

            foreach (var kvp in SelectorLookup)
            {
                if (set.Contains(kvp.Key))
                {
                    Selectors.Add(kvp.Value);
                }
            }
        }

        private void CheckPresets(object sender, EventArgs<IEnumerable<Constraint>> e)
        {
            foreach (var preset in Presets)
            {
                if (preset.IsActive && !IsSubset(preset.Value, e.Value))
                {
                    preset.IsActive = false;
                }
            }
        }

        private SelectorViewModel GetPresetSelector(Preset preset) => preset.Value.Select(constraint => constraint.Property).Distinct().Count() == 1 && SelectorLookup.TryGetValue(preset.Value[0].Property, out var selector) ? selector : null;

        public void Add(Preset preset)
        {
            IList<Preset> presets;

            if (GetPresetSelector(preset) is SelectorViewModel selector)
            {
                presets = selector.Presets;
            }
            else
            {
                presets = Presets;
            }

            presets.Add(preset);
            preset.PropertyChanged += PresetActiveChanged;
        }

        //private bool Contains(IEnumerable<Constraint> constraint) => IsSubset(constraint, GetValue());
        private bool IsSubset(IEnumerable<Constraint> source, IEnumerable<Constraint> other) => source.All(constraint => other.Contains(constraint));

        private void PresetActiveChanged(object sender, PropertyChangedEventArgs e)
        {
            var preset = (Preset)sender;

            //ValueChanged -= CheckPresets;

            if (preset.IsActive)
            {
                Constraints.Expression.Add(preset);
            }
            else
            {
                Constraints.Expression.Remove(preset);
            }

            var add = new List<ConstraintViewModel>();

            foreach (var constraint in preset.Value)
            {
                var existing = Constraints.Expression.OfType<ConstraintViewModel>().Where(cvm => Equals(cvm.Constraint, constraint)).FirstOrDefault();

                if (existing == null)
                {
                    if (preset.IsActive)
                    {
                        add.Add(existing = new ConstraintViewModel
                        {
                            Constraint = constraint
                        });
                    }
                    else
                    {
                        continue;
                    }
                }

                existing.IsShowing = !preset.IsActive;
            }

            AddConstraints(add);
            //ValueChanged += CheckPresets;
        }

        public void CreateNewConstraint(SelectorViewModel selector) => AddConstraint(selector, new ConstraintViewModel
        {
            Constraint = selector.DefaultConstraint
        });

        private void AddConstraints(params ConstraintViewModel[] constraints) => AddConstraints((IEnumerable<ConstraintViewModel>)constraints);
        private void AddConstraints(IEnumerable<ConstraintViewModel> constraints)
        {
            foreach (var constraint in constraints)
            {
                if (GetSelector(constraint) is SelectorViewModel selector)
                {
                    AddConstraint(selector, constraint);
                }
            }

            OnValueChanged();
        }

        public void RemoveConstraints(params ConstraintViewModel[] constraints) => RemoveConstraints((IEnumerable<ConstraintViewModel>)constraints);
        public void RemoveConstraints(IEnumerable<ConstraintViewModel> constraints)
        {
            foreach (var constraint in constraints)
            {
                Constraints.Expression.Remove(constraint);
            }

            OnValueChanged();
        }

        private void AddConstraint(SelectorViewModel selector, ConstraintViewModel constraint)
        {
            Constraints.AddConstraint(constraint);
            ConstraintChanged(constraint, new PropertyChangedEventArgs(nameof(ConstraintViewModel.Constraint)));

            SelectedChanged(selector, constraint);
        }

        private void OnValueChanged()
        {
            ValueChanged?.Invoke(this, new EventArgs<IEnumerable<Constraint>>(new List<Constraint>(this)));
        }

        public void Select(ConstraintViewModel constraint)
        {
            if (GetSelector(constraint) is SelectorViewModel selector)
            {
                SelectedChanged(selector, constraint);
            }
        }

        private void SelectedChanged(SelectorViewModel selector, ConstraintViewModel selected)
        {
            //selector.Constraint = selected;
        }

        public void Clear()
        {
            //ExpressionChanged(Constraints.Expression, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)Constraints.Expression));
            Constraints.Expression.Clear();
            OnValueChanged();
        }

        private SelectorViewModel GetSelector(ConstraintViewModel constraint) => Selectors.Where(selector => selector.Property == constraint.Constraint.Property).FirstOrDefault();
        private SelectorViewModel GetSelector(Constraint constraint) => Selectors.FirstOrDefault(selector => selector.Property == constraint.Property);

        private void ExpressionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Preset>())
                {
                    item.IsActive = false;
                }

                foreach (var item in e.OldItems.OfType<ConstraintViewModel>())
                {
                    if (SelectorLookup.TryGetValue(item.Constraint.Property, out var selector))
                    {
                        /*if (selector is MultiSelectorViewModel multiSelector && item.Value != null)
                        {
                            multiSelector.Values.Remove(item.Value);
                        }*/

                        //selector.Values?.Remove(item.Constraint.Value);
                    }

                    item.PropertyChanged += AddConstraint;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ConstraintViewModel>())
                {
                    /*if (item.Value != null && GetSelector(item) is MultiSelectorViewModel multiSelector && !multiSelector.Values.Contains(item.Value))
                    {
                        multiSelector.Values.Add(item.Value);
                    }*/
                }
            }
        }

        private void AddConstraint(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConstraintViewModel.Constraint))
            {
                return;
            }

            var constraint = (ConstraintViewModel)sender;

            constraint.PropertyChanged -= AddConstraint;

            if (!Constraints.Expression.Contains(constraint))
            {
                AddConstraints(constraint);
            }

            var selector = GetSelector(constraint);
            if (selector != null && selector.IsImmutable)
            {
                CreateNewConstraint(selector);
            }
        }

        private void ConstraintsChanged(object sender, NotifyCollectionChangedEventArgs e)
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
        }

        private void ConstraintChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ConstraintViewModel.Constraint))
            {
                return;
            }

            var constraint = (ConstraintViewModel)sender;

            if (!Validate(constraint))
            {
                RemoveConstraints(constraint);
            }
            else
            {
                OnValueChanged();
            }
            /*else if (!Expression.Expression.Contains(constraint))
            {
                Expression.Expression.Add(constraint);
            }*/
        }

        //private object CleanValue(object value) => double.TryParse(value, out var num) ? num : value;

        public bool Validate(ConstraintViewModel cvm) => Validate(cvm.Constraint);
        public bool Validate(Constraint constraint)
        {
            /*var selector = Selectors.FirstOrDefault(selector => selector.Property.Property == constraint.Constraint.Name);

            if (selector == null)
            {
                return false;
            }*/

            if (constraint.Property?.Values is SteppedValueRange range)
            {
                bool isAbsoluteMin = constraint.Comparison != Operators.Equal && Equals(constraint.Value, range.First);
                bool isAbsoluteMax = constraint.Comparison != Operators.Equal && Equals(constraint.Value, range.Last);

                return constraint.Value != null && !isAbsoluteMin && !isAbsoluteMax;
                //return !(Equals(constraint.Constraint, constraint.Default) || isAbsoluteMin || isAbsoluteMax);
            }
            else if (constraint.Property?.Values != null)
            {
                return constraint.Property.Values.OfType<object>().Contains(constraint.Value);
            }

            return true;
        }

        public IEnumerator<Constraint> GetEnumerator() => Constraints.Expression.OfType<ConstraintViewModel>().Select(constraint => constraint.Constraint).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}