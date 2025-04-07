using Movies.Models;
using Movies.ViewModels;
using System.Globalization;

namespace Movies
{
    [ContentProperty(nameof(Converters))]
    public class CompoundConverter : IValueConverter
    {
        public ICollection<IValueConverter> Converters { get; } = new List<IValueConverter>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Converters.Aggregate(value, (result, converter) => converter.Convert(result, targetType, parameter, culture));

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty(nameof(TypeToConverterMapping))]
    public class TypeDisplayConverter : IValueConverter
    {
        public MappingConverter? TypeToConverterMapping { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var converted = TypeToConverterMapping?.Convert(value.GetType(), targetType, parameter, culture);
            if (converted is IValueConverter converter)
            {
                return converter.Convert(value, targetType, parameter, culture);
            }
            else if (converted is string str)
            {
                return string.Format(str, value);
            }
            else
            {
                return value;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [BindingValueConverter]
    public class OperatorPredicateOperatorConverter : IValueConverter<OperatorPredicateBuilder, Operators?>
    {
        public object? Convert(OperatorPredicateBuilder? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value != null && (value.LHS == TMDB.SCORE || value is not PropertyPredicateBuilder || (value.RHS is IComparable && value.RHS is not string && value.RHS is not Enum)))
            {
                return value.Operator;
            }
            else
            {
                return null;
            }

            if (value?.LHS != TMDB.SCORE && value is PropertyPredicateBuilder)
            {
                if (value.RHS is IComparable && !(value.RHS is string) && !(value.RHS is Enum))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public object? ConvertBack(Operators? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [BindingValueConverter]
    public class OperatorPredicateLHSConverter : IValueConverter<OperatorPredicateBuilder, object>
    {
        public object? Convert(OperatorPredicateBuilder? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value != null && (value.LHS == TMDB.SCORE || value is not PropertyPredicateBuilder))
            {
                return value.LHS + ":";
            }
            else
            {
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

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
                else if (property != TMDB.SCORE && builder is PropertyPredicateBuilder)
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
            else if (item is SearchPredicateBuilder)
            {
                return new DataTemplate(() => new Label { IsVisible = false });
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