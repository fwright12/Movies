using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class OptionsLayoutBehavior : Behavior<View>
    {
        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            bindable.BindingContextChanged += OptionsContextChanged;
        }

        private void OptionsContextChanged(object sender, EventArgs e)
        {
            var bindable = (BindableObject)sender;

            if (bindable.BindingContext is OperatorEditor editor)
            {
                var vm = new OptionsViewModel(editor);
                SelectedItemsChanged(bindable, vm);
                
                bindable.SetBinding(bindable is CollectionView ? ItemsView.ItemsSourceProperty : BindableLayout.ItemsSourceProperty, new Binding(nameof(OptionsViewModel.Items), source: vm));
                bindable.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName != "SelectedItems")
                    {
                        return;
                    }

                    SelectedItemsChanged((BindableObject)sender, vm);
                };
                //bindable.SetBinding(bindable is CollectionView ? SelectableItemsView.SelectedItemsProperty : Selection.SelectedItemsProperty, new Binding(nameof(OptionsViewModel.SelectedItems), BindingMode.OneWayToSource, source: vm));
            }
        }

        private static void SelectedItemsChanged(BindableObject bindable, OptionsViewModel vm)
        {
            vm.SelectedItems = (IEnumerable)bindable.GetValue(bindable is CollectionView ? SelectableItemsView.SelectedItemsProperty : Selection.SelectedItemsProperty);
        }
    }

    public class OptionsViewModel
    {
        public IList<ObservableNode<object>> Items { get; }

        public OperatorEditor Template { get; }
        public IEnumerable SelectedItems
        {
            get => _SelectedItems;
            set
            {
                if (_SelectedItems is INotifyCollectionChanged oldObservable)
                {
                    oldObservable.CollectionChanged -= SelectedItemsChanged;
                }

                _SelectedItems = value;

                if (_SelectedItems != null)
                {
                    SelectedItemsChanged(value, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<object>(value.OfType<object>())));
                }
                if (_SelectedItems is INotifyCollectionChanged newObservable)
                {
                    newObservable.CollectionChanged += SelectedItemsChanged;
                }
            }
        }

        private IEnumerable _SelectedItems;

        public OptionsViewModel(OperatorEditor template)
        {
            Items = new ObservableCollection<ObservableNode<object>>();
            Template = template;

            foreach (var option in template.RHSOptions)
            {
                AddOption(option);
            }

            if (template.RHSOptions is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += OptionsChanged;
            }
        }

        public void RemoveOption(object option)
        {
            int index = IndexOf(option);

            if (index != -1)
            {
                Items.RemoveAt(index);
            }
        }

        public void AddOption(object option)
        {
            var builder = Template.CreateNew();

            if (builder is OperatorPredicateBuilder temp)
            {
                temp.LHS = Template.LHSOptions.OfType<Property>().FirstOrDefault(property => property.Values?.OfType<object>().Contains(option) == true) ?? Template.DefaultLHS;
                temp.RHS = option;
            }

            var node = new ObservableNode<object>(builder);
            int index = IndexOf(option);

            if (index == -1)
            {
                Items.Add(node);
            }
            else
            {
                return;
                var expression = new ObservableNode<object>("OR");

                foreach (var child in FlatTreeViewModel<object>.Flatten(Items[index]))
                {
                    expression.Children.Add(child);
                }
                expression.Children.Add(node);

                Items[index] = expression;
            }
        }

        private void OptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    RemoveOption(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    AddOption(item);
                }
            }
        }

        private void SelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var selected in e.NewItems.OfType<ObservableNode<object>>())
                {
                    var value = (selected.Value as OperatorPredicateBuilder)?.RHS;

                    if (value == null)
                    {
                        continue;
                    }

                    int index = IndexOf(value);

                    if (index != -1)
                    {
                        Items[index] = selected;
                    }
                }
            }
        }

        private int IndexOf(object value)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                if (item.Value is OperatorPredicateBuilder builder && Equals(value, builder.RHS))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public class TemplateToOptionsConverter : IValueConverter
    {
        public static readonly TemplateToOptionsConverter Instance = new TemplateToOptionsConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OperatorEditor template)
            {
                //var editor = (Editor<Item>)value;
                //var template = editor.Template as PropertyTemplate<Item>;

                var result = new List<ObservableNode<object>>();

                foreach (var option in template.RHSOptions)
                {
                    var builder = template.CreateNew();

                    if (builder is OperatorPredicateBuilder temp)
                    {
                        temp.RHS = option;
                    }

                    var node = new ObservableNode<object>(builder);
                    int index = result.FindIndex(node => (node.Value as OperatorPredicateBuilder)?.RHS == option);

                    if (index == -1)
                    {
                        result.Add(node);
                    }
                    else
                    {
                        continue;
                        var expression = new ObservableNode<object>("OR");

                        foreach (var child in FlatTreeViewModel<object>.Flatten(result[index]))
                        {
                            expression.Children.Add(child);
                        }
                        expression.Children.Add(node);

                        result[index] = expression;
                    }
                }

                return result;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}