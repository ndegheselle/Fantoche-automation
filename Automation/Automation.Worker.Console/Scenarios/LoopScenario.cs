using Automation.Plugins;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Console.Scenarios;

/// <summary>
/// A cycle in the graph, closed on the data it produces.
///
/// <code>
/// Start ─> Counter(+1) ─┬─> LoopGate  (open while Value &lt; Max) ─> back to Counter
///                       └─> ExitGate  (open once Value >= Max) ─> End
/// </code>
///
/// Both gates are pass-through : the counter reads "$previous.Value" the same way whether it is
/// entered from the start or looped back through the gate, the context walking past the gate to
/// the counter instance of the previous turn. The branch of a gate closing its output dies there,
/// which is what ends the loop.
/// </summary>
public class LoopScenario : IScenario
{
    private const int Max = 5;

    public string Name => "Loop";

    public string Description =>
        $"Counter incremented until it reaches {Max}, the gates opening the loop back or the exit. " +
        $"Expected output : Value = {Max} after {Max - 1} turns.";

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

        // Entered from the start then from the loop gate : in both cases "$previous.Value" is the
        // value of the turn before (the input of the workflow on the first one).
        GraphTask counter = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "Counter" },
            InputMappingJson = JsonConvert.SerializeObject(new { Message = "turn", Value = "$previous.Value", Add = 1 })
        };

        GraphTask loopGate = new GraphTask(ScenarioTasks.LoopGate)
        {
            Metadata = new ScopedMetadata() { Name = "LoopGate" },
            InputMappingJson = JsonConvert.SerializeObject(new { Value = "$previous.Value", Max, WhileUnder = true })
        };

        GraphTask exitGate = new GraphTask(ScenarioTasks.LoopGate)
        {
            Metadata = new ScopedMetadata() { Name = "ExitGate" },
            InputMappingJson = JsonConvert.SerializeObject(new { Value = "$previous.Value", Max, WhileUnder = false })
        };

        // The exit gate being pass-through, the end reads the counter of the last turn.
        GraphControl end = new GraphControl(AutomationControl.EndTask)
        {
            Metadata = new ScopedMetadata() { Name = "End" },
            InputMappingJson = JsonConvert.SerializeObject(new { Value = "$previous.Counter.Value", Message = "$previous.Counter.Message" })
        };

        workflow.Graph.Nodes.Add(start);
        workflow.Graph.Nodes.Add(counter);
        workflow.Graph.Nodes.Add(loopGate);
        workflow.Graph.Nodes.Add(exitGate);
        workflow.Graph.Nodes.Add(end);

        workflow.Graph.Connect(start, counter);
        workflow.Graph.Connect(counter, loopGate);
        workflow.Graph.Connect(counter, exitGate);
        workflow.Graph.Connect(loopGate, counter);
        workflow.Graph.Connect(exitGate, end);

        workflow.Graph.Refresh(ScenarioTasks.All);
        return workflow;
    }
}
