namespace Automation.App.Features.Workflows.Editor.History
{
    /// <summary>
    /// A <see cref="IReversibleAction"/> made of two delegates, for the actions simple enough to
    /// not deserve their own class.
    /// </summary>
    public class ReversibleAction : IReversibleAction
    {
        public string Name { get; }

        /// <summary>
        /// True unless the action is told otherwise : an action changes the graph until it says it
        /// only moved something around.
        /// </summary>
        public bool ChangesExecution { get; init; } = true;

        private readonly Action _execute;
        private readonly Action _revert;

        public ReversibleAction(string name, Action execute, Action revert)
        {
            Name = name;
            _execute = execute;
            _revert = revert;
        }

        public void Execute() => _execute();

        public void Revert() => _revert();
    }
}
