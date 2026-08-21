namespace Automation.App.Features.Workflows.Editor.History
{
    /// <summary>
    /// An action that can be applied and reverted, so that it can be tracked by the
    /// <see cref="EditorHistory"/>.
    /// </summary>
    public interface IReversibleAction
    {
        /// <summary>
        /// Name of the action, as displayed to the user.
        /// </summary>
        string Name { get; }

        void Execute();

        void Revert();
    }
}
