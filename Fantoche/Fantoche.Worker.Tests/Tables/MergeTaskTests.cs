using Fantoche.Plugins.Shared;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// Two files mapped then merged on the column they share :
/// <code>
/// Start ─┬─> Import customers ─> Customers (map) ─┬─> Tables (join) ─> Merge ─> End
///        └─> Import orders ────> Orders (map) ────┘
/// </code>
/// </summary>
[TestFixture]
internal sealed class MergeTaskTests
{
    private const string Customers = "client id,full name\n1,Alice\n2,Bob\n3,Carol\n";
    private const string Orders = "order,client,total\nA,2,10\nB,1,20\nC,2,30\nD,9,40\n";

    private CsvFiles _files = null!;

    [SetUp]
    public void SetUp() => _files = new CsvFiles();

    [TearDown]
    public void TearDown() => _files.Dispose();

    private Task<WorkflowRun> RunAsync(string kind = "Inner", object? customersColumns = null)
    {
        object customers = customersColumns ?? new[] { new { Name = "CustomerId", Column = "client id" }, new { Name = "Name", Column = "full name" } };
        TestWorkflow workflow = new TestWorkflow("Merge", requireOutput: false)
            .Start()
            .Task("Import customers", PluginTasks.ImportCsv, new { Path = "$previous.Customers" })
            .Task("Customers", PluginTasks.MapColumns, new { Source = "$previous", Columns = customers })
            .Task("Import orders", PluginTasks.ImportCsv, new { Path = "$previous.Orders" })
            .Task("Orders", PluginTasks.MapColumns, new
            {
                Source = "$previous",
                Columns = new[] { new { Name = "OrderId", Column = "order" }, new { Name = "CustomerId", Column = "client" }, new { Name = "Amount", Column = "total" } }
            })
            .Join("Tables", new { Customers = "$previous.Customers", Orders = "$previous.Orders" })
            .Task("Merge", PluginTasks.Merge, new
            {
                Left = "$previous.Customers",
                Right = "$previous.Orders",
                // What an editor offers : the stored column a name of the mapping stands for.
                On = new[] { new { Left = "$previous.Customers.Mapping.CustomerId", Right = "$previous.Orders.Mapping.CustomerId" } },
                Kind = kind,
            })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import customers", "Customers", "Tables")
            .Chain("Start", "Import orders", "Orders", "Tables")
            .Chain("Tables", "Merge", "End");

        string customersPath = _files.Write("customers.csv", Customers);
        string ordersPath = _files.Write("orders.csv", Orders);
        return workflow.RunAsync(new { Customers = customersPath, Orders = ordersPath });
    }

    [Test]
    public async Task Merge_KeepsTheMatchingRowsInTheOrderOfTheLeftTable()
    {
        WorkflowRun run = await RunAsync();

        List<object?> orders = await CsvFiles.ColumnAsync(run, run.OutputOf("Merge"), "OrderId");
        Assert.That(orders, Is.EqualTo(new[] { "B", "A", "C" }));
    }

    [Test]
    public async Task Merge_StoresTheColumnsUnderTheirNames()
    {
        WorkflowRun run = await RunAsync();

        TableRef merged = run.OutputOf("Merge")!.ToObject<TableRef>()!;
        Assert.Multiple(() =>
        {
            // The key of the right table repeats the left one : it is left out.
            Assert.That(merged.Columns.Select(x => x.Name), Is.EqualTo(new[] { "CustomerId", "Name", "OrderId", "Amount" }));
            Assert.That(merged.ColumnOf("Amount"), Is.EqualTo("Amount"), "the names keep reading their columns");
        });
    }

    [Test]
    public async Task LeftMerge_KeepsTheRowsMatchingNothing()
    {
        WorkflowRun run = await RunAsync(kind: "Left");

        List<object?> orders = await CsvFiles.ColumnAsync(run, run.OutputOf("Merge"), "OrderId");
        Assert.That(orders, Is.EqualTo(new object?[] { "B", "A", "C", null }));
    }

    [Test]
    public async Task RightColumnNamedAsALeftOne_IsPrefixed()
    {
        // Both tables then hold an "Amount".
        object customers = new[] { new { Name = "CustomerId", Column = "client id" }, new { Name = "Amount", Column = "full name" } };

        WorkflowRun run = await RunAsync(customersColumns: customers);

        TableRef merged = run.OutputOf("Merge")!.ToObject<TableRef>()!;
        Assert.That(merged.Columns.Select(x => x.Name), Is.EqualTo(new[] { "CustomerId", "Amount", "OrderId", "RightAmount" }));
    }
}
