using System;
using System.Globalization;

namespace RateListener.Converters
{
    public class IsNumberNegativeConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is {} numberStr && 
                double.TryParse(numberStr, out var number))
            {
                return number > 0;
            }
            return false;
        }
    }
}
