using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies
{
    public class CornerRadiusToThicknessConverter : IValueConverter<CornerRadius, Thickness>
    {
        public static readonly CornerRadiusToThicknessConverter Instance = new CornerRadiusToThicknessConverter();

        public object? Convert(CornerRadius value, Type targetType, object? parameter, CultureInfo culture)
        {
            return new Thickness(
                Math.Max(value.BottomLeft, value.TopLeft),
                Math.Max(value.TopLeft, value.TopRight),
                Math.Max(value.TopRight, value.BottomRight),
                Math.Max(value.BottomRight, value.BottomLeft)
                );
        }

        public object? ConvertBack(Thickness value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
