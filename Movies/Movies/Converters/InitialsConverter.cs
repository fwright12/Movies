using System;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;

namespace Movies
{
    public class InitialsConverter : IValueConverter
    {
        public static readonly InitialsConverter Instance = new InitialsConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? null : (string.Join(". ", value.ToString().Split(' ').Where(name => !string.IsNullOrEmpty(name)).Select(name => name[0])) + ".");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
