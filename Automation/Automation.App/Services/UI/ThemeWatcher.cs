using System;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace Automation.App.Services.UI;

/// <summary>
/// Applies the requested <see cref="ThemeMode"/> through WPF UI's <see cref="ApplicationThemeManager"/>.
/// Replaces the ShadUI <c>ThemeWatcher</c> used before the WPF migration.
/// </summary>
internal class ThemeWatcher
{
    private ThemeMode _current = ThemeMode.Dark;

    /// <summary>
    /// Applies the initial theme. Called once at startup.
    /// </summary>
    public void Initialize() => Apply(_current);

    /// <summary>
    /// Switches to the given <paramref name="mode"/> and applies it immediately.
    /// </summary>
    public void SwitchTheme(ThemeMode mode)
    {
        _current = mode;
        Apply(mode);
    }

    private static void Apply(ThemeMode mode)
    {
        ApplicationTheme theme = mode switch
        {
            ThemeMode.Light => ApplicationTheme.Light,
            ThemeMode.Dark => ApplicationTheme.Dark,
            _ => GetSystemTheme()
        };
        ApplicationThemeManager.Apply(theme, updateAccent: true);
    }

    private static ApplicationTheme GetSystemTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            // AppsUseLightTheme: 1 => light, 0 => dark.
            if (key?.GetValue("AppsUseLightTheme") is int appsUseLight && appsUseLight == 0)
                return ApplicationTheme.Dark;
        }
        catch
        {
            // Registry unavailable — fall back to dark.
        }

        return ApplicationTheme.Light;
    }
}
