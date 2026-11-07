using Fantoche.Plugins.Example.Tasks.Pdf;
using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Fantoche.Plugins.Example.Ui.Pdf;

/// <summary>
/// The interface of <see cref="ExportPdfTask"/> : the user composes the document (its headings, texts
/// and tables) and names its files. Outside a run it edits the mapping of the node, the tables it
/// exports being only known in a run, where the document is previewed on the first one.
/// </summary>
[TaskInterface(typeof(ExportPdfTask))]
public class PdfComposerInterface : ITaskInterface
{
    private PdfComposerViewModel? _composer;

    public bool Handles(EnumInterfaceMode mode) => mode is EnumInterfaceMode.Setup or EnumInterfaceMode.Execution;

    public object Show(TaskInterfaceContext context)
    {
        JObject parameters = context.Parameters as JObject ?? new JObject();
        _composer = new PdfComposerViewModel(parameters, context.Runtime);
        return new PdfComposerView { DataContext = _composer };
    }

    public Task<JToken?> ReadAsync()
    {
        PdfComposerViewModel composer = _composer ?? throw new InvalidOperationException("The interface isn't shown.");
        return Task.FromResult<JToken?>(composer.Answer());
    }
}
