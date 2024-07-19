using Automation.Shared.ViewModels;

namespace Automation.Supervisor.Client
{
    public interface ISupervisorClient
    {
        // TODO : Get progress ?
        public void ExecuteTask(Guid taksId);
    }

    public interface ITaskClient
    {
        public TaskNode? GetTask(Guid id);
        public WorkflowNode? GetWorkflow(Guid id);
        public int GetHistoryCount(Guid taskId);
        public IEnumerable<TaskHistory> GetHistory(Guid taskId, int number, int page);

        // Async version
        public Task<TaskNode?> GetTaskAsync(Guid id);
        public Task<WorkflowNode?> GetWorkflowAsync(Guid id);
        public Task<int> GetHistoryCountAsync(Guid taskId);
        public Task<IEnumerable<TaskHistory>> GetHistoryAsync(Guid taskId, int number, int page);
    }

    public interface IScopeClient
    {
        public Scope GetRootScope();
        public Scope? GetScope(Guid id);
        public int GetHistoryCount(Guid scopeId);
        public IEnumerable<TaskHistory> GetHistory(Guid scopeId, int number, int page);

        // Async version
        public Task<Scope> GetRootScopeAsync();
        public Task<Scope?> GetScopeAsync(Guid id);
        public Task<int> GetHistoryCountAsync(Guid scopeId);
        public Task<IEnumerable<TaskHistory>> GetHistoryAsync(Guid scopeId, int number, int page);
    }
}
