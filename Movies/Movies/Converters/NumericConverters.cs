using System;
using System.Globalization;
using Xamarin.Forms;

namespace Movies.Converters
{
    public class DoubleToIntConverter : IValueConverter
    {
        public static readonly DoubleToIntConverter Instance = new DoubleToIntConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is int l ? (double)l : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is double d ? (int)d : value;
    }

    public class DoubleToLongConverter : IValueConverter
    {
        public static readonly DoubleToLongConverter Instance = new DoubleToLongConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is long l ? (double)l : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is double d ? (long)d : value;
    }

    public class ObjectToDoubleConverter : IValueConverter
    {
        public static readonly ObjectToDoubleConverter Instance = new ObjectToDoubleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is double d ? d : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is double d ? d : value;
    }
}