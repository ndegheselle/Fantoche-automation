using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.UserInterfaces;

/// <summary>
/// The page of a task asking the user while it runs (see <see cref="IUserInterfaceTask"/>) : each
/// question shown by the <see cref="IPageHost"/> of the application, the task waiting for the
/// answer meanwhile, its instance reported as waiting for the user.
/// </summary>
internal sealed class PageSession : IUserInterface
{
    private readonly IPageHost _host;
    private readonly string _folder;
    private readonly object _task;
    private readonly TaskInstance _instance;
    private readonly Guid _runId;
    private readonly IStorage _storage;
    private readonly IProgress<TaskInstance>? _states;
    private ITaskRuntime? _runtime;

    /// <param name="task">The instance of the task running, whose page methods the page calls.</param>
    /// <param name="states">Where the instance is reported waiting for the user, then running again.</param>
    public PageSession(IPageHost host, string folder, object task, TaskInstance instance, Guid runId, IStorage storage, IProgress<TaskInstance>? states)
    {
        _host = host;
        _folder = folder;
        _task = task;
        _instance = instance;
        _runId = runId;
        _storage = storage;
        _states = states;
    }

    /// <summary>
    /// The runtime of the task, handed to its page methods : built after the session, which it holds.
    /// </summary>
    public void Bind(ITaskRuntime runtime) => _runtime = runtime;

    public async Task<TAnswer> AskAsync<TAnswer>(object? input, CancellationToken cancellation = default)
    {
        ITaskRuntime runtime = _runtime ?? throw new InvalidOperationException("The page is asked before the task runs.");
        TaskCompletionSource<TAnswer> answered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PageRequest request = new()
        {
            Instance = _instance,
            RunId = _runId,
            Folder = _folder,
            Input = input == null ? null : JToken.FromObject(input),
            Storage = _storage,
            CallAsync = (name, args, token) => PageMethods.CallAsync(_task, name, args, runtime, token),
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

    /// <summary>[answer] read as what the task expects : why it can't be, null once handed over.</summary>
    private static string? TryAnswer<TAnswer>(JToken answer, TaskCompletionSource<TAnswer> answered)
    {
        TAnswer value;
        try
        {
            value = answer.ToObject<TAnswer>()!;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or FormatException)
        {
            return $"The answer doesn't fit what the task expects : {exception.Message}";
        }

        return answered.TrySetResult(value) ? null : "The task was answered already.";
    }

    private void Report(EnumTaskState state)
    {
        _instance.State = state;
        _states?.Report(_instance);
    }
}
