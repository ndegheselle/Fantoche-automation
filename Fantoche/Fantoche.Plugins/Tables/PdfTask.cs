using System.Globalization;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Fantoche.Plugins.Tables;

/// <summary>
/// A line of the document, "label: value", each one a value or a reference to the row.
/// </summary>
public class PdfField
{
    public object? Label { get; set; }
    public object? Value { get; set; }
}

public class PdfParameters
{
    public TableRef Source { get; set; } = new();

    /// <summary>
    /// Name of the file of each row, ".pdf" being added : usually a reference to a column identifying the row.
    /// </summary>
    [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
    public object? FileName { get; set; } = RowScope.Index;

    [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
    public object? Title { get; set; }

    [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
    public List<PdfField> Fields { get; set; } = [];
}

public class PdfOutput
{
    public List<FileRef> Files { get; set; } = [];
}

/// <summary>
/// Save a PDF per row of the source : a title and a line per field.
/// </summary>
public class PdfTask : BaseTask<PdfParameters, PdfOutput>
{
    private const string FontFamily = "DejaVu Sans";
    private const double Margin = 50;
    private const double LineHeight = 20;

    static PdfTask()
    {
        // Outside of Windows PDFsharp finds no font on its own : the one embedded makes a document look
        // the same on every worker. PDFsharp refuses a second resolver, and another load of the plugin
        // sharing the same PDFsharp may already have set one.
        GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();
    }

    public override async Task<PdfOutput> DoAsync(PdfParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
        PdfOutput output = new();

        long index = 0;
        await foreach (var row in runtime.Storage.ReadRowsAsync(parameters.Source, token))
        {
            string name = FileNameOf(ValueOf(parameters.FileName, index, row));
            using MemoryStream content = Render(parameters, index, row);
            FileRef file = await runtime.Storage.SaveFileAsync(name, content, token);
            output.Files.Add(file);
            index++;
        }

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Saved {output.Files.Count} PDF"
        });
        return output;
    }

    private static MemoryStream Render(PdfParameters parameters, long index, IReadOnlyDictionary<string, object?> row)
    {
        using var document = new PdfDocument();
        PdfPage page = document.AddPage();

        using (XGraphics graphics = XGraphics.FromPdfPage(page))
        {
            var titleFont = new XFont(FontFamily, 18, XFontStyleEx.Bold);
            var textFont = new XFont(FontFamily, 12);
            double y = Margin;

            string title = TextOf(ValueOf(parameters.Title, index, row));
            graphics.DrawString(title, titleFont, XBrushes.Black, new XPoint(Margin, y));
            y += LineHeight * 2;

            foreach (PdfField field in parameters.Fields)
            {
                string label = TextOf(ValueOf(field.Label, index, row));
                string value = TextOf(ValueOf(field.Value, index, row));
                graphics.DrawString($"{label}: {value}", textFont, XBrushes.Black, new XPoint(Margin, y));
                y += LineHeight;
            }
        }

        var content = new MemoryStream();
        document.Save(content, false);
        content.Position = 0;
        return content;
    }

    /// <summary>
    /// [operand] for the row : what a row reference points at, anything else as it is.
    /// </summary>
    private static object? ValueOf(object? operand, long index, IReadOnlyDictionary<string, object?> row)
    {
        if (operand is not string reference || !RowScope.IsReference(reference))
            return operand;
        if (reference == RowScope.Index)
            return index;

        string column = RowScope.ColumnOf(reference);
        return row.TryGetValue(column, out object? value)
            ? value
            : throw new ArgumentException($"Column [{column}] not found in the table.");
    }

    private static string TextOf(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    private static string FileNameOf(object? value)
    {
        string name = TextOf(value);
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.pdf";
    }

    /// <summary>
    /// The embedded font for every family and style asked for, bold and italic being simulated.
    /// </summary>
    private sealed class EmbeddedFontResolver : IFontResolver
    {
        private const string FaceName = "DejaVuSans";

        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) => new(FaceName, bold, italic);

        public byte[]? GetFont(string faceName)
        {
            using Stream font = typeof(PdfTask).Assembly.GetManifestResourceStream($"{FaceName}.ttf")
                ?? throw new InvalidOperationException($"Font [{FaceName}] is not embedded in the plugin.");
            using var bytes = new MemoryStream();
            font.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
