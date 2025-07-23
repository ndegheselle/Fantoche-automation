using Automation.Shared.Data;
using System.Text.Json.Serialization;
using Usuel.Shared;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(Scope), "scope")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract partial class ScopedElement : ErrorValidationModel, IIdentifier
    {
        public Guid Id { get; set; }
        public List<Guid> ParentTree { get; set; } = [];
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
        public List<ScopedElement> Childrens { get; set; } = new List<ScopedElement>();

        public Scope() : base(EnumScopedType.Scope)
        { }

        public Scope(string name, List<Guid> parentTree) : base(new ScopedMetadata(name, EnumScopedType.Scope) { IsReadOnly = true})
        {
            ParentTree = parentTree;
            ParentId = parentTree.LastOrDefault();
        }
    }
}
