using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces;

/// <summary>
/// A task asking the user for its parameters : shown by the <see cref="IPageHost"/> of the
/// application, the instance asking reported as waiting for the user meanwhile.
/// </summary>
internal sealed class PageSession
{
    private readonly IPageHost _host;
    private readonly AutomationTask _automationTask;
    private readonly ITask _task;
    private readonly ITaskRuntime _runtime;
    private readonly TaskInstance _instance;
    private readonly Guid _runId;
    private readonly IProgress<TaskInstance>? _states;

    /// <param name="task">An instance of the task asked for, which the answer is read as the parameters of.</param>
    /// <param name="runtime">What the interface hands the rules of the task.</param>
    /// <param name="states">Where the instance is reported waiting for the user, then running again.</param>
    public PageSession(IPageHost host, AutomationTask automationTask, ITask task, ITaskRuntime runtime, TaskInstance instance, Guid runId, IProgress<TaskInstance>? states)
    {
        _host = host;
        _automationTask = automationTask;
        _task = task;
        _runtime = runtime;
        _instance = instance;
        _runId = runId;
        _states = states;
    }

    /// <summary>
    /// Ask the user with the parameters [input] until they answer : the answer, written as the
    /// parameters of the task are.
    /// </summary>
    public async Task<JToken?> AskAsync(JToken? input, CancellationToken cancellation)
    {
        TaskCompletionSource<JToken?> answered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PageRequest request = new()
        {
            Instance = _instance,
            RunId = _runId,
            Task = _automationTask,
            Input = input,
            Runtime = _runtime,
            Answer = answer => TryAnswer(answer, answered),
        };

        Report(EnumTaskState.AwaitingUser);
        try
        {
            using (_host.Show(request))
                return await answered.Task.WaitAsync(cancellation);
        }
        finally
        {
            Report(EnumTaskState.Progressing);
        }
    }

    /// <summary>[answer] read as the parameters of the task : why it can't be, null once handed over.</summary>
    private string? TryAnswer(JToken answer, TaskCompletionSource<JToken?> answered)
    {
        JToken? parameters = answer;
        Type? type = _task.Input?.Type;
        if (type != null)
        {
            try
            {
                // Written back from what it reads as : the task runs with what it would have read.
                object? value = answer.ToObject(type);
                parameters = value == null ? null : Schemas.ValueOf(value);
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or FormatException)
            {
                return $"The answer doesn't fit what the task expects : {exception.Message}";
            }
        }

        return answered.TrySetResult(parameters) ? null : "The task was answered already.";
    }

    private void Report(EnumTaskState state)
    {
        _instance.State = state;
        _states?.Report(_instance);
    }
}
