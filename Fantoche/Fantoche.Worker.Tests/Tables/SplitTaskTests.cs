using System.Text;
using Fantoche.Plugins.Shared;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// A table cut into batches :
/// <code>
/// Start ─> Import ─> Split ─> End
/// </code>
/// </summary>
[TestFixture]
internal sealed class SplitTaskTests
{
    private CsvFiles _files = null!;

    [SetUp]
    public void SetUp() => _files = new CsvFiles();

    [TearDown]
    public void TearDown() => _files.Dispose();

    private Task<WorkflowRun> RunAsync(int rows, int batchSize)
    {
        StringBuilder csv = new("n\n");
        for (int i = 0; i < rows; i++)
            csv.Append(i).Append('\n');
        string path = _files.Write("numbers.csv", csv.ToString());

        TestWorkflow workflow = new TestWorkflow("Split", requireOutput: false)
            .Start()
            .Task("Import", PluginTasks.ImportCsv, new { Path = "$previous.Path" })
            .Task("Split", PluginTasks.Split, new { Source = "$previous", BatchSize = batchSize })
            .End(new { Batches = "$previous.Batches" })
            .Chain("Start", "Import", "Split", "End");
        return workflow.RunAsync(new { Path = path });
    }

    [Test]
    public async Task Split_CutsTheRowsInOrder()
    {
        WorkflowRun run = await RunAsync(rows: 25, batchSize: 10);

        List<List<object?>> batches = [];
        foreach (var batch in run.OutputOf("Split", "Batches")!)
            batches.Add(await CsvFiles.ColumnAsync(run, batch, "n"));

        Assert.Multiple(() =>
        {
            Assert.That(batches.Select(x => x.Count), Is.EqualTo(new[] { 10, 10, 5 }));
            Assert.That(batches.SelectMany(x => x).Select(Convert.ToInt64), Is.EqualTo(Enumerable.Range(0, 25).Select(x => (long)x)));
        });
    }

    [Test]
    public async Task EmptyTable_GivesNoBatch()
    {
        WorkflowRun run = await RunAsync(rows: 0, batchSize: 10);

        Assert.That(run.OutputOf("Split", "Batches")!.ToObject<List<TableRef>>(), Is.Empty);
    }
}
