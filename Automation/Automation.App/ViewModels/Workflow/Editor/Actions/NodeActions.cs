using Automation.Dal.Models;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class NodeAdditionAction : SimpleTargetedAction<GraphNode>, IAction
    {
        public NodeAdditionAction(GraphNode target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.AddNode(_target);
        public IAction UndoAction => new NodeRemoveAction(_target);
    }

    internal class NodeRemoveAction : SimpleTargetedAction<GraphNode>, IAction
    {
        public NodeRemoveAction(GraphNode target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.RemoveNode(_target);
        public IAction UndoAction => new NodeAdditionAction(_target);
    }
}
