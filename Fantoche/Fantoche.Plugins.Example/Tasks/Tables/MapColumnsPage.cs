using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Example.Tasks.Tables;

/// <summary>
/// The page of <see cref="MapColumnsTask"/>, <c>UI/MapColumns</c> : the user picks the column of
/// each name, and names the other columns. The names are the ones of the workflow, written in the
/// editor, so the nodes after are written against them whatever the file calls its columns.
/// </summary>
public class MapColumnsPage : UserInterfaceTask<MapColumnsParameters>
{
    public override string PageFolder => "UI/MapColumns";

    [PageMethod]
    public List<ColumnMapping> Suggest(MapColumnsParameters parameters) => MapColumnsTask.Suggest(parameters);

    [PageMethod]
    public string? Check(MapColumnsParameters parameters) => MapColumnsTask.Check(parameters);
}
