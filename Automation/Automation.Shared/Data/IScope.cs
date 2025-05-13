namespace Automation.Shared.Data
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedMetadata
    {
        public string Name { get; set; } = "";
        public EnumScopedType Type { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }

        public ScopedMetadata()
        { }
    }

    public interface IScopedElement : IIdentifier
    {
        Guid? ParentId { get; set; }
        ScopedMetadata Metadata { get; set; }
    }

    public interface IScope : IScopedElement
    {
        public static readonly Guid ROOT_SCOPE_ID = new Guid("00000000-0000-0000-0000-000000000001");

        Dictionary<string, string> Context { get; }
        public IList<IScopedElement> Childrens { get; }
    }
}
