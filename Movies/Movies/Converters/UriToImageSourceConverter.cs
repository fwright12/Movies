using System;
using System.Globalization;
using Xamarin.Forms;

namespace Movies.Converters
{
    public class UriToImageSourceConverter : IValueConverter
    {
        public static readonly UriToImageSourceConverter Instance = new UriToImageSourceConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uri = value as Uri;

            if (uri == null)
            {
                if (value is string url)
                {
                    uri = new Uri(url);
                }
                else
                {
                    return value;
                }
            }

            if (uri.Scheme == "file")
            {
                return ImageSource.FromResource(uri.OriginalString.Replace(uri.Scheme, string.Empty).Trim('/', ':'));
            }
            else
            {
                return ImageSource.FromUri(uri);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}