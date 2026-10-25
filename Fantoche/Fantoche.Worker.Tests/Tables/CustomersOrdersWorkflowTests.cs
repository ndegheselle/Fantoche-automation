using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.UserInterfaces;
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

    /// <summary><c>Start ─> Import ─> Map (asking the user) ─> End</c></summary>
    private static TestWorkflow BuildMapAskingTheUser() => new TestWorkflow("Map", requireOutput: false)
        .Start()
        .Task("Import", PluginTasks.ImportCsv, new { Path = "$previous.Path" })
        .Task("Map", PluginTasks.MapColumns, new
        {
            Source = "$previous",
            Columns = new[] { new { Name = "CustomerId", Column = "" }, new { Name = "Name", Column = "" } },
            AskUser = true,
        })
        .End(new { Table = "$previous" })
        .Chain("Start", "Import", "Map", "End");

    private Task<WorkflowRun> RunMapAsync(ScriptedUser user)
    {
        string path = _files.Write("customers.csv", $"{CustomersOrdersWorkflow.CustomersHeader}\n1;Alice;Paris\n");
        return BuildMapAskingTheUser().RunAsync(new { Path = path }, user: user);
    }

    [Test]
    public async Task ColumnsThePageSuggests_AreTheOnesNamedAlike()
    {
        JToken? suggested = null;
        ScriptedUser user = new(async (request, cancellation) =>
        {
            suggested = await request.CallAsync("Suggest", request.Input, cancellation);
            return null;
        });
        using CancellationTokenSource stop = new();
        Task<WorkflowRun> running = BuildMapAskingTheUser().RunAsync(new { Path = _files.Write("customers.csv", $"{CustomersOrdersWorkflow.CustomersHeader}\n1;Alice;Paris\n") }, stop.Token, user);
        await user.FirstAskedAsync();
        using (CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10)))
        {
            while (suggested == null)
                await Task.Delay(10, timeout.Token);
        }
        stop.Cancel();
        await running;

        // "Name" is held by "full name", "CustomerId" looks like nothing of the file.
        Assert.That(suggested!.ToObject<List<ColumnMapping>>()!.Select(x => (x.Name, x.Column)), Is.EqualTo(new[] { ("CustomerId", ""), ("Name", "full name") }));
    }

    [Test]
    public async Task ColumnsThePagePicks_AreTheOnesTheMapGives()
    {
        ScriptedUser user = new(async (request, cancellation) =>
        {
            JObject answer = (JObject)request.Input!.DeepClone();
            answer["Columns"] = JArray.FromObject(new[] { new { Name = "CustomerId", Column = "client id" }, new { Name = "Name", Column = "full name" }, new { Name = "Town", Column = "town" } });
            // Checked by the task before the page answers.
            JToken? refusal = await request.CallAsync("Check", answer, cancellation);
            return refusal?.Type == JTokenType.String ? null : answer;
        });

        WorkflowRun run = await RunMapAsync(user);

        TableRef mapped = run.Instance.Output!["Table"]!.ToObject<TableRef>()!;
        Assert.That(mapped.Mapping, Is.EqualTo(new Dictionary<string, string> { ["CustomerId"] = "client id", ["Name"] = "full name", ["Town"] = "town" }));
    }

    [Test]
    public async Task ColumnsNamedTwice_AreRefusedByTheCheckOfThePage()
    {
        JToken? refusal = null;
        ScriptedUser user = new(async (request, cancellation) =>
        {
            JObject answer = (JObject)request.Input!.DeepClone();
            answer["Columns"] = JArray.FromObject(new[] { new { Name = "Id", Column = "client id" }, new { Name = "CustomerId", Column = "client id" } });
            refusal = await request.CallAsync("Check", answer, cancellation);
            answer["Columns"] = JArray.FromObject(new[] { new { Name = "CustomerId", Column = "client id" }, new { Name = "Name", Column = "full name" } });
            return answer;
        });

        await RunMapAsync(user);

        Assert.That(refusal?.Value<string>(), Is.EqualTo("The column [client id] is given two names."));
    }
}
