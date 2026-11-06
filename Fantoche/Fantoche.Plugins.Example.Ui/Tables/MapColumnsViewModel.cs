using System.Collections.ObjectModel;
using System.Data;
using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantoche.Plugins.Example.Ui.Tables;

/// <summary>A name of the workflow, and the stored column the user picked for it.</summary>
public partial class NamedColumn(string name, string column, Func<string, string> sampleOf) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Sample), nameof(IsMissing))]
    private string _column = column;

    public bool IsMissing => string.IsNullOrEmpty(Column);

    /// <summary>What the column holds on the first row.</summary>
    public string Sample => sampleOf(Column);
}

/// <summary>A stored column no name of the workflow picked, and the name the user gives it, if any.</summary>
public partial class OtherColumn(string column, string name, string sample) : ObservableObject
{
    public string Column { get; } = column;

    [ObservableProperty] private string _name = name;

    public string Sample { get; } = sample;
}

/// <summary>
/// The columns of the table being mapped, as the user picks them : the logic of
/// <see cref="MapColumnsView"/>, which only shows it. The suggestions and the checks are the rules of
/// <see cref="MapColumnsTask"/>.
/// </summary>
public partial class MapColumnsViewModel : ObservableObject
{
    private const int PreviewSize = 20;

    private readonly MapColumnsParameters _parameters;
    private readonly IStorage _storage;
    private TablePage? _page;

    /// <summary>
    /// The name given to each stored column outside the names of the workflow, kept while a name of
    /// the workflow picks the column, so it comes back once the column is free again.
    /// </summary>
    private readonly Dictionary<string, string> _otherNames = [];

    public string Title { get; }

    /// <summary>The columns as the table stores them.</summary>
    public ObservableCollection<string> StoredColumns { get; } = [];

    public ObservableCollection<NamedColumn> Expected { get; } = [];

    public ObservableCollection<OtherColumn> Others { get; } = [];

    /// <summary>The first rows, each column headed by the name it will be read by.</summary>
    [ObservableProperty] private DataView? _preview;

    [ObservableProperty] private string _summary = "";

    /// <summary>Why the table can't be read, the columns being left to pick.</summary>
    [ObservableProperty] private string? _error;

    public MapColumnsViewModel(MapColumnsParameters parameters, IStorage storage)
    {
        _parameters = parameters;
        _storage = storage;
        string tableName = parameters.Source.Name;
        Title = $"Columns of {(string.IsNullOrEmpty(tableName) ? "the table" : tableName)}";
    }

    /// <summary>Read the first rows, and start from the columns the task suggests.</summary>
    public async Task LoadAsync()
    {
        try
        {
            _page = await _storage.ReadPageAsync(_parameters.Source, 0, PreviewSize);
        }
        catch (Exception exception)
        {
            Error = $"The table can't be read : {exception.Message}";
            return;
        }

        foreach (TableColumn column in _page.Columns)
            StoredColumns.Add(column.Name);

        List<ColumnMapping> suggested = MapColumnsTask.Suggest(_parameters);
        HashSet<string> names = [.. suggested.Select(x => x.Name)];
        foreach (string column in StoredColumns)
        {
            string mappedName = _parameters.Source.Mapping.FirstOrDefault(x => x.Value == column).Key ?? "";
            _otherNames[column] = names.Contains(mappedName) ? "" : mappedName;
        }

        foreach (ColumnMapping mapping in suggested)
        {
            NamedColumn named = new(mapping.Name, mapping.Column, SampleOf);
            named.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NamedColumn.Column))
                    RefreshOthers();
            };
            Expected.Add(named);
        }
        RefreshOthers();
    }

    /// <summary>The parameters the task runs with, once every name of the workflow has a column.</summary>
    /// <exception cref="TaskInterfaceException">The table isn't read, or a name has no column yet.</exception>
    public MapColumnsParameters Answer()
    {
        if (Error != null)
            throw new TaskInterfaceException(Error);
        if (_page == null)
            throw new TaskInterfaceException("The table is still being read.");

        List<string> missing = [.. Expected.Where(x => x.IsMissing).Select(x => $"Pick the column of [{x.Name}].")];
        if (missing.Count > 0)
            throw new TaskInterfaceException(string.Join("\n", missing));
        return Parameters();
    }

    /// <summary>
    /// Every name of the workflow, then the other columns named : the columns no name picked and no
    /// one named are read under their own name.
    /// </summary>
    private MapColumnsParameters Parameters()
    {
        IEnumerable<ColumnMapping> expected = Expected.Select(x => new ColumnMapping { Name = x.Name, Column = x.Column });
        IEnumerable<ColumnMapping> others = Others
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new ColumnMapping { Name = x.Name.Trim(), Column = x.Column });
        return new MapColumnsParameters { Source = _parameters.Source, Columns = [.. expected, .. others] };
    }

    /// <summary>The columns left to name, since a name of the workflow picked another one.</summary>
    private void RefreshOthers()
    {
        HashSet<string> picked = [.. Expected.Select(x => x.Column)];
        Others.Clear();
        foreach (string column in StoredColumns.Where(x => !picked.Contains(x)))
        {
            OtherColumn other = new(column, _otherNames[column], SampleOf(column));
            other.PropertyChanged += (_, _) =>
            {
                _otherNames[column] = other.Name;
                RefreshPreview();
            };
            Others.Add(other);
        }
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_page == null)
            return;

        List<ColumnMapping> columns = Parameters().Columns;
        Dictionary<string, string> names = columns
            .GroupBy(x => x.Column)
            .ToDictionary(x => x.Key, x => x.First().Name);

        DataTable table = new();
        foreach (string column in StoredColumns)
        {
            string name = names.GetValueOrDefault(column, column);
            // The header tells the stored column apart once it is read by another name.
            string caption = name == column ? column : $"{name} ({column})";
            table.Columns.Add(new DataColumn(column, typeof(string)) { Caption = caption });
        }
        foreach (IReadOnlyDictionary<string, object?> row in _page.Rows)
            table.Rows.Add([.. StoredColumns.Select(column => TextOf(row.GetValueOrDefault(column)))]);

        Preview = table.DefaultView;
        Summary = $"{columns.Count} of {StoredColumns.Count} columns named, {_page.Total} rows";
    }

    private string SampleOf(string column)
    {
        IReadOnlyDictionary<string, object?>? first = _page?.Rows.FirstOrDefault();
        return first == null ? "" : TextOf(first.GetValueOrDefault(column));
    }

    private static string TextOf(object? value) => value?.ToString() ?? "";
}
