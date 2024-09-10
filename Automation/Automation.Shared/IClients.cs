using Automation.Shared.Base;
using Automation.Shared.Data;

namespace Automation.Shared
{
    public interface ICrudClient<T>
    {
        public Task<T?> GetByIdAsync(Guid id);
        public Task<Guid> CreateAsync(T element);
        public Task UpdateAsync(Guid id, T element);
        public Task DeleteAsync(Guid id);
    }

    public interface ITasksClient<T> : ICrudClient<T> where T : ITaskNode
    { }

    public interface IWorkflowsClient<T> : ICrudClient<T> where T : IWorkflowNode
    { }

    public interface IScopesClient<T> : ICrudClient<T>
    {
        public Task<T> GetRootAsync();
    }

    public interface IHistoryClient<T> where T : ITaskHistory
    {
        public Task<ListPageWrapper<T>> GetByTaskAsync(Guid taskId, int page, int pageSize);
        public Task<ListPageWrapper<T>> GetByScopeAsync(Guid scopeId, int page, int pageSize);
    }

    public interface IPackagesClient
    {
        public Task<IEnumerable<Package>> SearchAsync(string searchValue, int page, int pageSize);
    }
}
