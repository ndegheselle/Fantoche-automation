using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Tests;

/// <summary>
/// A cycle in the graph, closed on the data it produces.
///
/// <code>
/// Start ─> Counter(+1) ─┬─> LoopGate  (open while Value &lt; Max) ─> back to Counter
///                       └─> ExitGate  (open once Value >= Max) ─> End
/// </code>
///
/// Mirrors the "Loop" scenario of the console. Both gates are pass-through : the counter reads
/// "$previous.Value" the same way whether it is entered from the start or looped back through the
/// gate. The branch of a gate deactivating its output dies there, which is what ends the loop.
/// </summary>
[TestFixture]
internal sealed class LoopWorkflowTests
{
    private const int Max = 5;

    private static TestWorkflow Build() => new TestWorkflow("Loop")
        .Start()
        .Task("Counter", PluginTasks.Test, new { Message = "turn", Value = "$previous.Value", Add = 1 })
        .Task("LoopGate", PluginTasks.LoopGate, new { Value = "$previous.Value", Max, WhileUnder = true })
        .Task("ExitGate", PluginTasks.LoopGate, new { Value = "$previous.Value", Max, WhileUnder = false })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "Counter", "LoopGate")
        .Chain("Counter", "ExitGate")
        .Chain("LoopGate", "Counter")
        .Chain("ExitGate", "End");

    [Test]
    public async Task Loop_EndsOnTheValueItProduced()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(Max));
            Assert.That(run.Instance.Output?["Message"]?.Value<string>(), Is.EqualTo("turn -> task"));
        });
    }

    [Test]
    public async Task Loop_RunsItsBodyOncePerTurn()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            // 1 -> 5, so four turns, both gates being reached on each of them.
            Assert.That(run.CompletionsOf("Counter"), Is.EqualTo(Max - 1));
            Assert.That(run.CompletionsOf("LoopGate"), Is.EqualTo(Max - 1));
            Assert.That(run.CompletionsOf("ExitGate"), Is.EqualTo(Max - 1));
            Assert.That(run.CompletionsOf("End"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task LoopedBackNode_ReadsTheTurnBefore()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        // The gate being pass-through, the counter reads the counter of the previous turn — the
        // parameters of the run on the first one.
        Assert.That(
            run.Of("Counter").Where(x => x.State == EnumTaskState.Progressing).Select(x => x.Parameters?["Value"]?.Value<int>()),
            Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task ClosedGate_EndsTheBranchWithNoOutput()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        IReadOnlyList<NodeReport> loop = [.. run.Of("LoopGate").Where(x => x.State == EnumTaskState.Completed)];
        IReadOnlyList<NodeReport> exit = [.. run.Of("ExitGate").Where(x => x.State == EnumTaskState.Completed)];

        Assert.Multiple(() =>
        {
            // A gate deactivating its output still completes, it is the missing output that closes
            // the branch : the loop gate closes on the last turn only.
            Assert.That(loop.Where(x => x.Output == null).Select(x => x.Parameters?["Value"]?.Value<int>()),
                Is.EqualTo(new[] { Max }));
            // The exit gate is the other way around : open on the last turn only.
            Assert.That(exit.Where(x => x.Output != null).Select(x => x.Parameters?["Value"]?.Value<int>()),
                Is.EqualTo(new[] { Max }));
        });
    }

    [Test]
    public async Task Gates_NotifyWhatTheyDecided()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Messages, Does.Contain($"Gate open (2/{Max})"), "the loop gate of the first turn");
            Assert.That(run.Messages, Does.Contain($"Gate closed ({Max}/{Max})"), "the loop gate of the last turn");
            Assert.That(run.Messages, Does.Contain($"Gate open ({Max}/{Max})"), "the exit gate of the last turn");
        });
    }

    [Test]
    public async Task DeactivatedOutput_ClosesTheBranchItStandsOn()
    {
        TestWorkflow workflow = new TestWorkflow("Deactivated", requireOutput: false)
            .Start()
            .Task("Condition", PluginTasks.Conditional, new { TestDeactivate = true })
            .Task("Never", PluginTasks.Test, new { Message = "never", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Condition", "Never", "End");

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("Condition").Last().State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Of("Condition").Last().Output, Is.Null);
            Assert.That(run.Of("Never"), Is.Empty);
            Assert.That(run.Instance.Output, Is.Null);
        });
    }

    [Test]
    public async Task ActivatedOutput_CarriesTheBranchOn()
    {
        TestWorkflow workflow = new TestWorkflow("Activated")
            .Start()
            .Task("Condition", PluginTasks.Conditional, new { TestDeactivate = false })
            .Task("Next", PluginTasks.Test, new { Message = "next", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Condition", "Next", "End");

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.CompletionsOf("Next"), Is.EqualTo(1));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task WorkflowExpectingAnOutput_FailsWhenEveryBranchDied()
    {
        TestWorkflow workflow = new TestWorkflow("NoOutput")
            .Start()
            .Task("Condition", PluginTasks.Conditional, new { TestDeactivate = true })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Condition", "End");

        (WorkflowRun run, Exception? error) = await workflow.TryRunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.TypeOf<Executor.ExecutionException>());
            Assert.That(error?.Message, Is.EqualTo("Reached end of workflow without data."));
            Assert.That(run.CompletionsOf("End"), Is.EqualTo(0));
        });
    }
}
