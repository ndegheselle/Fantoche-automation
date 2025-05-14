using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(Scope), "scope")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract class ScopedElement : IScopedElement
    {
        [BsonId]
        public Guid Id { get; set; }
        public List<Guid> ParentTree { get; set; } = [];
        public Guid? ParentId { get; set; }
        public ScopedMetadata Metadata { get; set; }

        public ScopedElement(EnumScopedType type)
        {
            Metadata = new ScopedMetadata(type);
        }
    }

    public class Scope : ScopedElement
    {
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();
        [BsonIgnore]
        public List<ScopedElement> Childrens { get; set; } = new List<ScopedElement>();

        public Scope() : base(EnumScopedType.Scope)
        {
        }
    }
}
