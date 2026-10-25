using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Example.UI;

public class PickColumnsParameters
{
    /// <summary>What the user is asked to map, shown above the columns.</summary>
    public string Title { get; set; } = "";

    [JsonSchemaExtensionData(OutputPreview.SchemaKey, nameof(PickColumnsResult.Source))]
    public TableRef Source { get; set; } = new();

    /// <summary>
    /// The names the rest of the workflow reads the table by, each with the column it is expected
    /// to stand for : left empty, the page picks the column whose name looks the most alike.
    /// </summary>
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, nameof(PickColumnsResult.Columns))]
    public List<ColumnMapping> Columns { get; set; } = [];
}

public class PickColumnsResult
{
    public TableRef Source { get; set; } = new();

    /// <summary>The names asked for, each with the column the user picked, then the names the user gave to other columns.</summary>
    public List<ColumnMapping> Columns { get; set; } = [];
}

/// <summary>
/// The user picks the column of the table each name stands for, and can name the other ones : the
/// page of <c>UI/MapColumns</c>. <see cref="MapColumnsTask"/> applies the answer.
/// <para>
/// The names are the ones of the workflow, written in the editor, so the nodes after can be written
/// against them whatever the file calls its columns.
/// </para>
/// </summary>
public class PickColumnsTask : BaseUITask<PickColumnsParameters, PickColumnsResult>
{
    public override string PageFolder => "UI/MapColumns";
}
