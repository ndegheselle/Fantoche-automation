namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class NodeAddAction : IAction
    {
        public void Execute(GraphEditorViewModel editor)
        {
            editor.AddNode();
        }

        public IAction Undo()
        {
            return new NodeRemoveAction();
        }
    }

    internal class NodeRemoveAction : IAction
    {
        public void Execute(GraphEditorViewModel editor)
        {
            editor.RemoveNode();
        }

        public IAction Undo()
        {
            return new NodeAddAction();
        }
    }
}
