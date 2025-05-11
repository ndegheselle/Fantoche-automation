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

        // Ui specifics
        public AutomationTask Parent { get; set; } = new AutomationTask();
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
        public string Icon { get; set; } = "\uf1b2";
        public TargetedPackage? Package { get; set; }
        public List<AutomationTaskConnector> Inputs { get; set; } = [];
        public List<AutomationTaskConnector> Outputs { get; set; } = [];
        public List<Schedule> Schedules { get; set; } = [];

        IEnumerable<IAutomationTaskConnector> IAutomationTask.Inputs => Inputs;
        IEnumerable<IAutomationTaskConnector> IAutomationTask.Outputs => Outputs;
        IEnumerable<Schedule> IAutomationTask.Schedules => Schedules;

        public AutomationTask()
        {
            Type = EnumScopedType.Task;
        }
    }

    public class AutomationWorkflow : AutomationTask
    {
        public Graph Graph { get; } = new Graph();

        public AutomationWorkflow()
        {
            Type = EnumScopedType.Workflow;
        }
    }
}
