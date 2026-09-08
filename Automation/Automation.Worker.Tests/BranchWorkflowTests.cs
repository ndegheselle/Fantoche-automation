using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Tests;

/// <summary>
/// Parallel branches, and what merges them : a join waits for every branch reaching it, a map
/// reshapes each of them as it arrives, and the end closes the whole run.
/// <para>
/// Mirrors the "Branches" scenario of the console. The branch that has to arrive last is held by a
/// delay rather than by chance : the executor runs them for real, so what a test asserts of their
/// order has to be what the graph itself imposes.
/// </para>
/// </summary>
[TestFixture]
internal sealed class BranchWorkflowTests
{
    /// <summary>
    /// Two branches merged by a join, the second one held long enough to always arrive last.
    ///
    /// <code>
    /// Start ─┬─> A(+1) ───────────────┬─> Join ─> End
    ///        └─> Slow ─> B(+2) ───────┘
    /// </code>
    /// </summary>
    private static TestWorkflow BuildJoin(object joinMapping) => new TestWorkflow("Join")
        .Start()
        .Task("A", PluginTasks.Test, new { Message = "a", Value = "$previous.Value", Add = 1 })
        .Task("Slow", PluginTasks.Delay, new { DelayMs = 150 })
        .Task("B", PluginTasks.Test, new { Message = "b", Value = "$previous.Value", Add = 2 })
        .Join("Join", joinMapping)
        // The end reads what the join resolved, so it only reads what the mapping of the test put
        // in it.
        .End(new { Value = "$previous.Value" })
        .Chain("Start", "A", "Join")
        .Chain("Start", "Slow", "B", "Join")
        .Chain("Join", "End");

