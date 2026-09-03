using Automation.Services.Local.Models;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Dapper;
using Newtonsoft.Json;

namespace Automation.Services.Local.Database;

/// <summary>
/// Minimal content written to the SQLite database the first time it is created: the
/// <see cref="Scope.Root"/> holding every other element, the built-in
/// <see cref="AutomationControl.StartTask"/>/<see cref="AutomationControl.EndTask"/> elements every
/// graph relies on, and a "Samples" scope with something to look at and to run.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Write the starting content, unless the tree already holds something : the seed runs once, on
    /// a database that has never been written to, so whatever was removed since stays removed.
    /// </summary>
    public static void Seed(DatabaseFactory factory)
    {
        using var connection = factory.Create();

        if (connection.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM Scoped);"))
            return;

        // The controls hang under their own scope and the samples under theirs, so the elements are
        // written in the order they hang under each other : a parent is there before its children.
        List<ScopedElement> elements =
        [
            Scope.Root,
            Scope.Controls,
            AutomationControl.StartTask,
            AutomationControl.EndTask,
            AutomationControl.ShareTask,
            AutomationControl.JoinTask,
            .. Samples.Build(),
        ];

        using var transaction = connection.BeginTransaction();

        connection.Execute(
            ScopedModel.InsertQuery,
            elements.Select(ScopedModel.From).ToList(),
            transaction);

        // The graphs are written the way an edit writes them. Waiting on them here is harmless :
        // nothing else can be reading the database before the seed is done.
        foreach (AutomationWorkflow workflow in elements.OfType<AutomationWorkflow>())
            GraphStore.ReplaceAsync(connection, transaction, workflow).GetAwaiter().GetResult();

        transaction.Commit();
    }
}

/// <summary>
/// Sample tasks and workflows, the same graphs the console runs as its scenarios (see
/// Automation.Worker.Console/Scenarios) so the interface has something to display and to execute
/// right after a fresh install. They target the classes of the Automation.Plugins package, which
/// ships with the application.
/// </summary>
internal static class Samples
{
    /// <summary>
    /// Scope holding the samples. The ids are fixed rather than generated : the seed only runs
    /// once, but a database built by a previous version stays comparable to a new one.
    /// </summary>
    private static readonly Scope SamplesScope = new Scope()
    {
        Id = Id(0x01),
        ParentId = Scope.Root.Id,
        Metadata = new ScopedMetadata("Samples", EnumScopedType.Scope),
    };

    #region Tasks

    /// <summary>Adds <c>Add</c> to <c>Value</c> and tags the message : the only task producing an output.</summary>
    private static readonly AutomationTask Test = MakeTask(
        Id(0x10), "Test", "Automation.Plugins.TestTask",
        Schema(("Message", "string"), ("Value", "integer"), ("Add", "integer")),
        Schema(("Value", "integer"), ("Message", "string")));

    private static readonly AutomationTask Delay = MakeTask(
        Id(0x11), "Delay", "Automation.Plugins.TestDelay",
        Schema(("DelayMs", "integer")), EmptySchema, passThrough: true);

    private static readonly AutomationTask PassThrough = MakeTask(
        Id(0x12), "PassThrough", "Automation.Plugins.PassThroughTask",
        Schema(("Label", "string")), EmptySchema, passThrough: true);

    private static readonly AutomationTask LoopGate = MakeTask(
        Id(0x13), "LoopGate", "Automation.Plugins.LoopGateTask",
        Schema(("Value", "integer"), ("Max", "integer"), ("WhileUnder", "boolean")), EmptySchema, passThrough: true);

    #endregion

    public static IEnumerable<ScopedElement> Build()
    {
        return
        [
            SamplesScope,
            Test,
            Delay,
            PassThrough,
            LoopGate,
            BuildLinear(),
            BuildBranches(),
            BuildLoop(),
        ];
    }

