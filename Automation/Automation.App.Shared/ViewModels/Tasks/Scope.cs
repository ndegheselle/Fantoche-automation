using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class Scope : IScope
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IScope? Parent { get; set; }
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();
        public IList<INamed> Childrens { get; set; } = new List<INamed>();
    }
}
