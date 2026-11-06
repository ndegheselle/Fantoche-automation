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
/// The items in the order they are given : its page, <c>UserInterfaces/Reorder</c>, lets the user
/// put them in order first, by dragging them.
/// </summary>
public class ReorderTask : BaseTask<ReorderParameters, ReorderResult>, ITaskWithPage
{
    public string PageFolder => "UserInterfaces/Reorder";

    public override Task<ReorderResult> DoAsync(ReorderParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
        => Task.FromResult(new ReorderResult { Items = parameters.Items });

    /// <summary>The items in alphabetical order : what the page offers to start from.</summary>
    [PageMethod]
    public static List<string> Sorted(ReorderParameters parameters) => [.. parameters.Items.Order(StringComparer.CurrentCulture)];
}
