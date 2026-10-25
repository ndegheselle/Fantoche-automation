using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Packages;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Storage;
using Fantoche.Worker.UserInterfaces;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// What a node was reported as, when it was reported : an instance is reported several times and
/// mutated in between (progressing then completed), so what it held then is copied rather than
/// referenced.
/// </summary>
internal sealed record NodeReport(Guid Id, string NodeName, EnumTaskState State, JToken? Parameters, JToken? Output)
{
    public static NodeReport Of(TaskInstance instance) => new(
        instance.Id,
        instance.NodeName,
        instance.State,
        instance.Parameters?.DeepClone(),
        instance.Output?.DeepClone());

    public override string ToString() => $"{NodeName} {State} params:{Parameters} output:{Output}";
}

/// <summary>
/// A run of a workflow against the real executor, and everything it reported on the way.
/// <para>
/// The progress is reported synchronously rather than through <see cref="Progress{T}"/> : a report
/// posted to the thread pool can land after the run returned, which no assertion could wait for.
/// </para>
/// </summary>
internal sealed class WorkflowRun
{
    private readonly List<NodeReport> _reports = [];
    /// <summary>The last state of each node reported, as the history stores it.</summary>
    private readonly List<TaskInstance> _stored = [];
    private readonly List<TaskNotification> _notifications = [];
    private readonly object _lock = new();

    /// <summary>The instance of the workflow itself, once the run is over.</summary>
    public WorkflowInstance Instance { get; private set; } = null!;

    /// <summary>The packages the tasks were loaded from.</summary>
    public TestPackageManagement Packages { get; } = new();

    /// <summary>
    /// The storage the tasks were given : a folder of its own per run, created only if a task uses it.
    /// </summary>
    public LocalStorage Storage { get; } = new(Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N")));

    /// <summary>Every state change reported, in order.</summary>
    public IReadOnlyList<NodeReport> Reports
    {
        get { lock (_lock) return [.. _reports]; }
    }

    /// <summary>
    /// What the history holds of the nodes of the run once it is over, the ones of its nested runs
    /// included : what a resumed run is walked again from.
    /// </summary>
    public IReadOnlyList<TaskInstance> Stored
    {
        get { lock (_lock) return [.. _stored]; }
    }

    /// <summary>Every notification the tasks reported, in order.</summary>
    public IReadOnlyList<TaskNotification> Notifications
    {
        get { lock (_lock) return [.. _notifications]; }
    }

    /// <summary>
    /// Run [instance] and record everything it reports, [user] answering the tasks asking the user :
    /// a task asking fails without one, as on a worker reaching no application.
    /// </summary>
    public static async Task<WorkflowRun> ExecuteAsync(WorkflowInstance instance, CancellationToken? cancellation = null, IPageHost? user = null)
    {
        WorkflowRun run = new();
        run.Instance = await run.RunAsync(instance, cancellation, user);
        return run;
    }

    /// <summary>
    /// Run [instance] and hand back what it came to rather than throwing : a workflow refused
    /// before it reached its end still ran a part of its graph, which is what the run holds.
    /// </summary>
    public static async Task<(WorkflowRun Run, Exception? Error)> TryExecuteAsync(
        WorkflowInstance instance,
        CancellationToken? cancellation = null)
    {
        WorkflowRun run = new();
        try
        {
            run.Instance = await run.RunAsync(instance, cancellation, user: null);
            return (run, null);
        }
        catch (Exception exception)
        {
            run.Instance = instance;
            return (run, exception);
        }
    }

    private Task<WorkflowInstance> RunAsync(WorkflowInstance instance, CancellationToken? cancellation, IPageHost? user)
    {
        UserTasks users = user == null ? UserTasks.None : UserTasks.ShownBy(user);
        WorkflowExecutor executor = new(new TaskLoaders(Packages), Storage, users);
        TaskInstancesProgress progress = new()
        {
            StateChanges = new Sink<TaskInstance>(x =>
            {
                lock (_lock)
                {
                    _reports.Add(NodeReport.Of(x));
                    if (x.Id == instance.Id)
                        return;
                    _stored.RemoveAll(stored => stored.Id == x.Id);
                    _stored.Add(FantocheJson.Copy<TaskInstance>(x));
                }
            }),
            Notifications = new Sink<TaskNotification>(x =>
            {
                lock (_lock)
                    _notifications.Add(x);
            }),
        };

        return executor.ExecuteAsync(instance, progress, cancellation);
    }

    #region Reads

    /// <summary>Everything [node] was reported as, in order.</summary>
    public IReadOnlyList<NodeReport> Of(string node)
        => [.. Reports.Where(x => x.NodeName == node)];

    /// <summary>The states [node] went through, in order.</summary>
    public IReadOnlyList<EnumTaskState> States(string node)
        => [.. Of(node).Select(x => x.State)];

    /// <summary>How many times [node] ran to completion.</summary>
    public int CompletionsOf(string node)
        => Of(node).Count(x => x.State == EnumTaskState.Completed);

    /// <summary>What [node] was completed with, null when it never was.</summary>
    public JToken? OutputOf(string node)
        => Of(node).LastOrDefault(x => x.State == EnumTaskState.Completed)?.Output;

    /// <summary>What [node] was last run with, whatever it came to.</summary>
    public JToken? ParametersOf(string node)
        => Of(node).LastOrDefault()?.Parameters;

    /// <summary>The value of [field] of what [node] was completed with.</summary>
    public JToken? OutputOf(string node, string field)
        => OutputOf(node)?[field];

    /// <summary>Every message the tasks notified.</summary>
    public IEnumerable<string> Messages => Notifications.Select(x => x.Message);

    #endregion
}
