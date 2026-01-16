using System.Globalization;

namespace Movies
{
    [BindingValueConverter]
    public class ImageSizeConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null)
            {
                return null;
            }

            var path = values[0].ToString();

            if (values[1] == null || values[2] == null)
            {
                return path;
            }
            var width = (double)values[1];
            var height = (double)values[2];

            var display = DeviceDisplay.Current.MainDisplayInfo;
            if (AreEqual(width, display.Width / display.Density) || AreEqual(height, display.Height / display.Density))
            {
                return TMDB.GetFullSizeImage(path);
            }
            else
            {
                return path;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool AreEqual(double d1, double d2) => Math.Abs(d1 - d2) < 1;
    }
}