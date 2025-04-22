using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MauiApp.Converters;

public class InverseBoolConverter : IValueConverter
{
    public InverseBoolConverter() { }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true; // По умолчанию неактивно, если значение не bool
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}