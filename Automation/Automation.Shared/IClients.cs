using Automation.Shared.Base;
using Automation.Shared.Data;

namespace Automation.Shared
{
    public interface ICrudClient<TIn, TOut>
    {
        public Task<TOut?> GetByIdAsync(Guid id);
        public Task<Guid> CreateAsync(TIn element);
        public Task UpdateAsync(Guid id, TIn element);
        public Task DeleteAsync(Guid id);
    }

    public interface ITaskClient<T> : ICrudClient<ITaskNode, T> where T : ITaskNode
    { }

    public interface IWorkflowClient<T> : ICrudClient<IWorkflowNode, T> where T : IWorkflowNode
    { }

    public interface IScopeClient<T> : ICrudClient<IScope, T> where T : IScope
    {
        public Task<T> GetRootAsync();
    }

    public interface IHistoryClient<T> where T : ITaskHistory
    {
        public Task<ListPageWrapper<T>> GetByTaskAsync(Guid taskId, int page, int pageSize);
        public Task<ListPageWrapper<T>> GetByScopeAsync(Guid scopeId, int page, int pageSize);
    }
}
