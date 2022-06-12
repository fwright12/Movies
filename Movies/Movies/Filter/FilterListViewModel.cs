using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Movies.ViewModels
{
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
            if (root.Children.Count == 0)
            {
                Items = new ObservableCollection<T>();
            }
            else
            {
                Items = new ObservableCollection<T>(Flatten(root));
            }

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
                    foreach (var node in Flatten(child))
                    {
                        yield return node;
                    }
                }
            }
        }
    }

    public class OperatorEditor<T> : Editor<T>
    {
        public IEnumerable LHSOptions { get; set; } = new ObservableCollection<object>();
        public IEnumerable<Operators> OperatorOptions { get; set; } = new ObservableCollection<Operators>();
        public IEnumerable RHSOptions { get; set; } = new ObservableCollection<object>();

        public override IPredicateBuilder<T> CreateNew() => new PropertyPredicateBuilder<T>
        {
            LHS = LHSOptions?.OfType<object>().FirstOrDefault(),
            Operator = OperatorOptions?.FirstOrDefault() ?? Operators.Equal,
            //RHS = RHSOptions?.OfType<object>().FirstOrDefault(),
        };
    }

    public class ItemPropertyEditors : List<Editor<Item>>, IFilterable<Editor<Item>>
    {
        public IEnumerable<Editor<Item>> GetItems(List<Constraint> filters)
        {
            throw new NotImplementedException();
        }
    }

    public class PropertyEditorFilter<T> : MultiEditor<T>
    {
        public IEnumerable<Type> Types { get; }
        public List<Editor<T>> Defaults { get; set; } = new List<Editor<T>>();

        public FilterListViewModel<Editor<T>> Filter { get; }
        private Dictionary<Type, HashSet<Property>> Properties = new Dictionary<Type, HashSet<Property>>();

        public PropertyEditorFilter(params Type[] types)
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
            var typeEditor = new OperatorEditor<T>
            {
                LHSOptions = types
            };

            var editors = new MultiEditor<T>();
            Filter = new FilterListViewModel<Editor<T>>(Defaults)
            {
                Items = editors.Editors
            };
            //_ = editorFilter.LoadMore(int.MaxValue);

            Add(typeEditor);
            Add(editors);
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

        public FilterPredicate<Editor<T>> GetPredicate(FilterPredicate<T> predicate)
        {
            return new BooleanExp<Editor<T>>();
        }
    }

    public class TemplateToOptionsConverter : IValueConverter
    {
        public static readonly TemplateToOptionsConverter Instance = new TemplateToOptionsConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var template = (OperatorEditor<Item>)value;
            //var editor = (Editor<Item>)value;
            //var template = editor.Template as PropertyTemplate<Item>;
            return template?.RHSOptions.OfType<object>().Select(value =>
            {
                var builder = template.CreateNew();

                if (builder is OperatorPredicateBuilder<Item> temp)
                {
                    temp.RHS = value;
                }

                return new ObservableNode<object>(builder);
            });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public interface IPredicateBuilder<T>
    {
        public event EventHandler PredicateChanged;
        public FilterPredicate<T> Predicate { get; }
    }

    public class FilterListViewModel<T> : AsyncListViewModel<T>
    {
        public IPredicateBuilder<T> Editor
        {
            get => _Editor;
            set
            {
                if (_Editor != null)
                {
                    _Editor.PredicateChanged -= PredicateChanged;
                }

                _Editor = value;

                if (_Editor != null)
                {
                    _Editor.PredicateChanged += PredicateChanged;
                }
            }
        }

        private FilterList<T> Source;
        private IPredicateBuilder<T> _Editor;

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

        public void Filter(FilterPredicate<T> filter)
        {
            Source.Filter = filter;
            Refresh();
        }

        private void PredicateChanged(object sender, EventArgs e) => Filter(Editor.Predicate);

        private class FilterList<T> : IAsyncEnumerable<T>
        {
            public FilterPredicate<T> Filter { get; set; }

            public IAsyncEnumerable<T> Items { get; }

            public FilterList(IAsyncEnumerable<T> items)
            {
                Items = items;
            }

            public FilterList(IEnumerable<T> items)
            {
                //Items = items;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ((Items as IAsyncFilterable<T>)?.GetItems(new List<Constraint>(), cancellationToken) ?? Items).GetAsyncEnumerator(cancellationToken);
        }
    }
}