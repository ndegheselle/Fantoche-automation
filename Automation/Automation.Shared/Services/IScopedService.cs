using Automation.Shared.Base;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Services;

public interface IScopedService
{
    /// <summary>
    /// Get the childrens of a scope based on a [scopeId]
    /// </summary>
    public Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId);

    /// <summary>
    /// Search in all tasks, return a flat paginated list of elements.
    /// </summary>
    public Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default);

    /// <summary>
    /// Search in all tasks, return a flat list holding the elements corresponding to the search and
    /// every scope leading to them, so the tree can be rebuilt from their parents.
    /// </summary>
    public Task<List<ScopedElement>> SearchTreeAsync(string search = "");

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
    /// <exception cref="InvalidOperationException">
    /// The element, or one held under it, is read only or is still used by the graph of a workflow
    /// (see <see cref="GetUsagesAsync"/>).
    /// </exception>
    public Task<ScopedElement> RemoveAsync(ScopedElement element);

    /// <summary>
    /// The nodes using the task or workflow [taskId], each with the workflow holding it. Empty when
    /// it is used nowhere, which is what makes it removable.
    /// </summary>
    public Task<List<TaskUsage>> GetUsagesAsync(Guid taskId);

    /// <summary>
    /// Get the resolved context of the element [elementId] : the context of every scope from the root
    /// down to the one containing it, merged together (see <see cref="ScopeContextResolver.Resolve"/>).
    /// Read once, before an execution or before editing a graph : the context a workflow starts from.
    /// </summary>
    public Task<JObject> GetContextAsync(Guid elementId);

    /// <summary>
    /// Check whether [name] is unique among the direct children of the scope [parentId].
    /// The element [excludeId] (if provided) is ignored, so an element keeping its own name stays valid.
    /// </summary>
    public Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null);
}
