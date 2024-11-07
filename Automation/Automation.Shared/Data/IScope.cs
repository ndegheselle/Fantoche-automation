namespace Automation.Shared.Data
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public interface IScopedElement : INamed
    {
        List<Guid> ParentTree { get; set; }
        Guid? ParentId { get; set; }
        EnumScopedType Type { get; set; }
    }

    public interface IScope : IScopedElement
    {
        Dictionary<string, string> Context { get; }
        public IList<INamed> Childrens { get; }
    }
}
