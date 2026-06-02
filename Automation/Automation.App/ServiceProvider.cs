using System;
using Avalonia;
using ShadUI;

namespace Automation.App;

public static class ServiceProvider
{
    public static readonly Lazy<ThemeWatcher> Themes = new Lazy<ThemeWatcher>(() => new ThemeWatcher(Application.Current!));
    public static readonly Lazy<SettingsViewModel> Settings = new Lazy<SettingsViewModel>(() => new SettingsViewModel(Themes.Value));
}