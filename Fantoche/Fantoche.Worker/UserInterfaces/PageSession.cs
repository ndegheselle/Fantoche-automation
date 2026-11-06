using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces;

/// <summary>
/// The page of a task asking the user for its parameters (see <see cref="ITaskWithPage"/>) : shown
/// by the <see cref="IPageHost"/> of the application, the instance asking reported as waiting for
/// the user meanwhile.
/// </summary>
internal sealed class PageSession
{
    private readonly IPageHost _host;
    private readonly string _folder;
    private readonly ITask _task;
    private readonly ITaskRuntime _runtime;
    private readonly TaskInstance _instance;
    private readonly Guid _runId;
    private readonly IStorage _storage;
    private readonly IProgress<TaskInstance>? _states;

    /// <param name="task">An instance of the task asked for, whose page methods the page calls.</param>
    /// <param name="runtime">What the page methods are handed.</param>
    /// <param name="states">Where the instance is reported waiting for the user, then running again.</param>
    public PageSession(IPageHost host, string folder, ITask task, ITaskRuntime runtime, TaskInstance instance, Guid runId, IStorage storage, IProgress<TaskInstance>? states)
    {
        _host = host;
        _folder = folder;
        _task = task;
        _runtime = runtime;
        _instance = instance;
        _runId = runId;
        _storage = storage;
        _states = states;
    }

    /// <summary>
    /// Show the page with the parameters [input] until the user answers it : the answer, written as
    /// the parameters of the task are.
    /// </summary>
    public async Task<JToken?> AskAsync(JToken? input, CancellationToken cancellation)
    {
        TaskCompletionSource<JToken?> answered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PageRequest request = new()
        {
            Instance = _instance,
            RunId = _runId,
            Folder = _folder,
            Input = input,
            Storage = _storage,
            CallAsync = (name, args, token) => PageMethods.CallAsync(_task, name, args, _runtime, token),
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
