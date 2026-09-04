using Automation.Plugins;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Console.Scenarios;

/// <summary>
/// Parallel branches merged by a join, then by the end.
///
/// <code>
/// Start ─┬─> Quick(+1) ───────────────────────┬─> Join ──┐
///        ├─> Slow(delay) ─> Late(+100) ───────┘          ├─> End
///        └─> Sprint(+2) ─────────────────────────────────┘
/// </code>
///
/// With <see cref="WorkflowSettings.StopAtFirstEnd"/> false the end waits for everything : the
/// join runs once Quick and Late are done, then the end once the join and Sprint are.
/// With it true the Sprint branch reaches the end first and cancels the rest : the join is left
/// waiting and what the join would have produced never reaches the end.
/// </summary>
public class BranchScenario : IScenario
{
    private readonly bool _stopAtFirstEnd;

    public BranchScenario(bool stopAtFirstEnd)
    {
        _stopAtFirstEnd = stopAtFirstEnd;
    }

    public string Name => $"Branches (StopAtFirstEnd = {_stopAtFirstEnd})";

    public string Description => _stopAtFirstEnd
        ? "Sprint reaches the end while Slow is still delayed : the workflow is cancelled, " +
          "the join stays waiting. Expected output : Value = 1 + 2 = 3."
        : "Every branch runs, the join merges Quick and Late (which reads through the delay, so " +
          "1 + 100). The end then waits for the join and Sprint, and keeps what the join " +
          "produced : Value = 101.";

    public JToken Input => JToken.FromObject(new ScenarioInput() { Value = 1 });

    public AutomationWorkflow Build()
    {
        AutomationWorkflow workflow = new AutomationWorkflow()
        {
            Id = Guid.NewGuid(),
            Metadata = new ScopedMetadata() { Name = Name },
            InputSchema = JsonSchema.FromType<ScenarioInput>(),
            OutputSchema = JsonSchema.FromType<TestResult>(),
            WorkflowSettings = new WorkflowSettings() { StopAtFirstEnd = _stopAtFirstEnd },
        };

        GraphControl start = new GraphControl(AutomationControl.StartTask)
        {
            Metadata = new ScopedMetadata() { Name = "Start" },
        };

        GraphTask quick = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "Quick" },
            InputMappingJson = JsonConvert.SerializeObject(new { Message = "quick", Value = "$previous.Value", Add = 1 })
        };

        GraphTask slow = new GraphTask(ScenarioTasks.Delay)
        {
            Metadata = new ScopedMetadata() { Name = "Slow" },
            InputMappingJson = JsonConvert.SerializeObject(new TestDelayParameters() { DelayMs = 400 })
        };

        // The delay is pass-through : "$previous" here is still the start.
        GraphTask late = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "Late" },
            InputMappingJson = JsonConvert.SerializeObject(new { Message = "late", Value = "$previous.Value", Add = 100 })
        };

        GraphTask sprint = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "Sprint" },
            InputMappingJson = JsonConvert.SerializeObject(new { Message = "sprint", Value = "$previous.Value", Add = 2 })
        };

        // The join runs once every branch reaching it is completed, its context being indexed
        // by node name.
        GraphControl join = new GraphControl(AutomationControl.JoinTask)
        {
            Metadata = new ScopedMetadata() { Name = "Join" },
            InputMappingJson = JsonConvert.SerializeObject(new
            {
                Value = "$previous.Late.Value",
                Message = "$previous.Quick.Message"
            })
        };

        // The end merges what reaches it the same way, the join and the sprint branch here.
        GraphControl end = new GraphControl(AutomationControl.EndTask)
        {
            Metadata = new ScopedMetadata() { Name = "End" },
            InputMappingJson = JsonConvert.SerializeObject(new
            {
                Value = Reference("Join", "Value"),
                Message = Reference("Join", "Message")
            })
        };

        workflow.Graph.Nodes.Add(start);
        workflow.Graph.Nodes.Add(quick);
        workflow.Graph.Nodes.Add(slow);
        workflow.Graph.Nodes.Add(late);
        workflow.Graph.Nodes.Add(sprint);
        workflow.Graph.Nodes.Add(join);
        workflow.Graph.Nodes.Add(end);

        workflow.Graph.Connect(start, quick);
        workflow.Graph.Connect(start, slow);
        workflow.Graph.Connect(start, sprint);

        workflow.Graph.Connect(slow, late);

        workflow.Graph.Connect(quick, join);
        workflow.Graph.Connect(late, join);

        workflow.Graph.Connect(join, end);
        workflow.Graph.Connect(sprint, end);

        workflow.Graph.Refresh(ScenarioTasks.All);
        return workflow;
    }

    /// <summary>
    /// Reference to a value of the previous node : an end waiting for all of its branches reads
    /// them indexed by node name, while one stopping at the first branch reaching it only sees a
    /// single previous.
    /// </summary>
    private string Reference(string nodeName, string field)
        => _stopAtFirstEnd ? $"$previous.{field}" : $"$previous.{nodeName}.{field}";
}
