using Automation.App.Shared.ViewModels.Work;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class NodeAdditionAction : SimpleTargetedAction<GraphNode>, IAction
    {
        public NodeAdditionAction(GraphNode target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.AddNode(_target);
        public IAction Undo() => new NodeRemoveAction(_target);
    }

    internal class NodeRemoveAction : SimpleTargetedAction<GraphNode>, IAction
    {
        public NodeRemoveAction(GraphNode target) : base(target)
        {}

        public void Execute(GraphEditorViewModel editor) => editor.RemoveNode(_target);
        public IAction Undo() => new NodeAdditionAction(_target);
    }
}
