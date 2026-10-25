using System.Globalization;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Example.Tasks.Pdf;

public class ExportPdfParameters
{
    /// <summary>A document per table.</summary>
    public List<TableRef> Sources { get; set; } = [];

    /// <summary>The whole table when it holds no block.</summary>
    public PdfLayout Document { get; set; } = new();

    /// <summary>Name of the file of each document, ".pdf" being added : it can hold the same placeholders as the text of a block.</summary>
    public string FileName { get; set; } = "document-{index}";

    /// <summary>Let the user compose the document on a page, previewed on the first table.</summary>
    [JsonSchemaExtensionData(UserInterface.SchemaKey, true)]
    public bool AskUser { get; set; }
}

public class ExportPdfResult
{
    /// <summary>A file per source, in their order.</summary>
    public List<FileRef> Files { get; set; } = [];
}

/// <summary>
/// Save a PDF per table, laid out as the document says : its headings and paragraphs, then the
/// rows of the table where it has a table. The documents are rendered side by side, one per
/// processor, which is what makes a large table split in batches fast to export.
/// <para>
/// Asking the user, the page of <c>UI/ComposePdf</c> composes the document first, previewed on the
/// first table : as the page shows it, and as a PDF the task renders (see <see cref="PreviewAsync"/>).
/// </para>
/// </summary>
public class ExportPdfTask : BaseTask<ExportPdfParameters, ExportPdfResult>, IUserInterfaceTask
{
    public string PageFolder => "UI/ComposePdf";

    public override async Task<ExportPdfResult> DoAsync(ExportPdfParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
        if (runtime.UserInterface is IUserInterface page)
            parameters = await page.AskAsync<ExportPdfParameters>(parameters, token);

        int count = parameters.Sources.Count;
        FileRef[] files = new FileRef[count];
        int done = 0;

        ParallelOptions options = new() { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount };
        await Parallel.ForEachAsync(Enumerable.Range(0, count), options, async (index, itemToken) =>
        {
            TableRef source = parameters.Sources[index];
            long rows = await runtime.Storage.CountRowsAsync(source, itemToken);
            Placeholders placeholders = new(index + 1, count, rows);

            using MemoryStream content = await PdfRenderer.RenderAsync(parameters.Document, source, placeholders, runtime.Storage, itemToken);
            string name = FileNameOf(placeholders.Replace(parameters.FileName));
            files[index] = await runtime.Storage.SaveFileAsync(name, content, itemToken);

            int finished = Interlocked.Increment(ref done);
            runtime.Progress?.Report(new TaskNotification
            {
                Message = $"Saved {finished} of {count} PDF",
                Progress = (float)finished / count
            });
        });

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Saved {count} PDF"
        });
        return new ExportPdfResult { Files = [.. files] };
    }

    /// <summary>
    /// The document of the first table as [parameters] composes it, saved in the storage of the run
    /// for the page to show : what the export will look like, rendered by the export itself.
    /// </summary>
    [PageMethod]
    public async Task<FileRef> PreviewAsync(ExportPdfParameters parameters, ITaskRuntime runtime, CancellationToken cancellation)
    {
        TableRef source = parameters.Sources.FirstOrDefault()
            ?? throw new ArgumentException("There is no table to preview the document on.");
        long rows = await runtime.Storage.CountRowsAsync(source, cancellation);
        Placeholders placeholders = new(1, parameters.Sources.Count, rows);

        using MemoryStream content = await PdfRenderer.RenderAsync(parameters.Document, source, placeholders, runtime.Storage, cancellation);
        return await runtime.Storage.SaveFileAsync("preview.pdf", content, cancellation);
    }

    private static string FileNameOf(string text)
    {
        string name = text;
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.pdf";
    }
}

/// <summary>What the placeholders of the texts stand for in one document, see <see cref="PdfBlock"/>.</summary>
internal sealed record Placeholders(int Index, int Count, long Rows)
{
    public string Replace(string text) => text
        .Replace("{index}", Index.ToString(CultureInfo.InvariantCulture))
        .Replace("{count}", Count.ToString(CultureInfo.InvariantCulture))
        .Replace("{rows}", Rows.ToString(CultureInfo.InvariantCulture));
}
