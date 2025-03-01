namespace Automation.App.ViewModels.Workflow.Editor
{
    public class HistoryHandler
    {
        private Stack<Action> _undoStack = [];
        private Stack<Action> _redoStack = [];

        public void Add(Action action)
        {
            if (_isUndoing)
            {
                _redoStack.Push(action);
            }
            else
            {
                _undoStack.Push(action);
                // Can't roll back after a new action have been added
                _redoStack.Clear();
            }
        }

        public void Undo()
        {
            var action = _undoStack.Pop();
            try
            {
                _isUndoing = true;
                action.Invoke();
            }
            finally
            {
                _isUndoing = false;
            }
        }

        public void Redo()
        {
            if (_redoStack.Count <= 0)
                return;

            var action = _redoStack.Pop();
            action.Invoke();
        }
    }
}
