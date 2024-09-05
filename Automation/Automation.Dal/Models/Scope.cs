using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(Scope), "scope")]
    public class ScopedElement : INamed
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [BsonIgnore]
        public EnumScopedType Type { get; set; }

        public ScopedElement() 
        {}

        public ScopedElement(TaskNode task)
        {
            Id = task.Id;
            Name = task.Name;
            Type = task is WorkflowNode ? EnumScopedType.Workflow : EnumScopedType.Task;
        }
    }

    public class Scope : ScopedElement
    {
        public Guid? ParentId { get; set; }
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();

        [BsonIgnore]
        public List<ScopedElement> Childrens { get; set; } = new List<ScopedElement>();
    }
}
