using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces;

/// <summary>
/// A task asking the user, as the application shows its page (see <see cref="IPageHost"/>).
/// </summary>
public sealed class PageRequest
{
    /// <summary>The instance of the task asking.</summary>
    public required TaskInstance Instance { get; init; }

    /// <summary>The run the task belongs to : the execution at the top, the one followed by the user.</summary>
    public required Guid RunId { get; init; }

    /// <summary>The folder of the page, extracted on this machine.</summary>
    public required string Folder { get; init; }

    /// <summary>What the page reads as <c>window.fantoche.input</c>.</summary>
    public required JToken? Input { get; init; }

    /// <summary>The storage of the run, whose files and tables the page reads.</summary>
    public required IStorage Storage { get; init; }

    /// <summary>
    /// Call the page method [name] of the task with [args] : what it returns, see
    /// <see cref="PageMethodAttribute"/>.
    /// </summary>
    public required Func<string, JToken?, CancellationToken, Task<JToken?>> CallAsync { get; init; }

    /// <summary>
    /// Hand [answer] to the task : why it doesn't fit what the task expects, null once taken. Taken
    /// once, a second answer being refused.
    /// </summary>
    public required Func<JToken, string?> Answer { get; init; }
}

/// <summary>
/// What shows the pages of the tasks asking the user : the application, where the user is.
/// </summary>
public interface IPageHost
{
    /// <summary>
    /// Hold [request] until the returned registration is disposed : the task got its answer, was
    /// canceled or failed. The page is shown to the user meanwhile, right away or once asked for,
    /// and can be closed and opened again until it is answered.
    /// </summary>
    IDisposable Show(PageRequest request);
}
