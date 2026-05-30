using Automation.App.Services.Abstractions;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Services.Stubs
{
    /// <summary>Placeholder pending the worker+SQLite rework. See NotImplementedScopesService.</summary>
    public class NotImplementedTasksService : ITasksService
    {
        private const string Pending = "Tasks data source not implemented yet (pending worker+SQLite rework).";

        public Task<AutomationTask?> GetByIdAsync(Guid id) => throw new NotImplementedException(Pending);

        public Task<IReadOnlyList<AutomationTask>> GetAllAsync() => throw new NotImplementedException(Pending);

        public Task<Guid> CreateAsync(BaseAutomationTask task) => throw new NotImplementedException(Pending);

        public Task UpdateAsync(Guid id, BaseAutomationTask task) => throw new NotImplementedException(Pending);

        public Task DeleteAsync(Guid id) => throw new NotImplementedException(Pending);

        public Task<ListPageWrapper<TaskInstance>> GetInstancesAsync(Guid taskId, int page, int pageSize) => throw new NotImplementedException(Pending);

        public Task<TaskInstance> ExecuteAsync(Guid taskId, object? settings) => throw new NotImplementedException(Pending);

        public Task<IReadOnlyList<string>> GetTagsAsync() => throw new NotImplementedException(Pending);

        public Task<IReadOnlyList<BaseAutomationTask>> GetByTagAsync(string tag) => throw new NotImplementedException(Pending);
    }
}
