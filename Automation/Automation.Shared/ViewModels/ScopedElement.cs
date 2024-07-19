using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Automation.Shared.ViewModels
{
    public class Scope
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }
        [JsonIgnore]
        public Scope? Parent { get; set; }

        public string Name { get; set; }
        // TODO : Handle different type of objects
        public Dictionary<string, string> Context { get; set; } = new Dictionary<string, string>();

        [JsonIgnore]
        public List<Scope> SubScope { get; set; } = [];
        [JsonIgnore]
        public List<TaskNode> Childrens { get; set; } = [];
    }
}
