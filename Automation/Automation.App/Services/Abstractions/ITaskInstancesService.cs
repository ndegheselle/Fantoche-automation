using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.App.Services.Abstractions
{
    /// <summary>
    /// Task instance history and lookup. See <see cref="IScopesService"/> for the architecture rationale.
    /// </summary>
    public interface ITaskInstancesService
    {
        Task<TaskInstance?> GetByIdAsync(Guid id);

        Task<ListPageWrapper<TaskInstance>> GetAllAsync(int page, int pageSize);
    }
}
