using Automation.App.Services.Abstractions;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.App.Services.Stubs
{
    /// <summary>Placeholder pending the worker+SQLite rework. See NotImplementedScopesService.</summary>
    public class NotImplementedTaskInstancesService : ITaskInstancesService
    {
        private const string Pending = "Task instances data source not implemented yet (pending worker+SQLite rework).";

        public Task<TaskInstance?> GetByIdAsync(Guid id) => throw new NotImplementedException(Pending);

        public Task<ListPageWrapper<TaskInstance>> GetAllAsync(int page, int pageSize) => throw new NotImplementedException(Pending);
    }
}
