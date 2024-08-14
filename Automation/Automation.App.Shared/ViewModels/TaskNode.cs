using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels
{
    public class TaskNode : ITaskNode
    {
        public Guid Id { get; set; }
        public Guid ScopeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IList<ITaskConnector> Connectors { get; set; } = new List<ITaskConnector>();
    }
}
