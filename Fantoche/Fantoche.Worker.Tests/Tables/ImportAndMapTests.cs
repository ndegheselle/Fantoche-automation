using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// A file loaded into a table, its columns given clear names :
/// <code>
/// Start ─> Import ─> Map ─> End
/// </code>
/// </summary>
[TestFixture]
internal sealed class ImportAndMapTests
{
    private const string Customers = "client id;full name;town\n1;Alice;Paris\n2;Bob;Lyon\n";

    private CsvFiles _files = null!;

    [SetUp]
    public void SetUp() => _files = new CsvFiles();

    [TearDown]
    public void TearDown() => _files.Dispose();

    private Task<WorkflowRun> RunAsync(object columns)
    {
        string path = _files.Write("customers.csv", Customers);
        TestWorkflow workflow = new TestWorkflow("Import and map", requireOutput: false)
            .Start()
            .Task("Import", PluginTasks.ImportCsv, new { Path = "$previous.Path" })
            .Task("Map", PluginTasks.MapColumns, new { Source = "$previous", Columns = columns })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "Map", "End");
        return workflow.RunAsync(new { Path = path });
    }

    private static object Column(string name, string column) => new { Name = name, Column = column };

    private static string? FailureOf(WorkflowRun run, string node) => run.Of(node).Last().Output?.ToString();

    [Test]
    public async Task Import_DetectsTheDelimiterAndTheColumns()
    {
        WorkflowRun run = await RunAsync(Array.Empty<object>());

        TableRef table = run.OutputOf("Import")!.ToObject<TableRef>()!;
        Assert.That(table.Columns.Select(x => (x.Name, x.Type)), Is.EqualTo(new[] { ("client id", "BIGINT"), ("full name", "VARCHAR"), ("town", "VARCHAR") }));
    }

    [Test]
    public async Task Map_AddsTheNamesToTheTableWithoutCopyingIt()
    {
        WorkflowRun run = await RunAsync(new[] { Column("CustomerId", "client id"), Column("Name", "full name") });

        TableRef imported = run.OutputOf("Import")!.ToObject<TableRef>()!;
        TableRef mapped = run.OutputOf("Map")!.ToObject<TableRef>()!;
        Assert.Multiple(() =>
        {
            Assert.That(mapped.Id, Is.EqualTo(imported.Id));
            Assert.That(mapped.Mapping, Is.EqualTo(new Dictionary<string, string> { ["CustomerId"] = "client id", ["Name"] = "full name" }));
        });
    }

    [Test]
    public async Task NameGivenTwice_FailsTheMap()
    {
        WorkflowRun run = await RunAsync(new[] { Column("Name", "client id"), Column("name", "full name") });

        Assert.That(FailureOf(run, "Map"), Does.Contain("The name [name] is given twice."));
    }

    [Test]
    public async Task ColumnNamedTwice_FailsTheMap()
    {
        WorkflowRun run = await RunAsync(new[] { Column("Id", "client id"), Column("CustomerId", "client id") });

        Assert.That(FailureOf(run, "Map"), Does.Contain("The column [client id] is given two names."));
    }

    [Test]
    public async Task UnknownColumn_FailsTheMapNamingTheColumnsOfTheTable()
    {
        WorkflowRun run = await RunAsync(new[] { Column("City", "city") });

        Assert.That(FailureOf(run, "Map"), Does.Contain("Column [city] not found in the table, which holds : client id, full name, town."));
    }

    [Test]
    public async Task NameOfAnotherColumn_FailsTheMap()
    {
        // Once stored under its name, the column would clash with the town.
        WorkflowRun run = await RunAsync(new[] { Column("Town", "full name") });

        Assert.That(FailureOf(run, "Map"), Does.Contain("The name [Town] is the one of another column"));
    }
}
