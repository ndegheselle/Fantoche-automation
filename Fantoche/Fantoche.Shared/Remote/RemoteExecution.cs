using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Remote;

/// <summary>
/// What a caller sends to run a task on a worker : the instance to run, the supervisor reading
/// everything else from its database.
/// </summary>
public class ExecutionRequest
{
    public required Guid InstanceId { get; init; }
    public required Guid TaskId { get; init; }
    public Guid? NodeId { get; init; }
    public Guid? ParentInstanceId { get; init; }
    public string NodeName { get; init; } = "";
    public JToken? Parameters { get; init; }

    /// <summary>
    /// Only for a workflow : the context the run started from, a nested workflow reading the one of
    /// the run holding it (see <see cref="WorkflowInstance.GlobalContext"/>).
    /// </summary>
    public JToken? GlobalContext { get; init; }

    public static ExecutionRequest From(TaskInstance instance) => new()
    {
        InstanceId = instance.Id,
        TaskId = instance.TaskId,
        NodeId = instance.NodeId,
        ParentInstanceId = instance.ParentInstanceId,
        NodeName = instance.NodeName,
        Parameters = instance.Parameters,
        GlobalContext = (instance as WorkflowInstance)?.GlobalContext,
    };

    public TaskInstance ToInstance() => new()
    {
        Id = InstanceId,
        TaskId = TaskId,
        NodeId = NodeId,
        ParentInstanceId = ParentInstanceId,
        NodeName = NodeName,
        Parameters = Parameters,
        State = EnumTaskState.Progressing,
    };
}

/// <summary>
/// A task as the supervisor hands it to a worker, with everything the worker needs to run it : the
/// worker reads no database.
/// </summary>
public class RemoteExecution
{
    public required BaseAutomationTask Task { get; init; }

    /// <summary>
    /// Every task the graph of <see cref="Task"/> points at, nested workflows included, when it is a
    /// workflow : a graph only holds the ids of its tasks.
    /// </summary>
    public List<BaseAutomationTask> GraphTasks { get; init; } = [];

    public required TaskInstance Instance { get; init; }

    /// <summary>See <see cref="ExecutionRequest.GlobalContext"/>.</summary>
    public JToken? GlobalContext { get; init; }

    /// <summary>
    /// [task] run with [instance], the graph of a workflow having to be loaded (see
    /// <see cref="ScopedServiceExtensions.LoadRunnableAsync"/>).
    /// </summary>
    public static RemoteExecution From(BaseAutomationTask task, TaskInstance instance, JToken? globalContext)
    {
        Dictionary<Guid, BaseAutomationTask> graphTasks = [];
        if (task is AutomationWorkflow workflow)
            CollectGraphTasks(workflow, graphTasks);

        return new RemoteExecution
        {
            Task = task,
            GraphTasks = [.. graphTasks.Values],
            Instance = instance,
            GlobalContext = globalContext,
        };
    }

    /// <summary>
    /// The task and its instance as an executor runs them : the graphs refreshed and a workflow
    /// given a workflow instance.
    /// </summary>
    public (BaseAutomationTask Task, TaskInstance Instance) Unpack()
    {
        if (Task is not AutomationWorkflow workflow)
            return (Task, Instance);

        Dictionary<Guid, BaseAutomationTask> tasks = GraphTasks.ToDictionary(x => x.Id);
        workflow.Graph.Refresh(tasks, force: true);
        foreach (AutomationWorkflow nested in tasks.Values.OfType<AutomationWorkflow>())
            nested.Graph.Refresh(tasks, force: true);

        WorkflowInstance instance = new(workflow)
        {
            Id = Instance.Id,
            NodeId = Instance.NodeId,
            ParentInstanceId = Instance.ParentInstanceId,
            NodeName = Instance.NodeName,
            Parameters = Instance.Parameters,
            CreatedAt = Instance.CreatedAt,
            State = Instance.State,
            GlobalContext = GlobalContext,
        };
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
