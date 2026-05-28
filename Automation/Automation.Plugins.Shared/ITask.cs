namespace Automation.Plugins.Shared;

public enum EnumTaskNotificationState
{
    Info,
    Warning,
    Error,
    Success
}

public struct TaskNotification
{
    public TaskNotification()
    {
    }

    public EnumTaskNotificationState State { get; set; } = EnumTaskNotificationState.Info;
    public string Message { get; set; } = "";
    public float Progress { get; set; } = 0;
}

public class TaskConnector
{
    /// <summary>
    /// Branch name for output connectors (e.g. "true"/"false" on a condition).
    /// Empty for the default single connector.
    /// </summary>
    public string Name { get; set; } = "";

    public Type? Type { get; set; }
}

public interface ITask
{
    public TaskConnector? Input { get; }

    /// <summary>
    /// If a task has an output connector without a type the task is considered "pass-through"
    /// (the downstream context skips past it to read the upstream task's output instead).
    /// </summary>
    public TaskConnector? Output { get; }

    /// <summary>
    /// Named output slots the task can selectively activate via <see cref="ITaskRuntime.ActivateOutputs"/>.
    /// Empty (the default) means a single, always-active output connector. Multiple branches
    /// are how condition/split/router-style tasks describe their fan-out to the graph editor.
    /// </summary>
    public IReadOnlyList<string> OutputBranches => [];

    /// <summary>
    /// Execute the task asynchronously.
    /// </summary>
    /// <param name="parameters">Resolved parameters of the task (the node's parameters template with context references replaced).</param>
    /// <param name="runtime">Engine-provided capabilities (progress, output activation, ...).</param>
    /// <param name="cancellation">Cancellation token.</param>
    public Task<object?> DoAsync(object? parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null);
}

public static class TaskExtensions
{
    /// <summary>
    /// A task is "pass-through" when it has an output connector but no concrete output type.
    /// The next task's context skips past it and reads from the pass-through's predecessors.
    /// </summary>
    public static bool IsPassThrough(this ITask task)
        => task.Output is not null && task.Output.Type is null;
}

public abstract class BaseTask<TInput, TOutput> : ITask
{
    public TaskConnector? Input { get; } = new() { Type = typeof(TInput) };
    public TaskConnector? Output { get; } = new() { Type = typeof(TOutput) };

    public virtual IReadOnlyList<string> OutputBranches => [];

    public async Task<object?> DoAsync(object? parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null)
    {
        if (parameters is not TInput input)
            throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));

        return await DoAsync(input, runtime, cancellation);
    }

    public abstract Task<TOutput> DoAsync(TInput parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null);
}

public abstract class BasePassThroughTask<TInput> : ITask
{
    public TaskConnector? Input { get; } = new() { Type = typeof(TInput) };
    public TaskConnector? Output { get; } = new();

    public virtual IReadOnlyList<string> OutputBranches => [];

    public async Task<object?> DoAsync(object? parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null)
    {
        if (parameters is not TInput input)
            throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));
        await DoAsync(input, runtime, cancellation);
        // Pass-through tasks don't produce their own output — the downstream context
        // transparently walks past them to read the upstream tasks' outputs.
        return null;
    }

    public abstract Task DoAsync(TInput parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null);
}

public abstract class BaseOutputlessTask<TInput> : ITask
{
    public TaskConnector? Input { get; } = new() { Type = typeof(TInput) };
    public TaskConnector? Output { get; } = null;

    public IReadOnlyList<string> OutputBranches => [];

    public async Task<object?> DoAsync(object? parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null)
    {
        if (parameters is not TInput input)
            throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));
        await DoAsync(input, runtime, cancellation);
        return null;
    }

    public abstract Task DoAsync(TInput parameters, ITaskRuntime runtime,
        CancellationToken? cancellation = null);
}
