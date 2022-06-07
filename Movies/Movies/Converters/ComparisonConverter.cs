using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Movies.Converters
{
    public class ComparisonConverter : IValueConverter
    {
        public static readonly ComparisonConverter Instance = new ComparisonConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is Operators comparison && comparison != Operators.NotEqual ? new List<int> { (int)comparison + 1 } : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (Operators)(((value as IList<int>)?.FirstOrDefault() ?? 0) - 1);
    }
}
