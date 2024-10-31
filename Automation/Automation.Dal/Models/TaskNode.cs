using Automation.Shared.Data;

namespace Automation.Dal.Models
{
    public class TaskNode : ScopedElement, ITaskNode
    {
        public Guid ScopeId { get; set; }
        public IEnumerable<ITaskConnector> Inputs { get; set; } = [];
        public IEnumerable<ITaskConnector> Outputs { get; set; } = [];
        public TargetedPackage? Package { get; set; }

        public TaskNode() {
            Type = EnumScopedType.Task;
        }
    }

    public class TaskConnectors : ITaskConnector
    {
        public Guid Id { get; set; }
        public string Name {get;set;}
        public EnumTaskConnectorType Type {get;set;}
        public EnumTaskConnectorDirection Direction {get;set;}
    }
}
