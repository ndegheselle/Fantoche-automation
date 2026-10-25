using System.Globalization;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Pdf;

public class ComposePdfParameters
{
    /// <summary>What the user is asked to compose, shown above the editor.</summary>
    public string Title { get; set; } = "";

    /// <summary>The tables a document is made of each : the page previews the first one.</summary>
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, nameof(ComposePdfResult.Tables))]
    public List<TableRef> Tables { get; set; } = [];

    /// <summary>What the editor starts from : a heading named after the title over every column when empty.</summary>
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, nameof(ComposePdfResult.Document))]
    public PdfLayout Document { get; set; } = new();
}

public class ComposePdfResult
{
    public List<TableRef> Tables { get; set; } = [];
    public PdfLayout Document { get; set; } = new();
}

/// <summary>
/// The user composes the document of each table, previewed on the first one : the page of
/// <c>UserInterfaces/ComposePdf</c>. <see cref="ExportPdfTask"/> saves the documents.
/// </summary>
public class ComposePdfTask : BaseUITask<ComposePdfParameters, ComposePdfResult>
{
    public override string PageFolder => "UserInterfaces/ComposePdf";
}

public class ExportPdfParameters
{
    /// <summary>A document per table.</summary>
    public List<TableRef> Sources { get; set; } = [];

    /// <summary>The whole table when it holds no block.</summary>
    public PdfLayout Document { get; set; } = new();

    /// <summary>Name of the file of each document, ".pdf" being added : it can hold the same placeholders as the text of a block.</summary>
    public string FileName { get; set; } = "document-{index}";
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
/// </summary>
public class ExportPdfTask : BaseTask<ExportPdfParameters, ExportPdfResult>
{
    public override async Task<ExportPdfResult> DoAsync(ExportPdfParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
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
