using System.Collections.ObjectModel;

namespace Automation.App.ViewModels.Scopes
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
}
