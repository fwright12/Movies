using System;
using System.Globalization;
using Xamarin.Forms;

namespace Movies.Converters
{
    public class TimeSpanMinutesConverter : IValueConverter
    {
        public static readonly TimeSpanMinutesConverter Instance = new TimeSpanMinutesConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is TimeSpan span ? span.TotalMinutes : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is double minutes ? TimeSpan.FromMinutes(minutes) : default(TimeSpan);
    }
}