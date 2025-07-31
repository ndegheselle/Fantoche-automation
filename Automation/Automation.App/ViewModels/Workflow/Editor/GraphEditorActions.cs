using Automation.App.ViewModels.Workflow.Editor.Actions;
using System;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.CompilerServices;

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
        public IAction UndoAction { get; }
    }

    public class GraphEditorActions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Stack<IAction> _undoStack = [];
        private Stack<IAction> _redoStack = [];
        private readonly GraphEditorViewModel _editor;

        public bool IsUndoAvailable => _undoStack.Any();
        public bool IsRedoAvailable => _redoStack.Any();

        public GraphEditorActions(GraphEditorViewModel editor)
        {
            _editor = editor;
        }

        #region History handling
        public void Execute(IAction action)
        {
            action.Execute(_editor);
            _undoStack.Push(action);
            _redoStack.Clear();
            StackChanged();
        }

        public void Undo()
        {
            if (IsUndoAvailable == false)
                return;

            var action = _undoStack.Pop().UndoAction;
            action.Execute(_editor);
            _redoStack.Push(action);
            StackChanged();
        }

        public void Redo()
        {
            if (IsUndoAvailable == false)
                return;
            var action = _redoStack.Pop().UndoAction;
            action.Execute(_editor);
            _undoStack.Push(action);
            StackChanged();
        }

        private void StackChanged()
        {
            NotifyPropertyChanged(nameof(IsUndoAvailable));
            NotifyPropertyChanged(nameof(IsRedoAvailable));
        }
        #endregion
    }
}
