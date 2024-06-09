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
            Uri uri;

            try
            {
                uri = new Uri(value.ToString(), UriKind.RelativeOrAbsolute);

                if (uri.IsAbsoluteUri)
                {
                    if (uri.Scheme == "file")
                    {
                        return ImageSource.FromResource(uri.OriginalString.Replace(uri.Scheme, string.Empty).Trim('/', ':'));
                    }
                    else
                    {
                        var source = parameter as UriImageSource ?? new UriImageSource();
                        source.Uri = uri;
                        return source;
                    }
                }
            }
            catch
            { }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}