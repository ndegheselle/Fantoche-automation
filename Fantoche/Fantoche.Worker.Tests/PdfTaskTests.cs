using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The example workflow : a CSV loaded into a table, filtered, then a PDF saved per remaining row.
/// <code>
/// Start ─> Import ─> Filter ─> Pdf ─> End
/// </code>
/// </summary>
[TestFixture]
internal sealed class PdfTaskTests
{
    private const string Csv = "name,amount\nalice,10\nbob,20\ncarol/c,30\n";

    private string _folder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_folder, true);

    private async Task<WorkflowRun> RunAsync(object pdf)
    {
        string path = Path.Combine(_folder, "people.csv");
        await File.WriteAllTextAsync(path, Csv);

        TestWorkflow workflow = new TestWorkflow("Example", requireOutput: false)
            .Start()
            .Task("Import", PluginTasks.CsvImport, new { Path = "$previous.Path" })
            .Task("Filter", PluginTasks.Filter, new
            {
                Source = "$previous",
                Conditions = new[] { new { Left = "$row.columns.amount", Operator = "Greater", Right = 10 } }
            })
            .Task("Pdf", PluginTasks.Pdf, pdf)
            .End(new { Files = "$previous.Files" })
            .Chain("Start", "Import", "Filter", "Pdf", "End");
        return await workflow.RunAsync(new { Path = path });
    }

    private static List<FileRef> FilesOf(WorkflowRun run)
        => run.Instance.Output!["Files"]!.ToObject<List<FileRef>>()!;

    [Test]
    public async Task OnePdfPerRow_NamedAfterAColumn()
    {
        WorkflowRun run = await RunAsync(new
        {
            Source = "$previous",
            FileName = "$row.columns.name",
            Title = "$row.columns.name",
            Fields = new[] { new { Label = "Amount", Value = "$row.columns.amount" } }
        });

        // The "/" of a value can't be part of a file name.
        Assert.That(FilesOf(run).Select(x => x.Name), Is.EqualTo(new[] { "bob.pdf", "carol_c.pdf" }));
    }

    [Test]
    public async Task EveryFile_IsAOnePagePdf()
    {
        WorkflowRun run = await RunAsync(new { Source = "$previous", Title = "Report" });

        foreach (FileRef file in FilesOf(run))
        {
            await using Stream content = await run.Storage.OpenFileAsync(file);
            using PdfDocument document = PdfReader.Open(content, PdfDocumentOpenMode.Import);
            Assert.That(document.PageCount, Is.EqualTo(1), file.Name);
        }
    }

    [Test]
    public async Task FileName_DefaultsToTheIndex()
    {
        WorkflowRun run = await RunAsync(new { Source = "$previous" });

        Assert.That(FilesOf(run).Select(x => x.Name), Is.EqualTo(new[] { "0.pdf", "1.pdf" }));
    }

    [Test]
    public async Task UnknownColumn_FailsThePdf()
    {
        WorkflowRun run = await RunAsync(new { Source = "$previous", Title = "$row.columns.nope" });

        NodeReport pdf = run.Of("Pdf").Last();
        Assert.Multiple(() =>
        {
            Assert.That(pdf.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(pdf.Output?.ToString(), Does.Contain("Column [nope] not found in the table."));
        });
    }
}
