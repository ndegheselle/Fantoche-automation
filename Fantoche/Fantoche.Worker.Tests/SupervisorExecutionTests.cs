using Fantoche.Plugins;
using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using Fantoche.Services.Local;
using Fantoche.Services.Remote;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Supervisor.Api;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Storage;
using Fantoche.Worker.UserInterfaces;
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
    /// <summary>
    /// Where the copies of the application on the machine of the test keep their lock file : the
    /// folder of the supervisor in standalone mode, whose session the application shares.
    /// </summary>
    private string _sessionsFolder = null!;
    /// <summary>The session of the application, see <see cref="ApplicationExecution"/>.</summary>
    private RunnerSessions _applicationSessions = null!;
    private IAsyncDisposable? _registration;
    /// <summary>The application added to the workers of the supervisor, for the tasks asking the user.</summary>
    private IWorkerRegistration? _applicationRegistration;
    private RemoteTaskExecutor _caller = null!;

    /// <summary>The user of the application, answering the pages as <see cref="_answer"/> says.</summary>
    private ScriptedUser _user = null!;
    private Func<PageRequest, CancellationToken, Task<JToken?>> _answer = null!;
    private static readonly JToken Reordered = JToken.FromObject(new { Items = new[] { "b", "a" } });

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
            LocalSupervisor supervisor = new(supervisorFolder, Path.Combine(_folder, "worker"));
            _dispatcher = supervisor.Dispatcher;
            _supervisor = supervisor;
            _sessionsFolder = Path.Combine(supervisorFolder, "sessions");
            _applicationSessions = supervisor.Sessions;
        }
        else
        {
            _api = SupervisorApp.Build([
                "--urls", "http://127.0.0.1:0",
                $"--DATA_FOLDER={supervisorFolder}",
                $"--STORAGE_FOLDER={Path.Combine(_folder, "worker")}",
                "--Logging:LogLevel:Default=Warning"]);
            await _api.StartAsync();
            _dispatcher = _api.Services.GetRequiredService<Dispatcher>();

            SupervisorClient client = new(new Uri($"{_api.Urls.First()}/"));
            await client.History.StartAsync();
            await client.Dispatch.StartAsync();
            _supervisor = client;
            _sessionsFolder = Path.Combine(_folder, "sessions");
            _applicationSessions = new RunnerSessions(_sessionsFolder);
        }

        _answer = (_, _) => Task.FromResult<JToken?>(Reordered);
        _user = new ScriptedUser((request, cancellation) => _answer(request, cancellation));
        _worker = new LocalWorker(new TestPackageManagement(), Path.Combine(_folder, "worker"), UserTasks.HandedTo(new RemoteTaskExecutor(_supervisor)));
        _application = new LocalWorker(new TestPackageManagement(), Path.Combine(_folder, "application"), UserTasks.ShownBy(_user), _supervisor.OpenStorageAsync);
        _caller = new RemoteTaskExecutor(_supervisor);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_registration != null)
            await _registration.DisposeAsync();
        if (_applicationRegistration != null)
            await _applicationRegistration.DisposeAsync();
        if (_supervisor is IAsyncDisposable client)
            await client.DisposeAsync();
        if (_api != null)
            await _api.DisposeAsync();
        _worker.Dispose();
        _application.Dispose();
        _applicationSessions.Dispose();
    }

    private async Task ConnectWorkerAsync(LocalWorker? worker = null)
    {
        _registration = await _supervisor.AddWorkerAsync(worker ?? _worker, "test-worker", capacity: 2, EnumWorkerRoles.Runs);
    }

    /// <summary>Add the application to the workers, as it adds itself to run the tasks asking the user.</summary>
    private async Task ConnectApplicationAsync()
    {
        _applicationRegistration = await _supervisor.AddWorkerAsync(_application, ApplicationId, capacity: 0, EnumWorkerRoles.UserTasks);
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
    private ExecutionService ApplicationExecution() => new(_supervisor.Scoped, _supervisor.History, _application, ApplicationId, _applicationSessions);

    /// <summary>
    /// A run of the line workflow (see <see cref="StoreLineAsync"/>) as the history holds it once
    /// suspended right after First completed. First is stored as having produced 42, which it
    /// can't compute from the parameters : a run ending on it carried on from the history.
    /// </summary>
    private async Task<TaskInstance> StoreSuspendedLineAsync(string? applicationId)
    {
        AutomationWorkflow stored = await StoreLineAsync(await StoreTestAsync());
        AutomationWorkflow workflow = (AutomationWorkflow)await Shared.Services.ScopedServiceExtensions.LoadRunnableAsync(_supervisor.Scoped, stored.Id);
        BaseGraphTask Node(string name) => workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == name);

        TaskInstance run = new()
        {
            TaskId = workflow.Id,
            WorkflowVersion = workflow.Version,
            NodeName = workflow.Metadata.Name,
            Parameters = JToken.FromObject(new { Value = 1 }),
            ApplicationId = applicationId,
            NestedVersions = [],
            State = EnumTaskState.Suspended,
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

    /// <summary>
    /// A stored workflow waiting before it runs [test] : <c>Start ─> Wait ─> First(+10) ─> End</c>.
    /// Paused while it waits, it is resumed with First.
    /// </summary>
    private async Task<AutomationWorkflow> StoreWaitingLineAsync()
    {
        AutomationTask test = await StoreTestAsync();
        AutomationTask delay = await StoreDelayAsync();
        TestWorkflow line = new TestWorkflow("Waiting line")
            .Start()
            .Task("Wait", delay, new { DelayMs = 300 })
            .Task("First", test, new { Message = "first", Value = "$previous.Value", Add = 10 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Wait", "First", "End");
        return await StoreAsync(line.Workflow);
    }

    /// <summary>
    /// A stored workflow asking the user to reorder its items : <c>Start ─> Ask ─> End</c>, and
    /// the stored task it asks with.
    /// </summary>
    private async Task<(AutomationWorkflow Workflow, AutomationTask Reorder)> StoreAskAsync()
    {
        AutomationTask reorder = await StoreAsync(PluginTasks.Make(
            "Fantoche.Plugins.UserInterfaces.ReorderTask", "Reorder",
            JsonSchema.FromType<Plugins.UserInterfaces.ReorderParameters>(), JsonSchema.FromType<Plugins.UserInterfaces.ReorderResult>()));
        TestWorkflow ask = new TestWorkflow("Ask", requireOutput: false)
            .Start()
            .Task("Ask", reorder, new { Title = "Sort them", Items = "$previous.Items", AskUser = true })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Ask", "End");
        return (await StoreAsync(ask.Workflow), reorder);
    }

    /// <summary>The ask workflow dispatched to the workers, as a schedule starts it : its outcome.</summary>
    private async Task<TaskInstance> DispatchAskAsync()
    {
        var (workflow, _) = await StoreAskAsync();
        return await _caller.ExecuteAsync(workflow, InstanceOf(workflow, new { Items = new[] { "a", "b" } }));
    }

    /// <summary>The task asking the user the supervisor holds, once it waits for a user to take it.</summary>
    private async Task<Guid> WaitForTaskToTakeAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (true)
        {
            ExecutionStatus? waiting = _dispatcher.GetStatus().Pending.FirstOrDefault();
            if (waiting != null)
                return waiting.ExecutionId;
            await Task.Delay(20, timeout.Token);
        }
    }

    /// <summary>The user of the application takes the task asking the user, as answering it from its run does.</summary>
    private async Task TakeAsync()
    {
        Guid executionId = await WaitForTaskToTakeAsync();
        Assert.That(await _applicationRegistration!.TakeAsync(executionId), Is.True);
    }

    /// <summary>[node] of [runId] reached [state] in the history.</summary>
    private async Task WaitForNodeAsync(Guid runId, string node, EnumTaskState state)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (!(await _supervisor.History.GetChildrenAsync(runId)).Any(x => x.NodeName == node && x.State == state))
            await Task.Delay(10, timeout.Token);
    }

    /// <summary>A new version of the line workflow [workflowId], whose end hands the message over as its value.</summary>
    private async Task EditEndAsync(Guid workflowId)
    {
        AutomationWorkflow workflow = (AutomationWorkflow)(await _supervisor.Scoped.GetAsync(workflowId))!;
        BaseGraphTask end = workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == "End");
        end.InputTemplateJson = """{ "Value": "$previous.Message", "Message": "$previous.Message" }""";
        await _supervisor.Scoped.EditAsync(workflow);
    }

    /// <summary>The line workflow as the history holds it while [sessionId] runs it.</summary>
    private async Task<TaskInstance> StoreRunningLineAsync(string applicationId, string sessionId)
    {
        TaskInstance run = await StoreSuspendedLineAsync(applicationId);
        run.State = EnumTaskState.Progressing;
        run.SessionId = sessionId;
        await _supervisor.History.SaveAsync(run);
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
    /// A stored workflow importing a table and updating it, and the parameters pointing at the file
    /// it imports : <c>Start ─> Import ─> Update ─> End</c>.
    /// </summary>
    private async Task<(AutomationWorkflow Workflow, object Parameters)> StoreTablesWorkflowAsync()
    {
        AutomationTask import = await StoreAsync(PluginTasks.Make(typeof(ImportCsvTask).FullName!, "ImportCsv",
            JsonSchema.FromType<ImportCsvParameters>(), JsonSchema.FromType<TableRef>(), package: PluginTasks.TablesPackage));
        AutomationTask update = await StoreAsync(PluginTasks.Make(typeof(UpdateTask).FullName!, "Update",
            JsonSchema.FromType<UpdateParameters>(), JsonSchema.FromType<TableRef>(), package: PluginTasks.TablesPackage));

        object over10 = new { Left = "$row.columns.amount", Operator = "Greater", Right = 10 };
        TestWorkflow tables = new TestWorkflow("Tables", requireOutput: false)
            .Start()
            .Task("Import", import, new { Path = "$previous.Path" })
            .Task("Update", update, new { Source = "$previous", Conditions = new[] { over10 }, Set = new[] { new { Column = "large", Value = true } } })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "Update", "End");
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

    /// <summary>The names of the large amounts in [table], read from the storage of the worker kept in [workerFolder].</summary>
    private async Task<List<object?>> NamesInAsync(string workerFolder, TableRef table)
    {
        string file = FilesOf(workerFolder, table).Single();
        LocalStorage storage = new(Path.GetDirectoryName(Path.GetDirectoryName(file))!);

        List<object?> names = [];
        await foreach (var row in storage.ReadRowsAsync(table))
        {
            if (row["large"] is true)
                names.Add(row["name"]);
        }
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
    public async Task LosingTheWorker_SuspendsItsRunningTask()
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
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That(result.Output?.Value<string>(), Does.Contain("was lost"), "it tells why");
            Assert.That((await _supervisor.History.GetAsync(instance.Id))?.State, Is.EqualTo(EnumTaskState.Suspended));
        });
    }

    [Test]
    public async Task DispatchedRun_IsResumedFromItsHistory()
    {
        await ConnectWorkerAsync();
        TaskInstance suspended = await StoreSuspendedLineAsync(applicationId: null);

        await _supervisor.Dispatch.ResumeAsync(suspended.Id);
        TaskInstance run = await WaitForTheEndOfAsync(suspended.Id);
        var nodes = await _supervisor.History.GetChildrenAsync(suspended.Id);

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
        TaskInstance suspended = await StoreSuspendedLineAsync(ApplicationId);
        ExecutionService execution = ApplicationExecution();

        await execution.ResumeAsync(suspended.Id);
        TaskInstance run = await execution.WaitAsync(suspended.Id);

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
        TaskInstance suspended = await StoreSuspendedLineAsync(ApplicationId);

        await Assert.ThrowsAsync<Shared.Services.ExecutionException>(() => _supervisor.Dispatch.ResumeAsync(suspended.Id));
    }

    [Test]
    public async Task Run_PointsAtTheVersionItRan()
    {
        AutomationWorkflow workflow = await StoreLineAsync(await StoreTestAsync());
        await EditEndAsync(workflow.Id);
        ExecutionService execution = ApplicationExecution();

        TaskInstance started = await execution.StartAsync(workflow.Id, JToken.FromObject(new { Value = 1 }));

        Assert.That((await _supervisor.History.GetAsync(started.Id))?.WorkflowVersion, Is.EqualTo(2));
    }

    [Test]
    public async Task RunOfAnEditedWorkflow_ResumesOnTheVersionItRan()
    {
        TaskInstance suspended = await StoreSuspendedLineAsync(ApplicationId);
        // Version 2 ends on the message : a run carrying on with it would end on "stored".
        await EditEndAsync(suspended.TaskId);
        ExecutionService execution = ApplicationExecution();

        await execution.ResumeAsync(suspended.Id);
        TaskInstance run = await execution.WaitAsync(suspended.Id);

        Assert.That(run.Output?["Value"]?.Value<int>(), Is.EqualTo(42));
    }

    [Test]
    public async Task RunLeftByTheApplication_IsSuspendedWhenItStartsAgain()
    {
        TaskInstance left = await StoreRunningLineAsync(ApplicationId, sessionId: "gone");
        TaskInstance other = await StoreRunningLineAsync("other-application", sessionId: "gone");

        await ApplicationExecution().SuspendLeftRunsAsync();

        Assert.Multiple(async () =>
        {
            Assert.That((await _supervisor.History.GetAsync(left.Id))?.State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That((await _supervisor.History.GetAsync(other.Id))?.State, Is.EqualTo(EnumTaskState.Progressing), "only its own runs");
        });
    }

    [Test]
    public async Task RunOfAnotherRunningCopy_IsLeftAloneWhenTheApplicationStarts()
    {
        // Another copy of the application on the same machine, still running.
        using RunnerSessions otherCopy = new(_sessionsFolder);
        TaskInstance running = await StoreRunningLineAsync(ApplicationId, otherCopy.CurrentId);

        await ApplicationExecution().SuspendLeftRunsAsync();

        Assert.That((await _supervisor.History.GetAsync(running.Id))?.State, Is.EqualTo(EnumTaskState.Progressing));
    }

    [Test]
    public async Task PausedRun_IsResumedByTheApplication()
    {
        AutomationWorkflow workflow = await StoreWaitingLineAsync();
        ExecutionService execution = ApplicationExecution();

        TaskInstance started = await execution.StartAsync(workflow.Id, JToken.FromObject(new { Value = 1 }));
        await WaitForNodeAsync(started.Id, "Wait", EnumTaskState.Progressing);
        await execution.PauseAsync(started.Id);
        TaskInstance paused = await execution.WaitAsync(started.Id);
        await execution.ResumeAsync(started.Id);
        TaskInstance resumed = await execution.WaitAsync(started.Id);

        Assert.Multiple(() =>
        {
            Assert.That(paused.State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That(resumed.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(resumed.Output?["Value"]?.Value<int>(), Is.EqualTo(11));
        });
    }

    [Test]
    public async Task StoppingAPausedRun_CancelsIt()
    {
        AutomationWorkflow workflow = await StoreWaitingLineAsync();
        ExecutionService execution = ApplicationExecution();
        TaskInstance started = await execution.StartAsync(workflow.Id, JToken.FromObject(new { Value = 1 }));
        await WaitForNodeAsync(started.Id, "Wait", EnumTaskState.Progressing);
        await execution.PauseAsync(started.Id);
        await execution.WaitAsync(started.Id);

        await execution.CancelAsync(started.Id);

        Assert.That((await _supervisor.History.GetAsync(started.Id))?.State, Is.EqualTo(EnumTaskState.Canceled));
        await Assert.ThrowsAsync<Shared.Services.ExecutionException>(() => execution.ResumeAsync(started.Id), "a canceled run isn't resumed");
    }

    [Test]
    public async Task RunOfTheApplication_AsksItsOwnUser()
    {
        ExecutionService execution = ApplicationExecution();
        var (workflow, _) = await StoreAskAsync();

        TaskInstance started = await execution.StartAsync(workflow.Id, JToken.FromObject(new { Items = new[] { "a", "b" } }));
        TaskInstance ended = await execution.WaitAsync(started.Id);

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(ended.Output?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(_user.Asked.Single().RunId, Is.EqualTo(started.Id));
        });
    }

    [Test]
    public async Task TaskAskingTheUser_WaitsForAUserToTakeIt()
    {
        await ConnectWorkerAsync();
        await ConnectApplicationAsync();
        Task<TaskInstance> dispatched = DispatchAskAsync();

        Guid waiting = await WaitForTaskToTakeAsync();
        // Long enough for a page to be handed out, were it handed unasked.
        await Task.Delay(200);
        TaskInstance? stored = await _supervisor.History.GetAsync(waiting);

        Assert.Multiple(() =>
        {
            Assert.That(_user.Asked, Is.Empty, "no page shows up unasked");
            Assert.That(stored?.State, Is.EqualTo(EnumTaskState.AwaitingUser));
            Assert.That(dispatched.IsCompleted, Is.False);
        });
    }

    [Test]
    public async Task TaskTaken_RunsInTheApplicationOfItsUser()
    {
        await ConnectWorkerAsync();
        await ConnectApplicationAsync();
        Task<TaskInstance> dispatched = DispatchAskAsync();

        await TakeAsync();
        TaskInstance ended = await dispatched;

        Assert.Multiple(() =>
        {
            Assert.That(ended.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(ended.Output?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(_user.Asked.Single().RunId, Is.EqualTo(ended.Id), "asked with the run of the worker");
        });
    }

    [Test]
    public async Task TaskTakenAlready_IsRefusedTheSecondTime()
    {
        _answer = (_, _) => Task.FromResult<JToken?>(null);
        await ConnectWorkerAsync();
        await ConnectApplicationAsync();
        _ = DispatchAskAsync();
        Guid executionId = await WaitForTaskToTakeAsync();

        bool first = await _applicationRegistration!.TakeAsync(executionId);
        bool second = await _applicationRegistration.TakeAsync(executionId);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        });
    }

    [Test]
    public async Task ApplicationLostWhileAsking_SuspendsTheRunUntilItIsResumed()
    {
        _answer = (_, _) => Task.FromResult<JToken?>(null);
        await ConnectWorkerAsync();
        await ConnectApplicationAsync();
        Task<TaskInstance> dispatched = DispatchAskAsync();
        await TakeAsync();
        await _user.FirstAskedAsync();

        await _applicationRegistration!.DisposeAsync();
        TaskInstance suspended = await dispatched;
        _answer = (_, _) => Task.FromResult<JToken?>(Reordered);
        await ConnectApplicationAsync();
        await _supervisor.Dispatch.ResumeAsync(suspended.Id);
        await TakeAsync();
        TaskInstance resumed = await WaitForTheEndOfAsync(suspended.Id);

        Assert.Multiple(() =>
        {
            Assert.That(suspended.State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That(resumed.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(resumed.Output?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(_user.Asked, Has.Count.EqualTo(2), "asked again once resumed");
        });
    }

    [Test]
    public async Task TaskAskingTheUser_ReachesTheStorageOfItsRunFromTheApplication()
    {
        AutomationTask import = await StoreAsync(PluginTasks.Make(typeof(ImportCsvTask).FullName!, "ImportCsv",
            JsonSchema.FromType<ImportCsvParameters>(), JsonSchema.FromType<TableRef>(), package: PluginTasks.TablesPackage));
        AutomationTask map = await StoreAsync(PluginTasks.Make(typeof(MapColumnsTask).FullName!, "MapColumns",
            JsonSchema.FromType<MapColumnsParameters>(), JsonSchema.FromType<TableRef>(), package: PluginTasks.TablesPackage));
        TestWorkflow tables = new TestWorkflow("Pick", requireOutput: false)
            .Start()
            .Task("Import", import, new { Path = "$previous.Path" })
            .Task("Map", map, new { Source = "$previous", Columns = new[] { new { Name = "When", Column = "" } }, AskUser = true })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "Map", "End");
        AutomationWorkflow workflow = await StoreAsync(tables.Workflow);
        string csv = Path.Combine(_folder, "orders.csv");
        await File.WriteAllTextAsync(csv, "order,ordered on\nA,2026-03-01\nB,2026-03-02\n");

        // The page reads the table and writes a file, the way the pages of the tasks do.
        TablePage? read = null;
        FileRef? written = null;
        _answer = async (request, cancellation) =>
        {
            TableRef source = request.Input!["Source"]!.ToObject<TableRef>()!;
            read = await request.Storage.ReadPageAsync(source, 0, 10, cancellation);
            written = await request.Storage.SaveFileAsync("notes.txt", new MemoryStream([1, 2, 3]), cancellation);
            return JToken.FromObject(new { Source = source, Columns = new[] { new { Name = "When", Column = "ordered on" } } });
        };
        await ConnectWorkerAsync();
        await ConnectApplicationAsync();

        Task<TaskInstance> dispatched = _caller.ExecuteAsync(workflow, InstanceOf(workflow, new { Path = csv }));
        await TakeAsync();
        TaskInstance ended = await dispatched;

        Assert.Multiple(() =>
        {
            Assert.That(ended.Output?["Table"]?["Mapping"]?["When"]?.Value<string>(), Is.EqualTo("ordered on"));
            Assert.That(read?.Total, Is.EqualTo(2));
            Assert.That(read?.Rows[0]["ordered on"], Is.EqualTo(new DateOnly(2026, 3, 1)), "read as a date, as the storage itself reads it");
            Assert.That(Directory.GetFiles(Path.Combine(_folder, "worker"), "notes.txt", SearchOption.AllDirectories), Has.Length.EqualTo(1), $"written in the storage of the run ({written?.Id})");
        });
    }

    [Test]
    public async Task RunResumedByTwoCopiesAtOnce_IsResumedOnce()
    {
        TaskInstance suspended = await StoreSuspendedLineAsync(ApplicationId);
        using RunnerSessions otherCopy = new(_sessionsFolder);
        ExecutionService first = ApplicationExecution();
        ExecutionService second = new(_supervisor.Scoped, _supervisor.History, _application, ApplicationId, otherCopy);

        await first.ResumeAsync(suspended.Id);

        await Assert.ThrowsAsync<Shared.Services.ExecutionException>(() => second.ResumeAsync(suspended.Id));
        await first.WaitAsync(suspended.Id);
    }

    [Test]
    public async Task StateFromAWorkerNotRunningTheRun_IsDropped()
    {
        // A worker that lost its connection reports on a run suspended meanwhile, maybe resumed
        // elsewhere.
        TaskInstance suspended = await StoreSuspendedLineAsync(applicationId: null);
        TaskInstance late = FantocheJson.Copy(suspended);
        late.State = EnumTaskState.Canceled;

        await _dispatcher.ReportStateAsync("lost-connection", suspended.Id, late);

        Assert.That((await _supervisor.History.GetAsync(suspended.Id))?.State, Is.EqualTo(EnumTaskState.Suspended));
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
