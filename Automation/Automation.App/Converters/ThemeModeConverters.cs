using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Automation.App.Assets.Fonts;
using Automation.App.Services.UI;

namespace Automation.App.Converters;

public static class ThemeModeConverters
{
    private static readonly Dictionary<ThemeMode, string> Icons = new()
    {
        { ThemeMode.System, LucideIconFont.SunMoon },
        { ThemeMode.Light, LucideIconFont.Moon },
        { ThemeMode.Dark, LucideIconFont.Sun }
    };

    public static readonly IValueConverter ToLucideIcon = new ToLucideIconConverter();

    private sealed class ToLucideIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is ThemeMode mode && Icons.TryGetValue(mode, out var icon) ? icon : Icons[ThemeMode.System];

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
