using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskNode : ITaskNode
    {
        public Guid Id { get; set; }
        public Guid ScopeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ITaskConnector> Connectors { get; private set; } = new List<TaskConnector>();
    }
}
