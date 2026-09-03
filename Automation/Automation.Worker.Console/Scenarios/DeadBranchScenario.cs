using Automation.Plugins;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Console.Scenarios;

/// <summary>
/// Which branches a conditional closes, so in which order the join has to settle them.
/// </summary>
public enum EnumDeadBranch
{
    /// <summary>
    /// The dead branch dies before the live one reaches the join : the join sees it dead as it
    /// gathers its branches.
    /// </summary>
    DiesFirst,
    /// <summary>
    /// The live branch reaches the join first and holds : the join is resumed by the death of the
    /// other one, nothing else being left to reach it.
    /// </summary>
    DiesLast,
    /// <summary>
    /// Both branches die : nothing ever reaches the join, so neither it nor the end after it
    /// runs, and the workflow ends without data instead of hanging.
    /// </summary>
    Both,
}

/// <summary>
/// A conditional closing a branch that a join is waiting on.
///
/// <code>
/// Start ─┬─> DelayA ─> ConditionalA ─> A(+10) ───┬─> Join ─> End
///        └─> DelayB ─> ConditionalB ─> B(+1000) ─┘
/// </code>
///
/// A conditional deactivating its output kills its branch : the tasks after it never run. The
/// join has to crawl the graph to tell such a branch from one still working, or it would hold on
/// a node that can't ever reach it. The two delays only decide which branch settles first, the
/// outcome of the join being the same either way.
/// </summary>
public class DeadBranchScenario : IScenario
{
    private const int SlowMs = 400;

    private readonly EnumDeadBranch _deadBranch;

    public DeadBranchScenario(EnumDeadBranch deadBranch)
    {
        _deadBranch = deadBranch;
    }

    public string Name => $"Dead branch ({_deadBranch})";

    public string Description => _deadBranch switch
    {
        EnumDeadBranch.DiesFirst =>
            "B is closed and dies right away, A reaches the join afterwards : the join runs on A " +
            "alone. Expected output : Value = 1 + 10 = 11.",
        EnumDeadBranch.DiesLast =>
            "A reaches the join first and waits, B is closed behind a delay : the death of B " +
            "resumes the join, which runs on A alone. Expected output : Value = 1 + 10 = 11.",
        _ =>
            "Both branches are closed : nothing ever reaches the join, so neither it nor the end " +
            "runs. Expected output : none, the workflow completing without data.",
    };

    public JToken Input => JToken.FromObject(new ScenarioInput() { Value = 1 });

    public AutomationWorkflow Build()
    {
        bool bothDead = _deadBranch == EnumDeadBranch.Both;

        AutomationWorkflow workflow = new AutomationWorkflow()
        {
            Id = Guid.NewGuid(),
            Metadata = new ScopedMetadata() { Name = Name },
            InputSchema = JsonSchema.FromType<ScenarioInput>(),
            // Nothing reaches the end when every branch is dead : the workflow has no output to
            // promise anymore.
            OutputSchema = bothDead ? null : JsonSchema.FromType<TestResult>(),
        };

        GraphControl start = new GraphControl(AutomationControl.StartTask)
        {
            Metadata = new ScopedMetadata() { Name = "Start" },
        };

        // The delays only stagger the two branches, they are pass-through : "$previous" stays the
        // start for the tasks after them.
        GraphTask delayA = Delay("DelayA", _deadBranch == EnumDeadBranch.DiesFirst ? SlowMs : 0);
        GraphTask delayB = Delay("DelayB", _deadBranch == EnumDeadBranch.DiesLast ? SlowMs : 0);

        GraphTask conditionalA = Conditional("ConditionalA", closed: bothDead);
        GraphTask conditionalB = Conditional("ConditionalB", closed: true);

        GraphTask a = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "A" },
            ParametersJson = JsonConvert.SerializeObject(new { Message = "a", Value = "$previous.Value", Add = 10 })
        };

        // Never runs : the conditional before it closed its branch.
        GraphTask b = new GraphTask(ScenarioTasks.Test)
        {
            Metadata = new ScopedMetadata() { Name = "B" },
            ParametersJson = JsonConvert.SerializeObject(new { Message = "b", Value = "$previous.Value", Add = 1000 })
        };

        // Only reads the live branch : a reference into a dead one is never resolved, that branch
        // having produced nothing.
        GraphControl join = new GraphControl(AutomationControl.JoinTask)
        {
            Metadata = new ScopedMetadata() { Name = "Join" },
            ParametersJson = JsonConvert.SerializeObject(new
            {
                Value = "$previous.A.Value",
                Message = "$previous.A.Message"
            })
        };

        GraphControl end = new GraphControl(AutomationControl.EndTask)
        {
            Metadata = new ScopedMetadata() { Name = "End" },
            ParametersJson = JsonConvert.SerializeObject(new
            {
                Value = "$previous.Join.Value",
                Message = "$previous.Join.Message"
            })
        };

        workflow.Graph.Nodes.Add(start);
        workflow.Graph.Nodes.Add(delayA);
        workflow.Graph.Nodes.Add(delayB);
        workflow.Graph.Nodes.Add(conditionalA);
        workflow.Graph.Nodes.Add(conditionalB);
        workflow.Graph.Nodes.Add(a);
        workflow.Graph.Nodes.Add(b);
        workflow.Graph.Nodes.Add(join);
        workflow.Graph.Nodes.Add(end);

        workflow.Graph.Connect(start, delayA);
        workflow.Graph.Connect(start, delayB);

        workflow.Graph.Connect(delayA, conditionalA);
        workflow.Graph.Connect(delayB, conditionalB);

        workflow.Graph.Connect(conditionalA, a);
        workflow.Graph.Connect(conditionalB, b);

        workflow.Graph.Connect(a, join);
        workflow.Graph.Connect(b, join);

        workflow.Graph.Connect(join, end);

        workflow.Graph.Refresh(ScenarioTasks.All);
        return workflow;
    }

    private static GraphTask Delay(string name, int delayMs) => new GraphTask(ScenarioTasks.Delay)
    {
        Metadata = new ScopedMetadata() { Name = name },
        ParametersJson = JsonConvert.SerializeObject(new TestDelayParameters() { DelayMs = delayMs })
    };

    private static GraphTask Conditional(string name, bool closed) => new GraphTask(ScenarioTasks.Conditional)
    {
        Metadata = new ScopedMetadata() { Name = name },
        ParametersJson = JsonConvert.SerializeObject(new ConditionalParameters() { TestDeactivate = closed })
    };
}
