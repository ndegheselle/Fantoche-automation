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
/// Sample tasks and a workflow showing them at work, so the interface has something to display and
/// to execute right after a fresh install. They target the classes of the Fantoche.Plugins package,
/// which has to be added to the packages for the samples to run.
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

    private static readonly AutomationTask ImportCsv = MakeTask(
        Id(0x30), "Import CSV", "Fantoche.Plugins.Tables.ImportCsvTask",
        Schema(("Path", "string"), ("Name", "string")), TableSchema);

    /// <summary>The user picks the column each name stands for : a user interface task.</summary>
    private static readonly AutomationTask PickColumns = MakeTask(
        Id(0x31), "Pick columns", "Fantoche.Plugins.Tables.PickColumnsTask",
        PickColumnsInputSchema, PickColumnsOutputSchema);

    private static readonly AutomationTask MapColumns = MakeTask(
        Id(0x32), "Map columns", "Fantoche.Plugins.Tables.MapColumnsTask",
        MapColumnsInputSchema, TableSchema);

    private static readonly AutomationTask Merge = MakeTask(
        Id(0x33), "Merge", "Fantoche.Plugins.Tables.MergeTask",
        MergeInputSchema, TableSchema);

    private static readonly AutomationTask Update = MakeTask(
        Id(0x34), "Update", "Fantoche.Plugins.Tables.UpdateTask",
        UpdateInputSchema, TableSchema);

    private static readonly AutomationTask Split = MakeTask(
        Id(0x35), "Split", "Fantoche.Plugins.Tables.SplitTask",
        SplitInputSchema, SplitOutputSchema);

    /// <summary>The user composes the document of the PDF : a user interface task.</summary>
    private static readonly AutomationTask ComposePdf = MakeTask(
        Id(0x36), "Compose PDF", "Fantoche.Plugins.Pdf.ComposePdfTask",
        ComposePdfInputSchema, ComposePdfOutputSchema);

    private static readonly AutomationTask ExportPdf = MakeTask(
        Id(0x37), "Export PDF", "Fantoche.Plugins.Pdf.ExportPdfTask",
        ExportPdfInputSchema, ExportPdfOutputSchema);

    #endregion

    public static IEnumerable<ScopedElement> Build()
    {
        List<ScopedElement> elements =
        [
            SamplesScope,
            ImportCsv,
            PickColumns,
            MapColumns,
            Merge,
            Update,
            Split,
            ComposePdf,
            ExportPdf,
            BuildCustomersOrders(),
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
    /// Start ─┬─> Import customers ─> Pick customer columns ─> Customers ─┬─> Tables ─> Merge ─> Update ─> Split ─> Compose ─> Export ─> End
    ///        └─> Import orders ────> Pick order columns ────> Orders ────┘
    /// </code>
    /// Started with the paths of the two files of Fantoche.Plugins/Samples. The run waits
    /// for the user at both picks and at the composition. The nodes after the mapping read the
    /// columns by their names ("$previous.Customers.Mapping.CustomerId", "$row.columns.Amount"),
    /// whatever the files call them.
    /// </summary>
    private static AutomationWorkflow BuildCustomersOrders()
    {
        AutomationWorkflow workflow = MakeWorkflow(Id(0x24), "Customers and orders");

        GraphControl start = StartNode(0, 150, Schema(("Customers", "string"), ("Orders", "string")));

        GraphTask importCustomers = Node(new GraphTask(ImportCsv), "Import customers", 220, 0,
            new { Path = "$previous.Customers" });
        GraphTask pickCustomers = Node(new GraphTask(PickColumns), "Pick customer columns", 440, 0,
            new
            {
                Title = "Columns of the customers",
                Source = "$previous",
                Columns = new[] { Column("CustomerId"), Column("Name"), Column("City") },
            });
        GraphTask customers = Node(new GraphTask(MapColumns), "Customers", 660, 0,
            new { Source = "$previous.Source", Columns = "$previous.Columns" });

        GraphTask importOrders = Node(new GraphTask(ImportCsv), "Import orders", 220, 300,
            new { Path = "$previous.Orders" });
        GraphTask pickOrders = Node(new GraphTask(PickColumns), "Pick order columns", 440, 300,
            new
            {
                Title = "Columns of the orders",
                Source = "$previous",
                Columns = new[] { Column("OrderId"), Column("CustomerId"), Column("Amount"), Column("Date") },
            });
        GraphTask orders = Node(new GraphTask(MapColumns), "Orders", 660, 300,
            new { Source = "$previous.Source", Columns = "$previous.Columns" });

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
        GraphTask compose = Node(new GraphTask(ComposePdf), "Compose", 1760, 150,
            new { Title = "Orders", Tables = "$previous.Batches", Document = new { Landscape = true, Blocks = Array.Empty<object>() } });
        GraphTask export = Node(new GraphTask(ExportPdf), "Export", 1980, 150,
            new { Sources = "$previous.Tables", Document = "$previous.Document", FileName = "orders-{index}" });
        GraphControl end = EndNode(2200, 150, new { Files = "$previous.Files" }, Schema(("Files", "array")));

        Add(workflow, start, importCustomers, pickCustomers, customers, importOrders, pickOrders, orders,
            tables, merge, update, split, compose, export, end);

        workflow.Graph.Connect(start, importCustomers);
        workflow.Graph.Connect(importCustomers, pickCustomers);
        workflow.Graph.Connect(pickCustomers, customers);
        workflow.Graph.Connect(customers, tables);
        workflow.Graph.Connect(start, importOrders);
        workflow.Graph.Connect(importOrders, pickOrders);
        workflow.Graph.Connect(pickOrders, orders);
        workflow.Graph.Connect(orders, tables);
        workflow.Graph.Connect(tables, merge);
        workflow.Graph.Connect(merge, update);
        workflow.Graph.Connect(update, split);
        workflow.Graph.Connect(split, compose);
        workflow.Graph.Connect(compose, export);
        workflow.Graph.Connect(export, end);

        return workflow;
    }

    /// <summary>A name the page asks the column of, the column being picked by the user.</summary>
    private static object Column(string name) => new { Name = name, Column = "" };

    #region Building blocks

    /// <summary>
    /// The package the sample tasks run : it has to be added to the packages for the samples to run.
    /// A property rather than a field : the task fields above are initialized first, and would read a
    /// field declared after them as null.
    /// </summary>
    private static Version PluginsVersion => new("2.0.0");

    /// <summary>
    /// The schemas of the tasks, with the markers of the engine : a table or file handle
    /// ("x-fantoche-type"), the fields evaluated per row ("x-fantoche-scope") and what a task hands
    /// over of its input ("x-fantoche-output", "x-fantoche-output-mapping", see OutputPreview). The
    /// schemas they share are written once, put in place of their "&lt;name&gt;".
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

    private static string PickColumnsInputSchema => """
        {"type":"object","properties":{
            "Title":{"type":"string"},
            "Source":{"type":"object","x-fantoche-type":"table","x-fantoche-output":"Source"},
            "Columns":{"type":"array","items":<column mapping>,"x-fantoche-output":"Columns"}}}
        """
        .Replace("<column mapping>", ColumnMappingSchema);

    private static string PickColumnsOutputSchema => """
        {"type":"object","properties":{
            "Source":<table>,
            "Columns":{"type":"array","items":<column mapping>}}}
        """
        .Replace("<column mapping>", ColumnMappingSchema)
        .Replace("<table>", TableSchema);

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
            "Conditions":{"type":"array","x-fantoche-scope":"row","items":{"type":"object","properties":{
                "Left":{},
                "Operator":{"type":"string","enum":["Equal","NotEqual","Greater","GreaterOrEqual","Less","LessOrEqual","Contains"]},
                "Right":{}}}},
            "MatchAll":{"type":"boolean"},
            "Set":{"type":"array","x-fantoche-scope":"row","items":{"type":"object","properties":{"Column":{"type":"string"},"Value":{}}}}}}
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

    private static string ComposePdfInputSchema => """
        {"type":"object","properties":{
            "Title":{"type":"string"},
            "Tables":{"type":"array","items":{"type":"object","x-fantoche-type":"table"},"x-fantoche-output":"Tables"},
            "Document":{<layout>,"x-fantoche-output":"Document"}}}
        """
        .Replace("<layout>", PdfLayoutProperties);

    private static string ComposePdfOutputSchema => """
        {"type":"object","properties":{
            "Tables":{"type":"array","items":<table>},
            "Document":{<layout>}}}
        """
        .Replace("<table>", TableSchema)
        .Replace("<layout>", PdfLayoutProperties);

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

    private static Guid Id(byte index) => new Guid($"00000000-0000-0000-0000-2000000000{index:x2}");

    /// <summary>
    /// The start of a workflow. It declares what it hands over the way every other node declares
    /// what it produces : the control itself knows no shape, and anything reading the graph — a run,
    /// or the preview an editor shows of it — reads "$previous" of the first node from there.
    /// </summary>
    private static GraphControl StartNode(double x, double y, string schema)
        => Node(
            new GraphControl(AutomationControl.StartTask) { OutputSchemaJson = schema },
            "Start", x, y);

    /// <summary>
    /// The end of a workflow, declaring what it hands back : what an end reads is the output of the
    /// workflow, so the shape is declared on the node producing it.
    /// </summary>
    private static GraphControl EndNode(double x, double y, object mapping, string schema)
        => Node(
            new GraphControl(AutomationControl.EndTask) { InputSchemaJson = schema },
            "End", x, y, mapping);

    private static AutomationTask MakeTask(
        Guid id,
        string name,
        string className,
        string inputSchema,
        string outputSchema)
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
