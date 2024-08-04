using Automation.Shared.Data;

namespace Automation.Dal.Models
{
    public class Scope : IScope
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public IScope? Parent { get; set; }
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();
        public IList<INamed> Childrens { get; set; } = new List<INamed>();
    }
}
