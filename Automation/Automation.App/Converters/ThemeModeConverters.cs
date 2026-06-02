using System.Collections.Generic;
using Automation.App.Assets.Fonts;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ShadUI;

namespace Automation.App.Converters;

public static class ThemeModeConverters
{
    private static readonly Dictionary<ThemeMode, string> Icons = new()
    {
        { ThemeMode.System, LucideIconFont.SunMoon },
        { ThemeMode.Light, LucideIconFont.Moon },
        { ThemeMode.Dark, LucideIconFont.Sun }
    };

    public static readonly IValueConverter ToLucideIcon =
        new FuncValueConverter<ThemeMode, string>(mode => Icons.TryGetValue(mode, out var icon) ? icon : Icons[0]);
}