using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Editor.History
{
    /// <summary>
    /// Undo / redo stack of the editor. It also tells whether the current work needs to be saved,
    /// the save point being the action that was the last applied one when the work got saved.
    /// </summary>
    public partial class EditorHistory : ObservableObject
    {
        private readonly Stack<IReversibleAction> _applied = new();
        private readonly Stack<IReversibleAction> _reverted = new();

        /// <summary>
        /// Last applied action when the work was saved, null when saved with nothing applied.
        /// </summary>
        private IReversibleAction? _savePoint;

        public bool CanUndo => _applied.Count > 0;

        public bool CanRedo => _reverted.Count > 0;

        /// <summary>
        /// True when actions have been applied or reverted since the last <see cref="MarkSaved"/>.
        /// </summary>
        public bool HasUnsavedChanges => _savePoint != Current;

        private IReversibleAction? Current => _applied.Count > 0 ? _applied.Peek() : null;

        /// <summary>
        /// Apply an action and make it undoable, dropping whatever could be redone.
        /// </summary>
        public void Apply(IReversibleAction action)
        {
            action.Execute();
            _applied.Push(action);
            _reverted.Clear();
            NotifyChanged();
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            IReversibleAction action = _applied.Pop();
            action.Revert();
            _reverted.Push(action);
            NotifyChanged();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            IReversibleAction action = _reverted.Pop();
            action.Execute();
            _applied.Push(action);
            NotifyChanged();
        }

        /// <summary>
        /// Take the current state as the saved one.
        /// </summary>
        public void MarkSaved()
        {
            _savePoint = Current;
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        /// <summary>
        /// Forget every action, the current state becoming the saved one.
        /// </summary>
        public void Clear()
        {
            _applied.Clear();
            _reverted.Clear();
            _savePoint = null;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }
    }
}
