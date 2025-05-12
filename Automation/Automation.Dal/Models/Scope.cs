using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(Scope), "scope")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public class ScopedElement : IScopedElement
    {
        [BsonId]
        public Guid Id { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Guid> ParentTree { get; set; } = [];
        public Guid? ParentId { get; set; }

        [BsonIgnore]
        public EnumScopedType Type { get; set; }

        public ScopedElement()
        { }
    }

    public class Scope : ScopedElement
    {
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();

        [BsonIgnore]
        public List<ScopedElement> Childrens { get; set; } = new List<ScopedElement>();
    }
}
