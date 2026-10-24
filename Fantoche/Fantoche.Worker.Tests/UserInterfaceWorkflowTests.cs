using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A run reaching a user interface task : its branch holds there, nothing running meanwhile, until
/// the user answers (see SupervisorExecutionTests for the answer).
/// </summary>
[TestFixture]
internal sealed class UserInterfaceWorkflowTests
{
    /// <summary><c>Start ─> Ask (the user reorders) ─> End</c></summary>
    /// <param name="requireOutput">Whether it declares an output : nested, it needs one to be connected further.</param>
    private static TestWorkflow BuildAsk(string name = "Ask", bool requireOutput = false) => new TestWorkflow(name, requireOutput)
        .Start()
        .Task("Ask", PluginTasks.Reorder, new { Title = "Sort them", Items = "$previous.Items" })
        .End(new { Items = "$previous.Items" })
        .Chain("Start", "Ask", "End");

    [Test]
    public async Task Run_WaitsForTheUserAtAUserInterfaceTask()
    {
        WorkflowRun run = await BuildAsk().RunAsync(new { Items = new[] { "a", "b" } });

        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.AwaitingUser));
            Assert.That(run.States("Ask").Last(), Is.EqualTo(EnumTaskState.AwaitingUser));
            Assert.That(run.Of("End"), Is.Empty);
        });
    }

    [Test]
    public async Task UserInterfaceTask_IsGivenItsResolvedParameters()
    {
        WorkflowRun run = await BuildAsk().RunAsync(new { Items = new[] { "a", "b" } });

        // What the page reads as its input.
        Assert.That(run.ParametersOf("Ask")?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task NestedUserInterfaceTask_HoldsTheRunAboveIt()
    {
        TestWorkflow outer = new TestWorkflow("Outer", requireOutput: false)
            .Start()
            .Nested("Inner", BuildAsk("Inner ask", requireOutput: true), new { Items = "$previous.Items" })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Inner", "End");

        WorkflowRun run = await outer.RunAsync(new { Items = new[] { "a", "b" } });

        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.AwaitingUser));
            Assert.That(run.States("Inner").Last(), Is.EqualTo(EnumTaskState.AwaitingUser));
        });
    }

    [Test]
    public async Task AnsweredNestedTask_ResumesTheRunsAboveIt()
    {
        TestWorkflow outer = new TestWorkflow("Outer", requireOutput: false)
            .Start()
            .Nested("Inner", BuildAsk("Inner ask", requireOutput: true), new { Items = "$previous.Items" })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Inner", "End");
        WorkflowRun held = await outer.RunAsync(new { Items = new[] { "a", "b" } });

        // As the history holds it once answered (see UserAnswers).
        TaskInstance ask = held.Stored.Single(x => x.NodeName == "Ask");
        ask.Output = JToken.FromObject(new { Items = new[] { "b", "a" } });
        ask.State = EnumTaskState.Completed;
        WorkflowInstance resumed = WorkflowInstance.From(outer.Refreshed(), held.Instance);
        resumed.StoredInstances = held.Stored;
        WorkflowRun run = await WorkflowRun.ExecuteAsync(resumed);

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("Ask"), Is.Empty, "answered, it doesn't wait again");
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "b", "a" }));
        });
    }
}
