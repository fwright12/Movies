using Movies.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    public static class FilterHelpers
    {
        public static readonly IEnumerable<Property> PreferredFilterOrder = new List<Property>
        {
            Movie.RELEASE_DATE,
            Media.RUNTIME,
            Movie.CONTENT_RATING,// TVShow.CONTENT_RATING),
            Movie.GENRES,
            TVShow.GENRES,
            TMDB.SCORE,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS,
            ViewModels.CollectionViewModel.MonetizationType,
            ViewModels.CollectionViewModel.People,
            Movie.BUDGET,
            Movie.REVENUE,
            Media.KEYWORDS,
        };

        private static readonly HashSet<Property> ComparableProperties = new HashSet<Property>
        {
            Movie.RELEASE_DATE,
            Media.RUNTIME,
            TMDB.SCORE,
            Movie.BUDGET,
            Movie.REVENUE
        };

        private static readonly HashSet<Property> EqualityProperties = new HashSet<Property>
        {
            Movie.CONTENT_RATING,
            Movie.GENRES,
            TVShow.GENRES,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS,
            ViewModels.CollectionViewModel.MonetizationType,
            ViewModels.CollectionViewModel.People,
            Media.KEYWORDS
        };

        private static readonly HashSet<Property> MultiSelectableProperties = new HashSet<Property>
        {
            Movie.GENRES,
            TVShow.GENRES,
            Movie.WATCH_PROVIDERS,
            TVShow.WATCH_PROVIDERS,
            ViewModels.CollectionViewModel.MonetizationType,
        };

        private static readonly Dictionary<Property, object> DefaultValues = new Dictionary<Property, object>
        {
            [Movie.RELEASE_DATE] = new DateTime(1900, 1, 1)
        };

        public static bool IsComparable(Property property) => ComparableProperties.Contains(property);
        public static bool IsEquality(Property property) => EqualityProperties.Contains(property);

        public static bool TryGetDefaultValue(Property property, out object value) => DefaultValues.TryGetValue(property, out value);

        public static OperatorEditor GetComparableEditor(params Property[] properties) => GetComparableEditor(properties.FirstOrDefault()?.Values.OfType<object>().FirstOrDefault(), properties);
        public static OperatorEditor GetComparableEditor(object defaultValue, params Property[] properties) => GetEditor(properties, defaultValue, null, Operators.GreaterThan, Operators.Equal, Operators.LessThan);

        public static OperatorEditor GetEqualityEditor(ObservableNode<object> selected, params Property[] properties) => GetEditor(properties, null, selected, Operators.Equal);

        public static OperatorEditor GetEditor(IEnumerable<Property> properties, object defaultValue, ObservableNode<object> selected, params Operators[] operators)
        {
            var editor = new OperatorEditor
            {
                LHSOptions = new ObservableCollection<object>(properties),
                OperatorOptions = operators.ToList<Operators>(),
                RHSOptions = !properties.Skip(1).Any() ? properties.FirstOrDefault()?.Values : properties.SelectMany(property => property.Values.OfType<object>()),
                DefaultLHS = properties.FirstOrDefault(),
                DefaultOperator = operators?.FirstOrDefault() ?? Operators.Equal,
                DefaultRHS = defaultValue
            };

            if (selected != null)
            {
                editor.Select(selected);
            }
            else
            {
                editor.AddNew();
            }

            return editor;
        }

        public static void AddFilters(this CollectionViewModel collection, params Property[] properties) => AddFilters(collection, (IEnumerable<Property>)properties);

        public static void AddFilters(this CollectionViewModel collection, IEnumerable<Property> properties)
        {
            var list = properties.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var property = list[i];

                if ((property == Movie.WATCH_PROVIDERS && list.Remove(TVShow.WATCH_PROVIDERS)) || (property == TVShow.WATCH_PROVIDERS && list.Remove(Movie.WATCH_PROVIDERS)))
                {
                    collection.AddFilter(Movie.WATCH_PROVIDERS, TVShow.WATCH_PROVIDERS);
                }
                else if ((property == Movie.GENRES && list.Remove(TVShow.GENRES)) || (property == TVShow.GENRES && list.Remove(Movie.GENRES)))
                {
                    collection.AddFilter(Movie.GENRES, TVShow.GENRES);
                }
                else
                {
                    collection.AddFilter(property);
                }
            }
        }

        public static void AddFilter(this CollectionViewModel collection, params Property[] properties) => AddFilter(collection, (IEnumerable<Property>)properties);
        public static void AddFilter(this CollectionViewModel collection, IEnumerable<Property> properties)
        {
            var filters = collection.Filters;

            if (properties.All(property => FilterHelpers.IsComparable(property)))
            {
                if (!properties.Skip(1).Any() && properties.FirstOrDefault() is Property property && FilterHelpers.TryGetDefaultValue(property, out var value))
                {
                    filters.Add(FilterHelpers.GetComparableEditor(value, property));
                }
                else
                {
                    filters.Add(FilterHelpers.GetComparableEditor(properties.ToArray()));
                }
            }
            else if (properties.All(property => FilterHelpers.IsEquality(property)))
            {
                var array = properties.ToArray();
                var selected = collection.Source.Predicate is ExpressionBuilder builder && properties.All(property => MultiSelectableProperties.Contains(property)) ? builder.Root : null;

                filters.Add(FilterHelpers.GetEqualityEditor(selected, array));
            }
        }

        /*
         *  GetComparableEditor(new DateTime(1900, 1, 1), Movie.RELEASE_DATE),
            GetComparableEditor(Media.RUNTIME),
            GetEqualityEditor(Movie.CONTENT_RATING),// TVShow.CONTENT_RATING),
            GetEqualityEditor(predicate.Root, Movie.GENRES, TVShow.GENRES),
            GetComparableEditor(TMDB.SCORE),
            GetEqualityEditor(predicate.Root, Movie.WATCH_PROVIDERS, TVShow.WATCH_PROVIDERS),
            GetEqualityEditor(predicate.Root, MonetizationType),
            GetEqualityEditor(People),
            GetComparableEditor(Movie.BUDGET),
            GetComparableEditor(Movie.REVENUE),
            GetEqualityEditor(Media.KEYWORDS),
         */
    }

    public class ItemTypeSnapPoint : ElementSnapPoint
    {
        public ItemTypeSnapPoint()
        {
            PropertyChanging += ParentWillChange;
            PropertyChanged += ParentDidChange;
        }

        private void ParentWillChange(object sender, Xamarin.Forms.PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(Parent) && Parent != null)
            {
                Parent.DescendantAdded -= OnDescendantAdded;
            }
        }

        private void ParentDidChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Parent) && Parent != null)
            {
                Parent.DescendantAdded += OnDescendantAdded;
            }
        }

        private void OnDescendantAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is VisualElement ve && ve.BindingContext is OperatorEditor op && op.RHSOptions is Type[])
            {
                Parent.DescendantAdded -= OnDescendantAdded;
                Element = ve;
            }
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
            Reset();
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

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => ((Items as IAsyncFilterable<T>)?.GetAsyncEnumerator(Filter ?? FilterPredicate.TAUTOLOGY, cancellationToken) ?? Items.GetAsyncEnumerator(cancellationToken));
        }
    }
}