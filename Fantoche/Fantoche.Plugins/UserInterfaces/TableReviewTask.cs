using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.UserInterfaces;

public class TableReviewParameters
{
    /// <summary>What the user is asked to check, shown above the table.</summary>
    public string Title { get; set; } = "";
    public TableRef Table { get; set; } = new();
}

public class TableReviewResult
{
    public TableRef Table { get; set; } = new();
}

/// <summary>
/// The user checks a table, an import for instance, before the run carries on with it : the page
/// of <c>UserInterfaces/TableReview</c>, showing its rows a page at a time.
/// </summary>
public class TableReviewTask : BaseUserInterfaceTask<TableReviewParameters, TableReviewResult>
{
    public override string PageFolder => "UserInterfaces/TableReview";
}
