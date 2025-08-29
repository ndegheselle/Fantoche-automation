using Automation.Shared.Base;
using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(Scope), "scope")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract partial class ScopedElement : IIdentifier
    {
        public Guid Id { get; set; }
        [JsonIgnore]
        public Scope? Parent { get; set; }
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

        public void ChangeParent(Scope parent)
        {
            Parent = parent;
            ParentId = Parent.Id;
            ParentTree = [.. Parent.ParentTree, Parent.Id];
        }
    }

    public partial class Scope : ScopedElement
    {
        public static readonly Guid ROOT_SCOPE_ID = new Guid("00000000-0000-0000-0000-000000000001");
        public ObservableCollection<ScopedElement> Childrens { get; set; } = [];

        public Scope() : base(EnumScopedType.Scope)
        { }

        public Scope(string name, List<Guid> parentTree) : base(new ScopedMetadata(name, EnumScopedType.Scope) { IsReadOnly = true})
        {
            ParentTree = parentTree;
            ParentId = parentTree.LastOrDefault();
        }

        public Scope AddChild(ScopedElement element)
        {
            element.ChangeParent(this);
            Childrens.Add(element);
            return this;
        }
    }
}
