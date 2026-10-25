using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The rows of a mapped table matching conditions updated :
/// <code>
/// Start ─> Import ─> Map ─> Update ─> End
/// </code>
/// The conditions and the values reference the row by the names of the mapping ("$row.columns.Amount").
/// </summary>
[TestFixture]
internal sealed class UpdateTaskTests
{
    private const string Orders = "order,total,state\nA,10,new\nB,20,new\nC,30,sent\n";

    private CsvFiles _files = null!;

    [SetUp]
    public void SetUp() => _files = new CsvFiles();

    [TearDown]
    public void TearDown() => _files.Dispose();

    private Task<WorkflowRun> RunAsync(object[] conditions, object[] set, bool matchAll = true)
    {
        string path = _files.Write("orders.csv", Orders);
        TestWorkflow workflow = new TestWorkflow("Update", requireOutput: false)
            .Start()
            .Task("Import", PluginTasks.ImportCsv, new { Path = "$previous.Path" })
            .Task("Map", PluginTasks.MapColumns, new
            {
                Source = "$previous",
                Columns = new[] { new { Name = "OrderId", Column = "order" }, new { Name = "Amount", Column = "total" }, new { Name = "State", Column = "state" } }
            })
            .Task("Update", PluginTasks.Update, new { Source = "$previous", Conditions = conditions, Set = set, MatchAll = matchAll })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "Map", "Update", "End");
        return workflow.RunAsync(new { Path = path });
    }

    private static object Condition(object left, string op, object right) => new { Left = left, Operator = op, Right = right };
    private static object Set(string column, object? value) => new { Column = column, Value = value };

    [Test]
    public async Task Update_ChangesTheColumnOfTheMatchingRows()
    {
        WorkflowRun run = await RunAsync([Condition("$row.columns.Amount", "Greater", 15)], [Set("State", "priority")]);

        List<object?> states = await CsvFiles.ColumnAsync(run, run.OutputOf("Update"), "State");
        Assert.That(states, Is.EqualTo(new[] { "new", "priority", "priority" }));
    }

    [Test]
    public async Task Update_AddsAColumnEmptyForTheOtherRows()
    {
        WorkflowRun run = await RunAsync([Condition("$row.columns.State", "Equal", "new")], [Set("ToSend", true)]);

        TableRef updated = run.OutputOf("Update")!.ToObject<TableRef>()!;
        List<object?> flags = await CsvFiles.ColumnAsync(run, run.OutputOf("Update"), "ToSend");
        Assert.Multiple(() =>
        {
            Assert.That(flags, Is.EqualTo(new object?[] { true, true, null }));
            Assert.That(updated.Mapping, Does.ContainKey("ToSend"), "named by the workflow, as the mapped columns");
        });
    }

    [Test]
    public async Task Value_ReadsAnotherColumnOfTheRow()
    {
        WorkflowRun run = await RunAsync([Condition("$row.index", "Equal", 0)], [Set("State", "$row.columns.OrderId")]);

        List<object?> states = await CsvFiles.ColumnAsync(run, run.OutputOf("Update"), "State");
        Assert.That(states, Is.EqualTo(new[] { "A", "new", "sent" }));
    }

    [Test]
    public async Task MatchAny_UpdatesTheRowsMatchingOneCondition()
    {
        WorkflowRun run = await RunAsync(
            [Condition("$row.columns.OrderId", "Equal", "A"), Condition("$row.columns.Amount", "Equal", 30)],
            [Set("State", "checked")],
            matchAll: false);

        List<object?> states = await CsvFiles.ColumnAsync(run, run.OutputOf("Update"), "State");
        Assert.That(states, Is.EqualTo(new[] { "checked", "new", "checked" }));
    }

    [Test]
    public async Task UnknownColumnInACondition_FailsTheUpdate()
    {
        WorkflowRun run = await RunAsync([Condition("$row.columns.Price", "Greater", 1)], [Set("State", "x")]);

        NodeReport update = run.Of("Update").Last();
        Assert.Multiple(() =>
        {
            Assert.That(update.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(update.Output?.ToString(), Does.Contain("Column [Price] not found in the table, which holds : OrderId, Amount, State."));
        });
    }
}
