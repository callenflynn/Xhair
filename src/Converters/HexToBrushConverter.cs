using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Xhair;

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return System.Windows.Media.Brushes.White;
        }

        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(text);
            return new SolidColorBrush(color);
        }
        catch
        {
            return System.Windows.Media.Brushes.White;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
