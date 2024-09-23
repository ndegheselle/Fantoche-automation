using Automation.Shared.Base;
using Automation.Shared.Data;
using Automation.Shared.Packages;

namespace Automation.Dal.Models
{
    public class TaskNode : ScopedElement, ITaskNode
    {
        public Guid ScopeId { get; set; }
        public IEnumerable<ITaskConnector> Inputs { get; set; } = new List<TaskConnectors>();
        public IEnumerable<ITaskConnector> Outputs { get; set; } = new List<TaskConnectors>();
        public PackageInfos? Package { get; set; }

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
