using System.IO;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantoche.App.Features.Packages;
using Fantoche.App.Features.Packages.Controls;
using Fantoche.App.Features.Scheduler;
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
using Fantoche.Worker.Storage;
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
    public IExecutionService Execution => _provider.GetRequiredService<IExecutionService>();
    public IDispatchService Dispatch => _provider.GetRequiredService<IDispatchService>();
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
        string workerFolder = Path.Combine(Settings.LocalFolderPath, "worker");
        string storageFolder = Path.Combine(workerFolder, "storage");
        ISupervisor supervisor = Settings.IsStandalone ? CreateLocalSupervisor(storageFolder) : CreateSupervisorClient(storageFolder);
        LocalWorker worker = LocalWorker.InFolder(supervisor, workerFolder, storageFolder);

        // Alone, the application is the only worker there is to run the tasks set to run on one.
        string? dispatchId = Settings.IsStandalone ? Settings.ApplicationName : null;
        if (dispatchId != null)
            _ = supervisor.AddWorkerAsync(worker, dispatchId, Environment.ProcessorCount);

        services.AddSingleton(supervisor);
        services.AddSingleton(supervisor.Scoped);
        services.AddSingleton(supervisor.History);
        services.AddSingleton(supervisor.Packages);
        services.AddSingleton(supervisor.Dispatch);
        // The machine name stays the same from one start to the next : the application finds its runs again.
        ExecutionService execution = new(supervisor.Scoped, supervisor.History, worker, Environment.MachineName);
        _ = InterruptLeftRunsAsync(execution);
        services.AddSingleton<IExecutionService>(execution);
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
        services.AddSingleton<SchedulerViewModel>();
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
        RegisterView<ScheduleViewModel, ScheduleOverlay>();

        RegisterView<PackagesViewModel, PackagesPage>();
        RegisterView<PackageViewModel, PackageOverlay>();
        RegisterView<PackageSelectionViewModel, PackageSelectionOverlay>();

        RegisterView<SchedulerViewModel, SchedulerPage>();

        RegisterView<WorkersViewModel, WorkersPage>();
    }

    /// <summary>
    /// What the application left running when it last stopped is interrupted, so it can be resumed.
    /// The supervisor is waited for in remote mode.
    /// </summary>
    private static async Task InterruptLeftRunsAsync(ExecutionService execution)
    {
        while (true)
        {
            try
            {
                await execution.InterruptLeftRunsAsync();
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    /// <param name="storageFolder">The storage of the worker of the application, its only worker.</param>
    private ISupervisor CreateLocalSupervisor(string storageFolder)
    {
        var supervisor = new LocalSupervisor(Settings.LocalFolderPath, storageFolder);
        // Take the cost of opening the database off the first navigation.
        _ = Task.Run(supervisor.WarmUpDatabase);
        _ = supervisor.RunCleanupAsync(CancellationToken.None);
        return supervisor;
    }

    /// <param name="storageFolder">
    /// The storage of the worker of the application, out of reach of the supervisor : cleaned here
    /// by the same rule.
    /// </param>
    private ISupervisor CreateSupervisorClient(string storageFolder)
    {
        var supervisor = new SupervisorClient(Settings.SupervisorAddress);
        // Listens to the changes of the history and of the dispatch as soon as the supervisor is up.
        _ = supervisor.History.StartAsync();
        _ = supervisor.Dispatch.StartAsync();
        _ = Task.Run(() => ExecutionStorages.RemoveStartedBefore(storageFolder, LocalSupervisor.CleanupCutoff(DateTime.UtcNow)));
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
