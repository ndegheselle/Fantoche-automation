using System.Text.Json.Serialization;

namespace Automation.Shared.Data
{
    public class Scope
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        [JsonIgnore]
        public Scope? Parent { get; set; }

        public string Name { get; set; }
        // TODO : Handle different type of objects
        public Dictionary<string, string> Context { get; set; } = new Dictionary<string, string>();

        public List<Scope> SubScope { get; set; } = [];
        public List<TaskNode> Childrens { get; set; } = [];
    }
}
