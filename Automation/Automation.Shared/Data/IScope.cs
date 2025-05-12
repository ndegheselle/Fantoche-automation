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
        Guid? ParentId { get; set; }
        EnumScopedType Type { get; set; }
        string? Color { get; set; }
        string? Icon { get; set; }
    }

    public interface IScope : IScopedElement
    {
        public static readonly Guid ROOT_SCOPE_ID = new Guid("00000000-0000-0000-0000-000000000001");

        Dictionary<string, string> Context { get; }
        public IList<INamed> Childrens { get; }
    }
}
