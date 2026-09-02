using System.IO;
using Automation.App.Features.Home;
using Automation.App.Features.Packages;
using Automation.App.Features.Servers;
using Automation.App.Features.Storage;
using Automation.App.Features.Workflows;
using Automation.Services.Local;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Automation.App;

public class Settings
{
    public const string ApplicationName = "Automation";

    public string PackagesFolderPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "nuggets");
    public string LocalFolderPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), ApplicationName);
}

public class SpineViewModel : ObservableObject
{
    #region Singleton
    private static readonly Lazy<SpineViewModel> lazy =
        new Lazy<SpineViewModel>(() => new SpineViewModel());

    public static SpineViewModel Instance { get { return lazy.Value; } }
    #endregion

    #region UI
    public Navigator Navigator { get; }
    public OverlayService Overlays { get; } = new();
    public ToastService Toasts { get; } = new();
    #endregion

    #region services
    public Settings Settings { get; } = new Settings();
    public IPackagesService Packages { get; }
    public IHistoryService History { get; }
    public IScopedService Scoped { get; }
    public IExecutionService Execution { get; }
    #endregion

    private readonly Dictionary<Type, object> _pages;

    private SpineViewModel()
    {
        var dbContextFactory = new LocalDbContextFactory(Path.Combine(Settings.LocalFolderPath, "automation.db"));

        var history = new LocalHistoryService(dbContextFactory);
        var packages = new LocalPackagesService(Settings.PackagesFolderPath, Path.Join(Settings.LocalFolderPath, "cache"));
        var scoped = new LocalScopedService(dbContextFactory);
        Packages = packages;
        History = history;
        Scoped = scoped;
        Execution = new LocalExecutionService(scoped, history, dbContextFactory, packages.PackageManagement);

        // Take the cost of building the EF model off the first navigation.
        _ = Task.Run(() => dbContextFactory.WarmupAsync());

        _pages = new object[]
        {
            new HomeViewModel(),
            new WorkflowsViewModel(Scoped),
            new PackagesViewModel(Packages, Overlays, Toasts),
            new ServersViewModel(),
            new StorageViewModel(),
        }.ToDictionary(x => x.GetType());

        Navigator = new Navigator(Resolve);
        // Land on a page at startup.
        Navigator.Navigate(typeof(HomeViewModel));
    }

    object? Resolve(Type target)
    {
        return _pages.GetValueOrDefault(target);
    }
}
