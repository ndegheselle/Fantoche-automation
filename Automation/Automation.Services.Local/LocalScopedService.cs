using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;

namespace Automation.Services.Local;

public class LocalScopedService : IScopedService
{
    // In-memory store shared across instances until a real persistence layer exists
    private static readonly Dictionary<Guid, ScopedElement> _elements = [];

    static LocalScopedService()
    {
        var scope = new Scope { Id = Guid.NewGuid(), Metadata = new ScopedMetadata("Ingestion", EnumScopedType.Scope) };
        var workflow = new AutomationWorkflow { Id = Guid.NewGuid(), ParentId = Scope.ROOT_SCOPE_ID, Metadata = new ScopedMetadata("Daily import", EnumScopedType.Workflow) };
        var task = new AutomationTask { Id = Guid.NewGuid(), ParentId = scope.Id, Metadata = new ScopedMetadata("Fetch files", EnumScopedType.Task) };

        _elements.Add(scope.Id, scope);
        _elements.Add(workflow.Id, workflow);
        _elements.Add(task.Id, task);
    }

    public Task<ScopedElement> CreateAsync(ScopedElement element)
    {
        throw new NotImplementedException();
    }

    public Task<ScopedElement> EditAsync(ScopedElement element)
    {
        throw new NotImplementedException();
    }

    public Task<List<ScopedElement>> GetChildrens(Guid scopeId)
    {
        throw new NotImplementedException();
    }

    public Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        throw new NotImplementedException();
    }

    public Task<List<BaseAutomationTask>> Search(string search = "")
    {
        throw new NotImplementedException();
    }
}
