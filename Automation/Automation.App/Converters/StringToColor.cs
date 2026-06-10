using System;
using System.Globalization;
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
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return color.ToString();
        return null;
    }
}
