using System.Text.Json.Serialization;
using Automation.Shared.Base;

namespace Automation.Shared.Data.Scoped
{
    [JsonDerivedType(typeof(Scope), "scope")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract partial class ScopedElement : IIdentifier
    {
        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }
        public ScopedMetadata Metadata { get; set; }

        public ScopedElement(EnumScopedType type)
        {
            Metadata = new ScopedMetadata(type);
        }

        public ScopedElement(ScopedMetadata metadata)
        {
            Metadata = metadata;
        }
    }

    public partial class Scope : ScopedElement
    {
        public static readonly Guid ROOT_SCOPE_ID = new Guid("00000000-0000-0000-0000-000000000001");

        public string? ContextJson { get; set; }

        public Scope() : base(EnumScopedType.Scope)
        { }

        public Scope(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Scope) { IsReadOnly = true })
        {
            ParentId = parentId;
        }

        public void LoadChildren()
        {
            throw new NotImplementedException();
        }
    }
}