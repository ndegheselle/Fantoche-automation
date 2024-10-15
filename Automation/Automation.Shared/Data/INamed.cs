namespace Automation.Shared.Data
{
    public interface IIdentifier
    {
        Guid Id { get; set; }
    }

    public interface INamed : IIdentifier
    {
        string Name { get; }
    }
}
