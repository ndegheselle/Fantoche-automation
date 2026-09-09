using System.IO;
using Automation.App.Common;
using Automation.App.Features.Home;
using Automation.App.Features.Packages;
using Automation.App.Features.Servers;
using Automation.App.Features.Storage;
using Automation.App.Features.Workflows;
using Automation.Services.Local;
using Automation.Services.Local.Database;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Automation.App;

public class SpineViewModel : ObservableObject
{
    #region Singleton
    private static readonly Lazy<SpineViewModel> _instance = new(() => new SpineViewModel());

    public static SpineViewModel Instance => _instance.Value;
    #endregion

    #region UI
    public Navigator Navigator { get; }
    public OverlayService Overlays { get; } = new();
    public ToastService Toasts { get; } = new();
    #endregion

    #region services
    public Settings Settings { get; } = new Settings();

    /// <summary>What the pages are built with.</summary>
    public AppServices Services { get; }

    public IPackagesService Packages => Services.Packages;
    public IHistoryService History => Services.History;
    public IScopedService Scoped => Services.Scoped;
    public IExecutionService Execution => Services.Execution;
    #endregion

    private readonly Dictionary<Type, object> _pages;

    private SpineViewModel()
    {
        // The factory puts the schema and the starting content in place as it is built.
        var databaseFactory = new DatabaseFactory(Path.Combine(Settings.LocalFolderPath, "automation.db"));

        var history = new LocalHistoryService(databaseFactory);
        var packages = new LocalPackagesService(Settings.PackagesFolderPath, Path.Join(Settings.LocalFolderPath, "cache"));
        var scoped = new LocalScopedService(databaseFactory);
        Services = new AppServices(
            scoped,
            new LocalExecutionService(scoped, history, packages.PackageManagement),
            history,
            packages,
            Overlays,
            Toasts);

        // Take the cost of opening the database off the first navigation.
        _ = Task.Run(() => DatabaseFactory.WarmUpDatabase(databaseFactory));

        _pages = new object[]
        {
            new HomeViewModel(),
            new WorkflowsViewModel(Services),
            new PackagesViewModel(Packages, Overlays, Toasts),
            new ServersViewModel(),
            new StorageViewModel(),
        }.ToDictionary(x => x.GetType());

        Navigator = new Navigator(Resolve);
        // Land on a page at startup.
        Navigator.Navigate(typeof(HomeViewModel));
    }

    private object? Resolve(Type target) => _pages.GetValueOrDefault(target);
}
