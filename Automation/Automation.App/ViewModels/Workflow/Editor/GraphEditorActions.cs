namespace Automation.App.ViewModels.Workflow.Editor
{
    internal abstract class SimpleTargetedAction<T>
    {
        protected readonly T _target;
        public SimpleTargetedAction(T target) { _target = target; }
    }

    public interface IAction
    {
        public void Execute(GraphEditorViewModel editor);
        public IAction Undo();
    }

    public class GraphEditorActions
    {
        private Stack<IAction> _undoStack = [];
        private Stack<IAction> _redoStack = [];
        private readonly GraphEditorViewModel _editor;

        public GraphEditorActions(GraphEditorViewModel editor)
        {
            _editor = editor;
        }

        #region History handling
        private void Add(IAction action)
        {
            action.Execute(_editor);
            _undoStack.Push(action);
            _redoStack.Clear();
        }

        private void Undo()
        {
            var action = _undoStack.Pop();
            var undoAction = action.Undo();
            undoAction.Execute(_editor);
            _redoStack.Push(undoAction);
        }

        private void Redo()
        {
            var action = _redoStack.Pop();
            var undoAction = action.Undo();
            undoAction.Execute(_editor);
            _undoStack.Push(undoAction);
        }

        #endregion
    }
}
