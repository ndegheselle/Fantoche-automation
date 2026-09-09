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

        /// <summary>
        /// A new element of [type] named [name] under [parentId]. Which concrete element a kind
        /// stands for is known here rather than by whoever creates one.
        /// </summary>
        public static ScopedElement Create(EnumScopedType type, string name, Guid parentId) => type switch
        {
            EnumScopedType.Scope => new Scope(name, parentId),
            EnumScopedType.Workflow => new AutomationWorkflow(name, parentId),
            EnumScopedType.Task => new AutomationTask(name, parentId),
            _ => throw new NotSupportedException($"Unknown scoped type '{type}'")
        };
    }

    public partial class Scope : ScopedElement
    {
        /// <summary>
        /// Scope every other element ends up under : it is the only one without a parent, and is
        /// read only so it can't be edited or deleted.
        /// </summary>
        public static readonly Scope Root = new Scope()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Metadata = new ScopedMetadata("Root", EnumScopedType.Scope) { IsReadOnly = true },
        };
        /// <summary>Scope holding the default controls tasks.</summary>
        public static readonly Scope Controls = new Scope()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            ParentId = Root.Id,
            Metadata = new ScopedMetadata("Controls", EnumScopedType.Scope) { IsReadOnly = true },
        };

        public string? ContextJson { get; set; }

        public Scope() : base(EnumScopedType.Scope)
        { }

        public Scope(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Scope))
        {
            ParentId = parentId;
        }
    }
}