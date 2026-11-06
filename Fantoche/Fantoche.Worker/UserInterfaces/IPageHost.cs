using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces;

/// <summary>
/// A task asking the user, as the application shows it (see <see cref="IPageHost"/>) : with the
/// interface of the task (<see cref="BaseAutomationTask.Interface"/>), or the form generated from its
/// parameters.
/// </summary>
public sealed class PageRequest
{
    /// <summary>The instance of the task asking.</summary>
    public required TaskInstance Instance { get; init; }

    /// <summary>The run the task belongs to : the execution at the top, the one followed by the user.</summary>
    public required Guid RunId { get; init; }

    /// <summary>The task asking : its interface, and the schema of its parameters.</summary>
    public required AutomationTask Task { get; init; }

    /// <summary>The parameters the user starts from.</summary>
    public required JToken? Input { get; init; }

    /// <summary>
    /// What the rules of the task are handed, the storage of the run first : the interface reads the
    /// tables and files the parameters point at through it.
    /// </summary>
    public required ITaskRuntime Runtime { get; init; }

    /// <summary>
    /// Hand [answer] to the task : why it doesn't fit what the task expects, null once taken. Taken
    /// once, a second answer being refused.
    /// </summary>
    public required Func<JToken, string?> Answer { get; init; }
}

/// <summary>
/// What shows the tasks asking the user : the application, where the user is.
/// </summary>
public interface IPageHost
{
    /// <summary>
    /// Hold [request] until the returned registration is disposed : the task got its answer, was
    /// canceled or failed. It is shown to the user meanwhile, right away or once asked for, and can
    /// be closed and opened again until it is answered.
    /// </summary>
    IDisposable Show(PageRequest request);
}
