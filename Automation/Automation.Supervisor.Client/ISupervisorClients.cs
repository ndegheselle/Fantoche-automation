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
        public INode? GetNode(Guid id);

        // Async versions of the methods
        public Task<INode?> GetNodeAsync(Guid id);
    }

    public interface IScopeClient
    {
        public Scope GetRootScope();
        public ScopedElement? GetScoped(Guid id);
        public int GetScopedInstancesCount(Guid taskId);
        public IEnumerable<TaskInstance> GetScopedInstances(Guid taskId, int number, int page);

        public Task<Scope> GetRootScopeAsync();
        public Task<ScopedElement?> GetScopedAsync(Guid id);
        public Task<int> GetScopedInstancesCountAsync(Guid taskId);
        public Task<IEnumerable<TaskInstance>> GetScopedInstancesAsync(Guid taskId, int number, int page);
    }
}
