using System.ComponentModel;
using System.Globalization;
using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Example.Tasks.Pdf;

public class ExportPdfParameters
{
    [Description("A document per table.")]
    public List<TableRef> Sources { get; set; } = [];

    [Description("The whole table when it holds no block.")]
    public PdfLayout Document { get; set; } = new();

    [Description("Name of the file of each document, \".pdf\" being added. Can hold {index}, {count} and {rows}.")]
    public string FileName { get; set; } = "document-{index}";
}

public class ExportPdfResult
{
    [Description("A file per source, in their order.")]
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

    /// <summary>
    /// The document of the first table as [parameters] composes it, saved in the storage of the run
    /// for an interface to show : what the export will look like, rendered by the export itself.
    /// Kept for the interface composing the document, the next one to move to WPF.
    /// </summary>
    public static async Task<FileRef> PreviewAsync(ExportPdfParameters parameters, ITaskRuntime runtime, CancellationToken cancellation)
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
