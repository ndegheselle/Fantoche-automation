using Automation.Shared.Contracts;
using MongoDB.Bson.Serialization.Attributes;

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
        public IList<INamed> Childrens { get; set; } = new List<INamed>();
    }
}
