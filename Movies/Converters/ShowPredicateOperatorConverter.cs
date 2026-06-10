using Movies.ViewModels;
using System.Globalization;

namespace Movies
{
    [BindingValueConverter]
    public class ShowPredicateOperatorConverter : IValueConverter<OperatorPredicateBuilder, bool>
    {
        public object? Convert(OperatorPredicateBuilder? value, Type targetType, object? parameter, CultureInfo culture) => value != null && (value.LHS == TMDB.SCORE || value is not PropertyPredicateBuilder || (value.RHS is IComparable && value.RHS is not string && value.RHS is not Enum));

        public object? ConvertBack(bool value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
