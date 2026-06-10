using System.Globalization;

namespace Movies
{
    [ContentProperty(nameof(TypeToConverterMapping))]
    public class TypeDisplayConverter : IValueConverter
    {
        public MappingConverter? TypeToConverterMapping { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var converted = TypeToConverterMapping?.Convert(value.GetType(), targetType, parameter, culture);
            if (converted is IValueConverter converter)
            {
                return converter.Convert(value, targetType, parameter, culture);
            }
            else if (converted is string str)
            {
                return string.Format(str, value);
            }
            else
            {
                return value;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
