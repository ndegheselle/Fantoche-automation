using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Automation.App.Converters;

/// <summary>
/// Converts a hex color string (e.g. <c>#FF3366</c>) to an Avalonia <see cref="Color"/> and back,
/// so a <see cref="string"/> property can be bound to a <c>ColorPicker</c>.
/// </summary>
public class StringToColor : IValueConverter
{
    public static readonly StringToColor Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && Color.TryParse(text, out var color))
            return color;
        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return color.ToString();
        return AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// Converts a hex color string (e.g. <c>#FF3366</c>) to an Avalonia <see cref="IBrush"/>, returning
/// <c>null</c> when the string is empty or invalid so the target falls back to its inherited value.
/// </summary>
public class StringToBrush : IValueConverter
{
    public static readonly StringToBrush Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && Color.TryParse(text, out var color))
            return new SolidColorBrush(color);
        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISolidColorBrush brush)
            return brush.Color.ToString();
        return BindingOperations.DoNothing;
    }
}
