using System.IO;
using System.Net.Http;
using Fantoche.App.Common;
using Fantoche.App.Features.Home;
using Fantoche.App.Features.Packages;
using Fantoche.App.Features.Servers;
using Fantoche.App.Features.Settings;
using Fantoche.App.Features.Storage;
using Fantoche.App.Features.Workflows;
using Fantoche.Services.Local;
using Fantoche.Services.Local.Database;
using Fantoche.Services.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Executor.Remote;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public Settings Settings { get; } = Settings.Load();

    /// <summary>What the pages are built with.</summary>
    public AppServices Services { get; }

    public IPackagesService Packages => Services.Packages;
    public IHistoryService History => Services.History;
    public IScopedService Scoped => Services.Scoped;
    public IExecutionService Execution => Services.Execution;
    #endregion

    public IRelayCommand OpenSettingsCommand { get; }

    private readonly Dictionary<Type, object> _pages;

    private SpineViewModel()
    {
        Services = Settings.IsStandalone ? CreateStandaloneServices() : CreateSupervisorServices();

        _pages = new object[]
        {
            new HomeViewModel(),
            new WorkflowsViewModel(Services),
            new PackagesViewModel(Packages, Overlayer, Toasts),
            new ServersViewModel(),
            new StorageViewModel(),
            new SettingsViewModel(Settings, Toasts),
        }.ToDictionary(x => x.GetType());

        Navigator = new Navigator(Resolve);
        OpenSettingsCommand = new RelayCommand(() => Navigator.Navigate(typeof(SettingsViewModel)));
        // Land on a page at startup.
        Navigator.Navigate(typeof(HomeViewModel));
    }

    /// <summary>
    /// Everything in the application : its own database, packages and storage, and no worker to
    /// send a task to (one set to run on a worker fails).
    /// </summary>
    private AppServices CreateStandaloneServices()
    {
        // The factory puts the schema and the starting content in place as it is built.
        var databaseFactory = new DatabaseFactory(Path.Combine(Settings.LocalFolderPath, "Fantoche.db"));

        var history = new LocalHistoryService(databaseFactory);
        var packages = new LocalPackagesService(Settings.PackagesFolderPath, Path.Join(Settings.LocalFolderPath, "cache"));
        var scoped = new LocalScopedService(databaseFactory);
        var storage = new LocalStorage(Path.Combine(Settings.LocalFolderPath, "storage"));
        var executor = new MixedTaskExecutor(packages.PackageManagement, storage, remote: null);

        // Take the cost of opening the database off the first navigation.
        _ = Task.Run(() => DatabaseFactory.WarmUpDatabase(databaseFactory));

        return new AppServices(scoped, new ExecutionService(scoped, history, executor), history, packages, Overlayer, Toasts);
    }

    /// <summary>
    /// The data kept by the supervisor. The tasks run in the application read the packages and the
    /// storage the workers do, from the shared folder.
    /// </summary>
    private AppServices CreateSupervisorServices()
    {
        Uri supervisor = Settings.SupervisorAddress;
        var http = new HttpClient { BaseAddress = supervisor };

        var history = new RemoteHistoryService(http);
        var packages = new RemotePackagesService(http);
        var scoped = new RemoteScopedService(http);
        var packageManagement = new LocalPackageManagement(Path.Combine(Settings.SharedFolder, "nuget"), Path.Join(Settings.LocalFolderPath, "cache"));
        var storage = new LocalStorage(Path.Combine(Settings.SharedFolder, "storage"));
        var executor = new MixedTaskExecutor(packageManagement, storage, new RemoteTaskExecutor(supervisor));

        // Listens to the changes of the history as soon as the supervisor is up.
        _ = history.StartAsync();

        return new AppServices(scoped, new ExecutionService(scoped, history, executor), history, packages, Overlayer, Toasts);
    }

    private object? Resolve(Type target) => _pages.GetValueOrDefault(target);
}
