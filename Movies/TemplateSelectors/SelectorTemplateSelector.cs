using Movies.Models;
using Movies.ViewModels;

namespace Movies.Templates
{
    public class SelectorTemplateSelector : TypeTemplateSelector
    {
        public DataTemplate? ItemTypeTemplate { get; set; }
        public DataTemplate? MoneyPickerTemplate { get; set; }
        public DataTemplate? SmallValuesTemplate { get; set; }

        public DataTemplate? SearchTemplate { get; set; }
        public DataTemplate? MultiEditorTemplate { get; set; }

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
                return SearchTemplate!;
            }
            else if (selector is MultiEditor)
            {
                return MultiEditorTemplate!;
            }
            else if (selector is OperatorEditor op)
            {
                if (op.DefaultLHS as string == CollectionViewModel.ITEM_TYPE)
                {
                    return ItemTypeTemplate!;
                }
                else if (SmallValues.Intersect(op.LHSOptions.OfType<Property>()).Any())
                {
                    return SmallValuesTemplate!;
                }
                else if (op.DefaultLHS == Movie.BUDGET || op.DefaultLHS == Movie.REVENUE)
                {
                    return MoneyPickerTemplate!;
                }
                else if (op.LHSOptions.OfType<object>().FirstOrDefault() is Property property && TryGetTemplate(property.Type, out var selected))
                {
                    return selected;
                }
                else
                {
                    return SelectTemplate(item, container);
                }
            }

            return ObjectTemplate;
        }
    }
}
