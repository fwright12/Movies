using Movies.ViewModels;
using System.Globalization;

namespace Movies
{
    [BindingValueConverter]
    public class ShowPredicateLHSConverter : IValueConverter<OperatorPredicateBuilder, bool>
    {
        public object? Convert(OperatorPredicateBuilder? value, Type targetType, object? parameter, CultureInfo culture) => value != null && (value.LHS == TMDB.SCORE || value is not PropertyPredicateBuilder);

        public object? ConvertBack(bool value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
