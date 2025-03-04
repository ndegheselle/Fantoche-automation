using Automation.App.Shared.ViewModels.Work;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class NodeAction : IAction
    {
        private readonly GraphNode _target;
        private readonly bool _isAddition;

        public NodeAction(GraphNode target, bool isAddition)
        {
            _target = target;
            _isAddition = isAddition;
        }

        public void Execute(GraphEditorViewModel editor)
        {
            if (_isAddition)
            {
                editor.AddNode(_target);
            }
            else
            {
                editor.RemoveNode(_target);
            }
        }

        public IAction Undo()
        {
            return new NodeAction(_target, !_isAddition);
        }
    }
}
