using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;

namespace Automation.Shared.Services;

public interface IScopedService
{
    /// <summary>
    /// Get the childrens of a scope based on a [scopeId]
    /// </summary>
    public Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId);

    /// <summary>
    /// Search in all tasks.
    /// </summary>
    public Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default);

    /// <summary>
    /// Get all <see cref="AutomationTask"/> whose target points at the package [packageId],
    /// regardless of the targeted version.
    /// </summary>
    public Task<List<AutomationTask>> GetTasksByPackageAsync(string packageId);

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

    /// <summary>
    /// Check whether [name] is unique among the direct children of the scope [parentId].
    /// The element [excludeId] (if provided) is ignored, so an element keeping its own name stays valid.
    /// </summary>
    public Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null);

    /// <summary>
    /// Get a page of the execution history related to the scoped element [elementId], most recent first.
    /// For a task or workflow this is its own executions; for a scope it aggregates the executions of
    /// every task and workflow it contains, including those nested in descendant scopes.
    /// </summary>
    public Task<Paginated<TaskInstance>> GetHistoryAsync(Guid elementId, PaginationOptions options = default);
}
