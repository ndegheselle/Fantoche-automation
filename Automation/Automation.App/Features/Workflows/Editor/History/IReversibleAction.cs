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

        /// <summary>
        /// Whether the action changes what the graph would run into. False for an action that only
        /// changes how the graph is drawn : previewing one walks all of it, and where a node sits
        /// changes nothing about what it resolves to.
        /// </summary>
        bool ChangesExecution { get; }

        void Execute();

        void Revert();
    }
}
