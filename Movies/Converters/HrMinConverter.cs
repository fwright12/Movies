using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies
{
    public class HrMinConverter : IValueConverter
    {
        public static readonly HrMinConverter Instance = new HrMinConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is TimeSpan runtime ? ((runtime.Hours > 0 ? runtime.ToString(@"h\h\ ") : "") + runtime.ToString(@"m\m")) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
