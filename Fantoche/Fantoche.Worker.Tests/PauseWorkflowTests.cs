using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A run paused by the user : the nodes it runs finish, no other task starts, and a resume carries
/// on with what it didn't start.
/// </summary>
[TestFixture]
internal sealed class PauseWorkflowTests
{
    /// <summary>A run asked to pause : how it ended, and the run that resumes it from its history.</summary>
    private sealed record PausedRun(WorkflowInstance Instance, IReadOnlyList<TaskInstance> History, WorkflowInstance Resumed);

    /// <summary>
    /// Run [workflow] with [parameters], asking it to pause once [pauses] matches a state stored,
    /// and keep its history as a resume reads it.
    /// </summary>
    private static async Task<PausedRun> PauseAsync(TestWorkflow workflow, object parameters, Func<TaskInstance, bool> pauses)
    {
        using CancellationTokenSource pause = new();
        WorkflowInstance instance = workflow.Instance(parameters);
        instance.PauseRequested = pause.Token;
        List<TaskInstance> history = [];
        object gate = new();

        TaskInstancesProgress progress = new()
        {
            StateChanges = new Sink<TaskInstance>(x =>
            {
                lock (gate)
                {
                    if (x.Id != instance.Id)
                    {
                        history.RemoveAll(stored => stored.Id == x.Id);
                        history.Add(FantocheJson.Copy<TaskInstance>(x));
                    }
                    if (pauses(x))
                        pause.Cancel();
                }
            }),
        };
        LocalStorage storage = new(Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N")));
        WorkflowInstance paused = await new WorkflowExecutor(new TaskLoaders(new TestPackageManagement()), storage).ExecuteAsync(instance, progress);

        WorkflowInstance resumed = WorkflowInstance.From(workflow.Refreshed(), FantocheJson.Copy<TaskInstance>(paused));
        resumed.StoredInstances = history;
        return new PausedRun(paused, history, resumed);
    }

    private static Func<TaskInstance, bool> Reported(string node, EnumTaskState state)
        => x => x.NodeName == node && x.State == state;

    /// <summary><c>Start ─> First(+10) ─> Wait ─> Second(+100) ─> End</c>, the wait passing through.</summary>
    private static TestWorkflow BuildLine(string name = "Line") => new TestWorkflow(name)
        .Start()
        .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = 10 })
        .Task("Wait", PluginTasks.Delay, new { DelayMs = 100 })
        .Task("Second", PluginTasks.Test, new { Message = "$previous.Message", Value = "$previous.Value", Add = 100 })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "First", "Wait", "Second", "End");

    [Test]
    public async Task Pause_LetsTheRunningNodeFinish()
    {
        PausedRun paused = await PauseAsync(BuildLine(), new { Value = 1 }, Reported("Wait", EnumTaskState.Progressing));

        Assert.Multiple(() =>
        {
            Assert.That(paused.Instance.State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That(paused.Instance.Output?.Value<string>(), Is.EqualTo("Paused by the user."), "it tells why");
            Assert.That(paused.History.Single(x => x.NodeName == "Wait").State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(paused.History.Where(x => x.NodeName == "Second"), Is.Empty, "no task starts once paused");
        });
    }

    [Test]
    public async Task PausedRun_ResumesWithWhatItDidntStart()
    {
        PausedRun paused = await PauseAsync(BuildLine(), new { Value = 1 }, Reported("Wait", EnumTaskState.Progressing));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(paused.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("Wait"), Is.Empty);
            Assert.That(run.CompletionsOf("Second"), Is.EqualTo(1));
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
        });
    }

    [Test]
    public async Task RunWhoseTasksAreOver_ReachesItsEndRatherThanPausing()
    {
        // The end being a control, it is still reached once the last task is over.
        PausedRun paused = await PauseAsync(BuildLine(), new { Value = 1 }, Reported("Second", EnumTaskState.Progressing));

        Assert.Multiple(() =>
        {
            Assert.That(paused.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(paused.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
        });
    }

    /// <summary><c>Start ─> Inner (the line, nested) ─> After(+1000) ─> End</c></summary>
    private static TestWorkflow BuildNested() => new TestWorkflow("Outer")
        .Start()
        .Nested("Inner", BuildLine("Inner line"), new { Value = "$previous.Value" })
        .Task("After", PluginTasks.Test, new { Message = "$previous.Message", Value = "$previous.Value", Add = 1000 })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "Inner", "After", "End");

    [Test]
    public async Task NestedRun_PausesWithItsParent()
    {
        PausedRun paused = await PauseAsync(BuildNested(), new { Value = 1 }, Reported("Wait", EnumTaskState.Progressing));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(paused.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(paused.Instance.State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That(paused.History.Single(x => x.NodeName == "Inner").State, Is.EqualTo(EnumTaskState.Suspended));
            Assert.That(run.CompletionsOf("Second"), Is.EqualTo(1));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(1111));
        });
    }
}
