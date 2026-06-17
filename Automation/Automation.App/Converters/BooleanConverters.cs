using System;
using System.Globalization;
using System.Windows.Data;

namespace Automation.App.Converters;

public static class BooleanConverters
{
    public static readonly IValueConverter NullOrString = new NullOrStringConverter();
    public static readonly IValueConverter EnumEquals = new EnumEqualsConverter();

    private sealed class NullOrStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? null : parameter;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    private sealed class EnumEqualsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.Equals(parameter) ?? false;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
