using Fantoche.Plugins;
using Fantoche.Plugins.Shared;
using Fantoche.Plugins.Tables;
using Fantoche.Services.Local;
using Fantoche.Services.Remote;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Supervisor.Api;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// Executions sent to a worker through a supervisor, as an application sees them. Every part is the
/// real one, and every test runs against both supervisors : the one of the process (standalone
/// mode) and a remote one reached over the loopback, which have to behave the same.
/// </summary>
[TestFixture(Mode.Local)]
[TestFixture(Mode.Remote)]
internal sealed class SupervisorExecutionTests
{
    public enum Mode
    {
        Local,
        Remote,
    }

    private readonly Mode _mode;
    private string _folder = null!;
    private WebApplication? _api;
    private ISupervisor _supervisor = null!;
    private Dispatcher _dispatcher = null!;
    /// <summary>The worker the supervisor dispatches to.</summary>
    private LocalWorker _worker = null!;
    /// <summary>The worker of the application, which runs what the application starts.</summary>
    private LocalWorker _application = null!;
    private IAsyncDisposable? _registration;
    private RemoteTaskExecutor _caller = null!;

    public SupervisorExecutionTests(Mode mode)
    {
        _mode = mode;
    }

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        string supervisorFolder = Path.Combine(_folder, "supervisor");

        if (_mode == Mode.Local)
        {
            LocalSupervisor supervisor = new(supervisorFolder);
            _dispatcher = supervisor.Dispatcher;
            _supervisor = supervisor;
        }
        else
        {
            _api = SupervisorApp.Build([
                "--urls", "http://127.0.0.1:0",
                $"--DATA_FOLDER={supervisorFolder}",
                "--Logging:LogLevel:Default=Warning"]);
            await _api.StartAsync();
            _dispatcher = _api.Services.GetRequiredService<Dispatcher>();

            SupervisorClient client = new(new Uri($"{_api.Urls.First()}/"));
            await client.History.StartAsync();
            await client.Dispatch.StartAsync();
            _supervisor = client;
        }

