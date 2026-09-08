using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Executor;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Tests;

/// <summary>
/// What the executor does of a graph that doesn't hold up : a task that can't run fails its own
/// instance and closes its branch, while a mapping that can't be resolved stops the whole run.
/// </summary>
[TestFixture]
internal sealed class WorkflowExecutorErrorTests
{
    /// <summary>
    /// A line of two nodes, [broken] being the first one : whether the second one ran is whether
    /// the branch survived.
    /// </summary>
    private static TestWorkflow BuildLine(string node, AutomationTask task, object? mapping)
        => new TestWorkflow(node, requireOutput: false)
            .Start()
            .Task(node, task, mapping)
            .Task("Next", PluginTasks.Test, new { Message = "next", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", node, "Next", "End");

    #region A task that can't run

    [Test]
    public async Task Parameters_NotMatchingTheSchemaOfTheTask_FailTheNode()
    {
        // "Value" is an integer of TestParameters, the mapping hands over a string.
        TestWorkflow workflow = BuildLine("Bad", PluginTasks.Test, new { Message = "bad", Value = "not a number", Add = 1 });

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("Bad").Last().State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(run.Of("Bad").Last().Output?.Value<string>(),
                Does.Contain("Parameters don't correspond to schema"));
            // A failed node closes its branch, whatever was behind it never runs.
            Assert.That(run.Of("Next"), Is.Empty);
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
        });
    }

