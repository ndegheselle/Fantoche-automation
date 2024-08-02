using System.Text.Json.Serialization;

namespace Automation.Shared.Data
{
    public interface IScope
    {
        Guid Id { get; set; }
        Guid? ParentId { get; set; }
        IScope? Parent { get; set; }
        string Name { get; set; }
        Dictionary<string, string> Context { get; }
        List<IScope> SubScope { get; }
        List<ITaskNode> Childrens { get; }
    }

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
