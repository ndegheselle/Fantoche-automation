using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;

namespace Automation.Services.Local;

public class LocalScopedService : IScopedService
{
    // In-memory store shared across instances until a real persistence layer exists
    private static readonly Dictionary<Guid, ScopedElement> _elements = [];

    private readonly IHistoryService _historyService;

    static LocalScopedService()
    {
        // Seeded hierarchy (deterministic ids, see LocalSeed):
        //   Ingestion (scope)
        //     ├─ Fetch files (task)
        //     └─ Transform (scope)
        //          └─ Daily import (workflow)
        var ingestion = new Scope
        {
            Id = LocalSeed.IngestionScopeId,
            ParentId = Scope.ROOT_SCOPE_ID,
            Metadata = new ScopedMetadata("Ingestion", EnumScopedType.Scope)
        };
        var transform = new Scope
        {
            Id = LocalSeed.TransformScopeId,
            ParentId = ingestion.Id,
            Metadata = new ScopedMetadata("Transform", EnumScopedType.Scope)
        };
        var task = new AutomationTask
        {
            Id = LocalSeed.FetchFilesTaskId,
            ParentId = ingestion.Id,
            Metadata = new ScopedMetadata("Fetch files", EnumScopedType.Task) { IsReadOnly = true }
        };
        var workflow = new AutomationWorkflow
        {
            Id = LocalSeed.DailyImportWorkflowId,
            ParentId = transform.Id,
            Metadata = new ScopedMetadata("Daily import", EnumScopedType.Workflow)
        };

        _elements.Add(ingestion.Id, ingestion);
        _elements.Add(transform.Id, transform);
        _elements.Add(task.Id, task);
        _elements.Add(workflow.Id, workflow);
    }

    public LocalScopedService(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public Task<ScopedElement> CreateAsync(ScopedElement element)
    {
        element.Id = Guid.NewGuid();
        _elements.Add(element.Id, element);
        return Task.FromResult(element);
    }

    public Task<ScopedElement> EditAsync(ScopedElement element)
    {
        if (!_elements.ContainsKey(element.Id))
            throw new KeyNotFoundException();
        _elements[element.Id] = element;
        return Task.FromResult(element);
    }

    public Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId)
    {
        var children = _elements.Where(x => x.Value.ParentId == scopeId)
            .Select(x => x.Value)
            .ToList();
        return Task.FromResult(children);
    }

    public Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        throw new NotImplementedException();
    }

    public Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        var matched = _elements.Values
            .OfType<BaseAutomationTask>()
            .Where(x => string.IsNullOrWhiteSpace(search) || x.Metadata.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var items = matched
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToList();

        return Task.FromResult(new Paginated<BaseAutomationTask>
        {
            Items = items,
            Total = matched.Count,
            Options = options,
        });
    }

    public Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
    {
        bool unique = !_elements.Values.Any(x =>
            x.ParentId == parentId &&
            x.Id != excludeId &&
            string.Equals(x.Metadata.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(unique);
    }

    public Task<List<AutomationTask>> GetTasksByPackageAsync(string packageId)
    {
        var tasks = _elements.Values
            .OfType<AutomationTask>()
            .Where(t => t.Target != null && t.Target.Package.Id == packageId)
            .ToList();
        return Task.FromResult(tasks);
    }

    public Task<Paginated<TaskInstance>> GetHistoryAsync(Guid elementId, PaginationOptions options = default)
    {
        var taskIds = CollectExecutableIds(elementId).ToHashSet();
        return _historyService.SearchAsync(options, taskIds);
    }

    /// <summary>
    /// Collect the ids whose executions make up [elementId]'s history: the element itself when it is a
    /// task or workflow, or every task/workflow nested under it (recursively) when it is a scope.
    /// </summary>
    private static IEnumerable<Guid> CollectExecutableIds(Guid elementId)
    {
        if (!_elements.TryGetValue(elementId, out var element))
            yield break;

        switch (element)
        {
            case BaseAutomationTask:
                // Tasks and workflows are the executable leaves.
                yield return element.Id;
                break;
            case Scope:
                foreach (var child in _elements.Values.Where(x => x.ParentId == elementId))
                    foreach (var id in CollectExecutableIds(child.Id))
                        yield return id;
                break;
        }
    }
}