    [Test]
    public async Task ClassMissingFromThePackage_FailsTheNode()
    {
        WorkflowRun run = await BuildLine("Unknown", PluginTasks.UnknownClass, null).RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("Unknown").Last().State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(run.Of("Unknown").Last().Output?.Value<string>(),
                Does.Contain($"Could not get type [{PluginTasks.UnknownClass.Target!.ClassFullName}]"));
            Assert.That(run.Of("Next"), Is.Empty);
        });
    }

    [Test]
    public async Task PackageThatCantBeResolved_FailsTheNode()
    {
        WorkflowRun run = await BuildLine("Missing", PluginTasks.UnknownPackage, null).RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("Missing").Last().State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(run.Of("Missing").Last().Output?.Value<string>(), Does.Contain("Package not found"));
            Assert.That(run.Of("Next"), Is.Empty);
        });
    }

    [Test]
    public async Task TaskTargetingSomethingElseThanAPackage_FailsTheNode()
    {
        AutomationTask task = PluginTasks.Make(
            "Automation.Plugins.TestTask", "NoTarget", new JsonSchema(), new JsonSchema());
        task.Target = null;

        WorkflowRun run = await BuildLine("NoTarget", task, null).RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Of("NoTarget").Last().State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(run.Of("NoTarget").Last().Output?.Value<string>(), Does.Contain("Task target is not a package."));
        });
    }

    #endregion

    #region A mapping that can't be resolved

    [Test]
    public async Task NodeWithoutTemplate_StopsTheRun()
    {
        TestWorkflow workflow = BuildLine("NoTemplate", PluginTasks.Test, null);
        workflow["NoTemplate"].InputTemplateJson = null;

        (WorkflowRun run, Exception? error) = await workflow.TryRunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.TypeOf<ExecutionException>());
            Assert.That(error?.Message, Does.Contain("Node doesn't have any valid JSON template for parameters."));
            Assert.That(error?.Message, Does.Contain(workflow["NoTemplate"].Id.ToString()));
            Assert.That(run.Of("NoTemplate"), Is.Empty, "the node never reached the executor");
        });
    }

    [Test]
    public async Task NodeWithATemplateThatIsntJson_StopsTheRun()
    {
        TestWorkflow workflow = BuildLine("Invalid", PluginTasks.Test, "not json at all");

        (_, Exception? error) = await workflow.TryRunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.TypeOf<ExecutionException>());
            Assert.That(error?.Message, Does.Contain("Node doesn't have any valid JSON template for parameters."));
        });
    }

    [Test]
    public async Task ReferenceMissingFromTheContext_StopsTheRun()
    {
        TestWorkflow workflow = BuildLine("Broken", PluginTasks.Test,
            new { Message = "broken", Value = "$previous.Nope", Add = 1 });

        (_, Exception? error) = await workflow.TryRunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.TypeOf<ExecutionException>());
            Assert.That(error?.Message, Does.Contain($"Parameters error in node [{workflow["Broken"].Id}]"));
            Assert.That(error?.Message, Does.Contain("[previous.Nope] not found in context."));
        });
    }

    [Test]
    public async Task ReferenceMissingFromTheContextOfAControl_StopsTheRun()
    {
        // An end reached by a single branch resolves against that branch alone : its previous is
        // the output itself, not the outputs indexed by node name a join hands over.
        TestWorkflow workflow = new TestWorkflow("FlatEnd")
            .Start()
            .Task("First", PluginTasks.Test, new { Message = "first", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.First.Value" })
            .Chain("Start", "First", "End");

        (WorkflowRun run, Exception? error) = await workflow.TryRunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.TypeOf<ExecutionException>());
            Assert.That(error?.Message, Does.Contain($"Parameters error in control [{workflow["End"].Id}]"));
            Assert.That(error?.Message, Does.Contain("[previous.First.Value] not found in context."));
            Assert.That(run.CompletionsOf("First"), Is.EqualTo(1), "the branch ran up to the end");
        });
    }

    #endregion

    #region Cancellation

    [Test]
    public async Task CancellationFromOutside_CancelsWhateverIsRunning()
    {
        TestWorkflow workflow = new TestWorkflow("Canceled", requireOutput: false)
            .Start()
            .Task("Slow", PluginTasks.Delay, new { DelayMs = 30000 })
            .Task("Never", PluginTasks.Test, new { Message = "never", Value = "$previous.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Slow", "Never", "End");

        using CancellationTokenSource cancellation = new();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 }, cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(run.States("Slow"), Does.Contain(EnumTaskState.Canceled));
            Assert.That(run.Of("Never"), Is.Empty);
            // The run itself is completed : it is the tasks that carry the cancellation.
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output, Is.Null);
        });
    }

    #endregion

    #region Nested workflow

    [Test]
    public async Task NestedWorkflow_RunsItsOwnGraphAndHandsOverItsOutput()
    {
        TestWorkflow nested = new TestWorkflow("Nested")
            .Start()
            .Task("Inner", PluginTasks.Test, new { Message = "inner", Value = "$previous.Value", Add = 5 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Inner", "End");

        TestWorkflow workflow = new TestWorkflow("Parent")
            .Start()
            .Nested("Sub", nested, new { Value = "$previous.Value" })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Sub", "End");

        WorkflowRun run = await workflow.RunAsync(new { Value = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(6));
            // The nodes of the nested graph are reported the same way as the ones of its parent.
            Assert.That(run.CompletionsOf("Inner"), Is.EqualTo(1));
            // The nested instance is reported twice : once by the end of its own graph, once by
            // the branch of the parent it is a node of.
            Assert.That(run.CompletionsOf("Sub"), Is.EqualTo(2));
        });
    }

    #endregion

    #region Global context

    [Test]
    public async Task GlobalContext_OfTheRunIsReadByTheNodes()
    {
        // The context of the scopes the workflow belongs to, see LocalExecutionService.
        TestWorkflow workflow = new TestWorkflow("Global")
            .Start()
            .Task("First", PluginTasks.Test, new { Message = "first", Value = "$global.Value", Add = 1 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "First", "End");

        WorkflowInstance instance = workflow.Instance(new { Value = 1 });
        instance.GlobalContext = JToken.FromObject(new { Value = 100 });

        WorkflowRun run = await WorkflowRun.ExecuteAsync(instance);

        Assert.Multiple(() =>
        {
            Assert.That(run.ParametersOf("First")?["Value"]?.Value<int>(), Is.EqualTo(100));
            Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(101));
        });
    }

    [Test]
    public async Task GlobalContext_IsHandedOverToANestedWorkflow()
    {
        TestWorkflow nested = new TestWorkflow("Nested")
            .Start()
            .Task("Inner", PluginTasks.Test, new { Message = "inner", Value = "$global.Value", Add = 5 })
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Inner", "End");

        TestWorkflow workflow = new TestWorkflow("Parent")
            .Start()
            .Nested("Sub", nested)
            .End(new { Value = "$previous.Value", Message = "$previous.Message" })
            .Chain("Start", "Sub", "End");

        WorkflowInstance instance = workflow.Instance(new { Value = 1 });
        instance.GlobalContext = JToken.FromObject(new { Value = 100 });

        WorkflowRun run = await WorkflowRun.ExecuteAsync(instance);

        // A nested workflow walks its graph with a resolution of its own, the global context of the
        // run it belongs to reaching it all the same.
        Assert.That(run.Instance.Output?["Value"]?.Value<int>(), Is.EqualTo(105));
    }

    #endregion
}
