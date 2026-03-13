using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Xhair.Models;

namespace Xhair;

public sealed class CrosshairShapeVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CrosshairShape shape || parameter is not string expected)
        {
            return Visibility.Collapsed;
        }

        return string.Equals(shape.ToString(), expected, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
