namespace Automation.App.Features.Workflows.Editor.History
{
    /// <summary>
    /// Several actions applied as one. Reverting undoes them backwards, an action having to be undone
    /// before the ones applied before it, so a composed action never has to spell out its own inverse
    /// ordering.
    /// </summary>
    public class CompositeReversibleAction : IReversibleAction
    {
        public string Name { get; }

        /// <summary>
        /// True as soon as one of the composed actions changes the graph.
        /// </summary>
        public bool ChangesExecution => _actions.Any(x => x.ChangesExecution);

        private readonly IReversibleAction[] _actions;

        public CompositeReversibleAction(string name, params IReversibleAction[] actions)
        {
            Name = name;
            _actions = actions;
        }

        public void Execute()
        {
            foreach (IReversibleAction action in _actions)
                action.Execute();
        }

        public void Revert()
        {
            for (int i = _actions.Length - 1; i >= 0; i--)
                _actions[i].Revert();
        }
    }
}
