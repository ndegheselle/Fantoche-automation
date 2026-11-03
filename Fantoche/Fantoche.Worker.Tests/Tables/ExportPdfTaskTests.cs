using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// A PDF saved per batch of a table :
/// <code>
/// Start ─> Import ─> Split ─> Export ─> End
/// </code>
/// </summary>
[TestFixture]
internal sealed class ExportPdfTaskTests
{
    private CsvFiles _files = null!;

    /// <summary>
    /// The tests read the documents with a PDFsharp of their own, the plugin's being loaded apart
    /// (see TaskLoader) : it refuses to read until a license is chosen, as the plugin's does.
    /// </summary>
    [OneTimeSetUp]
    public void ChooseLicense() => PdfSharpLicense.SetAgplLicense();

    [SetUp]
    public void SetUp() => _files = new CsvFiles();

    [TearDown]
    public void TearDown() => _files.Dispose();

    /// <param name="user">Answers the page composing the document, attached to the export when given.</param>
    private Task<WorkflowRun> RunAsync(int rows, int batchSize, object document, string fileName = "orders-{index}", ScriptedUser? user = null)
    {
        StringBuilder csv = new("order,total\n");
        for (int i = 0; i < rows; i++)
            csv.Append($"order {i},{i * 10}\n");
        string path = _files.Write("orders.csv", csv.ToString());

        TestWorkflow workflow = new TestWorkflow("Export", requireOutput: false)
            .Start()
            .Task("Import", PluginTasks.ImportCsv, new { Path = "$previous.Path" })
            .Task("Split", PluginTasks.Split, new { Source = "$previous", BatchSize = batchSize })
            .Task("Export", PluginTasks.ExportPdf, new { Sources = "$previous.Batches", Document = document, FileName = fileName })
            .End(new { Files = "$previous.Files" })
            .Chain("Start", "Import", "Split", "Export", "End");
        if (user != null)
            workflow.Ui("Compose", PluginTasks.ExportPdfUi, of: "Export");
        return workflow.RunAsync(new { Path = path }, user: user);
    }

    private static readonly object Report = new
    {
        Blocks = new object[]
        {
            new { Kind = "Heading", Text = "Orders {index} of {count}" },
            new { Kind = "Text", Text = "{rows} orders" },
            new { Kind = "Table", Columns = new[] { new { Column = "order", Header = "Order" }, new { Column = "total", Header = "" } } },
        }
    };

    private static List<FileRef> FilesOf(WorkflowRun run) => run.OutputOf("Export", "Files")!.ToObject<List<FileRef>>()!;

    private static async Task<int> PagesOfAsync(WorkflowRun run, FileRef file)
    {
        await using Stream content = await run.Storage.OpenFileAsync(file);
        using PdfDocument document = PdfReader.Open(content, PdfDocumentOpenMode.Import);
        return document.PageCount;
    }

    [Test]
    public async Task Export_SavesAPdfPerBatchNamedAfterItsIndex()
    {
        WorkflowRun run = await RunAsync(rows: 25, batchSize: 10, Report);

        Assert.That(FilesOf(run).Select(x => x.Name), Is.EqualTo(new[] { "orders-1.pdf", "orders-2.pdf", "orders-3.pdf" }));
    }

    [Test]
    public async Task LongTable_GoesOnAsManyPagesAsItNeeds()
    {
        WorkflowRun run = await RunAsync(rows: 200, batchSize: 200, Report);

        Assert.That(await PagesOfAsync(run, FilesOf(run).Single()), Is.GreaterThan(1));
    }

    [Test]
    public async Task DocumentWithoutBlocks_ShowsTheWholeTable()
    {
        WorkflowRun run = await RunAsync(rows: 5, batchSize: 10, new { Blocks = Array.Empty<object>() });

        Assert.That(await PagesOfAsync(run, FilesOf(run).Single()), Is.EqualTo(1));
    }

    [Test]
    public async Task DocumentAnsweredByThePage_IsExported()
    {
        // The page answers the document it is handed, the kinds of its blocks written by name both ways.
        ScriptedUser user = new((request, _) => Task.FromResult(request.Input));

        WorkflowRun run = await RunAsync(rows: 25, batchSize: 10, Report, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(user.Asked.Single().Input?.SelectToken("Document.Blocks[0].Kind")?.ToString(), Is.EqualTo("Heading"));
            Assert.That(run.Of("Export").Last().State, Is.EqualTo(EnumTaskState.Completed));
        });
    }

    [Test]
    public async Task UnknownColumn_FailsTheExport()
    {
        object document = new { Blocks = new[] { new { Kind = "Table", Columns = new[] { new { Column = "price" } } } } };

        WorkflowRun run = await RunAsync(rows: 5, batchSize: 10, document);

        NodeReport export = run.Of("Export").Last();
        Assert.Multiple(() =>
        {
            Assert.That(export.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(export.Output?.ToString(), Does.Contain("Column [price] not found in the table"));
        });
    }
}
