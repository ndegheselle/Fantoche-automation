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

    public class TaskScopeEndpoint : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; }
        public Point Anchor { get; set; }
    }

    public class TaskScopeLink : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public TaskScopeEndpoint Source { get; set; }
        public TaskScopeEndpoint Target { get; set; }
    }

    public class TaskScope : ScopedElement
    {
        public ObservableCollection<TaskScopeEndpoint> Inputs { get; set; } = new ObservableCollection<TaskScopeEndpoint>();
        public ObservableCollection<TaskScopeEndpoint> Outputs { get; set; } = new ObservableCollection<TaskScopeEndpoint>();

        public TaskScope()
        {
            Type = EnumTaskType.Task;
        }
    }

    public class  WorkflowScope : TaskScope
    {
        public ObservableCollection<TaskScope> Nodes { get; } = new ObservableCollection<TaskScope>();
        public ObservableCollection<TaskScopeLink> Links { get; } = new ObservableCollection<TaskScopeLink>();

        public WorkflowScope()
        {
            Type = EnumTaskType.Workflow;
        }
    }
}
