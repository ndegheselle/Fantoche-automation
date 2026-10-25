using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.UserInterfaces;

public class ReorderParameters
{
    /// <summary>What the user is asked to sort, shown above the list.</summary>
    public string Title { get; set; } = "";
    public List<string> Items { get; set; } = [];
}

public class ReorderResult
{
    public List<string> Items { get; set; } = [];
}

/// <summary>
/// The user puts the items in the order of their choice by dragging them : the page of
/// <c>UserInterfaces/Reorder</c>.
/// </summary>
public class ReorderTask : BaseUITask<ReorderParameters, ReorderResult>
{
    public override string PageFolder => "UserInterfaces/Reorder";
}
