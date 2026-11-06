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
/// The items in the order they are given : asking the user lets them put the items in order first,
/// in the form generated from the parameters.
/// </summary>
public class ReorderTask : BaseTask<ReorderParameters, ReorderResult>
{
    public override Task<ReorderResult> DoAsync(ReorderParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
        => Task.FromResult(new ReorderResult { Items = parameters.Items });
}
