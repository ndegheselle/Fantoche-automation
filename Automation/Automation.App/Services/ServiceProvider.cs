using System;
using System.IO;
using Automation.App.Services.UI;
using Automation.Shared.Services;
using Automation.Worker.Packages;
using Avalonia;
using ShadUI;

namespace Automation.App.Services;

internal static class ServiceProvider
{
    #region Singletons
    private static readonly ToastManager _toastManager = new ToastManager();

    public static readonly Lazy<ThemeWatcher> Themes = new Lazy<ThemeWatcher>(() => new ThemeWatcher(Application.Current!));
    public static readonly Lazy<NavigationManager> Navigation = new Lazy<NavigationManager>(() => new NavigationManager());
    public static readonly Lazy<ToastDisplay> Toasts = new Lazy<ToastDisplay>(() => new ToastDisplay(_toastManager));
    #endregion

    public static readonly Lazy<IPackagesService> Packages = new Lazy<IPackagesService>(() =>
    {
        string nuggetLocalPath = Path.Join(Directory.GetCurrentDirectory(), "nugetlocal");
        return new LocalPackageManagement(nuggetLocalPath);
    });

    #region Transient
    public static MainViewModel Settings => new MainViewModel(Themes.Value, Navigation.Value, _toastManager);
    #endregion
}