    /// <summary>
    /// The straight line : one branch, every node running once.
    /// <code>
    /// Start -> First(+10) -> Share -> PassThrough -> Second(+$shared.Bonus) -> End
    /// </code>
    /// The share puts its parameters in the shared context, and both the share and the pass-through
    /// are transparent to "$previous.*". Expected output : Value = 1 + 10 + 100 = 111.
    /// </summary>
    private static AutomationWorkflow BuildLinear()
    {
        AutomationWorkflow workflow = MakeWorkflow(Id(0x20), "Linear");

        GraphControl start = Node(new GraphControl(AutomationControl.StartTask), "Start", 0, 0);
        GraphTask first = Node(new GraphTask(Test), "First", 220, 0,
            new { Message = "first", Value = "$previous.Value", Add = 10 });
        GraphControl share = Node(new GraphControl(AutomationControl.ShareTask), "Share", 440, 0,
            new { Bonus = 100, Origin = "$previous.Message" });
        GraphTask passThrough = Node(new GraphTask(PassThrough), "PassThrough", 660, 0,
            new { Label = "after the share" });
        // Both the share and the pass-through are transparent : "$previous" is still "First" here.
        GraphTask second = Node(new GraphTask(Test), "Second", 880, 0,
            new { Message = "second", Value = "$previous.Value", Add = "$shared.Bonus" });
        // The end merges every branch reaching it, so its context is indexed by node name.
        GraphControl end = Node(new GraphControl(AutomationControl.EndTask), "End", 1100, 0,
            new { Value = "$previous.Second.Value", Message = "$previous.Second.Message" });

        Add(workflow, start, first, share, passThrough, second, end);

        workflow.Graph.Connect(start, first);
        workflow.Graph.Connect(first, share);
        workflow.Graph.Connect(share, passThrough);
        workflow.Graph.Connect(passThrough, second);
        workflow.Graph.Connect(second, end);

        return workflow;
    }

    /// <summary>
    /// Parallel branches, a join and two ends racing each other.
    /// <code>
    /// Start -+-> Quick(+1) ------------------+-> Join -> EndJoin
    ///        +-> Slow(delay) -> Late(+100) --+
    ///        +-> Sprint(+2) -------------------------------> EndSprint
    /// </code>
    /// Every branch runs, the join waiting for Quick and Late. Expected output : Value = 101.
    /// Turning on "Stop at first end" makes the Sprint branch cancel the rest instead, the join
    /// then being left waiting (the ends have to be read as "$previous.*" in that case).
    /// </summary>
    private static AutomationWorkflow BuildBranches()
    {
        AutomationWorkflow workflow = MakeWorkflow(Id(0x21), "Branches");

        GraphControl start = Node(new GraphControl(AutomationControl.StartTask), "Start", 0, 150);
        GraphTask quick = Node(new GraphTask(Test), "Quick", 220, 0,
            new { Message = "quick", Value = "$previous.Value", Add = 1 });
        GraphTask slow = Node(new GraphTask(Delay), "Slow", 220, 150, new { DelayMs = 400 });
        // The delay is pass-through : "$previous" here is still the start.
        GraphTask late = Node(new GraphTask(Test), "Late", 440, 150,
            new { Message = "late", Value = "$previous.Value", Add = 100 });
        GraphTask sprint = Node(new GraphTask(Test), "Sprint", 220, 300,
            new { Message = "sprint", Value = "$previous.Value", Add = 2 });
        // The join runs once every branch reaching it is completed, its context being indexed by
        // node name.
        GraphControl join = Node(new GraphControl(AutomationControl.JoinTask), "Join", 660, 75,
            new { Value = "$previous.Late.Value", Message = "$previous.Quick.Message" });
        GraphControl endJoin = Node(new GraphControl(AutomationControl.EndTask), "EndJoin", 880, 75,
            new { Value = "$previous.Join.Value", Message = "$previous.Join.Message" });
        GraphControl endSprint = Node(new GraphControl(AutomationControl.EndTask), "EndSprint", 880, 300,
            new { Value = "$previous.Sprint.Value", Message = "$previous.Sprint.Message" });

        Add(workflow, start, quick, slow, late, sprint, join, endJoin, endSprint);

        workflow.Graph.Connect(start, quick);
        workflow.Graph.Connect(start, slow);
        workflow.Graph.Connect(start, sprint);
        workflow.Graph.Connect(slow, late);
        workflow.Graph.Connect(quick, join);
        workflow.Graph.Connect(late, join);
        workflow.Graph.Connect(join, endJoin);
        workflow.Graph.Connect(sprint, endSprint);

        return workflow;
    }

