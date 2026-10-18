using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Tests;

/// <summary>
/// <code>
/// Start ─> Import ─> Filter ─> End
/// </code>
/// The conditions reference the row ("$row.*"), which the engine leaves for the filter to resolve.
/// </summary>
[TestFixture]
internal sealed class FilterTaskTests
{
    private const string Csv = "name,amount,unit price\nalice,10,1.5\nbob,20,2.5\ncarol,30,3.5\n";

    private string _folder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_folder, true);

    private async Task<WorkflowRun> RunAsync(object[] conditions, bool matchAll = true)
    {
        string path = Path.Combine(_folder, "people.csv");
        await File.WriteAllTextAsync(path, Csv);

        TestWorkflow workflow = new TestWorkflow("Filter", requireOutput: false)
            .Start()
            .Task("Import", PluginTasks.CsvImport, new { Path = "$previous.Path" })
            .Task("Filter", PluginTasks.Filter, new { Source = "$previous", Conditions = conditions, MatchAll = matchAll })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "Filter", "End");
        return await workflow.RunAsync(new { Path = path });
    }

    private static async Task<List<object?>> NamesAsync(WorkflowRun run)
    {
        TableRef table = run.Instance.Output!["Table"]!.ToObject<TableRef>()!;
        List<object?> names = [];
        await foreach (var row in run.Storage.ReadRowsAsync(table))
            names.Add(row["name"]);
        return names;
    }

    private static object Condition(object left, string op, object right) => new { Left = left, Operator = op, Right = right };

    [Test]
    public async Task Column_ComparedToAValue()
    {
        WorkflowRun run = await RunAsync([Condition("$row.columns.amount", "Greater", 10)]);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "bob", "carol" }));
    }

    [Test]
    public async Task Index_StartsAtZero()
    {
        WorkflowRun run = await RunAsync([Condition("$row.index", "Less", 2)]);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "alice", "bob" }));
    }

    [Test]
    public async Task ColumnWithASpace_IsReadWithBrackets()
    {
        WorkflowRun run = await RunAsync([Condition("$row.columns['unit price']", "GreaterOrEqual", 2.5)]);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "bob", "carol" }));
    }

    [Test]
    public async Task Contains_ComparesAsText()
    {
        WorkflowRun run = await RunAsync([Condition("$row.columns.name", "Contains", "li")]);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "alice" }));
    }

    [Test]
    public async Task MatchAll_KeepsRowsMatchingEveryCondition()
    {
        WorkflowRun run = await RunAsync([
            Condition("$row.columns.amount", "Greater", 10),
            Condition("$row.columns.name", "NotEqual", "carol")]);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "bob" }));
    }

    [Test]
    public async Task MatchAny_KeepsRowsMatchingOneCondition()
    {
        WorkflowRun run = await RunAsync([
            Condition("$row.columns.name", "Equal", "alice"),
            Condition("$row.columns.amount", "Equal", 30)], matchAll: false);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "alice", "carol" }));
    }

    [Test]
    public async Task NoCondition_KeepsEveryRow()
    {
        WorkflowRun run = await RunAsync([]);

        Assert.That(await NamesAsync(run), Is.EqualTo(new[] { "alice", "bob", "carol" }));
    }

    [Test]
    public async Task UnknownRowReference_FailsTheFilter()
    {
        WorkflowRun run = await RunAsync([Condition("$row.nope", "Equal", 1)]);

        NodeReport filter = run.Of("Filter").Last();
        Assert.Multiple(() =>
        {
            Assert.That(filter.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(filter.Output?.ToString(), Does.Contain("[$row.nope] is not a row reference"));
        });
    }
}
