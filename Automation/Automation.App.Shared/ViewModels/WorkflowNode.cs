using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels
{
    public class WorkflowNode : IWorkflowNode
    {
        public Guid Id { get; set; }
        public Guid ScopeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IList<ITaskConnector> Connectors { get; private set; } = new List<ITaskConnector>();
        public IList<ITaskConnection> Connections { get; private set; } = new List<ITaskConnection>();
        public IList<ILinkedNode> Nodes { get; private set; } = new List<ILinkedNode>();
    }
}
