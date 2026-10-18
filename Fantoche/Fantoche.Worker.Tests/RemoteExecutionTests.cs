using Fantoche.Plugins;
using Fantoche.Shared.Data.Execution;
using Fantoche.Supervisor.Api;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Executor.Remote;
using Fantoche.Worker.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A task run through a supervisor, on a worker, as the caller sees it. Every part is the real one,
/// talking over the loopback.
/// </summary>
[TestFixture]
internal sealed class RemoteExecutionTests
{
    private WebApplication _supervisor = null!;
    private Uri _supervisorUrl = null!;
    private LocalStorage _storage = null!;
    private LocalTaskExecutor _workerExecutor = null!;
    private WorkerConnection? _worker;
    private RemoteTaskExecutor _caller = null!;

    [SetUp]
    public async Task SetUp()
    {
        _supervisor = SupervisorApp.Build(["--urls", "http://127.0.0.1:0", "--Logging:LogLevel:Default=Warning"]);
        await _supervisor.StartAsync();
        _supervisorUrl = new Uri($"{_supervisor.Urls.First()}/");

        _storage = new(Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N")));
        _workerExecutor = new LocalTaskExecutor(new TestPackageManagement(), _storage);
        _caller = new RemoteTaskExecutor(_supervisorUrl);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _caller.DisposeAsync();
        if (_worker != null)
            await _worker.DisposeAsync();
        _workerExecutor.Dispose();
        _storage.Dispose();
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

    private static TaskInstance TestInstance(int value, int add) => new()
    {
        TaskId = PluginTasks.Test.Id,
        Parameters = JToken.FromObject(new Plugins.TestParameters { Message = "remote", Value = value, Add = add }),
    };

    private static TaskInstance DelayInstance() => new()
    {
        TaskId = PluginTasks.Delay.Id,
        Parameters = JToken.FromObject(new TestDelayParameters { DelayMs = 60_000 }),
    };

    private static TestWorkflow Line() => new TestWorkflow("Line")
        .Start()
        .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = 10 })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "First", "End");

    [Test]
    public async Task Task_CompletesWithWhatTheWorkerProduced()
    {
        await ConnectWorkerAsync();

        TaskInstance result = await _caller.ExecuteAsync(PluginTasks.Test, TestInstance(1, 2));

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
        WorkflowInstance instance = Line().Instance(new { Value = 1 });

        TaskInstance result = await _caller.ExecuteAsync(instance.Workflow, instance);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(result.Output?["Value"]?.Value<int>(), Is.EqualTo(11));
        });
    }

    [Test]
    public async Task Workflow_RelaysTheStatesOfItsNodesToTheCaller()
    {
        await ConnectWorkerAsync();
        WorkflowInstance instance = Line().Instance(new { Value = 1 });
        List<NodeReport> reports = [];
        TaskInstancesProgress progress = new() { StateChanges = new Sink<TaskInstance>(x => reports.Add(NodeReport.Of(x))) };

        await _caller.ExecuteAsync(instance.Workflow, instance, progress);

        var first = reports.Where(x => x.NodeName == "First").Select(x => x.State);
        Assert.That(first, Is.EqualTo(new[] { EnumTaskState.Progressing, EnumTaskState.Completed }));
    }

    [Test]
    public async Task Task_WaitsForAWorkerWhenNoneIsConnected()
    {
        Task<TaskInstance> execution = _caller.ExecuteAsync(PluginTasks.Test, TestInstance(1, 2));
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
        using CancellationTokenSource cancellation = new();

        Task<TaskInstance> execution = _caller.ExecuteAsync(PluginTasks.Delay, DelayInstance(), cancellation: cancellation.Token);
        await WaitUntilRunningAsync();
        await cancellation.CancelAsync();
        TaskInstance result = await execution;

        Assert.That(result.State, Is.EqualTo(EnumTaskState.Canceled));
    }

    [Test]
    public async Task LosingTheWorker_FailsItsRunningTask()
    {
        await ConnectWorkerAsync();

        Task<TaskInstance> execution = _caller.ExecuteAsync(PluginTasks.Delay, DelayInstance());
        await WaitUntilRunningAsync();
        await _worker!.StopAsync();
        TaskInstance result = await execution;

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(result.Output?.Value<string>(), Does.Contain("was lost"));
        });
    }
}
