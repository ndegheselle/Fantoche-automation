using Automation.Shared.Data;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Work
{
    public class AutomationConnector : IAutomationConnector
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

    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public class AutomationTask : ScopedElement, IAutomationTask, INotifyPropertyChanged
    {
        public TargetedPackage? Package { get; set; }
        public List<AutomationConnector> Inputs { get; set; } = [];
        public List<AutomationConnector> Outputs { get; set; } = [];
        public List<Schedule> Schedules { get; set; } = [];

        IEnumerable<IAutomationConnector> IAutomationTask.Inputs => Inputs;
        IEnumerable<IAutomationConnector> IAutomationTask.Outputs => Outputs;
        IEnumerable<Schedule> IAutomationTask.Schedules => Schedules;

        public AutomationTask()
        {
            Type = EnumScopedType.Task;
        }
    }

    public class AutomationWorkflow : AutomationTask
    {
        public AutomationWorkflow()
        {
            Type = EnumScopedType.Workflow;
        }
    }
}
