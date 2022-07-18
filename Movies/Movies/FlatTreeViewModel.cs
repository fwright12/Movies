using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;

namespace Movies.ViewModels
{
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
            Leaves = new ObservableCollection<ObservableNode<T>>();

            if (Root.Children.Count > 0)
            {
                TreeChanged(root.Children, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Flatten(root).ToList()));
            }
            Root.SubtreeChanged += TreeChanged;
        }

        private void TreeChanged(object sender, NotifyCollectionChangedEventArgs e)
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
                        if (value.Value is SearchPredicateBuilder || (value.Value is OperatorPredicateBuilder builder && builder.LHS as string == CollectionViewModel.ITEM_TYPE))
                        {
                            continue;
                        }

                        Leaves.Add(value);
                    }
                }
            }
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
}