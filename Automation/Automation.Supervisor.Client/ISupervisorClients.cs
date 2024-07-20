using Automation.Shared;
using Automation.Shared.Data;

namespace Automation.Supervisor.Client
{
    public class TaskHistories : PageWrapper<IEnumerable<TaskHistory>> 
    {}

    public interface ISupervisorClient
    {
        // TODO : Get progress ?
        public void ExecuteTask(Guid taksId);
    }

    public interface ITaskClient
    {
        public Task<TaskNode?> GetTaskAsync(Guid id);
        public Task<T?> GetTaskAsync<T>(Guid id) where T : TaskNode;
        public Task<TaskHistories> GetHistoryAsync(Guid taskId, int pageSize, int page);
    }

    public interface IScopeClient
    {
        public Task<Scope?> GetScopeAsync(Guid id);
        public Task<T?> GetScopeAsync<T>(Guid id) where T : Scope;
        public Task<TaskHistories> GetHistoryAsync(Guid scopeId, int pageSize, int page);
    }
}
