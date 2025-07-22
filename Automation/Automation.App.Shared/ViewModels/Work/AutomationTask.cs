using Automation.Shared.Data;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Work
{
    public class AutomationTaskConnector : IAutomationTaskConnector
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public EnumTaskConnectorType Type { get; set; }
        public EnumTaskConnectorDirection Direction { get; set; }

        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
    }

    public class AutomationTaskConnection : IAutomationTaskConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }
    }

    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    public abstract class BaseAutomationTask : ScopedElement, IBaseAutomationTask
    {
        public List<AutomationTaskConnector> Inputs { get; set; } = [];
        public List<AutomationTaskConnector> Outputs { get; set; } = [];
        public List<Schedule> Schedules { get; set; } = [];

        IEnumerable<IAutomationTaskConnector> IBaseAutomationTask.Inputs => Inputs;
        IEnumerable<IAutomationTaskConnector> IBaseAutomationTask.Outputs => Outputs;
        IEnumerable<Schedule> IBaseAutomationTask.Schedules => Schedules;

        public BaseAutomationTask(EnumScopedType type) : base(type)
        { }
    }

    public class AutomationTask : BaseAutomationTask, IAutomationTask
    {
        ITaskTarget? IAutomationTask.Target => Target;
        public TaskTarget? Target { get; set; }
        public AutomationTask() : base(EnumScopedType.Task)
        { }
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        public Graph Graph { get; set; } = new Graph();
        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {
        }
    }
}
