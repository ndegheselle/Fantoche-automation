using System.IO;
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
using Fantoche.Services.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Executor;
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
        Services = CreateServices();

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
    /// The supervisor (in the application in standalone mode, reached through its API otherwise)
    /// and the worker of the application, which runs what the application starts.
    /// </summary>
    private AppServices CreateServices()
    {
        ISupervisor supervisor = Settings.IsStandalone ? CreateLocalSupervisor() : CreateSupervisorClient();
        LocalWorker worker = LocalWorker.InFolder(supervisor, Path.Combine(Settings.LocalFolderPath, "worker"));

        // Alone, the application is the only worker there is to run the tasks set to run on one.
        if (Settings.IsStandalone)
            _ = supervisor.AddWorkerAsync(worker, Settings.ApplicationName, Environment.ProcessorCount);

        var execution = new ExecutionService(supervisor.Scoped, supervisor.History, worker);
        return new AppServices(supervisor.Scoped, execution, supervisor.History, supervisor.Packages, Overlayer, Toasts);
    }

    private ISupervisor CreateLocalSupervisor()
    {
        var supervisor = new LocalSupervisor(Settings.LocalFolderPath);
        // Take the cost of opening the database off the first navigation.
        _ = Task.Run(supervisor.WarmUpDatabase);
        return supervisor;
    }

    private ISupervisor CreateSupervisorClient()
    {
        var supervisor = new SupervisorClient(Settings.SupervisorAddress);
        // Listens to the changes of the history as soon as the supervisor is up.
        _ = supervisor.History.StartAsync();
        return supervisor;
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
