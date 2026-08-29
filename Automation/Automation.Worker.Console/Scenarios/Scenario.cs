using Automation.Plugins;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Console.Scenarios;

/// <summary>
/// Input every scenario is started with. The property is named like <see cref="TestResult.Value"/>
/// so a node reading "$previous.Value" works both right after the start and further down the graph.
/// </summary>
public class ScenarioInput
{
    public int Value { get; set; }
}

/// <summary>
/// A workflow built to exercise the executor by hand, run by the console.
/// </summary>
public interface IScenario
{
    string Name { get; }

    /// <summary>
    /// What the scenario checks and what the run is expected to show.
    /// </summary>
    string Description { get; }

    JToken Input { get; }

    /// <summary>
    /// Build the workflow with its graph already refreshed, ready to be executed.
    /// </summary>
    AutomationWorkflow Build();
}

/// <summary>
/// Task definitions (the reusable "blueprints" the graph nodes point to) shared by the scenarios.
/// They all target the classes of the Automation.Plugins package.
/// </summary>
public static class ScenarioTasks
{
    public static readonly AutomationTask Test = MakeTask(
        "Automation.Plugins.TestTask", "Test",
        JsonSchema.FromType<TestParameters>(), JsonSchema.FromType<TestResult>());

    public static readonly AutomationTask Delay = MakeTask(
        "Automation.Plugins.TestDelay", "Delay",
        JsonSchema.FromType<TestDelayParameters>(), new JsonSchema(), passThrough: true);

    public static readonly AutomationTask PassThrough = MakeTask(
        "Automation.Plugins.PassThroughTask", "PassThrough",
        JsonSchema.FromType<PassThroughParameters>(), new JsonSchema(), passThrough: true);

    public static readonly AutomationTask Conditional = MakeTask(
        "Automation.Plugins.ConditionalTask", "Conditional",
        JsonSchema.FromType<ConditionalParameters>(), new JsonSchema(), passThrough: true);

    public static readonly AutomationTask LoopGate = MakeTask(
        "Automation.Plugins.LoopGateTask", "LoopGate",
        JsonSchema.FromType<LoopGateParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>
    /// Every task the graphs can point to, controls included : what
    /// <see cref="Shared.Data.Graph.TasksGraph.Refresh"/> needs to load the nodes.
    /// </summary>
    public static Dictionary<Guid, BaseAutomationTask> All => new()
    {
        { AutomationControl.StartTask.Id, AutomationControl.StartTask },
        { AutomationControl.EndTask.Id, AutomationControl.EndTask },
        { AutomationControl.ShareTask.Id, AutomationControl.ShareTask },
        { AutomationControl.JoinTask.Id, AutomationControl.JoinTask },
        { Test.Id, Test },
        { Delay.Id, Delay },
        { PassThrough.Id, PassThrough },
        { Conditional.Id, Conditional },
        { LoopGate.Id, LoopGate },
    };

    private static AutomationTask MakeTask(string className, string name, JsonSchema? input, JsonSchema? output, bool passThrough = false)
        => new AutomationTask()
        {
            Id = Guid.NewGuid(),
            Target = new PackageClassTarget()
            {
                Dll = "Automation.Plugins",
                ClassFullName = className,
                Package = new PackageIdentifier()
                {
                    Id = "Automation.Plugins",
                    Version = new Version("1.0.1")
                }
            },
            Metadata = new ScopedMetadata() { Name = name },
            InputSchema = input,
            OutputSchema = output,
            Settings = new TaskSettings() { IsPassingThrough = passThrough }
        };
}
