using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Remote;

/// <summary>
/// A task to run on a worker, with everything the worker needs to run it.
/// </summary>
public class RemoteExecution
{
    public required BaseAutomationTask Task { get; init; }

    /// <summary>
    /// Every task the graph of <see cref="Task"/> points at, nested workflows included, when it is a
    /// workflow : a graph only holds the ids of its tasks.
    /// </summary>
    public List<BaseAutomationTask> GraphTasks { get; init; } = [];

    public required RemoteInstance Instance { get; init; }

    /// <summary>
    /// [task] run with [instance], the graph of a workflow having to be refreshed.
    /// </summary>
    public static RemoteExecution From(BaseAutomationTask task, TaskInstance instance)
    {
        Dictionary<Guid, BaseAutomationTask> graphTasks = [];
        if (task is AutomationWorkflow workflow)
            CollectGraphTasks(workflow, graphTasks);

        return new RemoteExecution
        {
            Task = task,
            GraphTasks = [.. graphTasks.Values],
            Instance = RemoteInstance.From(instance),
        };
    }

    /// <summary>
    /// The task and its instance as an executor runs them : the graphs refreshed and a workflow
    /// given a workflow instance.
    /// </summary>
    public (BaseAutomationTask Task, TaskInstance Instance) Unpack()
    {
        if (Task is not AutomationWorkflow workflow)
            return (Task, Instance.ToInstance());

        Dictionary<Guid, BaseAutomationTask> tasks = GraphTasks.ToDictionary(x => x.Id);
        workflow.Graph.Refresh(tasks, force: true);
        foreach (AutomationWorkflow nested in tasks.Values.OfType<AutomationWorkflow>())
            nested.Graph.Refresh(tasks, force: true);

        WorkflowInstance instance = new(workflow);
        Instance.CopyTo(instance);
        return (workflow, instance);
    }

    private static void CollectGraphTasks(AutomationWorkflow workflow, Dictionary<Guid, BaseAutomationTask> tasks)
    {
        foreach (BaseGraphTask node in workflow.Graph.Nodes.OfType<BaseGraphTask>())
        {
            // The controls are known by every graph on its own.
            if (node is GraphControl)
                continue;

            BaseAutomationTask task = node.AutomationTask
                ?? throw new ExecutionException($"The tasks of the workflow '{workflow.Metadata.Name}' are not loaded (is the graph refreshed?).");

            if (!tasks.TryAdd(task.Id, task))
                continue;
            if (task is AutomationWorkflow nested)
                CollectGraphTasks(nested, tasks);
        }
    }
}

/// <summary>
/// The stored data of a <see cref="TaskInstance"/>, as it travels between the caller, the supervisor
/// and a worker. The instance itself can't : it links the ones before it and the node it runs.
/// </summary>
public record RemoteInstance
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid? NodeId { get; init; }
    public Guid? ParentInstanceId { get; init; }
    public string NodeName { get; init; } = "";
    public string? Shared { get; init; }
    public string? Parameters { get; init; }
    public string? Output { get; init; }
    /// <summary>Only for a workflow, see <see cref="WorkflowInstance.GlobalContext"/>.</summary>
    public string? GlobalContext { get; init; }
    public EnumTaskState State { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? FinishedAt { get; init; }

    public static RemoteInstance From(TaskInstance instance) => new()
    {
        Id = instance.Id,
        TaskId = instance.TaskId,
        NodeId = instance.NodeId,
        ParentInstanceId = instance.ParentInstanceId,
        NodeName = instance.NodeName,
        Shared = Write(instance.Shared),
        Parameters = Write(instance.Parameters),
        Output = Write(instance.Output),
        GlobalContext = Write((instance as WorkflowInstance)?.GlobalContext),
        State = instance.State,
        CreatedAt = instance.CreatedAt,
        FinishedAt = instance.FinishedAt,
    };

    public TaskInstance ToInstance()
    {
        TaskInstance instance = new();
        CopyTo(instance);
        return instance;
    }

    /// <summary>
    /// Copy onto [instance] what running it changed.
    /// </summary>
    public void ApplyResultTo(TaskInstance instance)
    {
        instance.State = State;
        instance.Output = Read(Output);
        // Set after the state, which stamps its own.
        instance.FinishedAt = FinishedAt;
    }

    /// <summary>
    /// This instance ended without having run to its end, with [error] as its output.
    /// </summary>
    public RemoteInstance Finished(EnumTaskState state, string? error = null) => this with
    {
        State = state,
        Output = error == null ? Output : Write(JToken.FromObject(error)),
        FinishedAt = DateTime.UtcNow,
    };

    internal void CopyTo(TaskInstance instance)
    {
        instance.Id = Id;
        instance.TaskId = TaskId;
        instance.NodeId = NodeId;
        instance.ParentInstanceId = ParentInstanceId;
        instance.NodeName = NodeName;
        instance.Shared = Read(Shared);
        instance.Parameters = Read(Parameters);
        instance.CreatedAt = CreatedAt;
        if (instance is WorkflowInstance workflowInstance)
            workflowInstance.GlobalContext = Read(GlobalContext);
        ApplyResultTo(instance);
    }

    private static string? Write(JToken? token) => token?.ToString(Formatting.None);

    private static JToken? Read(string? json) => json == null ? null : JToken.Parse(json);
}
