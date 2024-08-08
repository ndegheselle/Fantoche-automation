namespace Automation.Shared.Contracts
{
    public interface IScope : INamed
    {
        Guid? ParentId { get; set; }
        IScope? Parent { get; set; }

        Dictionary<string, string> Context { get; }
        public IList<INamed> Childrens { get; }
    }
}
