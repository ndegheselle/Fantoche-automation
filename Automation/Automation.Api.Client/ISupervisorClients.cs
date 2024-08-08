using Automation.Shared.Base;
using Automation.Shared.Contracts;

namespace Automation.Api.Client
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

    public interface ISupervisorClient
    {
        // TODO : Get progress ?
        public void ExecuteTask(Guid taksId);
    }

    public interface ITaskClient
    {
        public Task<TaskNode?> GetTaskAsync(Guid id);
        public Task<WorkflowNode?> GetWorkflowAsync(Guid id);
        public Task<TaskHistories> GetHistoryAsync(Guid taskId, int page, int pageSize);

        public Task<TaskNode> CreateTaskAsync(TaskNode task);
        public Task<WorkflowNode> CreateWorkflowAsync(WorkflowNode workflow);

        public Task<TaskNode> UpdateTaskAsync(TaskNode task);
        public Task<WorkflowNode> UpdateWorkflowAsync(WorkflowNode workflow);
    }

    public interface IScopeClient
    {
        public Task<Scope> GetRootScopeAsync(ScopeLoadOptions options = default);
        public Task<Scope?> GetScopeAsync(Guid id, ScopeLoadOptions options = default);
        public Task<TaskHistories> GetHistoryAsync(Guid scopeId, int page, int pageSize);

        public Task<Scope> CreateScopeAsync(Scope scope);
        public Task<Scope> UpdateScopeAsync(Scope scope);
    }
}
