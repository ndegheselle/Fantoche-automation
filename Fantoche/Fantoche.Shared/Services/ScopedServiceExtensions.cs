using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Shared.Services;

public static class ScopedServiceExtensions
{
    /// <summary>
    /// The task or workflow [taskId], ready to be executed : the graph of a workflow is refreshed with
    /// the tasks its nodes point at, nested workflows included. A workflow is read at [version], and
    /// its nested workflows at [nestedVersions] (see <see cref="NestedVersionsOf"/>) : a run resumes
    /// on the graphs it started on. Without them, the latest versions are read. The tasks are read
    /// as they are now.
    /// </summary>
    /// <exception cref="ExecutionException">Unknown element or version, not a task, or a graph pointing at unknown tasks.</exception>
    public static async Task<BaseAutomationTask> LoadRunnableAsync(
        this IScopedService scoped,
        Guid taskId,
        int? version = null,
        IReadOnlyDictionary<Guid, int>? nestedVersions = null)
    {
        ScopedElement element = (version == null ? await scoped.GetAsync(taskId) : await scoped.GetVersionAsync(taskId, version.Value))
            ?? throw new ExecutionException($"Unknown element '{taskId}'{(version == null ? "" : $" at version {version}")}.");

        if (element is not BaseAutomationTask task)
            throw new ExecutionException($"The element '{element.Metadata.Name}' is not a task or a workflow.");

        if (task is AutomationWorkflow workflow)
            await RefreshGraphAsync(scoped, workflow, nestedVersions ?? new Dictionary<Guid, int>(), []);

        return task;
    }

    /// <summary>
    /// The version of every workflow nested in [workflow], at any depth : what a run keeps to be
    /// resumed on the same graphs. Its graph has to be loaded.
    /// </summary>
    public static Dictionary<Guid, int> NestedVersionsOf(AutomationWorkflow workflow)
    {
        Dictionary<Guid, int> versions = [];
        CollectNestedVersions(workflow, versions);
        return versions;
    }

    private static void CollectNestedVersions(AutomationWorkflow workflow, Dictionary<Guid, int> versions)
    {
        IEnumerable<AutomationWorkflow> nested = workflow.Graph.Nodes
            .OfType<BaseGraphTask>()
            .Select(x => x.AutomationTask)
            .OfType<AutomationWorkflow>();
        foreach (AutomationWorkflow child in nested)
        {
            if (versions.TryAdd(child.Id, child.Version))
                CollectNestedVersions(child, versions);
        }
    }

    private static async Task RefreshGraphAsync(
        IScopedService scoped,
        AutomationWorkflow workflow,
        IReadOnlyDictionary<Guid, int> nestedVersions,
        HashSet<Guid> visited)
    {
        if (!visited.Add(workflow.Id))
            return;

        // Read from the graph itself : a past version may point at tasks the latest one doesn't.
        // The controls are known by the graph on its own.
        List<Guid> nodesIds = [.. workflow.Graph.Nodes.OfType<BaseGraphTask>().Where(x => x is not GraphControl).Select(x => x.TaskId).Distinct()];
        var elements = await scoped.GetAsync(nodesIds);

        Dictionary<Guid, BaseAutomationTask> tasks = [];
        foreach (var element in elements)
        {
            if (element is BaseAutomationTask task)
                tasks[task.Id] = task;
        }

        var missing = nodesIds.Where(id => !tasks.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new ExecutionException($"The workflow '{workflow.Metadata.Name}' points at unknown tasks : {string.Join(", ", missing)}.");

        foreach (AutomationWorkflow child in tasks.Values.OfType<AutomationWorkflow>().ToList())
        {
            AutomationWorkflow nested = child;
            if (nestedVersions.TryGetValue(child.Id, out int version) && version != child.Version)
            {
                nested = await scoped.GetVersionAsync(child.Id, version)
                    ?? throw new ExecutionException($"The workflow '{child.Metadata.Name}' has no version {version}.");
                tasks[child.Id] = nested;
            }
            await RefreshGraphAsync(scoped, nested, nestedVersions, visited);
        }

        workflow.Graph.Refresh(tasks, force: true);
    }
}
