using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Automation.App.Converters;

/// <summary>
/// General-purpose value converters used throughout the views. Exposed as static fields so they can
/// be referenced with <c>{x:Static converters:CommonConverters.X}</c> (no resource declaration needed).
/// </summary>
public static class CommonConverters
{
    /// <summary>true =&gt; Visible, false =&gt; Collapsed.</summary>
    public static readonly IValueConverter BoolToVisibility = new BoolToVisibilityConverter(false);

    /// <summary>true =&gt; Collapsed, false =&gt; Visible.</summary>
    public static readonly IValueConverter InverseBoolToVisibility = new BoolToVisibilityConverter(true);

    /// <summary>null =&gt; Visible, otherwise Collapsed.</summary>
    public static readonly IValueConverter NullToVisibility = new NullToVisibilityConverter(false);

    /// <summary>not null =&gt; Visible, otherwise Collapsed.</summary>
    public static readonly IValueConverter NotNullToVisibility = new NullToVisibilityConverter(true);

    /// <summary>Inverts a boolean.</summary>
    public static readonly IValueConverter InvertBool = new InvertBoolConverter();

    private sealed class BoolToVisibilityConverter : IValueConverter
    {
        private readonly bool _invert;
        public BoolToVisibilityConverter(bool invert) => _invert = invert;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            if (_invert) flag = !flag;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Visibility v && (v == Visibility.Visible) ^ _invert;
    }

    private sealed class NullToVisibilityConverter : IValueConverter
    {
        private readonly bool _visibleWhenNotNull;
        public NullToVisibilityConverter(bool visibleWhenNotNull) => _visibleWhenNotNull = visibleWhenNotNull;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNull = value is null || (value is string s && s.Length == 0);
            bool visible = _visibleWhenNotNull ? !isNull : isNull;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    private sealed class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is not bool b || !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is not bool b || !b;
    }
}
