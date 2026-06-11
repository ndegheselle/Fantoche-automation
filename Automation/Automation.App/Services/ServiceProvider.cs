using System;
using System.IO;
using Automation.App.Features.Packages;
using Automation.App.Features.Scoped.Components;
using Automation.App.Services.UI;
using Automation.Services.Local;
using Automation.Shared.Services;
using Avalonia;
using ShadUI;
using MetadataEditDialog = Automation.App.Features.Scoped.Components.MetadataEditDialog;

namespace Automation.App.Services;

internal static class ServiceProvider
{
    #region Singletons
    private static readonly ToastManager _toastManager = new ToastManager();
    private static readonly DialogManager _dialogManager = CreateDialogManager();

    public static DialogManager Dialogs => _dialogManager;

    private static DialogManager CreateDialogManager()
    {
        var manager = new DialogManager();
        manager.Register<MetadataEditDialog, MetadataEditVm>();
        return manager;
    }

    public static readonly Lazy<ThemeWatcher> Themes = new Lazy<ThemeWatcher>(() => new ThemeWatcher(Application.Current!));
    public static readonly Lazy<NavigationManager> Navigation = new Lazy<NavigationManager>(() => new NavigationManager());
    public static readonly Lazy<ToastDisplay> Toasts = new Lazy<ToastDisplay>(() => new ToastDisplay(_toastManager));
    #endregion

    #region Transient
    public static MainVm Main => new MainVm(Themes.Value, Navigation.Value, _toastManager, _dialogManager);

    public static IPackagesService Packages
    {
        get
        {
            string nuggetLocalPath = Path.Join(Directory.GetCurrentDirectory(), "nugetlocal");
            return new LocalPackagesService(nuggetLocalPath);
        }
    }

    public static IScopedService Scoped => new LocalScopedService();
    #endregion
}