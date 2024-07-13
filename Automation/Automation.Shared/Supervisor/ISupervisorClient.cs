using Automation.Shared.ViewModels;

namespace Automation.Shared.Supervisor
{
    public interface ISupervisorClient
    {
        // TODO : Get progress ?
        public void ExecuteTask(Guid taksId);
    }

    public interface INodeRepository
    {
        public Node? GetNode(Guid id);

        public int GetTaskInstancesCount(Guid taskId);
        public IEnumerable<TaskInstance> GetTaskInstances(Guid taskId, int number, int page);

        // Async versions of the methods
        public Task<Node?> GetNodeAsync(Guid id);
        public Task<int> GetTaskInstancesCountAsync(Guid taskId);
        public Task<IEnumerable<TaskInstance>> GetTaskInstancesAsync(Guid taskId, int number, int page);
    }

    public interface IScopeRepository
    {
        public Scope GetRootScope();
        public ScopedElement? GetScoped(Guid id);

        public Task<Scope> GetRootScopeAsync();
        public Task<ScopedElement?> GetScopedAsync(Guid id);
    }
}
