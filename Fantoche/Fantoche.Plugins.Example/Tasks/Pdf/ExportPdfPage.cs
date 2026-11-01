using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Example.Tasks.Pdf;

/// <summary>
/// The page of <see cref="ExportPdfTask"/>, <c>UI/ComposePdf</c> : the user composes the document,
/// previewed on the first table as the page shows it and as the PDF the export renders.
/// </summary>
public class ExportPdfPage : UserInterfaceTask<ExportPdfParameters>
{
    public override string PageFolder => "UI/ComposePdf";

    [PageMethod]
    public Task<FileRef> PreviewAsync(ExportPdfParameters parameters, ITaskRuntime runtime, CancellationToken cancellation)
        => ExportPdfTask.PreviewAsync(parameters, runtime, cancellation);
}
