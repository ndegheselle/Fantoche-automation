using Fantoche.Plugins;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Storage;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// Each task runs where its setting says, the nodes of a workflow run here included.
/// </summary>
[TestFixture]
internal sealed class MixedTaskExecutorTests
{
    /// <summary>
    /// The workers as the executor sees them : every task reaching them is completed with
    /// <c>Value = 1000</c>.
    /// </summary>
    private sealed class FakeRemote : ITaskExecutor
    {
        private readonly List<string> _ran = [];

        public IReadOnlyList<string> Ran
        {
            get { lock (_ran) return [.. _ran]; }
        }

        public Task<TaskInstance> ExecuteAsync(BaseAutomationTask task, TaskInstance instance,
            TaskInstancesProgress? progress = null, CancellationToken? cancellation = null)
        {
            lock (_ran)
                _ran.Add(instance.NodeName);
            instance.Output = JToken.FromObject(new TestResult { Value = 1000, Message = "remote" });
            instance.State = EnumTaskState.Completed;
            return Task.FromResult(instance);
        }
    }

    private readonly TestPackageManagement _packages = new();
    private readonly LocalStorage _storage = new(Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N")));

    private static AutomationTask RemoteTest()
    {
        AutomationTask task = PluginTasks.Make("Fantoche.Plugins.TestTask", "RemoteTest",
            JsonSchema.FromType<Plugins.TestParameters>(), JsonSchema.FromType<TestResult>());
        task.Settings.Execution = EnumExecutionMode.Remote;
        return task;
    }

    [Test]
    public async Task Workflow_SendsOnlyItsRemoteNodesToTheRemoteExecutor()
    {
        FakeRemote remote = new();
        using MixedTaskExecutor executor = new(_packages, _storage, remote);
        WorkflowInstance instance = new TestWorkflow("Mixed")
            .Start()
            .Task("Local", PluginTasks.Test, new { Message = "local", Value = "$previous.Value", Add = 1 })
            .Task("Remote", RemoteTest(), new { Message = "remote", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Local", "Remote", "End")
            .Instance(new { Value = 1 });

        await executor.ExecuteAsync(instance.Workflow, instance);

        Assert.That(remote.Ran, Is.EqualTo(new[] { "Remote" }));
    }

    [Test]
    public async Task RemoteNode_HandsItsOutputToTheNextNode()
    {
        using MixedTaskExecutor executor = new(_packages, _storage, new FakeRemote());
        WorkflowInstance instance = new TestWorkflow("Mixed")
            .Start()
            .Task("Remote", RemoteTest(), new { Message = "remote", Value = "$previous.Value", Add = 1 })
            .Task("Local", PluginTasks.Test, new { Message = "local", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Remote", "Local", "End")
            .Instance(new { Value = 1 });

        TaskInstance result = await executor.ExecuteAsync(instance.Workflow, instance);

        Assert.That(result.Output?["Value"]?.Value<int>(), Is.EqualTo(1001));
    }

    [Test]
    public async Task RemoteWorkflow_IsSentWhole()
    {
        FakeRemote remote = new();
        using MixedTaskExecutor executor = new(_packages, _storage, remote);
        TestWorkflow nested = new TestWorkflow("Nested")
            .Start()
            .Task("NestedTask", PluginTasks.Test, new { Message = "nested", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "NestedTask", "End");
        nested.Workflow.Settings.Execution = EnumExecutionMode.Remote;
        WorkflowInstance instance = new TestWorkflow("Outer")
            .Start()
            .Nested("Nested", nested, new { Value = "$previous.Value" })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Nested", "End")
            .Instance(new { Value = 1 });

        await executor.ExecuteAsync(instance.Workflow, instance);

        Assert.That(remote.Ran, Is.EqualTo(new[] { "Nested" }));
    }

    [Test]
    public async Task RemoteTask_InStandaloneMode_Fails()
    {
        using MixedTaskExecutor executor = new(_packages, _storage, remote: null);
        TaskInstance instance = new() { Parameters = JToken.FromObject(new Plugins.TestParameters { Value = 1 }) };

        TaskInstance result = await executor.ExecuteAsync(RemoteTest(), instance);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(result.Output?.Value<string>(), Does.Contain("standalone mode"));
        });
    }
}
