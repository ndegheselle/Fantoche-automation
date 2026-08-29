using Automation.Plugins;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Console.Scenarios;

/// <summary>
/// The straight line : one branch, every node running once.
///
/// <code>
/// Start ─> First ─> Share ─> PassThrough ─> Second ─> End
/// </code>
///
/// Checks the share control (its parameters land in the shared context, read further down as
/// "$shared.*") and that both the share and the pass-through are transparent to "$previous.*",
/// which keeps resolving to the last task having produced an output.
/// </summary>
public class LinearScenario : IScenario
{
    public string Name => "Linear";

    public string Description =>
        "Start > First(+10) > Share > PassThrough > Second(+$shared.Bonus) > End. " +
        "Expected output : Value = 1 + 10 + 100 = 111.";

    public JToken Input => JToken.FromObject(new ScenarioInput() { Value = 1 });

    public AutomationWorkflow Build()
    {
        AutomationWorkflow workflow = new AutomationWorkflow()
        {
            Id = Guid.NewGuid(),
            Metadata = new ScopedMetadata() { Name = Name },
            InputSchema = JsonSchema.FromType<ScenarioInput>(),
            OutputSchema = JsonSchema.FromType<TestResult>(),
        };

        GraphControl start = new GraphControl(AutomationControl.StartTask)
        {
            Metadata = new ScopedMetadata() { Name = "Start" },
        };

        GraphTask first = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "First" },
            ParametersJson = JsonConvert.SerializeObject(new { Message = "first", Value = "$previous.Value", Add = 10 })
        };

        // Everything the share receives is merged in the shared context of the workflow.
        GraphControl share = new GraphControl(AutomationControl.ShareTask)
        {
            Metadata = new ScopedMetadata() { Name = "Share" },
            ParametersJson = JsonConvert.SerializeObject(new { Bonus = 100, Origin = "$previous.Message" })
        };

        GraphTask passThrough = new GraphTask(ScenarioTasks.PassThrough)
        {
            Metadata = new ScopedMetadata() { Name = "PassThrough" },
            ParametersJson = JsonConvert.SerializeObject(new PassThroughParameters() { Label = "after the share" })
        };

        // Both the share and the pass-through are transparent : "$previous" is still "First" here.
        GraphTask second = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "Second" },
            ParametersJson = JsonConvert.SerializeObject(new { Message = "second", Value = "$previous.Value", Add = "$shared.Bonus" })
        };

        // The end merges every branch reaching it, so its context is indexed by node name.
        GraphControl end = new GraphControl(AutomationControl.EndTask)
        {
            Metadata = new ScopedMetadata() { Name = "End" },
            ParametersJson = JsonConvert.SerializeObject(new { Value = "$previous.Second.Value", Message = "$previous.Second.Message" })
        };

        workflow.Graph.Nodes.Add(start);
        workflow.Graph.Nodes.Add(first);
        workflow.Graph.Nodes.Add(share);
        workflow.Graph.Nodes.Add(passThrough);
        workflow.Graph.Nodes.Add(second);
        workflow.Graph.Nodes.Add(end);

        workflow.Graph.Connect(start, first);
        workflow.Graph.Connect(first, share);
        workflow.Graph.Connect(share, passThrough);
        workflow.Graph.Connect(passThrough, second);
        workflow.Graph.Connect(second, end);

        workflow.Graph.Refresh(ScenarioTasks.All);
        return workflow;
    }
}
