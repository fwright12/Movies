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
    public class ScrollLockBehavior : Behavior<ScrollView>
    {
        private double LastScrollX, LastScrollY;

        protected override void OnAttachedTo(ScrollView bindable)
        {
            base.OnAttachedTo(bindable);
            return;
            bindable.Scrolled += (sender, e) =>
            {
                double deltaX = e.ScrollX - LastScrollX;
                double deltaY = e.ScrollY - LastScrollY;

                //Print.Log(e.ScrollX, e.ScrollY, LastScrollY, deltaY);

                LastScrollX = e.ScrollX;
                LastScrollY = e.ScrollY;

                if (e.ScrollX != 0 || e.ScrollY != 0)
                {
                    ((View)bindable.Parent).HeightRequest += deltaY;
                    _ = bindable.ScrollToAsync(0, 0, false);
                }
            };
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

        public override void Reset()
        {
            if (Selected.Value is OperatorPredicateBuilder builder)
            {
                Reset(builder);
            }
            else //if (Selected.Value is ExpressionBuilder expression)
            {
                var items = Selected.Children;
                var remove = new List<ObservableNode<object>>();

                foreach (var item in items)
                {
                    if (item.Value is OperatorPredicateBuilder temp && LHSOptions.OfType<object>().Contains(temp.LHS))
                    {
                        remove.Add(item);
                    }
                }

                Selected.Remove(remove.ToArray());
            }
        }

        private void Reset(OperatorPredicateBuilder builder)
        {
            builder.LHS = DefaultLHS;
            builder.Operator = DefaultOperator;
            builder.RHS = DefaultRHS;
        }

        public override IPredicateBuilder CreateNew()
        {
            var builder = new PropertyPredicateBuilder();
            Reset(builder);
            return builder;
        }
    }

    public class PropertyEditorFilter : MultiEditor
    {
        public IEnumerable<Type> Types { get; }
        public Editor TypeSelector { get; }
        public List<Editor> Defaults { get; set; } = new List<Editor>();

        public FilterListViewModel<Editor> Filter { get; }
        //private Dictionary<Type, HashSet<Property>> Properties = new Dictionary<Type, HashSet<Property>>();

        public PropertyEditorFilter(Editor editor, params Type[] types)
        {
            Types = types;

            /*foreach (var type in Types)
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
                }

                Properties.Add(type, list);
            }*/

            //var typeEditor = new Editor<T>(new PropertyTemplate<T>(new Property<Type>("Types", types)));
            TypeSelector = new OperatorEditor
            {
                DefaultLHS = CollectionViewModel.ITEM_TYPE,
                RHSOptions = types
            };

            /*var editors = new MultiEditor<T>();
            Filter = new FilterListViewModel<Editor<T>>(Defaults)
            {
                //Items = editors.Editors
            };*/
            //_ = editorFilter.LoadMore(int.MaxValue);

            Add(TypeSelector);
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

        private FilterList Source;
        private IPredicateBuilder _Predicate;

        public FilterListViewModel(IAsyncEnumerable<T> source) : this(new FilterList(source)) { }
        public FilterListViewModel(IEnumerable<T> source) : this(new FilterList(Convert(source))) { }

        private static async IAsyncEnumerable<T> Convert(IEnumerable<T> source)
        {
            await Task.CompletedTask;
            foreach (var item in source)
            {
                yield return item;
            }
        }

        private FilterListViewModel(FilterList source) : base(source)
        {
            Source = source;
        }

        public void Filter(FilterPredicate filter)
        {
            Source.Filter = filter;
            Refresh();
        }

        private void PredicateChanged(object sender, EventArgs e) => Filter(Predicate.Predicate);

        private class FilterList : IAsyncEnumerable<T>
        {
            public FilterPredicate Filter { get; set; }

            public IAsyncEnumerable<T> Items { get; }

            public FilterList(IAsyncEnumerable<T> items)
            {
                Items = items;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ((Items as IAsyncFilterable<T>)?.GetItems(Filter ?? FilterPredicate.TAUTOLOGY, cancellationToken) ?? Items).GetAsyncEnumerator(cancellationToken);
        }
    }
}