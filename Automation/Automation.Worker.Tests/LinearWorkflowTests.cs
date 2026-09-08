using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Tests;

/// <summary>
/// The straight line : one branch, every node running once.
///
/// <code>
/// Start ─> First ─> Share ─> PassThrough ─> Second ─> End
/// </code>
///
/// Mirrors the "Linear" scenario of the console : the share control (its parameters landing in the
/// shared context, read further down as "$shared.*") and the transparency of both the share and
/// the pass-through to "$previous.*", which keeps resolving to the last node having produced an
/// output.
/// </summary>
[TestFixture]
internal sealed class LinearWorkflowTests
{
    /// <summary>The line of the scenario, run with <c>Value = 1</c>.</summary>
    private static TestWorkflow Build() => new TestWorkflow("Linear")
        .Start()
        .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = 10 })
        .Share("Share", new { Bonus = 100, Origin = "$previous.Message" })
        .Task("PassThrough", PluginTasks.PassThrough, new { Label = "after the share" })
        .Task("Second", PluginTasks.Test, new { Message = "second", Value = "$previous.Value", Add = "$shared.Bonus" })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "First", "Share", "PassThrough", "Second", "End");

    [Test]
    public async Task Line_CompletesWithTheOutputOfItsEnd()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            // 1 (input) + 10 (First) + 100 (the bonus the share fed the context with)
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(111));
            Assert.That(run.Instance.Output?["Message"]?.Value<string>(), Is.EqualTo("second -> task"));
        });
    }

    [Test]
    public async Task Line_RunsEveryNodeOnce()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            foreach (string node in new[] { "Start", "First", "Share", "PassThrough", "Second", "End" })
                Assert.That(run.CompletionsOf(node), Is.EqualTo(1), $"[{node}] is expected to run once.");
        });
    }

    [Test]
    public async Task Task_IsReportedProgressingThenCompleted()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.States("First"), Is.EqualTo(new[] { EnumTaskState.Progressing, EnumTaskState.Completed }));
            // A control is driven by the workflow itself, it is only reported once it is resolved.
            Assert.That(run.States("Share"), Is.EqualTo(new[] { EnumTaskState.Completed }));
        });
    }

    [Test]
    public async Task Share_HandsItsResolvedParametersToTheSharedContext()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            // A share produces nothing of its own : its output is what it resolved to.
            Assert.That(run.OutputOf("Share", "Bonus")?.Value<int>(), Is.EqualTo(100));
            Assert.That(run.OutputOf("Share", "Origin")?.Value<string>(), Is.EqualTo("first -> task"));
            // Which is what "$shared.Bonus" resolved to further down the branch.
            Assert.That(run.ParametersOf("Second")?["Add"]?.Value<int>(), Is.EqualTo(100));
        });
    }

    [Test]
    public async Task PassThroughAndShare_AreTransparentToPrevious()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        // "$previous.Value" of Second is read past the pass-through and the share, so it is still
        // the output of First.
        Assert.That(run.ParametersOf("Second")?["Value"]?.Value<int>(), Is.EqualTo(11));
        // The pass-through itself completes with an empty output rather than with none : a branch
        // is only closed by a task deactivating its output.
        Assert.That(run.OutputOf("PassThrough")?.ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo("{}"));
    }

    [Test]
    public async Task Notifications_OfTheTasksAreForwarded()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        Assert.That(run.Messages, Does.Contain("Passing through: after the share"));
    }

    [Test]
    public async Task Tasks_AreLoadedFromThePackageTheyTarget()
    {
        WorkflowRun run = await Build().RunAsync(new { Value = 1 });

        PackageClassTarget target = PluginTasks.Test.Target!;
        string expected = $"{target.Package.Id}/{target.Package.Version}/{target.Dll}";

        // The three tasks of the line are resolved against the package source they target, the
        // controls being driven by the workflow itself.
        Assert.That(run.Packages.Requests, Is.EqualTo(new[] { expected, expected, expected }));
    }

    [Test]
    public async Task StartParameters_OverrideWhatTheStartNodeDefaults()
    {
        TestWorkflow workflow = new TestWorkflow("Defaults")
            .Start(new { Value = 5, Bonus = 1 })
            .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = "$previous.Bonus" })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "First", "End");

        WorkflowRun run = await workflow.RunAsync(new { Value = 7 });

        Assert.Multiple(() =>
        {
            // The parameters of the run win over what the node holds, what it holds alone stands.
            Assert.That(run.ParametersOf("First")?["Value"]?.Value<int>(), Is.EqualTo(7));
            Assert.That(run.ParametersOf("First")?["Add"]?.Value<int>(), Is.EqualTo(1));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(8));
        });
    }

    [Test]
    public async Task StartNode_StandsAloneWhenTheRunHasNoParameters()
    {
        TestWorkflow workflow = new TestWorkflow("NoParameters")
            .Start(new { Value = 3 })
            .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "First", "End");

        WorkflowRun run = await workflow.RunAsync();

        Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(4));
    }
}
