using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;

namespace Automation.Services.Local;

public class LocalScopedService : IScopedService
{
    // In-memory store shared across instances until a real persistence layer exists
    private static readonly Dictionary<Guid, ScopedElement> _elements = [];

    static LocalScopedService()
    {
        var scope = new Scope { Id = Guid.NewGuid(), ParentId = Scope.ROOT_SCOPE_ID, Metadata = new ScopedMetadata("Ingestion", EnumScopedType.Scope) };
        var workflow = new AutomationWorkflow { Id = Guid.NewGuid(), ParentId = Scope.ROOT_SCOPE_ID, Metadata = new ScopedMetadata("Daily import", EnumScopedType.Workflow) };
        var task = new AutomationTask { Id = Guid.NewGuid(), ParentId = scope.Id, Metadata = new ScopedMetadata("Fetch files", EnumScopedType.Task) };

        _elements.Add(scope.Id, scope);
        _elements.Add(workflow.Id, workflow);
        _elements.Add(task.Id, task);
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

    public Task<List<ScopedElement>> GetChildrens(Guid scopeId)
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

    public Task<List<BaseAutomationTask>> Search(string search = "")
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
    {
        bool unique = !_elements.Values.Any(x =>
            x.ParentId == parentId &&
            x.Id != excludeId &&
            string.Equals(x.Metadata.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(unique);
    }
}
