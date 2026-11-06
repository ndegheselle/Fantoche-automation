using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json;

namespace Fantoche.Shared.Samples;

/// <summary>What adding a package of the samples created, to tell the user.</summary>
public sealed record SamplesCreated(IReadOnlyList<string> Elements, IReadOnlyList<string> MissingClasses);

/// <summary>
/// The samples of a fresh install, created once the packages they run are added rather than
/// written by hand in the database : the tasks are made from the classes the package holds, their
/// schemas generated from the types of their parameters. Only the example packages have samples.
/// <list type="bullet">
/// <item><see cref="PackageId"/> : its tasks, in a scope named after it, and the "Customers and
/// orders" workflow running them.</item>
/// <item><see cref="InterfacesPackageId"/> : its interfaces, given to the tasks they declare (see
/// <see cref="TaskInterfaceAttribute"/>).</item>
/// </list>
/// Adding a package again (another version) creates only what is missing.
/// </summary>
public static class ExampleSamples
{
    public const string PackageId = "Fantoche.Plugins.Example";
    public const string InterfacesPackageId = "Fantoche.Plugins.Example.Ui";

    public const string WorkflowName = "Customers and orders";

    private const string TablesNamespace = "Fantoche.Plugins.Example.Tasks.Tables";
    private const string ImportCsvClass = $"{TablesNamespace}.ImportCsvTask";
    private const string MapColumnsClass = $"{TablesNamespace}.MapColumnsTask";
    private const string MergeClass = $"{TablesNamespace}.MergeTask";
    private const string UpdateClass = $"{TablesNamespace}.UpdateTask";
    private const string SplitClass = $"{TablesNamespace}.SplitTask";
    private const string ExportPdfClass = "Fantoche.Plugins.Example.Tasks.Pdf.ExportPdfTask";

    /// <summary>The classes the workflow runs : without one of them, only the tasks are created.</summary>
    private static readonly string[] WorkflowClasses = [ImportCsvClass, MapColumnsClass, MergeClass, UpdateClass, SplitClass, ExportPdfClass];

    /// <summary>
    /// Create the tasks of the classes [classes] of the example package, and the workflow running
    /// them. [loadTask] loads a class, for the schemas of its parameters and of what it hands over.
    /// </summary>
    public static async Task<SamplesCreated> CreateAsync(IScopedService scoped, IReadOnlyList<ClassTarget> classes, Func<PackageClassTarget, Task<ITask>> loadTask)
    {
        List<ScopedElement> tree = await scoped.GetTreeAsync();
        List<string> created = [];

        Scope scope = await ScopeOfAsync(scoped, tree, created);
        Dictionary<string, AutomationTask> tasks = [];
        foreach (ClassTarget found in classes)
        {
            AutomationTask task = TaskOf(tree, found.ClassFullName)
                ?? await CreateTaskAsync(scoped, scope, found, loadTask, created);
            tasks[found.ClassFullName] = task;
        }

        List<string> missing = [.. WorkflowClasses.Where(x => !tasks.ContainsKey(x))];
        bool hasWorkflow = tree.Any(x => x is AutomationWorkflow && x.ParentId == scope.Id && x.Metadata.Name == WorkflowName);
        if (missing.Count == 0 && !hasWorkflow)
        {
            AutomationWorkflow workflow = BuildCustomersOrders(scope.Id, tasks);
            await scoped.CreateAsync(workflow);
            created.Add(WorkflowName);
        }

        return new SamplesCreated(created, missing);
    }

