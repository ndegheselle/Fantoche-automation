using Automation.Shared.ViewModels;

namespace Automation.Shared.Supervisor
{
    public interface ISupervisorClient
    {
        // TODO : Get progress ?
        public void ExecuteTask(Guid taksId);
    }

    // XXX : in theory Scoped are not nodes but just how nodes are organized, maybe it should be separated
    public interface INodeRepository
    {
        public Scope GetRootScope();
        public ScopedElement? GetScoped(Guid id);
        public Node? GetNode(Guid id);

        // Async versions of the methods
        public Task<Scope> GetRootScopeAsync();
        public Task<ScopedElement?> GetScopedAsync(Guid id);
        public Task<Node?> GetNodeAsync(Guid id);
    }
}
