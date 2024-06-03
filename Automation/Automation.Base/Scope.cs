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
        public string Name { get; set; }
        public EnumTaskType Type { get; set; }
        public Type TaskClass { get; set; }
    }

    public class Scope : ScopedElement
    {
        public ObservableCollection<ScopedElement> Childrens { get; set; } = [];
        public Scope()
        {
            Type = EnumTaskType.Scope;
        }
    }
}
