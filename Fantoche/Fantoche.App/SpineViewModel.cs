using System.IO;
using Fantoche.App.Common;
using Fantoche.App.Features.Home;
using Fantoche.App.Features.Packages;
using Fantoche.App.Features.Servers;
using Fantoche.App.Features.Storage;
using Fantoche.App.Features.Workflows;
using Fantoche.Services.Local;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Services;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Remote;
using Fantoche.Worker.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Fantoche.App;

public class SpineViewModel : ObservableObject
{
    #region Singleton
    private static readonly Lazy<SpineViewModel> _instance = new(() => new SpineViewModel());

    public static SpineViewModel Instance => _instance.Value;
    #endregion

    #region UI
    public Navigator Navigator { get; }
    public Overlayer Overlayer { get; } = new();
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
        var databaseFactory = new DatabaseFactory(Path.Combine(Settings.LocalFolderPath, "Fantoche.db"));

        var history = new LocalHistoryService(databaseFactory);
        var packages = new LocalPackagesService(Settings.PackagesFolderPath, Path.Join(Settings.LocalFolderPath, "cache"));
        var scoped = new LocalScopedService(databaseFactory);
        var storage = new LocalStorage(Path.Combine(Settings.LocalFolderPath, "storage"));
        var remote = Settings.SupervisorUrl == null ? null : new RemoteTaskExecutor(Settings.SupervisorUrl);
        var executor = new MixedTaskExecutor(packages.PackageManagement, storage, remote);
        Services = new AppServices(
            scoped,
            new LocalExecutionService(scoped, history, executor),
            history,
            packages,
            Overlayer,
            Toasts);

        // Take the cost of opening the database off the first navigation.
        _ = Task.Run(() => DatabaseFactory.WarmUpDatabase(databaseFactory));

        _pages = new object[]
        {
            new HomeViewModel(),
            new WorkflowsViewModel(Services),
            new PackagesViewModel(Packages, Overlayer, Toasts),
            new ServersViewModel(),
            new StorageViewModel(),
        }.ToDictionary(x => x.GetType());

        Navigator = new Navigator(Resolve);
        // Land on a page at startup.
        Navigator.Navigate(typeof(HomeViewModel));
    }

    private object? Resolve(Type target) => _pages.GetValueOrDefault(target);
}
