namespace Fantoche.Plugins.Shared;

/// <summary>
/// A task the user does rather than the machine : an HTML page shown to the user, whose answer is
/// the output of the task. Reaching it holds its branch until the user answers, nothing running
/// meanwhile, then the run carries on with the answer.
/// <para>
/// The page reads what the task was given as <c>window.fantoche.input</c>, and answers with
/// <c>window.fantoche.answer(value)</c> : a promise rejected when the answer doesn't fit the output
/// of the task, the page being closed once it does. It only reaches the files of its own folder.
/// </para>
/// </summary>
public interface IUserInterfaceTask : ITask
{
    /// <summary>
    /// The folder of the page, relative to the assembly of the task : its <c>index.html</c> is
    /// opened, the rest of the folder served along. Packed next to the assembly, see the
    /// <c>Fantoche.Plugins</c> project.
    /// </summary>
    string PageFolder { get; }
}

/// <summary>
/// A user interface task reading a <typeparamref name="TInput"/> and answered with a
/// <typeparamref name="TOutput"/>, see <see cref="IUserInterfaceTask"/>.
/// </summary>
public abstract class BaseUserInterfaceTask<TInput, TOutput> : IUserInterfaceTask
{
    public TaskConnector? Input { get; } = new() { Type = typeof(TInput) };
    public TaskConnector? Output { get; } = new() { Type = typeof(TOutput) };

    public abstract string PageFolder { get; }

    /// <summary>Never called : the engine waits for the user instead of running it.</summary>
    public Task<object?> DoAsync(object? parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
        => throw new InvalidOperationException("A user interface task is answered by the user, it is never run.");
}
