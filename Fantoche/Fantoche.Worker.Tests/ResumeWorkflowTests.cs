using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A run cut short and resumed from its history : it ends as a whole run would, the nodes it was
/// running running again and the ones it completed being kept.
/// <para>
/// The crash is the history as it stood once a given state was stored : what a process dying right
/// then leaves behind, whatever the run did afterwards.
/// </para>
/// </summary>
[TestFixture]
internal sealed class ResumeWorkflowTests
{
    /// <summary>A run cut short : the run to resume and what its history held.</summary>
    private sealed record CrashedRun(WorkflowInstance Resumed, IReadOnlyList<TaskInstance> History)
    {
        public TaskInstance Last(string node) => History.Last(x => x.NodeName == node);
    }

    /// <summary>
    /// Run [workflow] with [parameters] and keep its history as it stood once [crashes] matched the
    /// last state stored, the nodes it was running interrupted as an application finds them when it
    /// starts again.
    /// </summary>
    private static async Task<CrashedRun> CrashAsync(TestWorkflow workflow, object parameters, Func<TaskInstance, bool> crashes)
    {
        WorkflowInstance instance = workflow.Instance(parameters);
        TaskInstance storedRun = FantocheJson.Copy<TaskInstance>(instance);
        List<TaskInstance> history = [];
        bool isCrashed = false;
        object gate = new();

        TaskInstancesProgress progress = new()
        {
            StateChanges = new Sink<TaskInstance>(x =>
            {
                lock (gate)
                {
                    if (isCrashed)
                        return;
                    history.RemoveAll(stored => stored.Id == x.Id);
                    history.Add(FantocheJson.Copy<TaskInstance>(x));
                    isCrashed = crashes(x);
                }
            }),
        };
        LocalStorage storage = new(Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N")));
        await new WorkflowExecutor(new TaskLoaders(new TestPackageManagement()), storage).ExecuteAsync(instance, progress);
        Assert.That(isCrashed, Is.True, "The run never reached the state it was meant to crash on.");

        foreach (TaskInstance stored in history.Where(x => x.State is EnumTaskState.Pending or EnumTaskState.Progressing))
            stored.State = EnumTaskState.Interrupted;
        storedRun.State = EnumTaskState.Interrupted;

        WorkflowInstance resumed = WorkflowInstance.From(workflow.Refreshed(), storedRun);
        resumed.StoredInstances = history;
        return new CrashedRun(resumed, history);
    }

    private static Func<TaskInstance, bool> Reported(string node, EnumTaskState state)
        => x => x.NodeName == node && x.State == state;

    /// <summary>Matches the [count]-th report of [node] in [state].</summary>
    private static Func<TaskInstance, bool> Reported(string node, EnumTaskState state, int count)
    {
        int seen = 0;
        return x => x.NodeName == node && x.State == state && ++seen == count;
    }

    /// <summary><c>Start ─> First(+10) ─> Second(+100) ─> End</c></summary>
    private static TestWorkflow BuildLine(string name = "Line") => new TestWorkflow(name)
        .Start()
        .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = 10 })
        .Task("Second", PluginTasks.Test, new { Message = "$previous.Message", Value = "$previous.Value", Add = 100 })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "First", "Second", "End");

    [Test]
    public async Task InterruptedNode_RunsAgain()
    {
        CrashedRun crashed = await CrashAsync(BuildLine(), new { Value = 1 }, Reported("Second", EnumTaskState.Progressing));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.States("Second"), Is.EqualTo(new[] { EnumTaskState.Progressing, EnumTaskState.Completed }));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
        });
    }

    [Test]
    public async Task InterruptedNode_KeepsItsInstance()
    {
        CrashedRun crashed = await CrashAsync(BuildLine(), new { Value = 1 }, Reported("Second", EnumTaskState.Progressing));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        // Run again rather than replaced : the history holds the node once, as it ended.
        Assert.That(run.Of("Second").Select(x => x.Id).Distinct(), Is.EqualTo(new[] { crashed.Last("Second").Id }));
    }

    [Test]
    public async Task CompletedNode_DoesntRunAgain()
    {
        CrashedRun crashed = await CrashAsync(BuildLine(), new { Value = 1 }, Reported("Second", EnumTaskState.Progressing));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.That(run.Of("First"), Is.Empty);
    }

    [Test]
    public async Task Resume_CarriesOnFromTheLastCompletedNode()
    {
        CrashedRun crashed = await CrashAsync(BuildLine(), new { Value = 1 }, Reported("First", EnumTaskState.Completed));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("First"), Is.Empty);
            Assert.That(run.CompletionsOf("Second"), Is.EqualTo(1));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
        });
    }

    [Test]
    public async Task RunHavingReachedItsEnd_IsOnlyClosed()
    {
        CrashedRun crashed = await CrashAsync(BuildLine(), new { Value = 1 }, Reported("End", EnumTaskState.Completed));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.Reports.Select(x => x.Id), Is.EqualTo(new[] { crashed.Resumed.Id }), "no node runs");
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
        });
    }

    /// <summary>
    /// <code>
    /// Start ─┬─> A(+1) ─┬─> Join ─> End
    ///        └─> B(+2) ─┘
    /// </code>
    /// </summary>
    private static TestWorkflow BuildJoin() => new TestWorkflow("Join", requireOutput: false)
        .Start()
        .Task("A", PluginTasks.Test, new { Message = "a", Value = "$previous.Value", Add = 1 })
        .Task("B", PluginTasks.Test, new { Message = "b", Value = "$previous.Value", Add = 2 })
        .Join("Join", new { A = "$previous.A.Value", B = "$previous.B.Value" })
        .End(new { A = "$previous.A", B = "$previous.B" })
        .Chain("Start", "A", "Join")
        .Chain("Start", "B", "Join")
        .Chain("Join", "End");

    [Test]
    public async Task WaitingJoin_CountsTheBranchesThatAlreadyArrived()
    {
        CrashedRun crashed = await CrashAsync(BuildJoin(), new { Value = 1 }, Reported("Join", EnumTaskState.Waiting));
        Guid arrivedId = crashed.Last("Join").PreviousIds.Single();
        string arrived = crashed.History.Single(x => x.Id == arrivedId).NodeName;

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.Of(arrived), Is.Empty, "the branch that arrived doesn't run again");
            Assert.That(run.CompletionsOf("Join"), Is.EqualTo(1));
            Assert.That(run.Instance.Output?["A"]?.Value<int>(), Is.EqualTo(2));
            Assert.That(run.Instance.Output?["B"]?.Value<int>(), Is.EqualTo(3));
        });
    }

    /// <summary>
    /// <code>
    /// Start ─> Counter(+1) ─┬─> LoopGate  (open while Value &lt; 5) ─> back to Counter
    ///                       └─> ExitGate  (open once Value >= 5) ─> End
    /// </code>
    /// </summary>
    private static TestWorkflow BuildLoop() => new TestWorkflow("Loop")
        .Start()
        .Task("Counter", PluginTasks.Test, new { Message = "turn", Value = "$previous.Value", Add = 1 })
        .Task("LoopGate", PluginTasks.LoopGate, new { Value = "$previous.Value", Max = 5, WhileUnder = true })
        .Task("ExitGate", PluginTasks.LoopGate, new { Value = "$previous.Value", Max = 5, WhileUnder = false })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "Counter", "LoopGate")
        .Chain("Counter", "ExitGate")
        .Chain("LoopGate", "Counter")
        .Chain("ExitGate", "End");

    [Test]
    public async Task Loop_CarriesOnWithTheTurnItWasIn()
    {
        // The gate passing through, the counter after it reads the counter before it : the link
        // between the two comes from the history.
        CrashedRun crashed = await CrashAsync(BuildLoop(), new { Value = 1 }, Reported("LoopGate", EnumTaskState.Completed, count: 2));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(
                run.Of("Counter").Where(x => x.State == EnumTaskState.Progressing).Select(x => x.Parameters?["Value"]?.Value<int>()),
                Is.EqualTo(new[] { 3, 4 }));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(5));
        });
    }

    /// <summary><c>Start ─> Inner (the line, nested) ─> End</c></summary>
    private static TestWorkflow BuildNested() => new TestWorkflow("Outer")
        .Start()
        .Nested("Inner", BuildLine("Inner line"), new { Value = "$previous.Value" })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "Inner", "End");

    [Test]
    public async Task NestedRun_CarriesOnFromItsOwnHistory()
    {
        CrashedRun crashed = await CrashAsync(BuildNested(), new { Value = 1 }, Reported("Second", EnumTaskState.Progressing));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(crashed.Resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("First"), Is.Empty);
            Assert.That(run.CompletionsOf("Second"), Is.EqualTo(1));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
        });
    }
}
