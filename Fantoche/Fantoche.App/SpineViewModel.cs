using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantoche.App.Features.Packages;
using Fantoche.App.Features.Packages.Controls;
using Fantoche.App.Features.Settings;
using Fantoche.App.Features.Workers;
using Fantoche.App.Features.Workflows;
using Fantoche.App.Features.Workflows.Controls;
using Fantoche.App.Features.Workflows.Details.Controls;
using Fantoche.App.Features.Workflows.Editor;
using Fantoche.Services.Local;
using Fantoche.Services.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Executor;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>What the pages are built with : the services, and the pages themselves.</summary>
    private readonly ServiceProvider _provider;

    public IPackagesService Packages => _provider.GetRequiredService<IPackagesService>();
    public IHistoryService History => _provider.GetRequiredService<IHistoryService>();
    public IScopedService Scoped => _provider.GetRequiredService<IScopedService>();
    #endregion

    public IRelayCommand OpenSettingsCommand { get; }

    /// <summary>
    /// The view drawing each registered view model, merged into the application resources so a page
    /// or an overlay is drawn wherever it is shown.
    /// </summary>
    public ResourceDictionary ViewTemplates { get; } = new();

    private SpineViewModel()
    {
        ServiceCollection services = new();
        AddServices(services);
        AddPages(services);
        _provider = services.BuildServiceProvider();
        RegisterViews();

        Navigator = new Navigator(_provider.GetRequiredService);
        OpenSettingsCommand = new RelayCommand(() => Navigator.Navigate(typeof(SettingsViewModel)));
        // Land on a page at startup.
        Navigator.Navigate(typeof(WorkflowsViewModel));
    }

    /// <summary>
    /// The supervisor (in the application in standalone mode, reached through its API otherwise)
    /// with its services, the worker of the application, which runs what the application starts,
    /// and what the shell itself offers the pages.
    /// </summary>
    private void AddServices(ServiceCollection services)
    {
        ISupervisor supervisor = Settings.IsStandalone ? CreateLocalSupervisor() : CreateSupervisorClient();
        LocalWorker worker = LocalWorker.InFolder(supervisor, Path.Combine(Settings.LocalFolderPath, "worker"));

        // Alone, the application is the only worker there is to run the tasks set to run on one.
        string? dispatchId = Settings.IsStandalone ? Settings.ApplicationName : null;
        if (dispatchId != null)
            _ = supervisor.AddWorkerAsync(worker, dispatchId, Environment.ProcessorCount);

        services.AddSingleton(supervisor);
        services.AddSingleton(supervisor.Scoped);
        services.AddSingleton(supervisor.History);
        services.AddSingleton(supervisor.Packages);
        services.AddSingleton(supervisor.Dispatch);
        services.AddSingleton<IExecutionService>(new ExecutionService(supervisor.Scoped, supervisor.History, worker));
        services.AddSingleton(new ApplicationWorker(dispatchId));

        services.AddSingleton(Settings);
        services.AddSingleton<IOverlayer>(Overlayer);
        services.AddSingleton<IToastService>(Toasts);
    }

    /// <summary>
    /// The pages of the menu, which the navigator asks for by type. Singletons : built the first
    /// time they are navigated to, then kept so a page keeps its state. A page follows the events
    /// of a service only while it is shown (subscribed on load, unsubscribed on unload).
    /// <para>
    /// The overlays and the details of an element aren't here : each is built for the data it
    /// shows, which only the code opening it has.
    /// </para>
    /// </summary>
    private static void AddPages(ServiceCollection services)
    {
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<WorkflowsViewModel>();
        services.AddSingleton<PackagesViewModel>();
        services.AddSingleton<WorkersViewModel>();
    }

    /// <summary>
    /// The view drawing each page and overlay, wherever its view model is shown (see
    /// <see cref="ViewTemplates"/>).
    /// </summary>
    private void RegisterViews()
    {
        RegisterView<SettingsViewModel, SettingsPage>();

        RegisterView<WorkflowsViewModel, WorkflowsPage>();
        RegisterView<TaskSelectionViewModel, TaskSelectionOverlay>();
        RegisterView<TaskSettingsViewModel, TaskSettingsOverlay>();
        RegisterView<StartSettingsViewModel, StartSettingsOverlay>();
        RegisterView<InstanceDetailViewModel, InstanceDetailOverlay>();

        RegisterView<PackagesViewModel, PackagesPage>();
        RegisterView<PackageViewModel, PackageOverlay>();
        RegisterView<PackageSelectionViewModel, PackageSelectionOverlay>();

        RegisterView<WorkersViewModel, WorkersPage>();
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
        // Listens to the changes of the history and of the dispatch as soon as the supervisor is up.
        _ = supervisor.History.StartAsync();
        _ = supervisor.Dispatch.StartAsync();
        return supervisor;
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
}
