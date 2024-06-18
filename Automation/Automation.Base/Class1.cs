using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Automation.Base
{

    public enum EnumTaskType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Scope Parent { get; set; }
        public string Name { get; set; }
        public EnumTaskType Type { get; set; }
    }

    public class Scope : ScopedElement
    {
        public ObservableCollection<ScopedElement> Childrens { get; set; } = [];
        public Scope() { Type = EnumTaskType.Scope; }

        public void AddChild(ScopedElement child)
        {
            child.Parent = this;
            Childrens.Add(child);
        }
    }

    public class TaskScope : ScopedElement
    {
        public Point Location { get; set; }

        public ObservableCollection<ElementConnector> Inputs { get; set; } = new ObservableCollection<ElementConnector>();
        public ObservableCollection<ElementConnector> Outputs { get; set; } = new ObservableCollection<ElementConnector>();

        public TaskScope()
        {
            Type = EnumTaskType.Task;
        }
    }

    public class WorkflowScope : TaskScope
    {
        public ObservableCollection<TaskScope> Nodes { get; } = new ObservableCollection<TaskScope>();
        public ObservableCollection<ElementConnection> Connections { get; } = new ObservableCollection<ElementConnection>();

        public WorkflowScope()
        {
            Type = EnumTaskType.Workflow;
        }
    }
}
