using Movies.Models;
using Movies.ViewModels;
using System;
using Xamarin.Forms;

namespace Movies.Templates
{
    public class PredicateTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MoneyValueTemplate { get; set; }

        public DataTemplate FullExpressionTemplate { get; set; }
        public DataTemplate OperatorAndValueTemplate { get; set; }
        public DataTemplate ValueOnlyTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is OperatorPredicateBuilder builder)
            {
                var property = builder.LHS as Property;

                if (property == Movie.BUDGET || property == Movie.REVENUE)
                {
                    return MoneyValueTemplate;
                }
                else if (builder is PropertyPredicateBuilder)
                {
                    if (builder.RHS is IComparable && !(builder.RHS is string) && !(builder.RHS is Enum))
                    {
                        return OperatorAndValueTemplate;
                    }
                    else
                    {
                        return ValueOnlyTemplate;
                    }
                }
                else
                {
                    return FullExpressionTemplate;
                }
            }

            /*var constraint = (ConstraintViewModel)item;
            var property = constraint.Constraint.Property;
            var value = constraint.Constraint.Value;

            if (property == CollectionViewModel.SearchProperty || property is Property<ItemType?>)
            {
                constraint.IsShowing = false;
            }

            if (value is IComparable && !(value is string) && !(value is Enum))
            {
                constraint.ShowOperator = true;
            }

            if (property == Movie.BUDGET || property == Movie.REVENUE)
            {
                constraint.ShowLabel = true;
                return MoneyValueTemplate;
            }*/

            return TypeTemplateSelector.ObjectTemplate;
        }

        //protected override Type GetType(object item) => (item as ConstraintViewModel)?.Constraint.Value?.GetType() ?? base.GetType(item);
    }
}