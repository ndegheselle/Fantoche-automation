using Automation.Shared;
using Automation.Shared.Data;
using System.Dynamic;

namespace Automation.Shared.Data
{
    public class TaskHistories : PageWrapper<IEnumerable<TaskHistory>> 
    {
        public TaskHistories()
        {
            Data = new List<TaskHistory>();
        }
    }

    public struct ScopeLoadOptions 
    {
        public bool WithContext { get; set; } = true;
        public bool WithChildrens { get; set; } = true;

        public ScopeLoadOptions()
        {}
    }

    public interface ICrudRepository<T>
    {
        public Task<T?> GetByIdAsync(Guid id);
        public Task<T> CreateAsync(T element);
        public Task<T> UpdateAsync(T element);
    }

    public interface IWorkflowRepository : ICrudRepository<IWorkflowNode>
    { }

    public interface ITaskRepository : ICrudRepository<ITaskNode>
    { }

    public interface IScopeRepository : ICrudRepository<Scope>
    {
        public Task<TaskHistories> GetHistoryAsync(Guid scopeId, int page, int pageSize);
        public Task<Scope> GetRootScopeAsync(ScopeLoadOptions options = default);
        public Task<Scope?> GetByIdWithOptionsAsync(Guid id, ScopeLoadOptions options = default);
    }
}
