using Movies.Models;
using Movies.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Movies.Templates
{
    public class SelectorTemplateSelector : DataTemplateSelector
    {
        public TypeTemplateSelector FiniteValuesTemplate { get; set; }
        public TypeTemplateSelector InfiniteValuesTemplate { get; set; }

        public DataTemplate LargeValuesTemplate { get; set; }
        public DataTemplate SmallValuesTemplate { get; set; }

        public DataTemplate SearchTemplate { get; set; }
        public DataTemplate TypeTemplate { get; set; }
        public DataTemplate MultiEditorTemplate { get; set; }

        private static readonly HashSet<Property> SmallValues = new HashSet<Property>
        {
            CollectionViewModel.MonetizationType,
            Movie.CONTENT_RATING,
            Movie.GENRES,
            TVShow.CONTENT_RATING,
            TVShow.GENRES,
        };

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var selector = (Editor<Item>)item;

            if (selector is MultiEditor<Item>)
            {
                return MultiEditorTemplate;
            }

            //var property = selector.Property;
            //var operatorPredicate = selector.Value as OperatorPredicateBuilder<Item> ?? (selector.Value as ExpressionPredicateBuilder<Item>).Children.OfType<ObservableNode<IPredicateBuilder<Item>>>().FirstOrDefault()?.Value as OperatorPredicateBuilder<Item>;
            var property = (selector as OperatorEditor<Item>)?.LHSOptions.OfType<object>().FirstOrDefault() as Property;
            //var property = operatorPredicate?.LHS as Property;

            if (property == CollectionViewModel.SearchProperty)
            {
                return SearchTemplate;
            }

            if (property?.Type == typeof(Type) || SmallValues.Contains(property))
            {
                return SmallValuesTemplate;
            }

            var template = property?.Values == null ? InfiniteValuesTemplate : FiniteValuesTemplate;

            if (template.TryGetTemplate(property?.Type, out var selected))
            {
                return selected;
            }

            return template.SelectTemplate(item, container);
        }
    }
}
