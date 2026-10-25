using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The practical workflow end to end (see <see cref="CustomersOrdersWorkflow"/>), and the page of
/// the mapping feeding the task applying it.
/// </summary>
[TestFixture]
internal sealed class CustomersOrdersWorkflowTests
{
    private CsvFiles _files = null!;

    [SetUp]
    public void SetUp() => _files = new CsvFiles();

    [TearDown]
    public void TearDown() => _files.Dispose();

    private async Task<WorkflowRun> RunAsync(int customers, int orders, int batchSize)
    {
        string customersPath = Path.Combine(_files.Folder, "customers.csv");
        string ordersPath = Path.Combine(_files.Folder, "orders.csv");
        CustomersOrdersWorkflow.WriteCustomers(customersPath, customers);
        CustomersOrdersWorkflow.WriteOrders(ordersPath, orders, customers);

        return await CustomersOrdersWorkflow.Build(batchSize).RunAsync(new { Customers = customersPath, Orders = ordersPath });
    }

    [Test]
    public async Task Workflow_ExportsAPdfPerBatchOfTheMergedOrders()
    {
        WorkflowRun run = await RunAsync(customers: 10, orders: 25, batchSize: 10);

        List<FileRef> files = run.Instance.Output!["Files"]!.ToObject<List<FileRef>>()!;
        Assert.Multiple(() =>
        {
            Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(files.Select(x => x.Name), Is.EqualTo(new[] { "orders-1.pdf", "orders-2.pdf", "orders-3.pdf" }));
        });
    }

    [Test]
    public async Task Workflow_FlagsTheLargeOrdersOfTheMergedTable()
    {
        WorkflowRun run = await RunAsync(customers: 10, orders: 1000, batchSize: 1000);

        List<IReadOnlyDictionary<string, object?>> rows = await CsvFiles.RowsAsync(run, run.OutputOf("Update"));
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(1000));
            Assert.That(rows.Count(x => x["Priority"] is "high"), Is.EqualTo(500));
            Assert.That(rows[0]["Name"], Is.EqualTo("Customer 1"), "the customer merged with its order");
        });
    }

    /// <summary><c>Start ─> Import ─> Pick (the user) ─> Map ─> End</c></summary>
    private TestWorkflow BuildPick() => new TestWorkflow("Pick", requireOutput: false)
        .Start()
        .Task("Import", PluginTasks.ImportCsv, new { Path = "$previous.Path" })
        .Task("Pick", PluginTasks.PickColumns, new
        {
            Title = "Name the columns of the customers",
            Source = "$previous",
            Columns = new[] { new { Name = "CustomerId", Column = "" }, new { Name = "Name", Column = "" } }
        })
        .Task("Map", PluginTasks.MapColumns, new { Source = "$previous.Source", Columns = "$previous.Columns" })
        .End(new { Table = "$previous" })
        .Chain("Start", "Import", "Pick", "Map", "End");

    [Test]
    public async Task PickedColumns_AreTheOnesTheMapGives()
    {
        TestWorkflow pick = BuildPick();
        string path = _files.Write("customers.csv", $"{CustomersOrdersWorkflow.CustomersHeader}\n1;Alice;Paris\n");
        WorkflowRun held = await pick.RunAsync(new { Path = path });

        // What the page answers once the user picked the columns, as the history holds it (see UserAnswers).
        TaskInstance asked = held.Stored.Single(x => x.NodeName == "Pick");
        asked.Output = JToken.FromObject(new
        {
            Source = asked.Parameters!["Source"],
            Columns = new[] { new { Name = "CustomerId", Column = "client id" }, new { Name = "Name", Column = "full name" }, new { Name = "Town", Column = "town" } }
        });
        asked.State = EnumTaskState.Completed;
        WorkflowInstance resumed = WorkflowInstance.From(pick.Refreshed(), held.Instance);
        resumed.StoredInstances = held.Stored;
        WorkflowRun run = await WorkflowRun.ExecuteAsync(resumed);

        TableRef mapped = run.Instance.Output!["Table"]!.ToObject<TableRef>()!;
        Assert.Multiple(() =>
        {
            Assert.That(held.Instance.State, Is.EqualTo(EnumTaskState.AwaitingUser));
            Assert.That(mapped.Mapping, Is.EqualTo(new Dictionary<string, string> { ["CustomerId"] = "client id", ["Name"] = "full name", ["Town"] = "town" }));
        });
    }
}
