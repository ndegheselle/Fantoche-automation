using Automation.App.Shared.ViewModels.Work;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class ConnectionAdditionAction : SimpleTargetedAction<GraphConnection>, IAction
    {
        public ConnectionAdditionAction(GraphConnection target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.Connect(_target.Source, _target.Target);
        public IAction Undo() => new ConnectionRemoveAction(_target);
    }

    internal class ConnectionRemoveAction : SimpleTargetedAction<GraphConnection>, IAction
    {
        public ConnectionRemoveAction(GraphConnection target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.Disconnect(_target);
        public IAction Undo() => new ConnectionAdditionAction(_target);
    }


    internal class ConnectorDisconnectAction : SimpleTargetedAction<TaskConnector>, IAction
    {
        public ConnectorDisconnectAction(TaskConnector target) : base(target)
        { }

        public void Execute(GraphEditorViewModel editor) => editor.DisconnectConnector(_target);
        public IAction Undo() => new ConnectionAdditionAction(_target);
    }
}
