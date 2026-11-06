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

        // The graphs are written the way a creation writes them. Waiting on them here is harmless :
        // nothing else can be reading the database before the seed is done.
        foreach (AutomationWorkflow workflow in elements.OfType<AutomationWorkflow>())
            GraphStore.CreateAsync(connection, transaction, workflow).GetAwaiter().GetResult();

        transaction.Commit();
    }
}

/// <summary>
/// Sample tasks and workflows showing them at work, so the interface has something to display and
/// to execute right after a fresh install. They target the classes of the Fantoche.Plugins.Example
/// and Fantoche.Plugins packages, which have to be added to the packages for the samples to run.
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

    /// <summary>The samples of the engine itself : runs slow enough to be paused, stopped and resumed.</summary>
    private static readonly Scope EngineScope = new Scope()
    {
        Id = Id(0x02),
        ParentId = SamplesScope.Id,
        Metadata = new ScopedMetadata("Engine", EnumScopedType.Scope),
    };

    #region Tasks

    private static readonly AutomationTask ImportCsv = MakeTask(
        Id(0x30), "Import CSV", "Fantoche.Plugins.Example.Tasks.Tables.ImportCsvTask",
        Schema(("Path", "string"), ("Name", "string")), TableSchema);

    private static readonly AutomationTask MapColumns = MakeTask(
        Id(0x32), "Map columns", "Fantoche.Plugins.Example.Tasks.Tables.MapColumnsTask",
        MapColumnsInputSchema, TableSchema, hasPage: true);

    private static readonly AutomationTask Merge = MakeTask(
        Id(0x33), "Merge", "Fantoche.Plugins.Example.Tasks.Tables.MergeTask",
        MergeInputSchema, TableSchema);

    private static readonly AutomationTask Update = MakeTask(
        Id(0x34), "Update", "Fantoche.Plugins.Example.Tasks.Tables.UpdateTask",
        UpdateInputSchema, TableSchema);

    private static readonly AutomationTask Split = MakeTask(
        Id(0x35), "Split", "Fantoche.Plugins.Example.Tasks.Tables.SplitTask",
        SplitInputSchema, SplitOutputSchema);

    private static readonly AutomationTask ExportPdf = MakeTask(
        Id(0x37), "Export PDF", "Fantoche.Plugins.Example.Tasks.Pdf.ExportPdfTask",
        ExportPdfInputSchema, ExportPdfOutputSchema, hasPage: true);

    private static readonly AutomationTask AddValue = MakeEngineTask(
        Id(0x40), "Add", "Fantoche.Plugins.TestTask",
        Schema(("Message", "string"), ("Value", "integer"), ("Add", "integer")),
        Schema(("Value", "integer"), ("Message", "string")));

    private static readonly AutomationTask Delay = MakeEngineTask(
        Id(0x41), "Delay", "Fantoche.Plugins.TestDelay",
        Schema(("DelayMs", "integer")), PassThroughSchema, isPassThrough: true);

    private static readonly AutomationTask LoopGate = MakeEngineTask(
        Id(0x42), "Loop gate", "Fantoche.Plugins.LoopGateTask",
        Schema(("Value", "integer"), ("Max", "integer"), ("WhileUnder", "boolean")), PassThroughSchema, isPassThrough: true);

    private static readonly AutomationTask Conditional = MakeEngineTask(
        Id(0x43), "Conditional", "Fantoche.Plugins.ConditionalTask",
        Schema(("TestDeactivate", "boolean")), PassThroughSchema, isPassThrough: true);

    private static readonly AutomationTask Reorder = MakeEngineTask(
        Id(0x44), "Reorder", "Fantoche.Plugins.UserInterfaces.ReorderTask",
        ReorderInputSchema, ItemsSchema, hasPage: true);

    #endregion

    public static IEnumerable<ScopedElement> Build()
    {
        List<ScopedElement> elements =
        [
            SamplesScope,
            ImportCsv,
            MapColumns,
            Merge,
            Update,
            Split,
            ExportPdf,
            BuildCustomersOrders(),
            EngineScope,
            AddValue,
            Delay,
            LoopGate,
            Conditional,
            Reorder,
            BuildStates(),
        ];

        // The schemas of a workflow are declared by its start and its end, the element only holding
        // a copy for whoever reads it without its graph — written here the way storing one writes it.
        foreach (AutomationWorkflow workflow in elements.OfType<AutomationWorkflow>())
            workflow.DeriveSchemas();

        return elements;
    }

    /// <summary>
    /// Two files mapped and merged, the large orders flagged, then a PDF per batch of the orders.
    /// <code>
    /// Start ─┬─> Import customers ─> Customers ─┬─> Tables ─> Merge ─> Update ─> Split ─> Export ─> End
    ///        │                          │      │                                            │
    ///        │                     Pick the columns                               Compose the document
    ///        └─> Import orders ────> Orders ────┘
    ///                                   │
    ///                              Pick the columns
    /// </code>
    /// Started with the paths of the two files the script of Fantoche.Plugins.Example/Resources/Samples
    /// generates. The mappings and the export ask the user on the pages attached under them ; with
    /// the pages removed the run is unattended, the columns being the ones of those files. The nodes
    /// after the mapping read the columns by their names ("$previous.Customers.Mapping.CustomerId",
    /// "$row.columns.Amount"), whatever the files call them.
    /// </summary>
    private static AutomationWorkflow BuildCustomersOrders()
    {
        AutomationWorkflow workflow = MakeWorkflow(Id(0x24), "Customers and orders");

        GraphControl start = StartNode(0, 150, Schema(("Customers", "string"), ("Orders", "string")));

        GraphTask importCustomers = Node(new GraphTask(ImportCsv), "Import customers", 440, 0,
            new { Path = "$previous.Customers" });
        GraphTask customers = Node(new GraphTask(MapColumns), "Customers", 660, 0,
            new
            {
                Source = "$previous",
                Columns = new[] { Column("CustomerId", "client id"), Column("Name", "full name"), Column("City", "town") },
            });
        GraphUserInterface customersPage = Node(new GraphUserInterface(MapColumns), "Pick the customers columns", 660, 120);

        GraphTask importOrders = Node(new GraphTask(ImportCsv), "Import orders", 440, 300,
            new { Path = "$previous.Orders" });
        GraphTask orders = Node(new GraphTask(MapColumns), "Orders", 660, 300,
            new
            {
                Source = "$previous",
                Columns = new[] { Column("OrderId", "order no"), Column("CustomerId", "client"), Column("Amount", "total"), Column("Date", "ordered on") },
            });
        GraphUserInterface ordersPage = Node(new GraphUserInterface(MapColumns), "Pick the orders columns", 660, 420);

        GraphControl tables = Node(new GraphControl(AutomationControl.JoinTask), "Tables", 880, 150,
            new { Customers = "$previous.Customers", Orders = "$previous.Orders" });
        GraphTask merge = Node(new GraphTask(Merge), "Merge", 1100, 150,
            new
            {
                Left = "$previous.Orders",
                Right = "$previous.Customers",
                On = new[] { new { Left = "$previous.Orders.Mapping.CustomerId", Right = "$previous.Customers.Mapping.CustomerId" } },
                Kind = "Inner",
            });
        GraphTask update = Node(new GraphTask(Update), "Update", 1320, 150,
            new
            {
                Source = "$previous",
                Conditions = new[] { new { Left = "$row.columns.Amount", Operator = "GreaterOrEqual", Right = 500 } },
                MatchAll = true,
                Set = new[] { new { Column = "Priority", Value = "high" } },
            });
        GraphTask split = Node(new GraphTask(Split), "Split", 1540, 150,
            new { Source = "$previous", BatchSize = 10000 });
        GraphTask export = Node(new GraphTask(ExportPdf), "Export", 1760, 150,
            new
            {
                Sources = "$previous.Batches",
                Document = new { Landscape = true, Blocks = Array.Empty<object>() },
                FileName = "orders-{index}",
            });
        GraphUserInterface exportPage = Node(new GraphUserInterface(ExportPdf), "Compose the document", 1760, 270);
        GraphControl end = EndNode(1980, 150, new { Files = "$previous.Files" }, Schema(("Files", "array")));

        Add(workflow, start, importCustomers, customers, customersPage, importOrders, orders, ordersPage,
            tables, merge, update, split, export, exportPage, end);

        workflow.Graph.Connect(start, importCustomers);
        workflow.Graph.Connect(importCustomers, customers);
        workflow.Graph.Connect(customers, tables);
        workflow.Graph.Connect(start, importOrders);
        workflow.Graph.Connect(importOrders, orders);
        workflow.Graph.Connect(orders, tables);
        workflow.Graph.Connect(tables, merge);
        workflow.Graph.Connect(merge, update);
        workflow.Graph.Connect(update, split);
        workflow.Graph.Connect(split, export);
        workflow.Graph.Connect(export, end);

        workflow.Graph.Attach(customers, customersPage);
        workflow.Graph.Attach(orders, ordersPage);
        workflow.Graph.Attach(export, exportPage);

        return workflow;
    }

    /// <summary>
    /// What the engine shows of a run, slow enough to pause, stop and resume it on the way : a loop,
    /// a branch waiting, a join waiting for it, a branch closed and a page waiting for the user.
    /// <code>
    /// Start ─┬─> Wait 3 s ─> Count(+1) ─> Wait 2 s ─┬─> Again (under Max) ─> back to Count
    ///        │                                       └─> Done (reached Max) ──┐
    ///        ├─> Wait 15 s ─> Slow(+100) ────────────────────────────────────┼─> Results ─> Sort ─> End
    ///        │                                                                              │
    ///        └─> Closed ─> Never                                                      Put in order
    /// </code>
    /// The loop counts from the value the run starts with (1 by default) up to 5, a turn every
    /// 2 s, while the slow branch holds the join until its 15 s are over. The closed branch never
    /// gets further than its condition.
    /// </summary>
    private static AutomationWorkflow BuildStates()
    {
        const int Max = 5;
        AutomationWorkflow workflow = MakeWorkflow(Id(0x25), "Pause, stop and resume", EngineScope.Id);

        GraphControl start = StartNode(0, 150, Schema(("Value", "integer")), new { Value = 1 });

        GraphTask firstWait = Node(new GraphTask(Delay), "Wait 3 s", 220, 0, new { DelayMs = 3000 });
        GraphTask count = Node(new GraphTask(AddValue), "Count", 440, 0,
            new { Message = "count", Value = "$previous.Value", Add = 1 });
        GraphTask turnWait = Node(new GraphTask(Delay), "Wait 2 s", 660, 0, new { DelayMs = 2000 });
        GraphTask again = Node(new GraphTask(LoopGate), "Again", 880, -60,
            new { Value = "$previous.Value", Max, WhileUnder = true });
        GraphTask done = Node(new GraphTask(LoopGate), "Done", 880, 60,
            new { Value = "$previous.Value", Max, WhileUnder = false });

        GraphTask slowWait = Node(new GraphTask(Delay), "Wait 15 s", 220, 150, new { DelayMs = 15000 });
        GraphTask slow = Node(new GraphTask(AddValue), "Slow", 440, 150,
            new { Message = "slow", Value = "$previous.Value", Add = 100 });

        GraphTask closed = Node(new GraphTask(Conditional), "Closed", 220, 300, new { TestDeactivate = true });
        GraphTask never = Node(new GraphTask(AddValue), "Never", 440, 300,
            new { Message = "never", Value = "$previous.Value", Add = 1000 });

        GraphControl results = Node(new GraphControl(AutomationControl.JoinTask), "Results", 1100, 150,
            new { Items = new[] { "$previous.Count.Message", "$previous.Slow.Message" } });
        GraphTask sort = Node(new GraphTask(Reorder), "Sort", 1320, 150,
            new { Title = "Put the branches in the order you like", Items = "$previous.Items" });
        GraphUserInterface sortPage = Node(new GraphUserInterface(Reorder), "Put in order", 1320, 270);
        GraphControl end = EndNode(1540, 150, new { Items = "$previous.Items" }, Schema(("Items", "array")));

        Add(workflow, start, firstWait, count, turnWait, again, done, slowWait, slow, closed, never,
            results, sort, sortPage, end);

        workflow.Graph.Connect(start, firstWait);
        workflow.Graph.Connect(firstWait, count);
        workflow.Graph.Connect(count, turnWait);
        workflow.Graph.Connect(turnWait, again);
        workflow.Graph.Connect(turnWait, done);
        workflow.Graph.Connect(again, count);
        workflow.Graph.Connect(done, results);
        workflow.Graph.Connect(start, slowWait);
        workflow.Graph.Connect(slowWait, slow);
        workflow.Graph.Connect(slow, results);
        workflow.Graph.Connect(start, closed);
        workflow.Graph.Connect(closed, never);
        workflow.Graph.Connect(results, sort);
        workflow.Graph.Connect(sort, end);

        workflow.Graph.Attach(sort, sortPage);

        return workflow;
    }

    /// <summary>A name of the workflow and the column of the sample files it stands for.</summary>
    private static object Column(string name, string column) => new { Name = name, Column = column };

    #region Building blocks

    /// <summary>
    /// The package the table samples run : it has to be added to the packages for the samples to run.
    /// Its newest version rather than a fixed one, so the samples keep running as the plugins get
    /// new versions. A property rather than a field : the task fields above are initialized first,
    /// and would read a field declared after them as null.
    /// </summary>
    private static PackageIdentifier PluginsPackage => new() { Id = "Fantoche.Plugins.Example", Version = null };

    /// <summary>The package of the engine samples, see <see cref="PluginsPackage"/>.</summary>
    private static PackageIdentifier EnginePackage => new() { Id = "Fantoche.Plugins", Version = null };

    /// <summary>
    /// The schemas of the tasks, with the markers of the engine : a table or file handle
    /// ("x-fantoche-type"), the fields evaluated per row with the property holding their table
    /// ("x-fantoche-scope"), what a task hands over of its input ("x-fantoche-output",
    /// "x-fantoche-output-mapping", see OutputPreview). The schemas they share are written once, put in place of their "&lt;name&gt;".
    /// </summary>
    private const string TableSchema = """
        {"type":"object","x-fantoche-type":"table","properties":{
            "Id":{"type":"string"},
            "Name":{"type":"string"},
            "Columns":{"type":"array","items":{"type":"object","properties":{"Name":{"type":"string"},"Type":{"type":"string"}}}},
            "Mapping":{"type":"object","additionalProperties":{"type":"string"}}}}
        """;

    /// <summary>An item of a list of <c>ColumnMapping</c>.</summary>
    private const string ColumnMappingSchema = """
        {"type":"object","properties":{"Name":{"type":"string"},"Column":{"type":"string"}}}
        """;

    private static string MapColumnsInputSchema => """
        {"type":"object","properties":{
            "Source":{"type":"object","x-fantoche-type":"table","x-fantoche-output":""},
            "Columns":{"type":"array","items":<column mapping>,"x-fantoche-output-mapping":""}}}
        """
        .Replace("<column mapping>", ColumnMappingSchema);

    private const string MergeInputSchema = """
        {"type":"object","properties":{
            "Left":{"type":"object","x-fantoche-type":"table","x-fantoche-output":""},
            "Right":{"type":"object","x-fantoche-type":"table","x-fantoche-output":""},
            "On":{"type":"array","items":{"type":"object","properties":{"Left":{"type":"string"},"Right":{"type":"string"}}}},
            "Kind":{"type":"string","enum":["Inner","Left"]}}}
        """;

    private const string UpdateInputSchema = """
        {"type":"object","properties":{
            "Source":{"type":"object","x-fantoche-type":"table","x-fantoche-output":""},
            "Conditions":{"type":"array","x-fantoche-scope":"Source","items":{"type":"object","properties":{
                "Left":{},
                "Operator":{"type":"string","enum":["Equal","NotEqual","Greater","GreaterOrEqual","Less","LessOrEqual","Contains"]},
                "Right":{}}}},
            "MatchAll":{"type":"boolean"},
            "Set":{"type":"array","x-fantoche-scope":"Source","items":{"type":"object","properties":{"Column":{"type":"string"},"Value":{}}}}}}
        """;

    private const string SplitInputSchema = """
        {"type":"object","properties":{
            "Source":{"type":"object","x-fantoche-type":"table"},
            "BatchSize":{"type":"integer"}}}
        """;

    private static string SplitOutputSchema => """
        {"type":"object","properties":{"Batches":{"type":"array","items":<table>}}}
        """
        .Replace("<table>", TableSchema);

    /// <summary>The properties of a <c>PdfLayout</c>, put in an object of its own or in a property declaring more.</summary>
    private const string PdfLayoutProperties = """
        "type":"object","properties":{
            "Landscape":{"type":"boolean"},
            "Blocks":{"type":"array","items":{"type":"object","properties":{
                "Kind":{"type":"string","enum":["Heading","Text","Table"]},
                "Text":{"type":"string"},
                "Columns":{"type":"array","items":{"type":"object","properties":{"Column":{"type":"string"},"Header":{"type":"string"}}}}}}}}
        """;

    private static string ExportPdfInputSchema => """
        {"type":"object","properties":{
            "Sources":{"type":"array","items":{"type":"object","x-fantoche-type":"table"}},
            "Document":{<layout>},
            "FileName":{"type":"string"}}}
        """
        .Replace("<layout>", PdfLayoutProperties);

    private const string ExportPdfOutputSchema = """
        {"type":"object","properties":{
            "Files":{"type":"array","items":{"type":"object","x-fantoche-type":"file","properties":{"Id":{"type":"string"},"Name":{"type":"string"}}}}}}
        """;

    /// <summary>The output of a pass-through task : none, the next node reading past it.</summary>
    private const string PassThroughSchema = "{}";

    private const string ReorderInputSchema = """
        {"type":"object","properties":{"Title":{"type":"string"},"Items":{"type":"array","items":{"type":"string"}}}}
        """;

    private const string ItemsSchema = """
        {"type":"object","properties":{"Items":{"type":"array","items":{"type":"string"}}}}
        """;

    private static Guid Id(byte index) => new Guid($"00000000-0000-0000-0000-2000000000{index:x2}");

    /// <summary>
    /// The start of a workflow. It declares what it hands over the way every other node declares
    /// what it produces : the control itself knows no shape, and anything reading the graph — a run,
    /// or the preview an editor shows of it — reads "$previous" of the first node from there.
    /// </summary>
    /// <param name="defaults">The values the run starts with when the caller leaves them out.</param>
    private static GraphControl StartNode(double x, double y, string schema, object? defaults = null)
        => Node(
            new GraphControl(AutomationControl.StartTask) { OutputSchemaJson = schema },
            "Start", x, y, defaults);

    /// <summary>
    /// The end of a workflow, declaring what it hands back : what an end reads is the output of the
    /// workflow, so the shape is declared on the node producing it.
    /// </summary>
    private static GraphControl EndNode(double x, double y, object mapping, string schema)
        => Node(
            new GraphControl(AutomationControl.EndTask) { InputSchemaJson = schema },
            "End", x, y, mapping);

    /// <param name="package">The package holding [className], <see cref="PluginsPackage"/> by default.</param>
    private static AutomationTask MakeTask(
        Guid id,
        string name,
        string className,
        string inputSchema,
        string outputSchema,
        bool hasPage = false,
        Guid? parentId = null,
        PackageIdentifier? package = null,
        bool isPassThrough = false)
    {
        PackageIdentifier target = package ?? PluginsPackage;
        return new AutomationTask()
        {
            Id = id,
            ParentId = parentId ?? SamplesScope.Id,
            Metadata = new ScopedMetadata(name, EnumScopedType.Task) { Tags = ["Sample"] },
            Target = new PackageClassTarget()
            {
                Dll = target.Id,
                ClassFullName = className,
                HasPage = hasPage,
                Package = target,
            },
            InputSchemaJson = inputSchema,
            OutputSchemaJson = outputSchema,
            Settings = new TaskSettings() { IsPassingThrough = isPassThrough },
        };
    }

    /// <summary>A task of the engine samples, see <see cref="EnginePackage"/>.</summary>
    private static AutomationTask MakeEngineTask(
        Guid id, string name, string className, string inputSchema, string outputSchema, bool isPassThrough = false, bool hasPage = false)
        => MakeTask(id, name, className, inputSchema, outputSchema,
            hasPage: hasPage, parentId: EngineScope.Id, package: EnginePackage, isPassThrough: isPassThrough);

    /// <summary>
    /// A workflow with an empty graph. Its schemas are not set here : they are declared by the start
    /// and the end of the graph, and copied onto the element by
    /// <see cref="AutomationWorkflow.DeriveSchemas"/> once it is built.
    /// </summary>
    private static AutomationWorkflow MakeWorkflow(Guid id, string name, Guid? parentId = null)
        => new AutomationWorkflow()
        {
            Id = id,
            ParentId = parentId ?? SamplesScope.Id,
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