    /// <summary>
    /// An interface element (<see cref="AutomationInterface"/>) for each interface of [interfaces],
    /// hanging under each task running the class it declares : linked to it. A task with an
    /// interface already keeps it. The number of tasks linked to one.
    /// </summary>
    public static async Task<int> AttachInterfacesAsync(IScopedService scoped, IEnumerable<TaskInterfaceClass> interfaces)
    {
        List<ScopedElement> tree = await scoped.GetTreeAsync();
        HashSet<Guid> withInterface = [.. tree.OfType<AutomationInterface>().Select(x => x.ParentId).OfType<Guid>()];
        int attached = 0;
        foreach (TaskInterfaceClass found in interfaces.Where(x => x.TaskClass != null))
        {
            IEnumerable<AutomationTask> tasks = tree
                .OfType<AutomationTask>()
                .Where(x => x is not AutomationControl && !withInterface.Contains(x.Id) && x.Target?.ClassFullName == found.TaskClass);
            foreach (AutomationTask task in tasks)
            {
                AutomationInterface face = new(NameOf(found.Target.ClassFullName), task.Id)
                {
                    // The newest version each time it is shown, as the tasks of the samples do.
                    Target = new PackageClassTarget(new PackageIdentifier { Id = found.Target.Package.Id, Version = null }, found.Target.ClassFullName)
                    {
                        Dll = found.Target.Dll,
                    },
                };
                face.Metadata.Tags = ["Sample"];
                await scoped.CreateAsync(face);
                attached++;
            }
        }
        return attached;
    }

    /// <summary>The scope of the samples, under the root, named after the package : created the first time.</summary>
    private static async Task<Scope> ScopeOfAsync(IScopedService scoped, List<ScopedElement> tree, List<string> created)
    {
        if (tree.OfType<Scope>().FirstOrDefault(x => x.ParentId == Scope.Root.Id && x.Metadata.Name == PackageId) is Scope existing)
            return existing;

        Scope scope = (Scope)await scoped.CreateAsync(new Scope(PackageId, Scope.Root.Id));
        created.Add(PackageId);
        return scope;
    }

    /// <summary>The task already running [classFullName] of the package, wherever the user put it.</summary>
    private static AutomationTask? TaskOf(List<ScopedElement> tree, string classFullName)
        => tree.OfType<AutomationTask>()
            .FirstOrDefault(x => x is not AutomationControl && x.Target?.Package.Id == PackageId && x.Target.ClassFullName == classFullName);

    private static async Task<AutomationTask> CreateTaskAsync(IScopedService scoped, Scope scope, ClassTarget found, Func<PackageClassTarget, Task<ITask>> loadTask, List<string> created)
    {
        // The newest version each time it runs : the samples keep running as the package gets new ones.
        PackageClassTarget target = new(new PackageIdentifier { Id = PackageId, Version = null }, found.ClassFullName) { Dll = found.Dll };
        AutomationTask task = new(NameOf(found.ClassFullName), scope.Id) { Target = target };
        task.Metadata.Tags = ["Sample"];
        task.UpdateFromTask(await loadTask(target));

        AutomationTask stored = (AutomationTask)await scoped.CreateAsync(task);
        created.Add(stored.Metadata.Name);
        return stored;
    }

    /// <summary>The name a class reads by : "ImportCsvTask" as "Import csv".</summary>
    public static string NameOf(string classFullName)
    {
        string name = classFullName[(classFullName.LastIndexOf('.') + 1)..];
        if (name.EndsWith("Task", StringComparison.Ordinal) && name.Length > "Task".Length)
            name = name[..^"Task".Length];

        IEnumerable<string> words = SplitWords(name);
        string sentence = string.Join(' ', words.Select(x => x.ToLowerInvariant()));
        return char.ToUpperInvariant(sentence[0]) + sentence[1..];
    }

