using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Automation.Base
{
    public enum EnumTaskType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedElement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Scope Parent { get; set; }
        public string Name { get; set; }
        public EnumTaskType Type { get; set; }
    }

    // Move to 
    public class Scope : ScopedElement
    {
        public ObservableCollection<ScopedElement> Childrens { get; set; } = [];
        public Scope()
        {
            Type = EnumTaskType.Scope;
        }

        public void AddChild(ScopedElement child)
        {
            child.Parent = this;
            Childrens.Add(child);
        }
    }

    public class ElementEndpoint : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; }
        public Point Anchor { get; set; }
    }

    public class ElementLink : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ElementEndpoint Source { get; set; }
        public ElementEndpoint Target { get; set; }
    }

    public class TaskScope : ScopedElement
    {
        public ObservableCollection<ElementEndpoint> Inputs { get; set; } = new ObservableCollection<ElementEndpoint>();
        public ObservableCollection<ElementEndpoint> Outputs { get; set; } = new ObservableCollection<ElementEndpoint>();

        public TaskScope()
        {
            Type = EnumTaskType.Task;
        }
    }

    public class  WorkflowScope : TaskScope
    {
        public ObservableCollection<TaskScope> Nodes { get; } = new ObservableCollection<TaskScope>();
        public ObservableCollection<ElementLink> Links { get; } = new ObservableCollection<ElementLink>();

        public WorkflowScope()
        {
            Type = EnumTaskType.Workflow;
        }
    }
}
