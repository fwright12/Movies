using Movies.Models;
using Movies.ViewModels;
using System;
using Xamarin.Forms;

namespace Movies.Templates
{
    public class ConstraintTemplateSelector : TypeTemplateSelector
    {
        public DataTemplate ComparableTemplate { get; set; }
        public DataTemplate MoneyValueTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
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

            return base.OnSelectTemplate(item, container);
        }

        //protected override Type GetType(object item) => (item as ConstraintViewModel)?.Constraint.Value?.GetType() ?? base.GetType(item);
    }
}