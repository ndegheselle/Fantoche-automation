using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Automation.App.Converters;

/// <summary>
/// Converts a hex color string (e.g. <c>#FF3366</c>) to a <see cref="Color"/> and back, so a
/// <see cref="string"/> property can be bound to a color picker.
/// </summary>
public class StringToColor : IValueConverter
{
    public static readonly StringToColor Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && TryParse(text, out var color))
            return color;
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return color.ToString();
        return DependencyProperty.UnsetValue;
    }

    internal static bool TryParse(string text, out Color color)
    {
        try
        {
            color = (Color)ColorConverter.ConvertFromString(text);
            return true;
        }
        catch
        {
            color = default;
            return false;
        }
    }
}

/// <summary>
/// Converts a hex color string (e.g. <c>#FF3366</c>) to a <see cref="Brush"/>, returning
/// <see cref="Binding.DoNothing"/> when the string is empty or invalid so the target keeps its value.
/// </summary>
public class StringToBrush : IValueConverter
{
    public static readonly StringToBrush Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && StringToColor.TryParse(text, out var color))
            return new SolidColorBrush(color);
        return Binding.DoNothing;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
            return brush.Color.ToString();
        return Binding.DoNothing;
    }
}
