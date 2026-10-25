using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.UserInterfaces;

public class ReorderParameters
{
    /// <summary>What the user is asked to sort, shown above the list.</summary>
    public string Title { get; set; } = "";
    public List<string> Items { get; set; } = [];

    /// <summary>Let the user put the items in order on a page, rather than keeping them as given.</summary>
    [JsonSchemaExtensionData(UserInterface.SchemaKey, true)]
    public bool AskUser { get; set; }
}

public class ReorderResult
{
    public List<string> Items { get; set; } = [];
}

/// <summary>
/// The items in the order of the user's choice, put by dragging them on the page of
/// <c>UserInterfaces/Reorder</c> : as given when it doesn't ask the user.
/// </summary>
public class ReorderTask : BaseTask<ReorderParameters, ReorderResult>, IUserInterfaceTask
{
    public string PageFolder => "UserInterfaces/Reorder";

    public override async Task<ReorderResult> DoAsync(ReorderParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        if (runtime.UserInterface is not IUserInterface page)
            return new ReorderResult { Items = parameters.Items };

        return await page.AskAsync<ReorderResult>(parameters, cancellation ?? CancellationToken.None);
    }

    /// <summary>The items in alphabetical order, what the page offers to start from.</summary>
    [PageMethod]
    public List<string> Sorted(ReorderParameters parameters) => [.. parameters.Items.Order(StringComparer.CurrentCulture)];
}
