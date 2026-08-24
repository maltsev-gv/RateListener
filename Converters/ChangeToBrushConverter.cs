using System;
using System.Globalization;
using System.Windows.Media;

namespace RateListener.Converters;

public class ChangeToBrushConverter : ValueConverterBase
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString() is { } numberStr &&
            double.TryParse(numberStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            if (number > 0)
                return Brushes.Green;
            if (number < 0)
                return Brushes.Red;
        }
        return Brushes.Gray;
    }
}
