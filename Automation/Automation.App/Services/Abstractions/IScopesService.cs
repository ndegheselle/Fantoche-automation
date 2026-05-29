using Automation.Shared.Data.Scoped;

namespace Automation.App.Services.Abstractions
{
    /// <summary>
    /// MIGRATION ARCHITECTURE: the UI depends on these service interfaces, not on the REST API
    /// clients. Implementations will target SQLite / a local Automation.Worker / a remote API
    /// (future worker+SQLite rework). This decouples views from both the (stubbed/drifted) API
    /// clients and lets view-models be built against a stable contract using current
    /// Automation.Shared domain models.
    /// </summary>
    public interface IScopesService
    {
        Task<Scope> GetRootAsync();

        Task<Scope?> GetByIdAsync(Guid id, bool withChildren = true);

        /// <summary>Ancestor scopes of the given scope, root-first (for breadcrumbs).</summary>
        Task<IReadOnlyList<Scope>> GetParentScopesAsync(Guid scopeId);

        Task<Guid> CreateAsync(ScopedElement element);

        Task UpdateAsync(Guid id, ScopedElement element);

        Task DeleteAsync(Guid id);
    }
}
