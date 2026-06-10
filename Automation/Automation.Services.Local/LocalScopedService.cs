using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;

namespace Automation.Services.Local;

public class LocalScopedService : IScopedService
{
    // In-memory store shared across instances until a real persistence layer exists
    private static readonly List<ScopedElement> _roots = [];

    public Task<List<ScopedElement>> GetTreeAsync(string search = "")
    {
        if (string.IsNullOrWhiteSpace(search))
            return Task.FromResult(_roots.ToList());

        return Task.FromResult(_roots.Where(element => Matches(element, search)).ToList());
    }

    public Task<ScopedElement> CreateAsync(ScopedElement element, Scope? parent = null)
    {
        element.Id = Guid.NewGuid();
        if (parent == null)
            _roots.Add(element);
        else
            parent.AddChild(element);
        return Task.FromResult(element);
    }

    /// <summary>
    /// An element matches if its name contains [search] or if any of its descendants does.
    /// </summary>
    private static bool Matches(ScopedElement element, string search)
    {
        if (element.Metadata.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;
        return element is Scope scope && scope.Childrens.Any(child => Matches(child, search));
    }
}
