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

    public interface ITaskClient<T> : ICrudClient<T>
    { }

    public interface IWorkflowClient<T> : ICrudClient<T>
    { }

    public interface IScopeClient<T> : ICrudClient<T>
    {
        public Task<T> GetRootAsync();
    }

    public interface IHistoryClient<T> where T : ITaskHistory
    {
        public Task<PageWrapper<T>> GetByTaskAsync(Guid taskId, int page, int pageSize);
        public Task<PageWrapper<T>> GetByScopeAsync(Guid scopeId, int page, int pageSize);
    }
}
