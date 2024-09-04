namespace Automation.Shared.Data
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public interface IScope : INamed
    {
        Guid? ParentId { get; set; }
        Dictionary<string, string> Context { get; }
        public IList<INamed> Childrens { get; }
    }
}
