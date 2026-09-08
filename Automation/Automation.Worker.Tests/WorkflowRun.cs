using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Worker.Executor;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Tests;

/// <summary>
/// What a node was reported as, when it was reported : an instance is reported several times and
/// mutated in between (progressing then completed), so what it held then is copied rather than
/// referenced.
/// </summary>
internal sealed record NodeReport(string NodeName, EnumTaskState State, JToken? Parameters, JToken? Output)
{
    public static NodeReport Of(TaskInstance instance) => new(
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
    private sealed class Sink<T> : IProgress<T>
    {
        private readonly Action<T> _report;
        public Sink(Action<T> report) => _report = report;
        public void Report(T value) => _report(value);
    }

    private readonly List<NodeReport> _reports = [];
    private readonly List<TaskNotification> _notifications = [];
    private readonly object _lock = new();

    /// <summary>The instance of the workflow itself, once the run is over.</summary>
    public WorkflowInstance Instance { get; private set; } = null!;

    /// <summary>The packages the tasks were loaded from.</summary>
    public TestPackageManagement Packages { get; } = new();

    /// <summary>Every state change reported, in order.</summary>
    public IReadOnlyList<NodeReport> Reports
    {
        get { lock (_lock) return [.. _reports]; }
    }

    /// <summary>Every notification the tasks reported, in order.</summary>
    public IReadOnlyList<TaskNotification> Notifications
    {
        get { lock (_lock) return [.. _notifications]; }
    }

    /// <summary>Run [instance] and record everything it reports.</summary>
    public static async Task<WorkflowRun> ExecuteAsync(WorkflowInstance instance, CancellationToken? cancellation = null)
    {
        WorkflowRun run = new();
        run.Instance = await run.RunAsync(instance, cancellation);
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
            run.Instance = await run.RunAsync(instance, cancellation);
            return (run, null);
        }
        catch (Exception exception)
        {
            run.Instance = instance;
            return (run, exception);
        }
    }

    private Task<WorkflowInstance> RunAsync(WorkflowInstance instance, CancellationToken? cancellation)
    {
        WorkflowExecutor executor = new(Packages);
        TaskInstancesProgress progress = new()
        {
            StateChanges = new Sink<TaskInstance>(x =>
            {
                lock (_lock)
                    _reports.Add(NodeReport.Of(x));
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
