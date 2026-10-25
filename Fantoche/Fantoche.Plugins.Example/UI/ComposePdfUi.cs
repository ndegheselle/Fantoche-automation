using Fantoche.Plugins.Example.Tasks.Pdf;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Example.UI;

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
/// <c>UI/ComposePdf</c>. <see cref="ExportPdfTask"/> saves the documents.
/// </summary>
public class ComposePdfTask : BaseUITask<ComposePdfParameters, ComposePdfResult>
{
    public override string PageFolder => "UI/ComposePdf";
}
