using System.IO;
using System.Net.Http;
using System.Windows;
using Fantoche.App.Common;
using Fantoche.App.Features.Home;
using Fantoche.App.Features.Packages;
using Fantoche.App.Features.Packages.Controls;
using Fantoche.App.Features.Servers;
using Fantoche.App.Features.Settings;
using Fantoche.App.Features.Storage;
using Fantoche.App.Features.Workflows;
using Fantoche.App.Features.Workflows.Controls;
using Fantoche.App.Features.Workflows.Details.Controls;
using Fantoche.App.Features.Workflows.Editor;
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

    /// <summary>
    /// The view drawing each registered view model, merged into the application resources so a page
    /// or an overlay is drawn wherever it is shown.
    /// </summary>
    public ResourceDictionary ViewTemplates { get; } = new();

    /// <summary>How to build each page, keyed by its type, which is what the menu items target.</summary>
    private readonly Dictionary<Type, Func<object>> _factories = new();

    /// <summary>Built the first time they are navigated to, then kept so a page keeps its state.</summary>
    private readonly Dictionary<Type, object> _pages = new();

    private SpineViewModel()
    {
        Services = Settings.IsStandalone ? CreateStandaloneServices() : CreateSupervisorServices();

        Register<HomeViewModel, HomePage>(() => new());
        Register<SettingsViewModel, SettingsPage>(() => new(Settings, Toasts));

        Register<WorkflowsViewModel, WorkflowsPage>(() => new(Services));
        RegisterView<TaskSelectionViewModel, TaskSelectionOverlay>();
        RegisterView<TaskSettingsViewModel, TaskSettingsOverlay>();
        RegisterView<StartSettingsViewModel, StartSettingsOverlay>();
        RegisterView<InstanceDetailViewModel, InstanceDetailOverlay>();

        Register<PackagesViewModel, PackagesPage>(() => new(Packages, Overlayer, Toasts));
        RegisterView<PackageViewModel, PackageOverlay>();
        RegisterView<PackageSelectionViewModel, PackageSelectionOverlay>();

        Register<ServersViewModel, ServersPage>(() => new());
        Register<StorageViewModel, StoragePage>(() => new());

        Navigator = new Navigator(ResolvePage);
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
        var executor = new MixedTaskExecutor(packages.Cache, storage, remote: null);

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
        var packageManagement = new PackageCache(new PackageFolder(Path.Combine(Settings.SharedFolder, "nuget")), Path.Join(Settings.LocalFolderPath, "cache"));
        var storage = new LocalStorage(Path.Combine(Settings.SharedFolder, "storage"));
        var executor = new MixedTaskExecutor(packageManagement, storage, new RemoteTaskExecutor(supervisor));

        // Listens to the changes of the history as soon as the supervisor is up.
        _ = history.StartAsync();

        return new AppServices(scoped, new ExecutionService(scoped, history, executor), history, packages, Overlayer, Toasts);
    }

    /// <summary>Registers a page view model along with the view drawing it.</summary>
    private void Register<TViewModel, TView>(Func<TViewModel> create)
        where TViewModel : notnull
        where TView : FrameworkElement, new()
    {
        _factories[typeof(TViewModel)] = () => create();
        RegisterView<TViewModel, TView>();
    }

    /// <summary>Draws every <typeparamref name="TViewModel"/> with a new <typeparamref name="TView"/>.</summary>
    private void RegisterView<TViewModel, TView>() where TView : FrameworkElement, new()
    {
        var template = new DataTemplate(typeof(TViewModel))
        {
            VisualTree = new FrameworkElementFactory(typeof(TView)),
        };
        ViewTemplates.Add(template.DataTemplateKey, template);
    }

    private object ResolvePage(Type target)
    {
        if (_pages.TryGetValue(target, out object? page))
            return page;

        if (!_factories.TryGetValue(target, out Func<object>? create))
            throw new InvalidOperationException($"No page registered for {target.Name}.");

        page = create();
        _pages[target] = page;
        return page;
    }
}
