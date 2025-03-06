using Automation.App.Shared.ViewModels.Work;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class ConnectionsAdditionAction : SimpleTargetedAction<IEnumerable<GraphConnection>>, IAction
    {
        public ConnectionsAdditionAction(IEnumerable<GraphConnection> target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.Connect(_target);
        public IAction UndoAction => new ConnectionsRemoveAction(_target);
    }

    internal class ConnectionsRemoveAction : SimpleTargetedAction<IEnumerable<GraphConnection>>, IAction
    {
        public ConnectionsRemoveAction(IEnumerable<GraphConnection> target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.Disconnect(_target);
        public IAction UndoAction => new ConnectionsAdditionAction(_target);
    }
}
