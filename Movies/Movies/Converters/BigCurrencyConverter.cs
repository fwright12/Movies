using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Movies.Converters
{
    public class BigCurrencyConverter : IValueConverter
    {
        public static readonly BigCurrencyConverter Instance = new BigCurrencyConverter();

        private static readonly (double Value, string Postfix)[] Formats = new (double, string)[]
        {
            (Math.Pow(10, 9), "B"),
            (Math.Pow(10, 6), "M"),
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double currency))
            {
                return value;
            }

            var str = Math.Round(currency).ToString();
            var trailingZeros = str.Length - str.TrimEnd('0').Length;
            int decimals = 0;

            var format = Formats.FirstOrDefault(format => currency >= format.Value);

            if (trailingZeros < Math.Log10(format.Value) - 3)
            {
                format = default;
            }
            else if (currency != 0 && format.Value != 0)
            {
                currency /= format.Value;
                //decimals = currency.ToString().Length - ((int)currency).ToString().Length;
                decimals = Math.Max(0, (int)Math.Log10(format.Value) - trailingZeros);
            }

            return currency.ToString("C" + decimals) + format.Postfix;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}