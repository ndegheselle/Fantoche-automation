using Automation.Shared.Base;
using Automation.Shared.Data;
using Automation.Shared.Packages;

namespace Automation.Shared
{
    public interface ICrudClient<T>
    {
        public Task<T?> GetByIdAsync(Guid id);
        public Task<Guid> CreateAsync(T element);
        public Task UpdateAsync(Guid id, T element);
        public Task DeleteAsync(Guid id);
    }

    public interface ITasksClient<T> : ICrudClient<T>
    { }

    public interface IWorkflowsClient<T> : ICrudClient<T>
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
        public Task<ListPageWrapper<PackageInfos>> SearchAsync(string searchValue, int page, int pageSize);
    }
}
