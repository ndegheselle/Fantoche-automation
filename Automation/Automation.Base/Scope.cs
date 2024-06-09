using System.Collections.ObjectModel;

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
        public Guid Id { get; set; }
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

    public class  WorkflowScope : ScopedElement
    {
        public WorkflowScope()
        {
            Type = EnumTaskType.Workflow;
        }
    }

    public class TaskScope : ScopedElement
    {
        public Type TaskClass { get; set; }
        public TaskScope()
        {
            Type = EnumTaskType.Task;
        }
    }
}
