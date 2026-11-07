using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Fantoche.Plugins.Example.Tasks.Pdf;
using Fantoche.Plugins.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Fantoche.Plugins.Example.Ui.Pdf;

/// <summary>A column a table block shows, see <see cref="PdfColumn"/>.</summary>
public partial class PdfColumnViewModel : ObservableObject
{
    [ObservableProperty] private string _column = "";
    [ObservableProperty] private string _header = "";

    public PdfColumn ToColumn() => new() { Column = Column.Trim(), Header = Header };
}

/// <summary>A part of the document, see <see cref="PdfBlock"/>.</summary>
public partial class PdfBlockViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTable), nameof(IsText))]
    private EnumPdfBlock _kind;

    [ObservableProperty] private string _text = "";

    public bool IsTable => Kind == EnumPdfBlock.Table;

    public bool IsText => !IsTable;

    /// <summary>Every column of the table when empty.</summary>
    public ObservableCollection<PdfColumnViewModel> Columns { get; } = [];

    public PdfBlockViewModel(PdfBlock block)
    {
        _kind = block.Kind;
        _text = block.Text;
        foreach (PdfColumn column in block.Columns)
            Columns.Add(new PdfColumnViewModel { Column = column.Column, Header = column.Header });
    }

    public PdfBlock ToBlock() => new()
    {
        Kind = Kind,
        Text = IsTable ? "" : Text,
        Columns = IsTable ? [.. Columns.Select(x => x.ToColumn())] : [],
    };

    /// <summary>Add [placeholder] ({index}, {count} or {rows}) at the end of the text.</summary>
    [RelayCommand]
    private void AddPlaceholder(string placeholder) => Text = $"{Text}{placeholder}";

    [RelayCommand]
    private void AddColumn() => Columns.Add(new PdfColumnViewModel());

    [RelayCommand]
    private void RemoveColumn(PdfColumnViewModel column) => Columns.Remove(column);
}

