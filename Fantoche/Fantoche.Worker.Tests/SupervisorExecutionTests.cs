using Fantoche.Plugins;
using Fantoche.Plugins.Shared;
using Fantoche.Plugins.Tables;
using Fantoche.Services.Local;
using Fantoche.Services.Remote;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
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
            _supervisor = client;
        }

        _worker = new LocalWorker(_supervisor, new TestPackageManagement(), Path.Combine(_folder, "worker"));
        _application = new LocalWorker(_supervisor, new TestPackageManagement(), Path.Combine(_folder, "application"));
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
        while (_dispatcher.GetWorkers().Sum(x => x.Running) == 0)
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

    private static TaskInstance InstanceOf(BaseAutomationTask task, object parameters) => new()
    {
        TaskId = task.Id,
        NodeName = task.Metadata.Name,
        Parameters = JToken.FromObject(parameters),
    };

    /// <summary>
    /// Run, as the application starts it, a workflow sending a table it imported to a worker and
    /// reading back what the worker made of it : <c>Start ─> Import ─> Filter (on a worker) ─> Copy ─> End</c>.
    /// </summary>
    private async Task<TaskInstance> RunTablesWorkflowAsync()
    {
        AutomationTask import = await StoreAsync(PluginTasks.Make("Fantoche.Plugins.Tables.CsvImportTask", "CsvImport",
            JsonSchema.FromType<CsvImportParameters>(), JsonSchema.FromType<TableRef>()));
        AutomationTask filter = PluginTasks.Make("Fantoche.Plugins.Tables.FilterTask", "Filter",
            JsonSchema.FromType<FilterParameters>(), JsonSchema.FromType<TableRef>());
        filter.Settings.Execution = EnumExecutionMode.Remote;
        await StoreAsync(filter);
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

        ExecutionService execution = new(_supervisor.Scoped, _supervisor.History, _application);
        TaskInstance started = await execution.StartAsync(workflow.Id, JToken.FromObject(new { Path = csv }));
        return await execution.WaitAsync(started.Id);
    }

    /// <summary>The names in [table], read from the storage the application left it in.</summary>
    private async Task<List<object?>> NamesInTheApplicationAsync(TableRef table)
    {
        string file = Directory.GetFiles(Path.Combine(_folder, "application"), $"{table.Id}.parquet", SearchOption.AllDirectories).Single();
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
    public async Task LosingTheWorker_FailsItsRunningTask()
    {
        await ConnectWorkerAsync();
        AutomationTask delay = await StoreDelayAsync();

        Task<TaskInstance> execution = _caller.ExecuteAsync(delay, InstanceOf(delay, new { DelayMs = 60_000 }));
        await WaitUntilRunningAsync();
        await _registration!.DisposeAsync();
        _registration = null;
        TaskInstance result = await execution;

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(result.Output?.Value<string>(), Does.Contain("was lost"));
        });
    }

    [Test]
    public async Task RemovingATaskUsedByAWorkflow_IsRefusedAsInvalid()
    {
        AutomationTask test = await StoreTestAsync();
        await StoreLineAsync(test);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _supervisor.Scoped.RemoveAsync(test));
    }

    [Test]
    public async Task NodeOnAWorker_ReadsAndHandsBackTheStorageOfTheApplication()
    {
        await ConnectWorkerAsync();

        TaskInstance result = await RunTablesWorkflowAsync();
        TableRef table = result.Output!["Table"]!.ToObject<TableRef>()!;

        Assert.That(await NamesInTheApplicationAsync(table), Is.EqualTo(new[] { "bob", "carol" }));
    }

    [Test]
    public async Task ApplicationAsItsOwnWorker_RunsTheNodesSetToRunOnAWorker()
    {
        // As in standalone mode : the application is the only worker.
        await ConnectWorkerAsync(_application);

        TaskInstance result = await RunTablesWorkflowAsync();
        TableRef table = result.Output!["Table"]!.ToObject<TableRef>()!;

        Assert.That(await NamesInTheApplicationAsync(table), Is.EqualTo(new[] { "bob", "carol" }));
    }
}