        _worker = new LocalWorker(new TestPackageManagement(), Path.Combine(_folder, "worker"));
        _application = new LocalWorker(new TestPackageManagement(), Path.Combine(_folder, "application"));
        _caller = new RemoteTaskExecutor(_supervisor);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_registration != null)
            await _registration.DisposeAsync();
        if (_supervisor is IAsyncDisposable client)
            await client.DisposeAsync();
        if (_api != null)
            await _api.DisposeAsync();
        _worker.Dispose();
        _application.Dispose();
    }

    private async Task ConnectWorkerAsync(LocalWorker? worker = null)
    {
        _registration = await _supervisor.AddWorkerAsync(worker ?? _worker, "test-worker", capacity: 2);
    }

    /// <summary>
    /// Wait for the worker to have started an execution, so what follows happens while it runs.
    /// </summary>
    private async Task WaitUntilRunningAsync()
    {
        while (_dispatcher.GetStatus().Workers.Sum(x => x.Running.Count) == 0)
            await Task.Delay(20);
    }

    /// <summary>[task] stored by the supervisor, under the id it was given there.</summary>
    private async Task<T> StoreAsync<T>(T task) where T : BaseAutomationTask
    {
        task.ParentId = Scope.Root.Id;
        await _supervisor.Scoped.CreateAsync(task);
        return task;
    }

    private Task<AutomationTask> StoreTestAsync() => StoreAsync(PluginTasks.Make(
        "Fantoche.Plugins.TestTask", "Test", JsonSchema.FromType<Plugins.TestParameters>(), JsonSchema.FromType<TestResult>()));

    private Task<AutomationTask> StoreDelayAsync() => StoreAsync(PluginTasks.Make(
        "Fantoche.Plugins.TestDelay", "Delay", JsonSchema.FromType<TestDelayParameters>(), new JsonSchema(), passThrough: true));

    /// <summary>A stored workflow running [test] once : <c>Start ─> First ─> End</c>.</summary>
    private async Task<AutomationWorkflow> StoreLineAsync(AutomationTask test)
    {
        TestWorkflow line = new TestWorkflow("Line")
            .Start()
            .Task("First", test, new { Message = "first", Value = "$previous.Value", Add = 10 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "First", "End");
        return await StoreAsync(line.Workflow);
    }

    private const string ApplicationId = "test-application";

    /// <summary>The service the application starts and resumes its runs with.</summary>
    private ExecutionService ApplicationExecution() => new(_supervisor.Scoped, _supervisor.History, _application, ApplicationId);

    /// <summary>
    /// A run of the line workflow (see <see cref="StoreLineAsync"/>) as the history holds it once
    /// interrupted right after First completed. First is stored as having produced 42, which it
    /// can't compute from the parameters : a run ending on it carried on from the history.
    /// </summary>
    private async Task<TaskInstance> StoreInterruptedLineAsync(string? applicationId, string? definitionHash = null)
    {
        AutomationWorkflow stored = await StoreLineAsync(await StoreTestAsync());
        AutomationWorkflow workflow = (AutomationWorkflow)await Shared.Services.ScopedServiceExtensions.LoadRunnableAsync(_supervisor.Scoped, stored.Id);
        BaseGraphTask Node(string name) => workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == name);

        TaskInstance run = new()
        {
            TaskId = workflow.Id,
            NodeName = workflow.Metadata.Name,
            Parameters = JToken.FromObject(new { Value = 1 }),
            ApplicationId = applicationId,
            DefinitionHash = definitionHash ?? RunDefinition.HashOf(workflow),
            State = EnumTaskState.Interrupted,
        };
        TaskInstance start = new()
        {
            TaskId = Node("Start").TaskId,
            NodeId = Node("Start").Id,
            NodeName = "Start",
            ParentInstanceId = run.Id,
            Output = JToken.FromObject(new { Value = 1 }),
            State = EnumTaskState.Completed,
        };
        TaskInstance first = new()
        {
            TaskId = Node("First").TaskId,
            NodeId = Node("First").Id,
            NodeName = "First",
            ParentInstanceId = run.Id,
            PreviousIds = [start.Id],
            Output = JToken.FromObject(new { Value = 42, Message = "stored" }),
            State = EnumTaskState.Completed,
        };

        await _supervisor.History.SaveAsync(run);
        await _supervisor.History.SaveAsync(start);
        await _supervisor.History.SaveAsync(first);
        return run;
    }

    /// <summary>[runId] once over, read from the history : nobody waits on a resumed run.</summary>
    private async Task<TaskInstance> WaitForTheEndOfAsync(Guid runId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (true)
        {
            TaskInstance? run = await _supervisor.History.GetAsync(runId);
            if (run != null && run.State != EnumTaskState.Progressing)
                return run;
            await Task.Delay(20, timeout.Token);
        }
    }

    private static TaskInstance InstanceOf(BaseAutomationTask task, object parameters) => new()
    {
        TaskId = task.Id,
        NodeName = task.Metadata.Name,
        Parameters = JToken.FromObject(parameters),
    };

    /// <summary>
    /// A stored workflow importing a table and filtering it, and the parameters pointing at the file
    /// it imports : <c>Start ─> Import ─> Filter ─> Copy ─> End</c>.
    /// </summary>
    private async Task<(AutomationWorkflow Workflow, object Parameters)> StoreTablesWorkflowAsync()
    {
        AutomationTask import = await StoreAsync(PluginTasks.Make("Fantoche.Plugins.Tables.CsvImportTask", "CsvImport",
            JsonSchema.FromType<CsvImportParameters>(), JsonSchema.FromType<TableRef>()));
        AutomationTask filter = await StoreAsync(PluginTasks.Make("Fantoche.Plugins.Tables.FilterTask", "Filter",
            JsonSchema.FromType<FilterParameters>(), JsonSchema.FromType<TableRef>()));
        AutomationTask copy = await StoreAsync(PluginTasks.Make("Fantoche.Plugins.Tables.FilterTask", "Copy",
            JsonSchema.FromType<FilterParameters>(), JsonSchema.FromType<TableRef>()));

        object over10 = new { Left = "$row.columns.amount", Operator = "Greater", Right = 10 };
        TestWorkflow tables = new TestWorkflow("Tables", requireOutput: false)
            .Start()
            .Task("Import", import, new { Path = "$previous.Path" })
            .Task("Filter", filter, new { Source = "$previous", Conditions = new[] { over10 }, MatchAll = true })
            .Task("Copy", copy, new { Source = "$previous", Conditions = Array.Empty<object>(), MatchAll = true })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "Filter", "Copy", "End");
        AutomationWorkflow workflow = await StoreAsync(tables.Workflow);

        string csv = Path.Combine(_folder, "people.csv");
        await File.WriteAllTextAsync(csv, "name,amount\nalice,10\nbob,20\ncarol,30\n");
        return (workflow, new { Path = csv });
    }

    /// <summary>The table the tables workflow ends with, started as the application starts it.</summary>
    private async Task<TableRef> RunTablesInTheApplicationAsync()
    {
        var (workflow, parameters) = await StoreTablesWorkflowAsync();
        ExecutionService service = ApplicationExecution();
        TaskInstance started = await service.StartAsync(workflow.Id, JToken.FromObject(parameters));
        TaskInstance result = await service.WaitAsync(started.Id);
        return result.Output!["Table"]!.ToObject<TableRef>()!;
    }

    /// <summary>
    /// The table the tables workflow ends with, dispatched by the supervisor as a scheduled item is.
    /// </summary>
    private async Task<TableRef> DispatchTablesAsync()
    {
        var (workflow, parameters) = await StoreTablesWorkflowAsync();
        TaskInstance result = await _caller.ExecuteAsync(workflow, InstanceOf(workflow, parameters));
        return result.Output!["Table"]!.ToObject<TableRef>()!;
    }

    /// <summary>The files of [table] in the storage of the worker kept in [workerFolder].</summary>
    private string[] FilesOf(string workerFolder, TableRef table)
    {
        string folder = Path.Combine(_folder, workerFolder);
        if (!Directory.Exists(folder))
            return [];
        return Directory.GetFiles(folder, $"{table.Id}.parquet", SearchOption.AllDirectories);
    }

    /// <summary>The names in [table], read from the storage of the worker kept in [workerFolder].</summary>
    private async Task<List<object?>> NamesInAsync(string workerFolder, TableRef table)
    {
        string file = FilesOf(workerFolder, table).Single();
        LocalStorage storage = new(Path.GetDirectoryName(Path.GetDirectoryName(file))!);

        List<object?> names = [];
        await foreach (var row in storage.ReadRowsAsync(table))
            names.Add(row["name"]);
        return names;
    }

    [Test]
    public async Task Task_CompletesWithWhatTheWorkerProduced()
    {
        await ConnectWorkerAsync();
        AutomationTask test = await StoreTestAsync();

        TaskInstance result = await _caller.ExecuteAsync(test, InstanceOf(test, new { Value = 1, Add = 2 }));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(result.Output?["Value"]?.Value<int>(), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Workflow_CompletesWithTheOutputOfItsEnd()
    {
        await ConnectWorkerAsync();
        AutomationWorkflow workflow = await StoreLineAsync(await StoreTestAsync());

        TaskInstance result = await _caller.ExecuteAsync(workflow, InstanceOf(workflow, new { Value = 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(result.Output?["Value"]?.Value<int>(), Is.EqualTo(11));
        });
    }

    [Test]
    public async Task Workflow_ReadsTheGlobalContextItWasStartedWith()
    {
        await ConnectWorkerAsync();
        TestWorkflow line = new TestWorkflow("Global")
            .Start()
            .Task("First", await StoreTestAsync(), new { Message = "first", Value = "$previous.Value", Add = "$global.Bonus" })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "First", "End");
        AutomationWorkflow workflow = await StoreAsync(line.Workflow);
        WorkflowInstance instance = new(workflow)
        {
            Parameters = JToken.FromObject(new { Value = 1 }),
            GlobalContext = JToken.FromObject(new { Bonus = 100 }),
        };

        TaskInstance result = await _caller.ExecuteAsync(workflow, instance);

        Assert.That(result.Output?["Value"]?.Value<int>(), Is.EqualTo(101));
    }

    [Test]
    public async Task Workflow_NodesAreStoredInTheHistoryOfTheSupervisor()
    {
        await ConnectWorkerAsync();
        AutomationWorkflow workflow = await StoreLineAsync(await StoreTestAsync());
        TaskInstance instance = InstanceOf(workflow, new { Value = 1 });

        await _caller.ExecuteAsync(workflow, instance);
        var nodes = await _supervisor.History.GetChildrenAsync(instance.Id);

        Assert.That(nodes.Single(x => x.NodeName == "First").State, Is.EqualTo(EnumTaskState.Completed));
    }

    [Test]
    public async Task History_TellsTheApplicationsAboutTheChanges()
    {
        await ConnectWorkerAsync();
        AutomationTask test = await StoreTestAsync();
        TaskInstance instance = InstanceOf(test, new { Value = 1, Add = 2 });
        TaskCompletionSource<TaskInstance> completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _supervisor.History.InstanceUpdated += x =>
        {
            if (x.Id == instance.Id && x.State == EnumTaskState.Completed)
                completed.TrySetResult(x);
        };

        await _caller.ExecuteAsync(test, instance);
        TaskInstance told = await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(told.Output?["Value"]?.Value<int>(), Is.EqualTo(3));
    }

    [Test]
    public async Task UnknownTask_Fails()
    {
        await ConnectWorkerAsync();
        AutomationTask unknown = PluginTasks.Make("Fantoche.Plugins.TestTask", "Unknown", new JsonSchema(), new JsonSchema());

        TaskInstance result = await _caller.ExecuteAsync(unknown, InstanceOf(unknown, new { }));

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(result.Output?.Value<string>(), Does.Contain("Unknown element"));
        });
    }

    [Test]
    public async Task Task_WaitsForAWorkerWhenNoneIsConnected()
    {
        AutomationTask test = await StoreTestAsync();

        Task<TaskInstance> execution = _caller.ExecuteAsync(test, InstanceOf(test, new { Value = 1, Add = 2 }));
        await Task.Delay(200);
        Assert.That(execution.IsCompleted, Is.False);

        await ConnectWorkerAsync();
        TaskInstance result = await execution;

        Assert.That(result.State, Is.EqualTo(EnumTaskState.Completed));
    }

    [Test]
    public async Task Cancelling_StopsTheTaskOnTheWorker()
    {
        await ConnectWorkerAsync();
        AutomationTask delay = await StoreDelayAsync();
        using CancellationTokenSource cancellation = new();

        Task<TaskInstance> execution = _caller.ExecuteAsync(delay, InstanceOf(delay, new { DelayMs = 60_000 }), cancellation: cancellation.Token);
        await WaitUntilRunningAsync();
        await cancellation.CancelAsync();
        TaskInstance result = await execution;

        Assert.That(result.State, Is.EqualTo(EnumTaskState.Canceled));
    }

    [Test]
    public async Task Status_IsPushedWithWhatTheWorkersRunAndWhatWaitsForRoom()
    {
        await ConnectWorkerAsync();
        AutomationTask delay = await StoreDelayAsync();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<DispatchStatus> full = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _supervisor.Dispatch.StatusChanged += status =>
        {
            if (status.Pending.Count > 0)
                full.TrySetResult(status);
        };

        // One more than the capacity of the worker.
        List<Task<TaskInstance>> executions = [.. Enumerable.Range(0, 3)
            .Select(_ => _caller.ExecuteAsync(delay, InstanceOf(delay, new { DelayMs = 60_000 }), cancellation: cancellation.Token))];
        DispatchStatus status = await full.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();
        await Task.WhenAll(executions);

        Assert.Multiple(() =>
        {
            Assert.That(status.Workers.Single().Id, Is.EqualTo("test-worker"));
            Assert.That(status.Workers.Single().Running.Select(x => x.TaskId), Is.EqualTo(new[] { delay.Id, delay.Id }));
            Assert.That(status.Pending.Select(x => x.TaskId), Is.EqualTo(new[] { delay.Id }));
        });
    }

    [Test]
    public async Task LosingTheWorker_InterruptsItsRunningTask()
    {
        await ConnectWorkerAsync();
        AutomationTask delay = await StoreDelayAsync();
        TaskInstance instance = InstanceOf(delay, new { DelayMs = 60_000 });

        Task<TaskInstance> execution = _caller.ExecuteAsync(delay, instance);
        await WaitUntilRunningAsync();
        await _registration!.DisposeAsync();
        _registration = null;
        TaskInstance result = await execution;

        Assert.Multiple(async () =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Interrupted));
            Assert.That((await _supervisor.History.GetAsync(instance.Id))?.State, Is.EqualTo(EnumTaskState.Interrupted));
        });
    }

    [Test]
    public async Task DispatchedRun_IsResumedFromItsHistory()
    {
        await ConnectWorkerAsync();
        TaskInstance interrupted = await StoreInterruptedLineAsync(applicationId: null);

        await _supervisor.Dispatch.ResumeAsync(interrupted.Id);
        TaskInstance run = await WaitForTheEndOfAsync(interrupted.Id);
        var nodes = await _supervisor.History.GetChildrenAsync(interrupted.Id);

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Output?["Value"]?.Value<int>(), Is.EqualTo(42));
            Assert.That(nodes.Count(x => x.NodeName == "First"), Is.EqualTo(1), "First doesn't run again");
        });
    }

    [Test]
    public async Task ApplicationRun_IsResumedByTheApplication()
    {
        TaskInstance interrupted = await StoreInterruptedLineAsync(ApplicationId);
        ExecutionService execution = ApplicationExecution();

        await execution.ResumeAsync(interrupted.Id);
        TaskInstance run = await execution.WaitAsync(interrupted.Id);

        Assert.Multiple(() =>
        {
            Assert.That(run.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Output?["Value"]?.Value<int>(), Is.EqualTo(42));
        });
    }

    [Test]
    public async Task ApplicationRun_IsNotResumedByTheSupervisor()
    {
        await ConnectWorkerAsync();
        TaskInstance interrupted = await StoreInterruptedLineAsync(ApplicationId);

        await Assert.ThrowsAsync<Shared.Services.ExecutionException>(() => _supervisor.Dispatch.ResumeAsync(interrupted.Id));
    }

    [Test]
    public async Task RunOfAChangedWorkflow_IsNotResumed()
    {
        await ConnectWorkerAsync();
        TaskInstance interrupted = await StoreInterruptedLineAsync(applicationId: null, definitionHash: "before the change");

        await Assert.ThrowsAsync<Shared.Services.ExecutionException>(() => _supervisor.Dispatch.ResumeAsync(interrupted.Id));
    }

    [Test]
    public async Task RunLeftByTheApplication_IsInterruptedWhenItStartsAgain()
    {
        TaskInstance left = await StoreInterruptedLineAsync(ApplicationId);
        left.State = EnumTaskState.Progressing;
        await _supervisor.History.SaveAsync(left);
        TaskInstance other = await StoreInterruptedLineAsync("other-application");
        other.State = EnumTaskState.Progressing;
        await _supervisor.History.SaveAsync(other);

        await ApplicationExecution().InterruptLeftRunsAsync();

        Assert.Multiple(async () =>
        {
            Assert.That((await _supervisor.History.GetAsync(left.Id))?.State, Is.EqualTo(EnumTaskState.Interrupted));
            Assert.That((await _supervisor.History.GetAsync(other.Id))?.State, Is.EqualTo(EnumTaskState.Progressing), "only its own runs");
        });
    }

    [Test]
    public async Task StateFromAWorkerNotRunningTheRun_IsDropped()
    {
        // A worker that lost its connection reports on a run interrupted meanwhile, maybe resumed
        // elsewhere.
        TaskInstance interrupted = await StoreInterruptedLineAsync(applicationId: null);
        TaskInstance late = FantocheJson.Copy(interrupted);
        late.State = EnumTaskState.Canceled;

        await _dispatcher.ReportStateAsync("lost-connection", interrupted.Id, late);

        Assert.That((await _supervisor.History.GetAsync(interrupted.Id))?.State, Is.EqualTo(EnumTaskState.Interrupted));
    }

    [Test]
    public async Task RemovingATaskUsedByAWorkflow_IsRefusedAsInvalid()
    {
        AutomationTask test = await StoreTestAsync();
        await StoreLineAsync(test);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _supervisor.Scoped.RemoveAsync(test));
    }

    [Test]
    public async Task WorkflowStartedInTheApplication_RunsThereEvenWithAWorkerConnected()
    {
        await ConnectWorkerAsync();

        TableRef table = await RunTablesInTheApplicationAsync();
        List<object?> names = await NamesInAsync("application", table);

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new[] { "bob", "carol" }));
            Assert.That(FilesOf("worker", table), Is.Empty);
        });
    }

    [Test]
    public async Task DispatchedWorkflow_KeepsItsTablesOnTheWorker()
    {
        await ConnectWorkerAsync();

        TableRef table = await DispatchTablesAsync();
        List<object?> names = await NamesInAsync("worker", table);

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new[] { "bob", "carol" }));
            Assert.That(FilesOf("application", table), Is.Empty);
        });
    }

    [Test]
    public async Task ApplicationAsItsOwnWorker_RunsWhatIsDispatched()
    {
        // As in standalone mode : the application is the only worker.
        await ConnectWorkerAsync(_application);

        TableRef table = await DispatchTablesAsync();

        Assert.That(await NamesInAsync("application", table), Is.EqualTo(new[] { "bob", "carol" }));
    }
}
