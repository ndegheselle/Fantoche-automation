using Fantoche.Plugins;
using Fantoche.Services.Remote;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Supervisor.Api;
using Fantoche.Supervisor.Api.Execution;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Executor.Remote;
using Fantoche.Worker.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A task run through a supervisor, on a worker, as an application sees it. Every part is the real
/// one, talking over the loopback : the supervisor with its database, the worker and the caller.
/// </summary>
[TestFixture]
internal sealed class RemoteExecutionTests
{
    private string _folder = null!;
    private WebApplication _supervisor = null!;
    private Uri _supervisorUrl = null!;
    private HttpClient _http = null!;
    private RemoteScopedService _scoped = null!;
    private RemoteHistoryService _history = null!;
    private LocalTaskExecutor _workerExecutor = null!;
    private WorkerConnection? _worker;
    private RemoteTaskExecutor _caller = null!;

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        string sharedFolder = Path.Combine(_folder, "shared");
        _supervisor = SupervisorApp.Build([
            "--urls", "http://127.0.0.1:0",
            $"--DATA_FOLDER={Path.Combine(_folder, "supervisor")}",
            "--Logging:LogLevel:Default=Warning"]);
        await _supervisor.StartAsync();
        _supervisorUrl = new Uri($"{_supervisor.Urls.First()}/");

        _http = new HttpClient { BaseAddress = _supervisorUrl };
        _scoped = new RemoteScopedService(_http);
        _history = new RemoteHistoryService(_http);
        _workerExecutor = new LocalTaskExecutor(new TestPackageManagement(), new LocalStorage(Path.Combine(sharedFolder, "storage")));
        _caller = new RemoteTaskExecutor(_supervisorUrl);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _caller.DisposeAsync();
        if (_worker != null)
            await _worker.DisposeAsync();
        await _history.DisposeAsync();
        _workerExecutor.Dispose();
        _http.Dispose();
        await _supervisor.DisposeAsync();
    }

    private async Task ConnectWorkerAsync()
    {
        _worker = new WorkerConnection(_supervisorUrl, "test-worker", capacity: 2, _workerExecutor);
        await _worker.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// Wait for the worker to have started an execution, so what follows happens while it runs.
    /// </summary>
    private async Task WaitUntilRunningAsync()
    {
        Dispatcher dispatcher = _supervisor.Services.GetRequiredService<Dispatcher>();
        while (dispatcher.GetWorkers().Sum(x => x.Running) == 0)
            await Task.Delay(20);
    }

    /// <summary>[task] stored by the supervisor, under the id it was given there.</summary>
    private async Task<T> StoreAsync<T>(T task) where T : BaseAutomationTask
    {
        task.ParentId = Scope.Root.Id;
        await _scoped.CreateAsync(task);
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
        var nodes = await _history.GetChildrenAsync(instance.Id);

        Assert.That(nodes.Single(x => x.NodeName == "First").State, Is.EqualTo(EnumTaskState.Completed));
    }

    [Test]
    public async Task History_PushesTheChangesToTheApplications()
    {
        await ConnectWorkerAsync();
        await _history.StartAsync();
        AutomationTask test = await StoreTestAsync();
        TaskInstance instance = InstanceOf(test, new { Value = 1, Add = 2 });
        TaskCompletionSource<TaskInstance> completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _history.InstanceUpdated += x =>
        {
            if (x.Id == instance.Id && x.State == EnumTaskState.Completed)
                completed.TrySetResult(x);
        };

        await _caller.ExecuteAsync(test, instance);
        TaskInstance pushed = await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(pushed.Output?["Value"]?.Value<int>(), Is.EqualTo(3));
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
        await _worker!.StopAsync();
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

        await Assert.ThrowsAsync<InvalidOperationException>(() => _scoped.RemoveAsync(test));
    }
}
