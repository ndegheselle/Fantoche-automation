using Automation.Shared.Data.Scoped;

namespace Automation.Shared.Services;

public interface IScopedService
{
    /// <summary>
    /// Get the childrens of a scope based on a [scopeId]
    /// </summary>
    public Task<List<ScopedElement>> GetChildrens(Guid scopeId);

    /// <summary>
    /// Search in all tasks.
    /// </summary>
    public Task<List<BaseAutomationTask>> Search(string search = "");

    /// <summary>
    /// Create a new element.
    /// </summary>
    public Task<ScopedElement> CreateAsync(ScopedElement element);

    /// <summary>
    /// Edit an existing element.
    /// </summary>
    public Task<ScopedElement> EditAsync(ScopedElement element);

    /// <summary>
    /// Remove an existing element.
    /// </summary>
    public Task<ScopedElement> RemoveAsync(ScopedElement element);
}
