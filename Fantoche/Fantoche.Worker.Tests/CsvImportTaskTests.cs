using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker.Tests;

/// <summary>
/// <code>
/// Start ─> Import ─> End
/// </code>
/// </summary>
[TestFixture]
internal sealed class CsvImportTaskTests
{
    private string _folder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_folder, true);

    private static TestWorkflow Build() => new TestWorkflow("CsvImport", requireOutput: false)
        .Start()
        .Task("Import", PluginTasks.CsvImport, new { Path = "$previous.Path" })
        .End(new { Table = "$previous" })
        .Chain("Start", "Import", "End");

    [Test]
    public async Task Import_HandsOverTheTableOfTheFile()
    {
        string path = Path.Combine(_folder, "people.csv");
        await File.WriteAllTextAsync(path, "name,amount\nalice,10\nbob,20\n");

        WorkflowRun run = await Build().RunAsync(new { Path = path });

        TableRef table = run.Instance.Output!["Table"]!.ToObject<TableRef>()!;
        List<object?> names = [];
        await foreach (var row in run.Storage.ReadRowsAsync(table))
            names.Add(row["name"]);

        Assert.Multiple(() =>
        {
            Assert.That(table.Columns.Select(x => (x.Name, x.Type)), Is.EqualTo(new[] { ("name", "VARCHAR"), ("amount", "BIGINT") }));
            Assert.That(names, Is.EqualTo(new[] { "alice", "bob" }));
        });
    }

    [Test]
    public async Task MissingFile_FailsTheImport()
    {
        string path = Path.Combine(_folder, "missing.csv");

        WorkflowRun run = await Build().RunAsync(new { Path = path });

        NodeReport import = run.Of("Import").Last();
        Assert.Multiple(() =>
        {
            Assert.That(import.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(import.Output?.ToString(), Does.Contain(path));
        });
    }

    [Test]
    public async Task Import_OfANestedWorkflow_IsStoredUnderTheOutermostRun()
    {
        string path = Path.Combine(_folder, "people.csv");
        await File.WriteAllTextAsync(path, "name,amount\nalice,10\n");
        // Declaring an output, which gives the node of the nested workflow an output to be chained by.
        TestWorkflow inner = new TestWorkflow("Inner")
            .Start()
            .Task("Import", PluginTasks.CsvImport, new { Path = "$previous.Path" })
            .End(new { Table = "$previous" })
            .Chain("Start", "Import", "End");
        TestWorkflow outer = new TestWorkflow("Outer", requireOutput: false)
            .Start()
            .Nested("Inner", inner, new { Path = "$previous.Path" })
            .End(new { Table = "$previous.Table" })
            .Chain("Start", "Inner", "End");

        WorkflowRun run = await outer.RunAsync(new { Path = path });
        TaskStorageUsage usage = run.StorageFolder.MeasureUsage().Single();

        // The storage is measured by run : what a run saved has to be found under where it started.
        Assert.Multiple(() =>
        {
            Assert.That(usage.TaskId, Is.EqualTo(outer.Workflow.Id));
            Assert.That(usage.Runs, Is.EqualTo(1));
        });
    }
}
