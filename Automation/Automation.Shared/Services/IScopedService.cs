using Automation.Shared.Data.Scoped;

namespace Automation.Shared.Services;

public interface IScopedService
{
    /// <summary>
    /// Get the tree of scoped elements, filtered by [search] on the elements names.
    /// </summary>
    public Task<List<ScopedElement>> GetTreeAsync(string search = "");

    /// <summary>
    /// Create a new element inside [parent], or at the root if no parent is given.
    /// </summary>
    public Task<ScopedElement> CreateAsync(ScopedElement element, Scope? parent = null);
}
