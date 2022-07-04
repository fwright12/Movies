using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class ObjectToViewConverter : IValueConverter
    {
        public static readonly ObjectToViewConverter Instance = new ObjectToViewConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter is DataTemplateSelector selector)
            {
                return selector.SelectTemplate(value, null).CreateContent();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ReverseListConverter : IValueConverter
    {
        public static readonly ReverseListConverter Instance = new ReverseListConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value is IEnumerable items) ? items.OfType<object>().Reverse() : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Convert(value, targetType, parameter, culture);
    }

    public class DoubleToLongConverter : IValueConverter
    {
        public static readonly DoubleToLongConverter Instance = new DoubleToLongConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is long l ? (double)l : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is double d ? (long)d : value;
    }

    public class TreeNodeTemplateSelector<T> : DataTemplateSelector
    {
        public DataTemplate INodeTemplate { get; set; }
        public DataTemplate LeafNodeTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var node = (ObservableNode<T>)item;
            var template = node.Children.Count == 0 ? LeafNodeTemplate : INodeTemplate;

            return (template as DataTemplateSelector)?.SelectTemplate(node.Value, container) ?? template;
        }
    }

    public class TreeToListConverter<T> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new FlatTreeViewModel<T>((ObservableNode<T>)value).Leaves;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FlatTreeViewModel<T>
    {
        public ObservableNode<T> Root { get; }

        public IList<ObservableNode<T>> Leaves { get; }

        public FlatTreeViewModel(ObservableNode<T> root)
        {
            Root = root;
            if (root.Children.Count == 0)
            {
                Leaves = new ObservableCollection<ObservableNode<T>>();
            }
            else
            {
                Leaves = new ObservableCollection<ObservableNode<T>>(Flatten(root));
            }

            Root.SubtreeChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<ObservableNode<T>>())
                    {
                        foreach (var value in Flatten(item))
                        {
                            Leaves.Remove(value);
                        }
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<ObservableNode<T>>())
                    {
                        foreach (var value in Flatten(item))
                        {
                            Leaves.Add(value);
                        }
                    }
                }
            };
        }

        public static IEnumerable<ObservableNode<T>> Flatten(ObservableNode<T> root)
        {
            if (root.Children.Count == 0)
            {
                yield return root;
            }
            else
            {
                foreach (var child in root.Children)
                {
                    foreach (var node in Flatten(child))
                    {
                        yield return node;
                    }
                }
            }
        }
    }

    public class OperatorEditor : Editor
    {
        public IEnumerable LHSOptions { get; set; } = new ObservableCollection<object>();
        public IEnumerable<Operators> OperatorOptions { get; set; } = new ObservableCollection<Operators>();
        public IEnumerable RHSOptions { get; set; } = new ObservableCollection<object>();

        public object DefaultLHS { get; set; }
        public Operators DefaultOperator { get; set; } = Operators.Equal;
        public object DefaultRHS { get; set; }

        public override IPredicateBuilder CreateNew() => new PropertyPredicateBuilder
        {
            LHS = DefaultLHS,
            Operator = DefaultOperator,
            RHS = DefaultRHS
        };
    }

    public class PropertyEditorFilter : MultiEditor
    {
        public IEnumerable<Type> Types { get; }
        public List<Editor> Defaults { get; set; } = new List<Editor>();

        public FilterListViewModel<Editor> Filter { get; }
        private Dictionary<Type, HashSet<Property>> Properties = new Dictionary<Type, HashSet<Property>>();

        public PropertyEditorFilter(Editor editor, params Type[] types)
        {
            Types = types;

            foreach (var type in Types)
            {
                var list = new HashSet<Property>();

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

                Properties.Add(type, list);
            }

            //var typeEditor = new Editor<T>(new PropertyTemplate<T>(new Property<Type>("Types", types)));
            var typeEditor = new OperatorEditor
            {
                RHSOptions = types
            };

            /*var editors = new MultiEditor<T>();
            Filter = new FilterListViewModel<Editor<T>>(Defaults)
            {
                //Items = editors.Editors
            };*/
            //_ = editorFilter.LoadMore(int.MaxValue);

            Add(typeEditor);
            Add(editor);
        }

        /*public void Add(PredicateTemplate<T> template)
        {
            if (!Lookup.TryGetValue(template.Property, out var group))
            {
                Lookup.Add(template.Property, group = new ObservableGroup<Property, PredicateEditor<T>>(template.Property));
                Editors.Add(group);
            }

            var editor = new OperatorEditor<T>(template)
            {
                //Name = (template as PropertyTemplate<T>)?.Property.Name
            };
            editor.CreateNew();
        }*/

        public FilterPredicate GetPredicate(FilterPredicate predicate)
        {
            return new BooleanExpression();
        }
    }

    public interface IPredicateBuilder
    {
        public event EventHandler PredicateChanged;
        public FilterPredicate Predicate { get; }
    }

    public class FilterListViewModel<T> : AsyncListViewModel<T>
    {
        public IPredicateBuilder Predicate
        {
            get => _Predicate;
            set
            {
                if (_Predicate != null)
                {
                    _Predicate.PredicateChanged -= PredicateChanged;
                }

                _Predicate = value;

                if (_Predicate != null)
                {
                    _Predicate.PredicateChanged += PredicateChanged;
                }
            }
        }

        private FilterList<T> Source;
        private IPredicateBuilder _Predicate;

        public FilterListViewModel(IAsyncEnumerable<T> source) : this(new FilterList<T>(source)) { }
        public FilterListViewModel(IEnumerable<T> source) : this(new FilterList<T>(Convert(source))) { }

        private static async IAsyncEnumerable<T> Convert<T>(IEnumerable<T> source)
        {
            await Task.CompletedTask;
            foreach (var item in source)
            {
                yield return item;
            }
        }

        private FilterListViewModel(FilterList<T> source) : base(source)
        {
            Source = source;
        }

        public void Filter(FilterPredicate filter)
        {
            Source.Filter = filter;
            Refresh();
        }

        private void PredicateChanged(object sender, EventArgs e) => Filter(Predicate.Predicate);

        private class FilterList<T> : IAsyncEnumerable<T>
        {
            public FilterPredicate Filter { get; set; }

            public IAsyncEnumerable<T> Items { get; }

            public FilterList(IAsyncEnumerable<T> items)
            {
                Items = items;
            }

            public FilterList(IEnumerable<T> items)
            {
                //Items = items;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ((Items as IAsyncFilterable<T>)?.GetItems(Filter ?? FilterPredicate.TAUTOLOGY, cancellationToken) ?? Items).GetAsyncEnumerator(cancellationToken);
        }
    }
}