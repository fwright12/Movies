using Movies.Models;
using Movies.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

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
            Movie.GENRES,
            TVShow.GENRES,
        };

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var selector = (ViewModels.Editor)item;

            if (selector is Editor<SearchPredicateBuilder>)
            {
                return SearchTemplate;
            }
            else if (selector is MultiEditor)
            {
                return MultiEditorTemplate;
            }

            //var property = selector.Property;
            //var operatorPredicate = selector.Value as OperatorPredicateBuilder<Item> ?? (selector.Value as ExpressionPredicateBuilder<Item>).Children.OfType<ObservableNode<IPredicateBuilder<Item>>>().FirstOrDefault()?.Value as OperatorPredicateBuilder<Item>;

            if (selector is OperatorEditor op)
            {
                if (op.DefaultLHS as string == CollectionViewModel.ITEM_TYPE || SmallValues.Intersect(op.LHSOptions.OfType<Property>()).Any())
                {
                    return SmallValuesTemplate;
                }

                //var template = !op.RHSOptions.OfType<object>().Any() ? InfiniteValuesTemplate : FiniteValuesTemplate;
                var template = FiniteValuesTemplate;

                if (op.LHSOptions.OfType<object>().FirstOrDefault() is Property property && template.TryGetTemplate(property.Type, out var selected))
                {
                    return selected;
                }

                return template.SelectTemplate(item, container);
            }

            return TypeTemplateSelector.ObjectTemplate;
        }
    }
}
