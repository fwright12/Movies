using System;
using System.Globalization;
using Xamarin.Forms;

namespace Movies.Views
{
    public class ImageSizeConverter : IValueConverter
    {
        public static readonly ImageSizeConverter Instance = new ImageSizeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is string str ? TMDB.GetFullSizeImage(str) : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}