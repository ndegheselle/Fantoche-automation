using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Shared.Services;

public static class ScopedServiceExtensions
{
    /// <summary>
    /// The task or workflow [taskId], ready to be executed : the graph of a workflow is refreshed with
    /// the tasks its nodes point at, nested workflows included. A workflow is read at [version], its
    /// latest one when none is given : a run resumes on the version it started on. Its nested
    /// workflows and its tasks are read as they are now.
    /// </summary>
    /// <exception cref="ExecutionException">Unknown element or version, not a task, or a graph pointing at unknown tasks.</exception>
    public static async Task<BaseAutomationTask> LoadRunnableAsync(this IScopedService scoped, Guid taskId, int? version = null)
    {
        ScopedElement element = (version == null ? await scoped.GetAsync(taskId) : await scoped.GetVersionAsync(taskId, version.Value))
            ?? throw new ExecutionException($"Unknown element '{taskId}'{(version == null ? "" : $" at version {version}")}.");

        if (element is not BaseAutomationTask task)
            throw new ExecutionException($"The element '{element.Metadata.Name}' is not a task or a workflow.");

        if (task is AutomationWorkflow workflow)
            await RefreshGraphAsync(scoped, workflow, []);

        return task;
    }

    private static async Task RefreshGraphAsync(IScopedService scoped, AutomationWorkflow workflow, HashSet<Guid> visited)
    {
        if (!visited.Add(workflow.Id))
            return;

        // The controls are left out by the store, the graph knows them on its own.
        var nodesIds = await scoped.GetGraphTaskIdsAsync(workflow.Id);
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

        foreach (var child in tasks.Values.OfType<AutomationWorkflow>())
            await RefreshGraphAsync(scoped, child, visited);

        workflow.Graph.Refresh(tasks, force: true);
    }
}
