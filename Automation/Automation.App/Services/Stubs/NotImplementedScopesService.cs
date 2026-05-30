using Automation.App.Services.Abstractions;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Services.Stubs
{
    /// <summary>
    /// Placeholder registered during the migration so view-models can be built and the shell runs.
    /// Replace with a real implementation (SQLite / Automation.Worker / API) in the data rework.
    /// </summary>
    public class NotImplementedScopesService : IScopesService
    {
        private const string Pending = "Scopes data source not implemented yet (pending worker+SQLite rework).";

        public Task<Scope> GetRootAsync() => throw new NotImplementedException(Pending);

        public Task<Scope?> GetByIdAsync(Guid id, bool withChildren = true) => throw new NotImplementedException(Pending);

        public Task<IReadOnlyList<Scope>> GetParentScopesAsync(Guid scopeId) => throw new NotImplementedException(Pending);

        public Task<Guid> CreateAsync(ScopedElement element) => throw new NotImplementedException(Pending);

        public Task UpdateAsync(Guid id, ScopedElement element) => throw new NotImplementedException(Pending);

        public Task DeleteAsync(Guid id) => throw new NotImplementedException(Pending);

        public Task<ListPageWrapper<TaskInstance>> GetInstancesAsync(Guid scopeId, int page, int pageSize) => throw new NotImplementedException(Pending);
    }
}
