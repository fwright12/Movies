using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies.Converters
{
    public class UriToImageSourceConverter : IValueConverter<string, ImageSource>
    {
        public TimeSpan CacheValiditity { get; set; }
        public bool CachingEnabled { get; set; }

        public object? Convert(string? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var source = (ImageSource)value;

            if (source is UriImageSource uriSource)
            {
                uriSource.CacheValidity = CacheValiditity;
                uriSource.CachingEnabled = CachingEnabled;
            }

            return source;
        }

        public object? ConvertBack(ImageSource? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}