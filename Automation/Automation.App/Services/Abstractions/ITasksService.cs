using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Services.Abstractions
{
    /// <summary>
    /// Task definitions (CRUD), their instances, execution and tagging. See <see cref="IScopesService"/>
    /// for the architecture rationale. Returns current Automation.Shared domain models.
    /// </summary>
    public interface ITasksService
    {
        Task<AutomationTask?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<AutomationTask>> GetAllAsync();

        Task<Guid> CreateAsync(BaseAutomationTask task);

        Task UpdateAsync(Guid id, BaseAutomationTask task);

        Task DeleteAsync(Guid id);

        Task<ListPageWrapper<TaskInstance>> GetInstancesAsync(Guid taskId, int page, int pageSize);

        Task<TaskInstance> ExecuteAsync(Guid taskId, object? settings);

        Task<IReadOnlyList<string>> GetTagsAsync();

        Task<IReadOnlyList<BaseAutomationTask>> GetByTagAsync(string tag);
    }
}
