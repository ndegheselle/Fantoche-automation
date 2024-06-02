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
        public List<ScopedElement> Childrens { get; set; } = new List<ScopedElement>();

        public Scope()
        {
            Type = EnumTaskType.Scope;
        }
    }
}
