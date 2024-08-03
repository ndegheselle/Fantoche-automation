namespace Automation.Shared.Data
{
    public interface IScope
    {
        Guid Id { get; set; }
        string Name { get; set; }

        Guid? ParentId { get; set; }
        IScope? Parent { get; set; }

        Dictionary<string, string> Context { get; }
        IList<IScope> SubScope { get; }
        IList<ITaskNode> Childrens { get; }
    }
}