    /// <summary>
    /// A cycle in the graph, closed on the data it produces.
    /// <code>
    /// Start -> Counter(+1) -+-> LoopGate (open while Value under Max) -> back to Counter
    ///                       +-> ExitGate (open once Value reached Max) -> End
    /// </code>
    /// Both gates are pass-through, so the counter reads "$previous.Value" the same way whether it
    /// is entered from the start or looped back. Expected output : Value = 5 after 4 turns.
    /// </summary>
    private static AutomationWorkflow BuildLoop()
    {
        const int max = 5;
        AutomationWorkflow workflow = MakeWorkflow(Id(0x22), "Loop");

        GraphControl start = Node(new GraphControl(AutomationControl.StartTask), "Start", 0, 75);
        GraphTask counter = Node(new GraphTask(Test), "Counter", 220, 75,
            new { Message = "turn", Value = "$previous.Value", Add = 1 });
        GraphTask loopGate = Node(new GraphTask(LoopGate), "LoopGate", 440, 0,
            new { Value = "$previous.Value", Max = max, WhileUnder = true });
        GraphTask exitGate = Node(new GraphTask(LoopGate), "ExitGate", 440, 150,
            new { Value = "$previous.Value", Max = max, WhileUnder = false });
        // The exit gate being pass-through, the end reads the counter of the last turn.
        GraphControl end = Node(new GraphControl(AutomationControl.EndTask), "End", 660, 150,
            new { Value = "$previous.Counter.Value", Message = "$previous.Counter.Message" });

        Add(workflow, start, counter, loopGate, exitGate, end);

        workflow.Graph.Connect(start, counter);
        workflow.Graph.Connect(counter, loopGate);
        workflow.Graph.Connect(counter, exitGate);
        workflow.Graph.Connect(loopGate, counter);
        workflow.Graph.Connect(exitGate, end);

        return workflow;
    }

    #region Building blocks

    private const string EmptySchema = "{\"type\":\"object\"}";

    /// <summary>
    /// The workflows all take a single "Value" and give back what the Test task produces, so a run
    /// can be started with nothing but a number.
    /// </summary>
    private static readonly string WorkflowInputSchema = Schema(("Value", "integer"));
    private static readonly string WorkflowOutputSchema = Schema(("Value", "integer"), ("Message", "string"));

    private static Guid Id(byte index) => new Guid($"00000000-0000-0000-0000-2000000000{index:x2}");

    private static AutomationTask MakeTask(
        Guid id,
        string name,
        string className,
        string inputSchema,
        string outputSchema,
        bool passThrough = false)
        => new AutomationTask()
        {
            Id = id,
            ParentId = SamplesScope.Id,
            Metadata = new ScopedMetadata(name, EnumScopedType.Task) { Tags = ["Sample"] },
            Target = new PackageClassTarget()
            {
                Dll = "Automation.Plugins",
                ClassFullName = className,
                Package = new PackageIdentifier() { Id = "Automation.Plugins", Version = new Version("1.0.2") }
            },
            InputSchemaJson = inputSchema,
            OutputSchemaJson = outputSchema,
            Settings = new TaskSettings() { IsPassingThrough = passThrough }
        };

    private static AutomationWorkflow MakeWorkflow(Guid id, string name)
        => new AutomationWorkflow()
        {
            Id = id,
            ParentId = SamplesScope.Id,
            Metadata = new ScopedMetadata(name, EnumScopedType.Workflow) { Tags = ["Sample"] },
            InputSchemaJson = WorkflowInputSchema,
            OutputSchemaJson = WorkflowOutputSchema,
        };

    /// <summary>
    /// Name a node, place it on the canvas and give it the parameters template it runs with. The
    /// metadata comes cloned from the task, so only the name changes : the icon stays the one of
    /// what the node points to.
    /// </summary>
    private static TNode Node<TNode>(TNode node, string name, double x, double y, object? parameters = null)
        where TNode : BaseGraphTask
    {
        node.Metadata.Name = name;
        node.LocationX = x;
        node.LocationY = y;
        if (parameters != null)
            node.ParametersJson = JsonConvert.SerializeObject(parameters);
        return node;
    }

    private static void Add(AutomationWorkflow workflow, params GraphNode[] nodes)
    {
        foreach (GraphNode node in nodes)
            workflow.Graph.Nodes.Add(node);
    }

    /// <summary>
    /// JSON schema of an object with [properties], written by hand rather than generated from a
    /// type : the seed knows the plugin tasks by their class name only, like any other package.
    /// </summary>
    private static string Schema(params (string Name, string Type)[] properties)
    {
        string content = string.Join(",", properties.Select(x => $"\"{x.Name}\":{{\"type\":\"{x.Type}\"}}"));
        return $"{{\"type\":\"object\",\"properties\":{{{content}}}}}";
    }

    #endregion
}
