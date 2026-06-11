using Automation.App.Base;
using Automation.App.Features.Packages;
using Automation.App.Features.Scoped;
using Automation.App.Features.Workflows;
using Automation.App.Services;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App;

internal partial class MainVm : ViewModelBase
{
    private readonly ThemeWatcher _themeWatcher;

    public NavigationManager Navigation { get; private set; }
    public ToastManager ToastManager { get; private set; }
    public DialogManager DialogManager { get; private set; }

    public MainVm(ThemeWatcher themeWatcher, NavigationManager navigation, ToastManager toastManager, DialogManager dialogManager)
    {
        _themeWatcher = themeWatcher;
        Navigation = navigation;
        ToastManager = toastManager;
        DialogManager = dialogManager;
        
        OpenWorkflows();
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
    private void OpenPackages()
    {
        Navigation.Navigate(new PackagesPageVm(ServiceProvider.Packages, ServiceProvider.Dialogs, ServiceProvider.Toasts.Value, ServiceProvider.Navigation.Value));
    }
    
    [RelayCommand]
    private void OpenWorkflows()
    {
        Navigation.Navigate(new WorkflowsPageVm(ServiceProvider.Scoped));
    }
    #endregion
}