    [Test]
    public async Task Join_ReadsItsBranchesByNodeName()
    {
        TestWorkflow workflow = BuildJoin(new
        {
            Value = "$previous.A.Value",
            Message = "$previous.B.Message"
        });

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.OutputOf("Join", "Value")?.Value<int>(), Is.EqualTo(2), "A : 1 + 1");
            Assert.That(run.OutputOf("Join", "Message")?.Value<string>(), Is.EqualTo("b -> task"));
            // The join hands over what it resolved, so the end reads it as a single previous.
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Join_RunsOnceEveryBranchArrived()
    {
        WorkflowRun run = await BuildJoin(new { Value = "$previous.B.Value" }).RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            // The branch arriving first is closed by the join, only the last one carries on.
            Assert.That(run.CompletionsOf("Join"), Is.EqualTo(1));
            Assert.That(run.CompletionsOf("End"), Is.EqualTo(1));
            Assert.That(run.OutputOf("Join", "Value")?.Value<int>(), Is.EqualTo(3), "B : 1 + 2");
        });
    }

    [Test]
    public async Task Join_ReadsPastThePassThroughOfItsBranch()
    {
        WorkflowRun run = await BuildJoin(new { Value = "$previous.B.Value" }).RunAsync(new { Value = 1 });

        // The delay is pass-through : B resolved against the start, not against the delay.
        Assert.That(run.ParametersOf("B")?["Value"]?.Value<int>(), Is.EqualTo(1));
    }

    [Test]
    public async Task Join_MergesTheSharedContextOfEveryBranch()
    {
        TestWorkflow workflow = new TestWorkflow("SharedBranches")
            .Start()
            .Task("A", PluginTasks.Test, new { Message = "a", Value = "$previous.Value", Add = 1 })
            .Share("ShareA", new { FromA = 10 })
            .Task("Slow", PluginTasks.Delay, new { DelayMs = 150 })
            .Task("B", PluginTasks.Test, new { Message = "b", Value = "$previous.Value", Add = 2 })
            .Share("ShareB", new { FromB = 20 })
            .Join("Join", new { FromA = "$shared.FromA", FromB = "$shared.FromB" })
            .Task("After", PluginTasks.Test, new { Message = "after", Value = "$shared.FromA", Add = "$shared.FromB" })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "A", "ShareA", "Join")
            .Chain("Start", "Slow", "B", "ShareB", "Join")
            .Chain("Join", "After", "End");

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            // What each branch shared reaches the join, whichever branch resumed it.
            Assert.That(run.OutputOf("Join", "FromA")?.Value<int>(), Is.EqualTo(10));
            Assert.That(run.OutputOf("Join", "FromB")?.Value<int>(), Is.EqualTo(20));
            // And walks on down the merged branch.
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(30));
        });
    }

    /// <summary>
    /// The scenario of the console : the branch reaching the end first closes the whole run.
    ///
    /// <code>
    /// Start ─┬─> Quick(+1) ──────────────────┬─> Join ──┐
    ///        ├─> Slow ─> Late(+100) ─────────┘          ├─> End
    ///        └─> Sprint(+2) ────────────────────────────┘
    /// </code>
    /// </summary>
    private static TestWorkflow BuildRace() => new TestWorkflow("Branches")
        .Start()
        .Task("Quick", PluginTasks.Test, new { Message = "quick", Value = "$previous.Value", Add = 1 })
        .Task("Slow", PluginTasks.Delay, new { DelayMs = 1000 })
        .Task("Late", PluginTasks.Test, new { Message = "late", Value = "$previous.Value", Add = 100 })
        .Task("Sprint", PluginTasks.Test, new { Message = "sprint", Value = "$previous.Value", Add = 2 })
        .Join("Join", new { Value = "$previous.Late.Value", Message = "$previous.Quick.Message" })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "Quick", "Join")
        .Chain("Start", "Slow", "Late", "Join")
        .Chain("Start", "Sprint", "End")
        .Chain("Join", "End");

    [Test]
    public async Task End_ClosesTheRunWithWhatReachedItFirst()
    {
        WorkflowRun run = await BuildRace().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            // The sprint branch reached the end while the slow one was still delayed.
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(3), "1 + 2");
            Assert.That(run.Instance.Output?["Message"]?.Value<string>(), Is.EqualTo("sprint -> task"));
        });
    }

    [Test]
    public async Task End_CancelsWhateverIsStillRunning()
    {
        WorkflowRun run = await BuildRace().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.States("Slow"), Does.Contain(EnumTaskState.Canceled));
            // The branch died on the cancellation, what was behind it never ran.
            Assert.That(run.Of("Late"), Is.Empty);
            // And the join is left waiting for a branch that will never arrive.
            Assert.That(run.CompletionsOf("Join"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Map_ReshapesWhatTheBranchCarries()
    {
        TestWorkflow workflow = new TestWorkflow("Map")
            .Start()
            .Task("A", PluginTasks.Test, new { Message = "a", Value = "$previous.Value", Add = 1 })
            .Map("Map", new { Renamed = "$previous.Value" })
            .Task("B", PluginTasks.Test, new { Message = "b", Value = "$previous.Renamed", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "A", "Map", "B", "End");

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.OutputOf("Map", "Renamed")?.Value<int>(), Is.EqualTo(2));
            // A map isn't transparent : B read what the map produced, under the name it gave it.
            Assert.That(run.ParametersOf("B")?["Value"]?.Value<int>(), Is.EqualTo(2));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Map_RunsOncePerBranchReachingIt()
    {
        // No end : nothing closes the run, so both branches are walked to their last node.
        TestWorkflow workflow = new TestWorkflow("MapBranches", requireOutput: false)
            .Start()
            .Task("A", PluginTasks.Test, new { Message = "a", Value = "$previous.Value", Add = 1 })
            .Task("B", PluginTasks.Test, new { Message = "b", Value = "$previous.Value", Add = 2 })
            .Map("Map", new { Carried = "$previous.Value" })
            .Task("After", PluginTasks.Test, new { Message = "after", Value = "$previous.Carried", Add = 0 })
            .Chain("Start", "A", "Map")
            .Chain("Start", "B", "Map")
            .Chain("Map", "After");

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            // Unlike a join, a map holds no branch back : it runs again for each of them.
            Assert.That(run.CompletionsOf("Map"), Is.EqualTo(2));
            Assert.That(run.CompletionsOf("After"), Is.EqualTo(2));
            Assert.That(
                run.Of("After").Where(x => x.State == EnumTaskState.Completed).Select(x => x.Output?["Value"]?.Value<int>()),
                Is.EquivalentTo(new[] { 2, 3 }));
            // A workflow declaring no output ends empty rather than in error.
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output, Is.Null);
        });
    }
}
