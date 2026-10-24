using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;

namespace Fantoche.Shared.Remote;

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

    /// <summary>
    /// For a workflow being resumed : what the history holds of its run (see
    /// <see cref="WorkflowInstance.StoredInstances"/>). Empty for a run starting.
    /// </summary>
    public List<TaskInstance> StoredInstances { get; init; } = [];

    /// <summary>
    /// [task] run with [instance], the graph of a workflow having to be loaded (see
    /// <see cref="ScopedServiceExtensions.LoadRunnableAsync"/>).
    /// </summary>
    public static RemoteExecution From(BaseAutomationTask task, TaskInstance instance, IReadOnlyList<TaskInstance>? storedInstances = null)
    {
        Dictionary<Guid, BaseAutomationTask> graphTasks = [];
        if (task is AutomationWorkflow workflow)
            CollectGraphTasks(workflow, graphTasks);

        return new RemoteExecution
        {
            Task = task,
            GraphTasks = [.. graphTasks.Values],
            Instance = instance,
            StoredInstances = [.. storedInstances ?? []],
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

        WorkflowInstance instance = WorkflowInstance.From(workflow, Instance);
        instance.StoredInstances = StoredInstances;
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
