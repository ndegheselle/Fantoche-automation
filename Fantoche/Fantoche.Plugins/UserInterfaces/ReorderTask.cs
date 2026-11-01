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
/// The items in the order they are given : its page, <see cref="ReorderPage"/>, lets the user put
/// them in order first.
/// </summary>
public class ReorderTask : BaseTask<ReorderParameters, ReorderResult>
{
    public override Task<ReorderResult> DoAsync(ReorderParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
        => Task.FromResult(new ReorderResult { Items = parameters.Items });

    /// <summary>The items in alphabetical order.</summary>
    public static List<string> Sorted(ReorderParameters parameters) => [.. parameters.Items.Order(StringComparer.CurrentCulture)];
}

/// <summary>
/// The page of <see cref="ReorderTask"/>, <c>UserInterfaces/Reorder</c> : the user puts the items in
/// order by dragging them.
/// </summary>
public class ReorderPage : UserInterfaceTask<ReorderParameters>
{
    public override string PageFolder => "UserInterfaces/Reorder";

    /// <summary>What the page offers to start from.</summary>
    [PageMethod]
    public List<string> Sorted(ReorderParameters parameters) => ReorderTask.Sorted(parameters);
}
