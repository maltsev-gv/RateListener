using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace RateListener.Converters;

public abstract class ValueConverterBase : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        this;

    /// <summary>
    /// Convert value from ViewModel => View
    /// </summary>
    public abstract object Convert(
        object value,
        Type targetType,
        object parameter,
        System.Globalization.CultureInfo culture);

    /// <summary>
    /// Convert value from View => ViewModel
    /// </summary>
    public virtual object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        System.Globalization.CultureInfo culture) =>
        value;
}