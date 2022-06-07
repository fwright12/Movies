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

        public DataTemplate FullExpressionTemplate { get; set; }
        public DataTemplate OperatorAndValueTemplate { get; set; }
        public DataTemplate ValueOnlyTemplate { get; set; }

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
            var selector = (PredicateEditor<Item>)item;
            //var property = selector.Property;
            //var operatorPredicate = selector.Value as OperatorPredicateBuilder<Item> ?? (selector.Value as ExpressionPredicateBuilder<Item>).Children.OfType<ObservableNode<IPredicateBuilder<Item>>>().FirstOrDefault()?.Value as OperatorPredicateBuilder<Item>;
            var property = (selector.Template as PropertyTemplate<Item>).Property;
            //var property = operatorPredicate?.LHS as Property;

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
