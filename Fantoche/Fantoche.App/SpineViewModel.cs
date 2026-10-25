using System.IO;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantoche.App.Common;
using Fantoche.App.Features.Packages;
using Fantoche.App.Features.Packages.Controls;
using Fantoche.App.Features.Scheduler;
using Fantoche.App.Features.Settings;
using Fantoche.App.Features.UserInterfaces;
using Fantoche.App.Features.Workers;
using Fantoche.App.Features.Workflows;
using Fantoche.App.Features.Workflows.Controls;
using Fantoche.App.Features.Workflows.Details.Controls;
using Fantoche.App.Features.Workflows.Editor;
using Fantoche.Services.Local;
using Fantoche.Services.Remote;
using Fantoche.Shared.Remote;
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
    public UserPrompts Prompts => _provider.GetRequiredService<UserPrompts>();
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
        // The machine name stays the same from one start to the next : the application finds its runs
        // again. The copies running side by side on the machine tell their runs apart by session,
        // the one of the supervisor in standalone mode : both live and die with the process.
        ISupervisor supervisor;
        RunnerSessions sessions;
        if (Settings.IsStandalone)
        {
            LocalSupervisor local = CreateLocalSupervisor(storageFolder);
            supervisor = local;
            sessions = local.Sessions;
        }
        else
        {
            supervisor = CreateSupervisorClient(storageFolder);
            sessions = new RunnerSessions(Path.Combine(Settings.LocalFolderPath, "sessions"));
        }
        // The tasks asking the user run here, their page shown by the application : the ones of its
        // own runs, and the ones of the workers its user takes from the supervisor, which only
        // hands them over then.
        // The prompts and the worker need each other : the registration is set right after both exist.
        Task<IWorkerRegistration>? registration = null;
        UserPrompts prompts = new(
            Path.Combine(Settings.LocalFolderPath, "browser"),
            supervisor.History,
            async executionId => await (await registration!).TakeAsync(executionId));
        prompts.Asked += request => Ui.Post(() => Toasts.Info($"'{request.Instance.NodeName}' waits for you : answer it from its run.", "A task asks for you"));
        LocalWorker worker = LocalWorker.InFolder(supervisor, workerFolder, storageFolder, prompts);

        // Alone, the application is also the only worker there is to run what the supervisor dispatches.
        string dispatchId = Settings.IsStandalone ? Settings.ApplicationName : Environment.MachineName;
        EnumWorkerRoles roles = Settings.IsStandalone ? EnumWorkerRoles.Runs | EnumWorkerRoles.UserTasks : EnumWorkerRoles.UserTasks;
        registration = supervisor.AddWorkerAsync(worker, dispatchId, Environment.ProcessorCount, roles);

        services.AddSingleton(supervisor);
        services.AddSingleton(supervisor.Scoped);
        services.AddSingleton(supervisor.History);
        services.AddSingleton(supervisor.Packages);
        services.AddSingleton(supervisor.Dispatch);
        ExecutionService execution = new(supervisor.Scoped, supervisor.History, worker, Environment.MachineName, sessions);
        _ = SuspendLeftRunsAsync(execution);
        services.AddSingleton<IExecutionService>(execution);
        services.AddSingleton(prompts);
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
    /// What a copy of the application left running as it stopped is suspended as this one starts,
    /// so it can be resumed. The supervisor is waited for in remote mode.
    /// </summary>
    private static async Task SuspendLeftRunsAsync(ExecutionService execution)
    {
        while (true)
        {
            try
            {
                await execution.SuspendLeftRunsAsync();
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    /// <summary>
    /// Remove the storage of the runs of the application the history doesn't hold anymore, the
    /// supervisor being waited for.
    /// </summary>
    private static async Task CleanStorageAsync(ISupervisor supervisor, string storageFolder)
    {
        while (true)
        {
            try
            {
                IReadOnlyList<Guid> runIds = await supervisor.History.GetRunIdsAsync(Environment.MachineName);
                new ExecutionStorages(storageFolder).RemoveAllBut(runIds.ToHashSet());
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    /// <param name="storageFolder">The storage of the worker of the application, its only worker.</param>
    private LocalSupervisor CreateLocalSupervisor(string storageFolder)
    {
        var supervisor = new LocalSupervisor(Settings.LocalFolderPath, storageFolder);
        // Take the cost of opening the database off the first navigation.
        _ = Task.Run(supervisor.WarmUpDatabase);
        _ = supervisor.RunCleanupAsync(CancellationToken.None);
        // Started on the worker of the application, its only worker.
        _ = supervisor.Scheduler.RunAsync(CancellationToken.None);
        return supervisor;
    }

    /// <param name="storageFolder">
    /// The storage of the worker of the application, out of reach of the supervisor : the folders
    /// of the runs it removed are removed here.
    /// </param>
    private ISupervisor CreateSupervisorClient(string storageFolder)
    {
        var supervisor = new SupervisorClient(Settings.SupervisorAddress);
        // Listens to the changes of the history and of the dispatch as soon as the supervisor is up.
        _ = supervisor.History.StartAsync();
        _ = supervisor.Dispatch.StartAsync();
        _ = CleanStorageAsync(supervisor, storageFolder);
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
