using Automation.App.Base;
using Automation.App.Features.Test;
using Automation.App.Services;
using CommunityToolkit.Mvvm.Input;
using ShadUI;
namespace Automation.App;

internal partial class MainViewModel : ViewModelBase
{
    private readonly ThemeWatcher _themeWatcher;

    public NavigationManager Navigation { get; private set; }
    public ToastManager ToastManager { get; private set; }

    public MainViewModel(ThemeWatcher themeWatcher, NavigationManager navigation, ToastManager toastManager)
    {
        _themeWatcher = themeWatcher;
        Navigation = navigation;
        ToastManager = toastManager;
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

    #region Navigation
    [RelayCommand]
    private void OpenTest()
    {
        Navigation.Navigate(new TestViewModel(ServiceProvider.Toasts.Value));
    }
    #endregion
}
