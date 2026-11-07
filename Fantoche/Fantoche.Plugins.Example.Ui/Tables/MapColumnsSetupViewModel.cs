using System.Collections.ObjectModel;
using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;

namespace Fantoche.Plugins.Example.Ui.Tables;

/// <summary>A name of the workflow, and the stored column it is expected to stand for, if known.</summary>
public partial class ExpectedName : ObservableObject
{
    [ObservableProperty] private string _name = "";

    /// <summary>Left empty, the column whose name looks the most alike is picked in the run.</summary>
    [ObservableProperty] private string _column = "";

    public ColumnMapping ToMapping() => new() { Name = Name.Trim(), Column = Column.Trim() };
}

/// <summary>
/// The names of the workflow as the user writes them outside a run : the logic of
/// <see cref="MapColumnsSetupView"/>, which only shows it. The table isn't read yet : each name is
/// given the column it is expected to stand for, the run picking the actual one
/// (<see cref="MapColumnsViewModel"/>). The names are checked by the rule of
/// <see cref="MapColumnsTask"/>.
/// </summary>
public partial class MapColumnsSetupViewModel : ObservableObject
{
    private readonly JObject _parameters;

    /// <summary>What the source can point at : the objects the node reads ("$previous").</summary>
    public IReadOnlyList<string> SourceReferences { get; }

    /// <summary>The table to name the columns of, a reference to what the node reads.</summary>
    [ObservableProperty] private string _source;

    public ObservableCollection<ExpectedName> Names { get; } = [];

    /// <param name="parameters">The mapping of the node, or the parameters it is started with.</param>
    /// <exception cref="InvalidOperationException">The source or the names are written in a way only the generated form edits.</exception>
    public MapColumnsSetupViewModel(JObject parameters, IReadOnlyList<TaskReference> references)
    {
        _parameters = parameters;
        SourceReferences = [.. references.Where(x => x.Type == "object").Select(x => x.Reference)];

        _source = parameters[nameof(MapColumnsParameters.Source)] switch
        {
            null or { Type: JTokenType.Null } => SourceReferences.FirstOrDefault() ?? "",
            JValue { Type: JTokenType.String } reference => (string)reference!,
            JToken other => throw new InvalidOperationException($"The source is written as [{other}] : the generated form edits it."),
        };

        List<ColumnMapping> columns = parameters[nameof(MapColumnsParameters.Columns)] switch
        {
            null or { Type: JTokenType.Null } => [],
            JArray written => written.ToObject<List<ColumnMapping>>() ?? [],
            JToken other => throw new InvalidOperationException($"The names are read from [{other}] : the generated form edits where they come from."),
        };
        foreach (ColumnMapping column in columns)
            Names.Add(new ExpectedName { Name = column.Name, Column = column.Column });
    }

    [RelayCommand]
    private void AddName() => Names.Add(new ExpectedName());

    [RelayCommand]
    private void RemoveName(ExpectedName name) => Names.Remove(name);

    /// <summary>The mapping as it came, with the source and the names written here.</summary>
    /// <exception cref="TaskInterfaceException">A name is missing or given twice.</exception>
    public JObject Answer()
    {
        List<ColumnMapping> columns = [.. Names.Select(x => x.ToMapping())];
        // The rule of the task rather than a copy : what it refuses, the run would fail on.
        string? refusal = MapColumnsTask.CheckNames(columns);
        if (refusal != null)
            throw new TaskInterfaceException(refusal);
        if (string.IsNullOrWhiteSpace(Source))
            throw new TaskInterfaceException("Pick the table to name the columns of.");

        JObject answer = (JObject)_parameters.DeepClone();
        answer[nameof(MapColumnsParameters.Source)] = Source.Trim();
        answer[nameof(MapColumnsParameters.Columns)] = JToken.FromObject(columns);
        return answer;
    }
}
