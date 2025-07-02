using Automation.Shared.Data;
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
    public class AutomationTask : ScopedElement, IAutomationTask, INotifyPropertyChanged
    {
        public PackageClassTarget? Target { get; set; }
        public List<AutomationTaskConnector> Inputs { get; set; } = [];
        public List<AutomationTaskConnector> Outputs { get; set; } = [];
        public List<Schedule> Schedules { get; set; } = [];

        IEnumerable<IAutomationTaskConnector> IAutomationTask.Inputs => Inputs;
        IEnumerable<IAutomationTaskConnector> IAutomationTask.Outputs => Outputs;
        IEnumerable<Schedule> IAutomationTask.Schedules => Schedules;
        ITaskTarget? IAutomationTask.Target => Target;

        public AutomationTask() : base(EnumScopedType.Task)
        {}
    }

    public class AutomationWorkflow : AutomationTask
    {
        public Graph Graph { get; } = new Graph();

        public AutomationWorkflow()
        {
            Metadata.Type = EnumScopedType.Workflow;
        }
    }
}
