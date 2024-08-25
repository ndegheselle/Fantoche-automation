using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Automation.Dal.Models
{
    public class Scope : IScope
    {
        [BsonId]
        public Guid Id { get; set; }
        [BsonId]
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IScope? Parent { get; set; }
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();

        [BsonIgnore]
        public IList<INamed> Childrens { get; set; } = new List<INamed>();
    }
}