/// <summary>
/// The document <see cref="ExportPdfTask"/> renders, as the user composes it : the logic of
/// <see cref="PdfComposerView"/>, which only shows it. Edits <see cref="ExportPdfParameters.Document"/>
/// and <see cref="ExportPdfParameters.FileName"/>, the other parameters being handed back as they
/// came : outside a run, they may point at what the node reads ("$previous.Batches").
/// </summary>
public partial class PdfComposerViewModel : ObservableObject
{
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        // The schema of the parameters names the kinds of blocks rather than numbering them.
        Converters = [new StringEnumConverter()],
    });

    private readonly JObject _parameters;
    private readonly List<TableRef> _sources;
    private readonly ITaskRuntime? _runtime;

    public IReadOnlyList<EnumPdfBlock> Kinds { get; } = Enum.GetValues<EnumPdfBlock>();

    public ObservableCollection<PdfBlockViewModel> Blocks { get; } = [];

    /// <summary>What a table can show : the columns of the first source, under their names. Empty outside a run.</summary>
    public IReadOnlyList<string> SourceColumns { get; }

    [ObservableProperty] private bool _landscape;

    [ObservableProperty] private string _fileName;

    /// <summary>Whether the document can be previewed : in a run, with a table to render it on.</summary>
    public bool CanPreviewDocument => _runtime != null && _sources.Count > 0;

    public string Summary => CanPreviewDocument
        ? $"{_sources.Count} document(s) to export, one per table. The preview renders the first one."
        : "The tables are only known in a run : the preview shows there.";

    /// <summary>What happened to the last preview : where it opened, or why it couldn't be rendered.</summary>
    [ObservableProperty] private string? _previewStatus;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviewCommand))]
    private bool _isPreviewing;

    /// <param name="parameters">The parameters in a run, the mapping of the node outside of it.</param>
    /// <param name="runtime">Null outside a run.</param>
    /// <exception cref="InvalidOperationException">The document is read from elsewhere rather than written here.</exception>
    public PdfComposerViewModel(JObject parameters, ITaskRuntime? runtime)
    {
        _parameters = parameters;
        _runtime = runtime;
        _sources = runtime == null ? [] : parameters[nameof(ExportPdfParameters.Sources)]?.ToObject<List<TableRef>>() ?? [];

        PdfLayout layout = parameters[nameof(ExportPdfParameters.Document)] switch
        {
            null or { Type: JTokenType.Null } => new PdfLayout(),
            JObject document => document.ToObject<PdfLayout>() ?? new PdfLayout(),
            JToken other => throw new InvalidOperationException($"The document is read from [{other}] : the generated form edits where it comes from."),
        };
        _landscape = layout.Landscape;
        foreach (PdfBlock block in layout.Blocks)
            Blocks.Add(new PdfBlockViewModel(block));

        _fileName = parameters[nameof(ExportPdfParameters.FileName)]?.Value<string>() ?? new ExportPdfParameters().FileName;

        TableRef? first = _sources.FirstOrDefault();
        SourceColumns = first == null ? [] : [.. first.Columns.Select(x => first.NameOf(x.Name))];
    }

    [RelayCommand]
    private void AddHeading() => AddBlock(EnumPdfBlock.Heading);

    [RelayCommand]
    private void AddText() => AddBlock(EnumPdfBlock.Text);

    [RelayCommand]
    private void AddTable() => AddBlock(EnumPdfBlock.Table);

    private void AddBlock(EnumPdfBlock kind) => Blocks.Add(new PdfBlockViewModel(new PdfBlock { Kind = kind }));

    [RelayCommand]
    private void RemoveBlock(PdfBlockViewModel block) => Blocks.Remove(block);

    [RelayCommand]
    private void MoveUp(PdfBlockViewModel block) => Move(block, -1);

    [RelayCommand]
    private void MoveDown(PdfBlockViewModel block) => Move(block, 1);

    private void Move(PdfBlockViewModel block, int offset)
    {
        int index = Blocks.IndexOf(block);
        int target = index + offset;
        if (index < 0 || target < 0 || target >= Blocks.Count)
            return;
        Blocks.Move(index, target);
    }

    /// <summary>Render the first document with the export itself, and open it with the viewer of the system.</summary>
    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task Preview()
    {
        IsPreviewing = true;
        PreviewStatus = "Rendering the preview…";
        try
        {
            PdfLayout layout = CheckedLayout();
            ExportPdfParameters parameters = new() { Sources = _sources, Document = layout, FileName = FileName };
            FileRef file = await ExportPdfTask.PreviewAsync(parameters, _runtime!, CancellationToken.None);
            string path = await SaveLocallyAsync(file);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            PreviewStatus = $"Preview opened : {path}";
        }
        catch (Exception exception)
        {
            PreviewStatus = $"The preview can't be rendered : {exception.Message}";
        }
        finally
        {
            IsPreviewing = false;
        }
    }

    private bool CanPreview() => CanPreviewDocument && !IsPreviewing;

    /// <summary>
    /// The parameters as they came, with the document and the name of the files composed here.
    /// </summary>
    /// <exception cref="TaskInterfaceException">The export would refuse the document.</exception>
    public JObject Answer()
    {
        PdfLayout layout = CheckedLayout();
        JObject answer = (JObject)_parameters.DeepClone();
        answer[nameof(ExportPdfParameters.Document)] = JToken.FromObject(layout, Serializer);
        answer[nameof(ExportPdfParameters.FileName)] = FileName;
        return answer;
    }

    /// <summary>The document as composed, refused as the export would refuse it.</summary>
    /// <exception cref="TaskInterfaceException">The export would refuse it.</exception>
    private PdfLayout CheckedLayout()
    {
        PdfLayout layout = new() { Landscape = Landscape, Blocks = [.. Blocks.Select(x => x.ToBlock())] };
        // The rule of the task rather than a copy : what it refuses, the run would fail on.
        string? refusal = ExportPdfTask.Check(layout, _sources);
        if (refusal != null)
            throw new TaskInterfaceException(refusal);
        return layout;
    }

    /// <summary>
    /// A copy of [file] on this machine for the viewer to open : the storage of the run may be the
    /// one of the workers, reached through the supervisor.
    /// </summary>
    private async Task<string> SaveLocallyAsync(FileRef file)
    {
        string path = Path.Combine(Path.GetTempPath(), $"fantoche-preview-{file.Id}.pdf");
        await using Stream content = await _runtime!.Storage.OpenFileAsync(file);
        await using FileStream local = File.Create(path);
        await content.CopyToAsync(local);
        return path;
    }
}
