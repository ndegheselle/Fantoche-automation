using Fantoche.Services.Local.Models;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Dapper;
using Newtonsoft.Json;

namespace Fantoche.Services.Local.Database;

/// <summary>
/// Minimal content written to the SQLite database the first time it is created: the
/// <see cref="Scope.Root"/> holding every other element, the built-in controls every graph relies on
/// (<see cref="AutomationControl.All"/>), and a "Samples" scope with something to look at and to run.
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
            .. AutomationControl.All,
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
/// Fantoche.Worker.Console/Scenarios) so the interface has something to display and to execute
/// right after a fresh install. They target the classes of the Fantoche.Plugins package, which
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
        Id(0x10), "Test", "Fantoche.Plugins.TestTask",
        Schema(("Message", "string"), ("Value", "integer"), ("Add", "integer")),
        Schema(("Value", "integer"), ("Message", "string")));

    private static readonly AutomationTask Delay = MakeTask(
        Id(0x11), "Delay", "Fantoche.Plugins.TestDelay",
        Schema(("DelayMs", "integer")), EmptySchema, passThrough: true);

    private static readonly AutomationTask PassThrough = MakeTask(
        Id(0x12), "PassThrough", "Fantoche.Plugins.PassThroughTask",
        Schema(("Label", "string")), EmptySchema, passThrough: true);

    private static readonly AutomationTask LoopGate = MakeTask(
        Id(0x13), "LoopGate", "Fantoche.Plugins.LoopGateTask",
        Schema(("Value", "integer"), ("Max", "integer"), ("WhileUnder", "boolean")), EmptySchema, passThrough: true);

    private static readonly AutomationTask CsvImport = MakeTask(
        Id(0x14), "CsvImport", "Fantoche.Plugins.Tables.CsvImportTask",
        Schema(("Path", "string")), TableSchema);

    private static readonly AutomationTask Filter = MakeTask(
        Id(0x15), "Filter", "Fantoche.Plugins.Tables.FilterTask",
        FilterInputSchema, TableSchema);

    private static readonly AutomationTask Pdf = MakeTask(
        Id(0x16), "Pdf", "Fantoche.Plugins.Tables.PdfTask",
        PdfInputSchema, PdfOutputSchema);

    #endregion

    public static IEnumerable<ScopedElement> Build()
    {
        List<ScopedElement> elements =
        [
            SamplesScope,
            Test,
            Delay,
            PassThrough,
            LoopGate,
            CsvImport,
            Filter,
            Pdf,
            BuildLinear(),
            BuildBranches(),
            BuildLoop(),
            BuildExample(),
        ];

        // The schemas of a workflow are declared by its start and its end, the element only holding
        // a copy for whoever reads it without its graph — written here the way storing one writes it.
        foreach (AutomationWorkflow workflow in elements.OfType<AutomationWorkflow>())
            workflow.DeriveSchemas();

        return elements;
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

        GraphControl start = StartNode(0, 0);
        GraphTask first = Node(new GraphTask(Test), "First", 220, 0,
            new { Message = "first", Value = "$previous.Value", Add = 10 });
        GraphControl share = Node(new GraphControl(AutomationControl.ShareTask), "Share", 440, 0,
            new { Bonus = 100, Origin = "$previous.Message" });
        GraphTask passThrough = Node(new GraphTask(PassThrough), "PassThrough", 660, 0,
            new { Label = "after the share" });
        // Both the share and the pass-through are transparent : "$previous" is still "First" here.
        GraphTask second = Node(new GraphTask(Test), "Second", 880, 0,
            new { Message = "second", Value = "$previous.Value", Add = "$shared.Bonus" });
        // An end is not a join : it resolves against the single branch reaching it, so "$previous"
        // is what that branch handed over rather than something indexed by node name.
        GraphControl end = EndNode(1100, 0, new { Value = "$previous.Value", Message = "$previous.Message" });

        Add(workflow, start, first, share, passThrough, second, end);

        workflow.Graph.Connect(start, first);
        workflow.Graph.Connect(first, share);
        workflow.Graph.Connect(share, passThrough);
        workflow.Graph.Connect(passThrough, second);
        workflow.Graph.Connect(second, end);

        return workflow;
    }

    /// <summary>
    /// Parallel branches merged by a join.
    /// <code>
    /// Start -+-> Quick(+1) ------------------+
    ///        +-> Slow(delay) -> Late(+100) --+-> Join -> End
    ///        +-> Sprint(+2) -----------------+
    /// </code>
    /// Every branch runs and the join waits for all three of them. The end hangs off the join alone
    /// rather than racing it : an end closes the workflow as soon as a single branch reaches it and
    /// cancels whatever is still running, so a branch wired straight to it would decide the output
    /// and leave the join waiting forever.
    /// <para>
    /// Expected output : Value = 101 (what Late produced), Message from Quick.
    /// </para>
    /// </summary>
    private static AutomationWorkflow BuildBranches()
    {
        AutomationWorkflow workflow = MakeWorkflow(Id(0x21), "Branches");

        GraphControl start = StartNode(0, 150);
        GraphTask quick = Node(new GraphTask(Test), "Quick", 220, 0,
            new { Message = "quick", Value = "$previous.Value", Add = 1 });
        GraphTask slow = Node(new GraphTask(Delay), "Slow", 220, 150, new { DelayMs = 400 });
        // The delay is pass-through : "$previous" here is still the start.
        GraphTask late = Node(new GraphTask(Test), "Late", 440, 150,
            new { Message = "late", Value = "$previous.Value", Add = 100 });
        GraphTask sprint = Node(new GraphTask(Test), "Sprint", 220, 300,
            new { Message = "sprint", Value = "$previous.Value", Add = 2 });
        // The join runs once every branch reaching it is completed, and it is the only node whose
        // context is indexed by node name : it reads them all at once.
        GraphControl join = Node(new GraphControl(AutomationControl.JoinTask), "Join", 660, 150,
            new { Value = "$previous.Late.Value", Message = "$previous.Quick.Message" });
        // The end resolves against the join alone, so it reads what the join handed over.
        GraphControl end = EndNode(880, 150, new { Value = "$previous.Value", Message = "$previous.Message" });

        Add(workflow, start, quick, slow, late, sprint, join, end);

        workflow.Graph.Connect(start, quick);
        workflow.Graph.Connect(start, slow);
        workflow.Graph.Connect(start, sprint);
        workflow.Graph.Connect(slow, late);
        workflow.Graph.Connect(quick, join);
        workflow.Graph.Connect(late, join);
        workflow.Graph.Connect(sprint, join);
        workflow.Graph.Connect(join, end);

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

        GraphControl start = StartNode(0, 75);
        GraphTask counter = Node(new GraphTask(Test), "Counter", 220, 75,
            new { Message = "turn", Value = "$previous.Value", Add = 1 });
        GraphTask loopGate = Node(new GraphTask(LoopGate), "LoopGate", 440, 0,
            new { Value = "$previous.Value", Max = max, WhileUnder = true });
        GraphTask exitGate = Node(new GraphTask(LoopGate), "ExitGate", 440, 150,
            new { Value = "$previous.Value", Max = max, WhileUnder = false });
        // The exit gate being pass-through, "$previous" here is the counter of the last turn.
        GraphControl end = EndNode(660, 150, new { Value = "$previous.Value", Message = "$previous.Message" });

        Add(workflow, start, counter, loopGate, exitGate, end);

        workflow.Graph.Connect(start, counter);
        workflow.Graph.Connect(counter, loopGate);
        workflow.Graph.Connect(counter, exitGate);
        workflow.Graph.Connect(loopGate, counter);
        workflow.Graph.Connect(exitGate, end);

        return workflow;
    }

    /// <summary>
    /// A minimal real life case : a CSV loaded into a table, its rows filtered, then a PDF saved per
    /// remaining row.
    /// <code>
    /// Start -> Import -> Filter(amount > 10) -> Pdf -> End
    /// </code>
    /// Started with the path of a CSV having a "name" and an "amount" column, e.g.
    /// "name,amount\nalice,10\nbob,20". Expected output : the files bob.pdf and so on.
    /// </summary>
    private static AutomationWorkflow BuildExample()
    {
        AutomationWorkflow workflow = MakeWorkflow(Id(0x23), "Example");

        GraphControl start = StartNode(0, 0, Schema(("Path", "string")));
        GraphTask import = Node(new GraphTask(CsvImport), "Import", 220, 0,
            new { Path = "$previous.Path" });
        GraphTask filter = Node(new GraphTask(Filter), "Filter", 440, 0,
            new
            {
                Source = "$previous",
                Conditions = new[] { new { Left = "$row.columns.amount", Operator = "Greater", Right = 10 } },
                MatchAll = true,
            });
        GraphTask pdf = Node(new GraphTask(Pdf), "Pdf", 660, 0,
            new
            {
                Source = "$previous",
                FileName = "$row.columns.name",
                Title = "$row.columns.name",
                Fields = new[] { new { Label = "Amount", Value = "$row.columns.amount" } },
            });
        GraphControl end = EndNode(880, 0, new { Files = "$previous.Files" }, Schema(("Files", "array")));

        Add(workflow, start, import, filter, pdf, end);

        workflow.Graph.Connect(start, import);
        workflow.Graph.Connect(import, filter);
        workflow.Graph.Connect(filter, pdf);
        workflow.Graph.Connect(pdf, end);

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

    /// <summary>
    /// The package the sample tasks run : it has to be added to the packages for the samples to run.
    /// A property rather than a field : the task fields above are initialized first, and would read a
    /// field declared after them as null.
    /// </summary>
    private static Version PluginsVersion => new("1.1.0");

    /// <summary>
    /// The schemas of the table tasks, with the markers of the engine : a table handle
    /// ("x-fantoche-type") and the fields evaluated per row ("x-fantoche-scope").
    /// </summary>
    private const string TableSchema = """
        {"type":"object","x-fantoche-type":"table","properties":{
            "Id":{"type":"string"},
            "Columns":{"type":"array","items":{"type":"object","properties":{"Name":{"type":"string"},"Type":{"type":"string"}}}}}}
        """;

    private const string FilterInputSchema = """
        {"type":"object","properties":{
            "Source":{"type":"object","x-fantoche-type":"table"},
            "Conditions":{"type":"array","x-fantoche-scope":"row","items":{"type":"object","properties":{
                "Left":{},
                "Operator":{"type":"string","enum":["Equal","NotEqual","Greater","GreaterOrEqual","Less","LessOrEqual","Contains"]},
                "Right":{}}}},
            "MatchAll":{"type":"boolean"}}}
        """;

    private const string PdfInputSchema = """
        {"type":"object","properties":{
            "Source":{"type":"object","x-fantoche-type":"table"},
            "FileName":{"x-fantoche-scope":"row"},
            "Title":{"x-fantoche-scope":"row"},
            "Fields":{"type":"array","x-fantoche-scope":"row","items":{"type":"object","properties":{"Label":{},"Value":{}}}}}}
        """;

    private const string PdfOutputSchema = """
        {"type":"object","properties":{
            "Files":{"type":"array","items":{"type":"object","x-fantoche-type":"file","properties":{"Id":{"type":"string"},"Name":{"type":"string"}}}}}}
        """;

    private static Guid Id(byte index) => new Guid($"00000000-0000-0000-0000-2000000000{index:x2}");

    /// <summary>
    /// The start of a workflow. It declares what it hands over the way every other node declares
    /// what it produces : the control itself knows no shape, and anything reading the graph — a run,
    /// or the preview an editor shows of it — reads "$previous" of the first node from there.
    /// </summary>
    private static GraphControl StartNode(double x, double y, string? schema = null)
        => Node(
            new GraphControl(AutomationControl.StartTask) { OutputSchemaJson = schema ?? WorkflowInputSchema },
            "Start", x, y);

    /// <summary>
    /// The end of a workflow, declaring what it hands back : what an end reads is the output of the
    /// workflow, so the shape is declared on the node producing it.
    /// </summary>
    private static GraphControl EndNode(double x, double y, object mapping, string? schema = null)
        => Node(
            new GraphControl(AutomationControl.EndTask) { InputSchemaJson = schema ?? WorkflowOutputSchema },
            "End", x, y, mapping);

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
                Dll = "Fantoche.Plugins",
                ClassFullName = className,
                Package = new PackageIdentifier() { Id = "Fantoche.Plugins", Version = PluginsVersion }
            },
            InputSchemaJson = inputSchema,
            OutputSchemaJson = outputSchema,
            Settings = new TaskSettings() { IsPassingThrough = passThrough }
        };

    /// <summary>
    /// A workflow with an empty graph. Its schemas are not set here : they are declared by the start
    /// and the end of the graph, and copied onto the element by
    /// <see cref="AutomationWorkflow.DeriveSchemas"/> once it is built.
    /// </summary>
    private static AutomationWorkflow MakeWorkflow(Guid id, string name)
        => new AutomationWorkflow()
        {
            Id = id,
            ParentId = SamplesScope.Id,
            Metadata = new ScopedMetadata(name, EnumScopedType.Workflow) { Tags = ["Sample"] },
        };

    /// <summary>
    /// Name a node, place it on the canvas and give it the mapping it runs with. The
    /// metadata comes cloned from the task, so only the name changes : the icon stays the one of
    /// what the node points to.
    /// </summary>
    private static TNode Node<TNode>(TNode node, string name, double x, double y, object? mapping = null)
        where TNode : BaseGraphTask
    {
        node.Metadata.Name = name;
        node.LocationX = x;
        node.LocationY = y;
        if (mapping != null)
            node.InputTemplateJson = JsonConvert.SerializeObject(mapping);
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
