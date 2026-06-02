using Automation.App.Base;
using CommunityToolkit.Mvvm.Input;
using ShadUI;
namespace Automation.App;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ThemeWatcher _themeWatcher;
    
    public SettingsViewModel(ThemeWatcher themeWatcher)
    {
        _themeWatcher = themeWatcher;
    }

    private ThemeMode _currentTheme;
    public ThemeMode CurrentTheme
    {
        get => _currentTheme;
        private set => SetProperty(ref _currentTheme, value);
    }

    [RelayCommand]
    private void SwitchTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            _ => ThemeMode.System
        };

        _themeWatcher.SwitchTheme(CurrentTheme);
    }
}
