using Movies.Models;
using Movies.ViewModels;

namespace Movies.Templates
{
    public class SelectorTemplateSelector : TypeTemplateSelector
    {
        public DataTemplate? ItemTypeTemplate { get; set; }
        public DataTemplate? MoneyTemplate { get; set; }
        public DataTemplate? ScoreTemplate { get; set; }
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
            return SelectNamedTemplate(selector) ?? base.OnSelectTemplate(item, container);
        }

        protected override Type GetType(object item)
        {
            return ((item as OperatorEditor)?.LHSOptions.OfType<object>().FirstOrDefault() as Property)?.Type ?? base.GetType(item);
        }

        private DataTemplate? SelectNamedTemplate(ViewModels.Editor selector)
        {
            if (selector is Editor<SearchPredicateBuilder>)
            {
                return SearchTemplate;
            }
            else if (selector is MultiEditor)
            {
                return MultiEditorTemplate;
            }
            else if (selector is OperatorEditor op)
            {
                if (op.DefaultLHS as string == CollectionViewModel.ITEM_TYPE)
                {
                    return ItemTypeTemplate;
                }
                else if (SmallValues.Intersect(op.LHSOptions.OfType<Property>()).Any())
                {
                    return SmallValuesTemplate;
                }
                else if (op.DefaultLHS == Movie.BUDGET || op.DefaultLHS == Movie.REVENUE)
                {
                    return MoneyTemplate;
                }
                else if (op.DefaultLHS == TMDB.SCORE)
                {
                    return ScoreTemplate;
                }
            }

            return null;
        }
    }
}