    private static IEnumerable<string> SplitWords(string name)
    {
        int start = 0;
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsUpper(name[i]))
                continue;
            yield return name[start..i];
            start = i;
        }
        yield return name[start..];
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
    /// generates. The mappings and the export ask the user through the nodes attached under them ;
    /// with those removed the run is unattended, the columns being the ones of those files. The nodes
    /// after the mapping read the columns by their names ("$previous.Customers.Mapping.CustomerId",
    /// "$row.columns.Amount"), whatever the files call them.
    /// </summary>
    private static AutomationWorkflow BuildCustomersOrders(Guid scopeId, Dictionary<string, AutomationTask> tasks)
    {
        AutomationTask importCsv = tasks[ImportCsvClass];
        AutomationTask mapColumns = tasks[MapColumnsClass];
        AutomationTask exportPdf = tasks[ExportPdfClass];

        AutomationWorkflow workflow = new(WorkflowName, scopeId);
        workflow.Metadata.Tags = ["Sample"];

        GraphControl start = Node(
            new GraphControl(AutomationControl.StartTask) { OutputSchemaJson = Schema(("Customers", "string"), ("Orders", "string")) },
            "Start", 0, 150);

        GraphTask importCustomers = Node(new GraphTask(importCsv), "Import customers", 440, 0,
            new { Path = "$previous.Customers" });
        GraphTask customers = Node(new GraphTask(mapColumns), "Customers", 660, 0,
            new
            {
                Source = "$previous",
                Columns = new[] { Column("CustomerId", "client id"), Column("Name", "full name"), Column("City", "town") },
            });
        GraphUserInterface customersAsking = Node(new GraphUserInterface(mapColumns), "Pick the customers columns", 660, 120);

        GraphTask importOrders = Node(new GraphTask(importCsv), "Import orders", 440, 300,
            new { Path = "$previous.Orders" });
        GraphTask orders = Node(new GraphTask(mapColumns), "Orders", 660, 300,
            new
            {
                Source = "$previous",
                Columns = new[] { Column("OrderId", "order no"), Column("CustomerId", "client"), Column("Amount", "total"), Column("Date", "ordered on") },
            });
        GraphUserInterface ordersAsking = Node(new GraphUserInterface(mapColumns), "Pick the orders columns", 660, 420);

        GraphControl tables = Node(new GraphControl(AutomationControl.JoinTask), "Tables", 880, 150,
            new { Customers = "$previous.Customers", Orders = "$previous.Orders" });
        GraphTask merge = Node(new GraphTask(tasks[MergeClass]), "Merge", 1100, 150,
            new
            {
                Left = "$previous.Orders",
                Right = "$previous.Customers",
                On = new[] { new { Left = "$previous.Orders.Mapping.CustomerId", Right = "$previous.Customers.Mapping.CustomerId" } },
                Kind = "Inner",
            });
        GraphTask update = Node(new GraphTask(tasks[UpdateClass]), "Update", 1320, 150,
            new
            {
                Source = "$previous",
                Conditions = new[] { new { Left = "$row.columns.Amount", Operator = "GreaterOrEqual", Right = 500 } },
                MatchAll = true,
                Set = new[] { new { Column = "Priority", Value = "high" } },
            });
        GraphTask split = Node(new GraphTask(tasks[SplitClass]), "Split", 1540, 150,
            new { Source = "$previous", BatchSize = 10000 });
        GraphTask export = Node(new GraphTask(exportPdf), "Export", 1760, 150,
            new
            {
                Sources = "$previous.Batches",
                Document = new { Landscape = true, Blocks = Array.Empty<object>() },
                FileName = "orders-{index}",
            });
        GraphUserInterface exportAsking = Node(new GraphUserInterface(exportPdf), "Compose the document", 1760, 270);
        GraphControl end = Node(
            new GraphControl(AutomationControl.EndTask) { InputSchemaJson = Schema(("Files", "array")) },
            "End", 1980, 150, new { Files = "$previous.Files" });

        foreach (GraphNode node in new GraphNode[] { start, importCustomers, customers, customersAsking, importOrders, orders, ordersAsking, tables, merge, update, split, export, exportAsking, end })
            workflow.Graph.Nodes.Add(node);

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

        workflow.Graph.Attach(customers, customersAsking);
        workflow.Graph.Attach(orders, ordersAsking);
        workflow.Graph.Attach(export, exportAsking);

        // The schemas of a workflow are declared by its start and its end, the element holding a copy.
        workflow.DeriveSchemas();
        return workflow;
    }

    /// <summary>A name of the workflow and the column of the sample files it stands for.</summary>
    private static object Column(string name, string column) => new { Name = name, Column = column };

    /// <summary>
    /// Name a node, place it on the canvas and give it the mapping it runs with. The metadata comes
    /// cloned from the task, so only the name changes.
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

    /// <summary>The schema of an object of [properties], for the boundary of the workflow, which declares it.</summary>
    private static string Schema(params (string Name, string Type)[] properties)
    {
        string content = string.Join(",", properties.Select(x => $"\"{x.Name}\":{{\"type\":\"{x.Type}\"}}"));
        return $"{{\"type\":\"object\",\"properties\":{{{content}}}}}";
    }
}
